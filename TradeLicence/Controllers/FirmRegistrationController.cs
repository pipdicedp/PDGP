using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Citizen side of "Registration of Firms" (Indian Partnership Act 1932).
    /// Same shape as ElectricityController: Draft -> Submitted -> officer workflow
    /// (WorkflowEngineService), documents AES-encrypted in the database.
    /// </summary>
    [Authorize]
    public class FirmRegistrationController : Controller
    {
        private const string ServiceType = RegistrationWorkflowConfig.FirmServiceType;

        private readonly FirmRegistrationDbContext _context;
        private readonly ApplicationDbContext _sharedContext;
        private readonly IFileEncryptionService _encryption;
        private readonly WorkflowEngineService<FirmApplication> _engine;

        public FirmRegistrationController(FirmRegistrationDbContext context, ApplicationDbContext sharedContext, IFileEncryptionService encryption)
        {
            _context = context;
            _sharedContext = sharedContext;
            _encryption = encryption;
            _engine = new WorkflowEngineService<FirmApplication>(context, sharedContext, ServiceType);
        }

        // Officers share the cookie scheme and also put their own id in NameIdentifier,
        // so an officer must never be treated as a citizen.
        private int? GetCurrentUserId()
        {
            if (User.IsInRole("Officer")) return null;
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private bool IsAjax =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        // { success:false, errors:{ "FieldName":["message"], ... } } - shown next to each field by registration_form.js
        private IActionResult ValidationErrorsJson()
        {
            var errors = ModelState
                .Where(e => e.Value != null && e.Value.Errors.Count > 0)
                .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());
            return BadRequest(new { success = false, errors });
        }

        private static bool IsEditable(string? status) => status == "Draft" || status == "ReturnedToApplicant";

        // ---------------- My Applications ----------------

        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var userId = GetCurrentUserId();

            var applications = userId.HasValue
                ? await _context.FirmApplications.AsNoTracking()
                    .Where(a => a.UserId == userId.Value)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync()
                : new List<FirmApplication>();

            var ids = applications.Select(a => a.Id).ToList();
            ViewBag.Payments = await _sharedContext.WorkflowPayments.AsNoTracking()
                .Where(p => p.ServiceType == ServiceType && ids.Contains(p.ApplicationId))
                .ToDictionaryAsync(p => p.ApplicationId);

            return View(applications);
        }

        // ---------------- Form: new / edit ----------------

        [HttpGet]
        public IActionResult Index()
        {
            if (!GetCurrentUserId().HasValue) return Forbid();
            return View(new FirmFormViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var app = await _context.FirmApplications.AsNoTracking()
                .Include(a => a.Partners)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.UserId != userId) return Forbid();
            if (!IsEditable(app.Status)) return RedirectToAction(nameof(Preview), new { id });

            return View("Index", ToViewModel(app));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(FirmFormViewModel model, string actionButton)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            bool submit = actionButton == "submit";

            FirmApplication? app = null;
            if (model.Id > 0)
            {
                app = await _context.FirmApplications.Include(a => a.Partners)
                    .FirstOrDefaultAsync(a => a.Id == model.Id);
                if (app == null) return NotFound();
                if (app.UserId != userId) return Forbid();
                if (!IsEditable(app.Status)) return Forbid();
            }

            var existingPartners = app?.Partners.ToDictionary(p => p.Id) ?? new Dictionary<int, FirmPartner>();
            model.Partners ??= new List<FirmPartnerInput>();
            // a posted id that isn't one of this application's partners = tampering
            if (model.Partners.Any(p => p.Id != 0 && !existingPartners.ContainsKey(p.Id))) return Forbid();

            // ---------- validate ----------
            var uploads = new Dictionary<IFormFile, (byte[] Bytes, string ContentType)>();
            async Task ReadUpload(IFormFile? file, string label, bool pdfOnly, string key = "")
            {
                if (file == null) return;
                var (bytes, contentType, error) = await RegistrationFileHelper.ReadAsync(file, label, pdfOnly);
                if (error != null) ModelState.AddModelError(key, error);
                else uploads[file] = (bytes!, contentType!);
            }

            await ReadUpload(model.FormIFile, "Form-I", true, nameof(model.FormIFile));
            await ReadUpload(model.PartnershipDeedFile, "Partnership deed", true, nameof(model.PartnershipDeedFile));
            await ReadUpload(model.AddressProofFile, "Rental agreement / utility bill", true, nameof(model.AddressProofFile));
            for (int i = 0; i < model.Partners.Count; i++)
            {
                await ReadUpload(model.Partners[i].IdProofFile, $"Partner {i + 1} ID proof", false, $"Partners[{i}]");
                await ReadUpload(model.Partners[i].SignatureFile, $"Partner {i + 1} signature", false, $"Partners[{i}]");
            }

            if (!string.IsNullOrWhiteSpace(model.MobileNumber) && !Regex.IsMatch(model.MobileNumber.Trim(), @"^[6-9]\d{9}$"))
                ModelState.AddModelError(nameof(model.MobileNumber), "Enter a valid 10-digit mobile number.");
            if (!string.IsNullOrWhiteSpace(model.Email) && !Regex.IsMatch(model.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                ModelState.AddModelError(nameof(model.Email), "Enter a valid email id.");

            if (submit)
            {
                void Need(string? value, string field, string message)
                {
                    if (string.IsNullOrWhiteSpace(value)) ModelState.AddModelError(field, message);
                }
                Need(model.ApplicantName, nameof(model.ApplicantName), "Applicant name is required.");
                Need(model.MobileNumber, nameof(model.MobileNumber), "Mobile number is required.");
                Need(model.Email, nameof(model.Email), "Email id is required.");
                Need(model.ProposedFirmName, nameof(model.ProposedFirmName), "Proposed name of the firm is required.");
                Need(model.FirmName, nameof(model.FirmName), "Firm name is required.");
                Need(model.PrincipalPlace, nameof(model.PrincipalPlace), "Principal place of business is required.");
                Need(model.Station, nameof(model.Station), "Station is required.");

                if (model.Partners.Count == 0)
                    ModelState.AddModelError(nameof(model.Partners), "Add at least one partner.");

                for (int i = 0; i < model.Partners.Count; i++)
                {
                    var p = model.Partners[i];
                    existingPartners.TryGetValue(p.Id, out var ex);
                    if (string.IsNullOrWhiteSpace(p.Name) || !p.DateOfJoining.HasValue || string.IsNullOrWhiteSpace(p.PermanentAddress))
                        ModelState.AddModelError($"Partners[{i}]", $"Partner {i + 1}: name, date of joining and permanent address are required.");
                    if (p.DateOfJoining.HasValue && p.DateOfJoining.Value.Date > DateTime.Today)
                        ModelState.AddModelError($"Partners[{i}]", $"Partner {i + 1}: date of joining cannot be a future date.");
                    if (p.IdProofFile == null && ex?.IdProofDocumentId == null)
                        ModelState.AddModelError($"Partners[{i}]", $"Partner {i + 1}: ID proof is required.");
                    if (p.SignatureFile == null && ex?.SignatureDocumentId == null)
                        ModelState.AddModelError($"Partners[{i}]", $"Partner {i + 1}: signature is required.");
                }

                if (model.FormIFile == null && app?.FormIDocumentId == null)
                    ModelState.AddModelError(nameof(model.FormIFile), "Scanned copy of Form-I is required.");
                if (model.PartnershipDeedFile == null && app?.PartnershipDeedDocumentId == null)
                    ModelState.AddModelError(nameof(model.PartnershipDeedFile), "Scanned copy of the partnership deed is required.");
                if (model.AddressProofFile == null && app?.AddressProofDocumentId == null)
                    ModelState.AddModelError(nameof(model.AddressProofFile), "Rental agreement / utility bill (proof of address) is required.");
                if (!model.Declaration)
                    ModelState.AddModelError(nameof(model.Declaration), "Please accept the declaration.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAjax) return ValidationErrorsJson();
                Hydrate(model, app, existingPartners);
                return View("Index", model);
            }

            // ---------- persist ----------
            if (app == null)
            {
                app = new FirmApplication { UserId = userId, CreatedDate = DateTime.Now };
                _context.FirmApplications.Add(app);
            }

            app.ApplicantName = Clean(model.ApplicantName);
            app.ApplicantAddress = Clean(model.ApplicantAddress);
            app.MobileNumber = Clean(model.MobileNumber);
            app.Email = Clean(model.Email);
            app.ProposedFirmName = Clean(model.ProposedFirmName);
            app.FirmName = Clean(model.FirmName);
            app.PrincipalPlace = Clean(model.PrincipalPlace);
            app.OtherPlace = Clean(model.OtherPlace);
            app.Station = Clean(model.Station);
            app.DeclarationAccepted = model.Declaration;
            app.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();   // gives a new application its Id

            if (string.IsNullOrEmpty(app.ApplicationNumber))
                app.ApplicationNumber = $"FRM/{DateTime.Now:yyyy}/{app.Id:D6}";

            app.FormIDocumentId = await StoreAsync(app.Id, "FormI", model.FormIFile, app.FormIDocumentId, uploads);
            app.PartnershipDeedDocumentId = await StoreAsync(app.Id, "PartnershipDeed", model.PartnershipDeedFile, app.PartnershipDeedDocumentId, uploads);
            app.AddressProofDocumentId = await StoreAsync(app.Id, "AddressProof", model.AddressProofFile, app.AddressProofDocumentId, uploads);

            // partners removed from the table
            var keptIds = model.Partners.Where(p => p.Id != 0).Select(p => p.Id).ToHashSet();
            foreach (var removed in app.Partners.Where(p => !keptIds.Contains(p.Id)).ToList())
            {
                await DeleteDocumentsAsync(app.Id, removed.IdProofDocumentId, removed.SignatureDocumentId);
                _context.FirmPartners.Remove(removed);
                app.Partners.Remove(removed);
            }

            foreach (var input in model.Partners)
            {
                FirmPartner partner;
                if (input.Id != 0) partner = existingPartners[input.Id];
                else
                {
                    partner = new FirmPartner { FirmApplicationId = app.Id };
                    app.Partners.Add(partner);
                }

                partner.Name = Clean(input.Name) ?? string.Empty;
                partner.DateOfJoining = input.DateOfJoining;
                partner.PermanentAddress = Clean(input.PermanentAddress);
                partner.IdProofDocumentId = await StoreAsync(app.Id, "PartnerIdProof", input.IdProofFile, partner.IdProofDocumentId, uploads);
                partner.SignatureDocumentId = await StoreAsync(app.Id, "PartnerSignature", input.SignatureFile, partner.SignatureDocumentId, uploads);
            }

            await _context.SaveChangesAsync();

            if (!submit)
            {
                TempData["Message"] = "Draft saved. You can continue it from My Applications.";
                return RedirectToAction(nameof(MyApplications));
            }

            if (app.Status == "ReturnedToApplicant")
            {
                // stage + assigned officer untouched -> lands back with the same officer
                var result = await _engine.ResubmitAsync(app.Id, userId);
                if (!result.Success)
                {
                    if (IsAjax) return BadRequest(new { success = false, errors = new Dictionary<string, string[]> { [""] = new[] { result.Error ?? "Could not resubmit the application." } } });
                    TempData["Error"] = result.Error;
                    return RedirectToAction(nameof(Preview), new { id = app.Id });
                }
            }
            else
            {
                app.Status = "Submitted";
                app.CurrentStage = "Initial Scrutiny";
                app.AssignedOfficerId = null;
                app.PaymentStatus = "NotRequested";
                app.SubmittedDate = DateTime.Now;
                app.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // The submit button posts via AJAX and shows the SweetAlert itself; a plain post
            // falls back to the status page, which shows the same popup from TempData.
            if (IsAjax)
                return Json(new { success = true, applicationNumber = app.ApplicationNumber, redirectUrl = Url.Action(nameof(MyApplications)) });

            TempData["SubmitSuccess"] = true;
            TempData["SubmittedAppNumber"] = app.ApplicationNumber;
            return RedirectToAction(nameof(MyApplications));
        }

        // ---------------- Preview (read-only) ----------------

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var app = await _context.FirmApplications.AsNoTracking()
                .Include(a => a.Partners)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.UserId != userId) return Forbid();

            ViewBag.DocNames = await _context.FirmApplicationDocuments.AsNoTracking()
                .Where(d => d.FirmApplicationId == id)
                .ToDictionaryAsync(d => d.DocumentId, d => d.FileName ?? d.DocumentType);
            ViewBag.Payment = await _engine.GetPaymentAsync(id);
            var (history, _, _) = await _engine.GetHistoryContextAsync(id, app.CurrentStage);
            ViewBag.History = history;

            return View(app);
        }

        // ---------------- Payment (temporary test action, same as Electricity) ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayNow(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var result = await _engine.PayNowAsync(id, userId);
            if (result.Success) TempData["Message"] = result.Message;
            else TempData["Error"] = result.Error;

            return RedirectToAction(nameof(MyApplications));
        }

        // ---------------- Own documents (decrypted on the fly, owner only) ----------------

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int documentId, bool download = false)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var doc = await _context.FirmApplicationDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (doc == null || doc.FileData.Length == 0) return NotFound();

            var ownerId = await _context.FirmApplications.AsNoTracking()
                .Where(a => a.Id == doc.FirmApplicationId)
                .Select(a => a.UserId)
                .FirstOrDefaultAsync();
            if (ownerId != userId) return Forbid();

            var bytes = _encryption.Decrypt(doc.FileData, doc.FileIV);

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.Append("Content-Disposition",
                $"{(download ? "attachment" : "inline")}; filename=\"{RegistrationFileHelper.SafeFileName(doc.DocumentType, doc.DocumentId, doc.ContentType)}\"");

            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }

        // ---------------- helpers ----------------

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // Stores (or replaces) one encrypted document and returns its DocumentId.
        // No new file -> the existing id is kept as it is.
        private async Task<int?> StoreAsync(int appId, string documentType, IFormFile? file, int? existingId,
            Dictionary<IFormFile, (byte[] Bytes, string ContentType)> uploads)
        {
            if (file == null || !uploads.TryGetValue(file, out var upload)) return existingId;

            var (cipher, iv) = _encryption.Encrypt(upload.Bytes);

            FirmApplicationDocument? doc = null;
            if (existingId.HasValue)
                doc = await _context.FirmApplicationDocuments
                    .FirstOrDefaultAsync(d => d.DocumentId == existingId.Value && d.FirmApplicationId == appId);

            if (doc == null)
            {
                doc = new FirmApplicationDocument { FirmApplicationId = appId, DocumentType = documentType };
                _context.FirmApplicationDocuments.Add(doc);
            }

            doc.FileName = RegistrationFileHelper.CleanFileName(file.FileName);
            doc.ContentType = upload.ContentType;
            doc.FileData = cipher;
            doc.FileIV = iv;
            doc.UploadedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();   // needed so a new document has its DocumentId
            return doc.DocumentId;
        }

        private async Task DeleteDocumentsAsync(int appId, params int?[] documentIds)
        {
            var ids = documentIds.Where(i => i.HasValue).Select(i => i!.Value).ToList();
            if (ids.Count == 0) return;
            var docs = await _context.FirmApplicationDocuments
                .Where(d => d.FirmApplicationId == appId && ids.Contains(d.DocumentId))
                .ToListAsync();
            _context.FirmApplicationDocuments.RemoveRange(docs);
        }

        private static FirmFormViewModel ToViewModel(FirmApplication app) => new()
        {
            Id = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            Status = app.Status,
            OfficerRemarks = app.OfficerRemarks,
            ApplicantName = app.ApplicantName,
            ApplicantAddress = app.ApplicantAddress,
            MobileNumber = app.MobileNumber,
            Email = app.Email,
            ProposedFirmName = app.ProposedFirmName,
            FirmName = app.FirmName,
            PrincipalPlace = app.PrincipalPlace,
            OtherPlace = app.OtherPlace,
            Station = app.Station,
            Declaration = app.DeclarationAccepted,
            FormIDocumentId = app.FormIDocumentId,
            PartnershipDeedDocumentId = app.PartnershipDeedDocumentId,
            AddressProofDocumentId = app.AddressProofDocumentId,
            Partners = app.Partners.OrderBy(p => p.Id).Select(p => new FirmPartnerInput
            {
                Id = p.Id,
                Name = p.Name,
                DateOfJoining = p.DateOfJoining,
                PermanentAddress = p.PermanentAddress,
                IdProofDocumentId = p.IdProofDocumentId,
                SignatureDocumentId = p.SignatureDocumentId
            }).ToList()
        };

        // After a failed validation: put back everything the form shows but doesn't post.
        private static void Hydrate(FirmFormViewModel model, FirmApplication? app, Dictionary<int, FirmPartner> existingPartners)
        {
            if (app != null)
            {
                model.ApplicationNumber = app.ApplicationNumber;
                model.Status = app.Status;
                model.OfficerRemarks = app.OfficerRemarks;
                model.FormIDocumentId = app.FormIDocumentId;
                model.PartnershipDeedDocumentId = app.PartnershipDeedDocumentId;
                model.AddressProofDocumentId = app.AddressProofDocumentId;
            }
            foreach (var p in model.Partners)
            {
                if (p.Id != 0 && existingPartners.TryGetValue(p.Id, out var ex))
                {
                    p.IdProofDocumentId = ex.IdProofDocumentId;
                    p.SignatureDocumentId = ex.SignatureDocumentId;
                }
            }
        }
    }
}
