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
    /// Citizen side of "Registration / Renewal of Society" (Societies Registration Act 1860).
    /// Same shape as FirmRegistrationController / ElectricityController.
    /// </summary>
    [Authorize]
    public class SocietyRegistrationController : Controller
    {
        private const string ServiceType = RegistrationWorkflowConfig.SocietyServiceType;

        private readonly SocietyRegistrationDbContext _context;
        private readonly ApplicationDbContext _sharedContext;
        private readonly IFileEncryptionService _encryption;
        private readonly WorkflowEngineService<SocietyApplication> _engine;

        public SocietyRegistrationController(SocietyRegistrationDbContext context, ApplicationDbContext sharedContext, IFileEncryptionService encryption)
        {
            _context = context;
            _sharedContext = sharedContext;
            _encryption = encryption;
            _engine = new WorkflowEngineService<SocietyApplication>(context, sharedContext, ServiceType);
        }

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
                ? await _context.SocietyApplications.AsNoTracking()
                    .Where(a => a.UserId == userId.Value)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync()
                : new List<SocietyApplication>();

            var ids = applications.Select(a => a.Id).ToList();
            ViewBag.Payments = await _sharedContext.WorkflowPayments.AsNoTracking()
                .Where(p => p.ServiceType == ServiceType && ids.Contains(p.ApplicationId))
                .ToDictionaryAsync(p => p.ApplicationId);

            return View(applications);
        }

        // ---------------- Form: new / edit ----------------

        [HttpGet]
        public IActionResult Index(string? type)
        {
            if (!GetCurrentUserId().HasValue) return Forbid();
            return View(new SocietyFormViewModel
            {
                ApplicationType = string.Equals(type, "Renewal", StringComparison.OrdinalIgnoreCase) ? "Renewal" : "Registration"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var app = await _context.SocietyApplications.AsNoTracking()
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.UserId != userId) return Forbid();
            if (!IsEditable(app.Status)) return RedirectToAction(nameof(Preview), new { id });

            return View("Index", ToViewModel(app));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SocietyFormViewModel model, string actionButton)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            bool submit = actionButton == "submit";

            SocietyApplication? app = null;
            if (model.Id > 0)
            {
                app = await _context.SocietyApplications.Include(a => a.Members)
                    .FirstOrDefaultAsync(a => a.Id == model.Id);
                if (app == null) return NotFound();
                if (app.UserId != userId) return Forbid();
                if (!IsEditable(app.Status)) return Forbid();
            }

            var existingMembers = app?.Members.ToDictionary(m => m.Id) ?? new Dictionary<int, SocietyMember>();
            model.Members ??= new List<SocietyMemberInput>();
            if (model.Members.Any(m => m.Id != 0 && !existingMembers.ContainsKey(m.Id))) return Forbid();

            if (model.ApplicationType != "Registration" && model.ApplicationType != "Renewal")
                model.ApplicationType = "Registration";
            bool renewal = model.ApplicationType == "Renewal";

            // ---------- validate ----------
            var uploads = new Dictionary<IFormFile, (byte[] Bytes, string ContentType)>();
            async Task ReadUpload(IFormFile? file, string label, bool pdfOnly, string key = "")
            {
                if (file == null) return;
                var (bytes, contentType, error) = await RegistrationFileHelper.ReadAsync(file, label, pdfOnly);
                if (error != null) ModelState.AddModelError(key, error);
                else uploads[file] = (bytes!, contentType!);
            }

            await ReadUpload(model.MemorandumFile, "Memorandum of Association / Society", true, nameof(model.MemorandumFile));
            await ReadUpload(model.RulesFile, "Rules and Regulations of the Society", true, nameof(model.RulesFile));
            await ReadUpload(model.PowerOfAutonomyFile, "Power of Autonomy", true, nameof(model.PowerOfAutonomyFile));
            await ReadUpload(model.AddressProofFile, "Proof of the address of the society", true, nameof(model.AddressProofFile));
            await ReadUpload(model.NameAvailabilityFile, "Copy of Name Availability", true, nameof(model.NameAvailabilityFile));
            for (int i = 0; i < model.Members.Count; i++)
            {
                await ReadUpload(model.Members[i].IdProofFile, $"Member {i + 1} ID proof", false, $"Members[{i}]");
                await ReadUpload(model.Members[i].SignatureFile, $"Member {i + 1} signature", false, $"Members[{i}]");
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
                Need(model.ApplicantDesignation, nameof(model.ApplicantDesignation), "Designation of the applicant is required.");
                Need(model.SocietyName, nameof(model.SocietyName), "Name of the society is required.");
                Need(model.SocietyAddress, nameof(model.SocietyAddress), "Address of the society is required.");
                if (!renewal)
                    Need(model.ProposedSocietyName, nameof(model.ProposedSocietyName), "Proposed name of the association / society is required.");
                if (renewal)
                {
                    Need(model.ExistingRegistrationNumber, nameof(model.ExistingRegistrationNumber), "Existing registration number is required for renewal.");
                    if (!model.LicenceExpiryDate.HasValue)
                        ModelState.AddModelError(nameof(model.LicenceExpiryDate), "Registration expiry date is required for renewal.");
                }

                if (model.Members.Count == 0)
                    ModelState.AddModelError(nameof(model.Members), "Add at least one governing body member.");

                for (int i = 0; i < model.Members.Count; i++)
                {
                    var m = model.Members[i];
                    existingMembers.TryGetValue(m.Id, out var ex);
                    if (string.IsNullOrWhiteSpace(m.Name) || string.IsNullOrWhiteSpace(m.Designation)
                        || !m.DateOfAppointment.HasValue || string.IsNullOrWhiteSpace(m.Address))
                        ModelState.AddModelError($"Members[{i}]", $"Member {i + 1}: name, designation, date of appointment and address are required.");
                    if (m.DateOfAppointment.HasValue && m.DateOfAppointment.Value.Date > DateTime.Today)
                        ModelState.AddModelError($"Members[{i}]", $"Member {i + 1}: date of appointment cannot be a future date.");
                    if (m.IdProofFile == null && ex?.IdProofDocumentId == null)
                        ModelState.AddModelError($"Members[{i}]", $"Member {i + 1}: ID proof is required.");
                    if (m.SignatureFile == null && ex?.SignatureDocumentId == null)
                        ModelState.AddModelError($"Members[{i}]", $"Member {i + 1}: signature is required.");
                }

                if (model.MemorandumFile == null && app?.MemorandumDocumentId == null)
                    ModelState.AddModelError(nameof(model.MemorandumFile), "Memorandum of Association / Society is required.");
                if (model.RulesFile == null && app?.RulesDocumentId == null)
                    ModelState.AddModelError(nameof(model.RulesFile), "Rules and Regulations of the Society is required.");
                if (model.AddressProofFile == null && app?.AddressProofDocumentId == null)
                    ModelState.AddModelError(nameof(model.AddressProofFile), "Proof of the address of the society (utility bill / rental agreement) is required.");
                if (!model.Declaration)
                    ModelState.AddModelError(nameof(model.Declaration), "Please accept the declaration.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAjax) return ValidationErrorsJson();
                Hydrate(model, app, existingMembers);
                return View("Index", model);
            }

            // ---------- persist ----------
            if (app == null)
            {
                app = new SocietyApplication { UserId = userId, CreatedDate = DateTime.Now };
                _context.SocietyApplications.Add(app);
            }

            app.ApplicationType = model.ApplicationType;
            app.ExistingRegistrationNumber = renewal ? Clean(model.ExistingRegistrationNumber) : null;
            app.LicenceExpiryDate = renewal ? model.LicenceExpiryDate : null;
            app.ApplicantName = Clean(model.ApplicantName);
            app.ApplicantAddress = Clean(model.ApplicantAddress);
            app.MobileNumber = Clean(model.MobileNumber);
            app.Email = Clean(model.Email);
            app.ApplicantDesignation = Clean(model.ApplicantDesignation);
            app.SocietyName = Clean(model.SocietyName);
            app.ProposedSocietyName = Clean(model.ProposedSocietyName);
            app.SocietyAddress = Clean(model.SocietyAddress);
            app.DeclarationAccepted = model.Declaration;
            app.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (string.IsNullOrEmpty(app.ApplicationNumber))
                app.ApplicationNumber = $"SOC/{DateTime.Now:yyyy}/{app.Id:D6}";

            app.MemorandumDocumentId = await StoreAsync(app.Id, "Memorandum", model.MemorandumFile, app.MemorandumDocumentId, uploads);
            app.RulesDocumentId = await StoreAsync(app.Id, "Rules", model.RulesFile, app.RulesDocumentId, uploads);
            app.PowerOfAutonomyDocumentId = await StoreAsync(app.Id, "PowerOfAutonomy", model.PowerOfAutonomyFile, app.PowerOfAutonomyDocumentId, uploads);
            app.AddressProofDocumentId = await StoreAsync(app.Id, "AddressProof", model.AddressProofFile, app.AddressProofDocumentId, uploads);
            app.NameAvailabilityDocumentId = await StoreAsync(app.Id, "NameAvailability", model.NameAvailabilityFile, app.NameAvailabilityDocumentId, uploads);

            var keptIds = model.Members.Where(m => m.Id != 0).Select(m => m.Id).ToHashSet();
            foreach (var removed in app.Members.Where(m => !keptIds.Contains(m.Id)).ToList())
            {
                await DeleteDocumentsAsync(app.Id, removed.IdProofDocumentId, removed.SignatureDocumentId);
                _context.SocietyMembers.Remove(removed);
                app.Members.Remove(removed);
            }

            foreach (var input in model.Members)
            {
                SocietyMember member;
                if (input.Id != 0) member = existingMembers[input.Id];
                else
                {
                    member = new SocietyMember { SocietyApplicationId = app.Id };
                    app.Members.Add(member);
                }

                member.Name = Clean(input.Name) ?? string.Empty;
                member.Designation = Clean(input.Designation);
                member.DateOfAppointment = input.DateOfAppointment;
                member.Address = Clean(input.Address);
                member.IdProofDocumentId = await StoreAsync(app.Id, "MemberIdProof", input.IdProofFile, member.IdProofDocumentId, uploads);
                member.SignatureDocumentId = await StoreAsync(app.Id, "MemberSignature", input.SignatureFile, member.SignatureDocumentId, uploads);
            }

            await _context.SaveChangesAsync();

            if (!submit)
            {
                TempData["Message"] = "Draft saved. You can continue it from My Applications.";
                return RedirectToAction(nameof(MyApplications));
            }

            if (app.Status == "ReturnedToApplicant")
            {
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

            var app = await _context.SocietyApplications.AsNoTracking()
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();
            if (app.UserId != userId) return Forbid();

            ViewBag.DocNames = await _context.SocietyApplicationDocuments.AsNoTracking()
                .Where(d => d.SocietyApplicationId == id)
                .ToDictionaryAsync(d => d.DocumentId, d => d.FileName ?? d.DocumentType);
            ViewBag.Payment = await _engine.GetPaymentAsync(id);
            var (history, _, _) = await _engine.GetHistoryContextAsync(id, app.CurrentStage);
            ViewBag.History = history;

            return View(app);
        }

        // ---------------- Payment (temporary test action) ----------------

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

        // ---------------- Own documents ----------------

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int documentId, bool download = false)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Forbid();

            var doc = await _context.SocietyApplicationDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (doc == null || doc.FileData.Length == 0) return NotFound();

            var ownerId = await _context.SocietyApplications.AsNoTracking()
                .Where(a => a.Id == doc.SocietyApplicationId)
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

        private async Task<int?> StoreAsync(int appId, string documentType, IFormFile? file, int? existingId,
            Dictionary<IFormFile, (byte[] Bytes, string ContentType)> uploads)
        {
            if (file == null || !uploads.TryGetValue(file, out var upload)) return existingId;

            var (cipher, iv) = _encryption.Encrypt(upload.Bytes);

            SocietyApplicationDocument? doc = null;
            if (existingId.HasValue)
                doc = await _context.SocietyApplicationDocuments
                    .FirstOrDefaultAsync(d => d.DocumentId == existingId.Value && d.SocietyApplicationId == appId);

            if (doc == null)
            {
                doc = new SocietyApplicationDocument { SocietyApplicationId = appId, DocumentType = documentType };
                _context.SocietyApplicationDocuments.Add(doc);
            }

            doc.FileName = RegistrationFileHelper.CleanFileName(file.FileName);
            doc.ContentType = upload.ContentType;
            doc.FileData = cipher;
            doc.FileIV = iv;
            doc.UploadedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return doc.DocumentId;
        }

        private async Task DeleteDocumentsAsync(int appId, params int?[] documentIds)
        {
            var ids = documentIds.Where(i => i.HasValue).Select(i => i!.Value).ToList();
            if (ids.Count == 0) return;
            var docs = await _context.SocietyApplicationDocuments
                .Where(d => d.SocietyApplicationId == appId && ids.Contains(d.DocumentId))
                .ToListAsync();
            _context.SocietyApplicationDocuments.RemoveRange(docs);
        }

        private static SocietyFormViewModel ToViewModel(SocietyApplication app) => new()
        {
            Id = app.Id,
            ApplicationNumber = app.ApplicationNumber,
            Status = app.Status,
            OfficerRemarks = app.OfficerRemarks,
            ApplicationType = app.ApplicationType,
            ExistingRegistrationNumber = app.ExistingRegistrationNumber,
            LicenceExpiryDate = app.LicenceExpiryDate,
            ApplicantName = app.ApplicantName,
            ApplicantAddress = app.ApplicantAddress,
            MobileNumber = app.MobileNumber,
            Email = app.Email,
            ApplicantDesignation = app.ApplicantDesignation,
            SocietyName = app.SocietyName,
            ProposedSocietyName = app.ProposedSocietyName,
            SocietyAddress = app.SocietyAddress,
            Declaration = app.DeclarationAccepted,
            MemorandumDocumentId = app.MemorandumDocumentId,
            RulesDocumentId = app.RulesDocumentId,
            PowerOfAutonomyDocumentId = app.PowerOfAutonomyDocumentId,
            AddressProofDocumentId = app.AddressProofDocumentId,
            NameAvailabilityDocumentId = app.NameAvailabilityDocumentId,
            Members = app.Members.OrderBy(m => m.Id).Select(m => new SocietyMemberInput
            {
                Id = m.Id,
                Name = m.Name,
                Designation = m.Designation,
                DateOfAppointment = m.DateOfAppointment,
                Address = m.Address,
                IdProofDocumentId = m.IdProofDocumentId,
                SignatureDocumentId = m.SignatureDocumentId
            }).ToList()
        };

        private static void Hydrate(SocietyFormViewModel model, SocietyApplication? app, Dictionary<int, SocietyMember> existingMembers)
        {
            if (app != null)
            {
                model.ApplicationNumber = app.ApplicationNumber;
                model.Status = app.Status;
                model.OfficerRemarks = app.OfficerRemarks;
                model.MemorandumDocumentId = app.MemorandumDocumentId;
                model.RulesDocumentId = app.RulesDocumentId;
                model.PowerOfAutonomyDocumentId = app.PowerOfAutonomyDocumentId;
                model.AddressProofDocumentId = app.AddressProofDocumentId;
                model.NameAvailabilityDocumentId = app.NameAvailabilityDocumentId;
            }
            foreach (var m in model.Members)
            {
                if (m.Id != 0 && existingMembers.TryGetValue(m.Id, out var ex))
                {
                    m.IdProofDocumentId = ex.IdProofDocumentId;
                    m.SignatureDocumentId = ex.SignatureDocumentId;
                }
            }
        }
    }
}
