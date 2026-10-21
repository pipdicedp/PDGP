using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// The Fire Service Department's side of a Fire NOC application: the full
    /// application preview + the standard 4-stage officer workflow (Initial
    /// Scrutiny -> Verification -> Inspection -> Approval), run by the shared
    /// WorkflowEngineService on FireClearanceApplication.
    ///
    /// Action names match what _OfficerWorkflowPanel posts to, same as
    /// CafDepartmentOfficer / WaterOfficer / ElectricityOfficer. An officer must
    /// belong to the "Fire" department to open or act on a fire application.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class FireOfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFireFormService _fireService;
        private readonly IFireNocPdfService _pdf;
        private readonly IFileEncryptionService _encryption;

        public FireOfficerController(
            ApplicationDbContext context,
            IFireFormService fireService,
            IFireNocPdfService pdf,
            IFileEncryptionService encryption)
        {
            _context = context;
            _fireService = fireService;
            _pdf = pdf;
            _encryption = encryption;
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentOfficerId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : (int?)null;
        }

        // There is no applicant-payment step for a Fire NOC, so Inspection
        // doesn't wait for a payment.
        private WorkflowEngineService<FireClearanceApplication> CreateEngine() =>
            new WorkflowEngineService<FireClearanceApplication>(
                _context, _context, FireWorkflowConfig.ServiceType, FireWorkflowConfig.Department,
                requirePaymentAtInspection: false);

        // An Administrator sits outside the 4 workflow stages, so they aren't
        // tied to the "Fire" department. They're allowed in so they can
        // approve/reject a Fire NOC directly (see ApproveApplication / AdminQueue).
        private static bool IsAdministrator(Officer officer) =>
            string.Equals(officer.Designation, "Admin", StringComparison.OrdinalIgnoreCase);

        // A Fire department officer may work a fire application — and so may an
        // Administrator, who approves it directly.
        private async Task<(Officer? Officer, FireClearanceApplication? App)> GetAuthorizedAsync(int id)
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return (null, null);

            var officer = await _context.Officers.FindAsync(officerId.Value);
            var app = await _context.FireClearanceApplications.FindAsync(id);
            if (officer == null || app == null) return (null, null);

            if (!IsAdministrator(officer)
                && !string.Equals(officer.Department, FireWorkflowConfig.Department, StringComparison.OrdinalIgnoreCase))
                return (null, null);

            return (officer, app);
        }

        // The queue itself is built in OfficerController.Index (the Fire department routes it here);
        // this keeps "Index" resolvable for _OfficerWorkflowPanel's "Back to Dashboard" link.
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        /// <summary>
        /// Administrator's Fire NOC queue: every submitted fire application,
        /// whatever stage it's at, so an admin can open one and approve it
        /// directly (the 4-stage DEO/Manager/Inspection chain is skipped).
        /// Reuses the shared officer queue view.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AdminQueue()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return Forbid();

            var officer = await _context.Officers.FindAsync(officerId.Value);
            if (officer == null || !IsAdministrator(officer)) return Forbid();

            var apps = await _context.FireClearanceApplications
                .Where(a => a.Status == "Submitted")
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            var items = apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.Id,
                ServiceType = "Fire NOC",
                DisplayName = a.Name ?? "-",
                Contact = a.MobileNo ?? "-",
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                SubmittedDate = a.SubmittedDate ?? DateTime.UtcNow,
                ViewUrl = Url.Action("ViewApplication", "FireOfficer", new { id = a.Id })!
            }).ToList();

            ViewBag.Designation = officer.Designation;
            ViewBag.Department = officer.Department;
            return View("~/Views/Officer/SharedQueue.cshtml", items);
        }

        // ---------------- Full application preview + workflow panel ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var engine = CreateEngine();
            var currentStage = app.CurrentStage;

            var forwardableByStage = await engine.GetForwardableOfficersAsync(currentStage);
            var (history, officerNames, revertTarget) = await engine.GetHistoryContextAsync(id, currentStage);
            var (stageDocuments, stageDocOfficerNames) = await engine.GetStageDocumentsAsync(id);

            var preview = await _fireService.LoadPreviewAsync(app.LoginId);
            preview.AllowEdit = false;
            preview.Collapsible = true;

            var isAdmin = IsAdministrator(officer);

            ViewBag.CurrentStage = currentStage;
            // An Administrator may approve at any stage, so for them the
            // "Final Approval" (Approve/Reject) panel is always available.
            ViewBag.IsFinalStage = engine.IsFinalStage(currentStage) || isAdmin;
            ViewBag.AdminOverride = isAdmin;
            ViewBag.ForwardableByStage = forwardableByStage;
            ViewBag.WorkflowHistory = history;
            ViewBag.OfficerNames = officerNames;
            ViewBag.RevertTarget = revertTarget;
            ViewBag.StageDocuments = stageDocuments;
            ViewBag.StageDocumentOfficerNames = stageDocOfficerNames;
            ViewBag.CanUploadStageDocument = currentStage == "Verification" || currentStage == "Inspection";
            ViewBag.ExistingStageDocumentForCurrentStage = stageDocuments.FirstOrDefault(d => d.Stage == currentStage);
            ViewBag.Payment = null;
            ViewBag.SubmittedDate = app.SubmittedDate ?? app.ModifiedDate ?? DateTime.UtcNow;

            // Fire NOC has no applicant-payment step; "Return to Applicant" IS available.
            ViewBag.HidePayment = true;

            return View(new FireApplicationVm
            {
                Application = app,
                Preview = preview,
                DocumentSizes = await _fireService.GetDocumentSizesAsync(app.LoginId)
            });
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine().ForwardToOfficerAsync(id, officerId, targetStage, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "forward";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertToPreviousOfficer(int id, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine().RevertToPreviousOfficerAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "revert";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Final decision ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine().ApproveApplicationAsync(id, remarks, allowAnyStage: IsAdministrator(officer));
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "approve";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine().RejectApplicationAsync(id, remarks, IsAdministrator(officer));
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "reject";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToApplicant(int id, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine().ReturnToApplicantAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "return";
            return RedirectToAction(nameof(Index));
        }

        // The panel names this action, but a Fire NOC workflow has no applicant payment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendPaymentRequest(int id) =>
            BadRequest(new { error = "There is no payment step for a Fire NOC application." });

        // ---------------- Officer-uploaded stage documents (Verification / Inspection) ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDocument(int id, IFormFile file)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            var currentStage = app.CurrentStage;
            if (currentStage != "Verification" && currentStage != "Inspection")
                return BadRequest(new { error = "A supporting document can only be uploaded at the Verification or Inspection stage." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var (cipherBytes, iv) = _encryption.Encrypt(ms.ToArray());

            var doc = await CreateEngine().UploadStageDocumentAsync(
                id, currentStage, officer.OfficerId, file.FileName, file.ContentType, cipherBytes, iv);

            return Json(new
            {
                docId = doc.WorkflowSupportingDocumentId,
                fileName = doc.FileName,
                contentType = doc.ContentType,
                uploadedDate = doc.UploadedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm tt")
            });
        }

        private async Task<WorkflowSupportingDocument?> GetAuthorizedStageDocumentAsync(int docId)
        {
            var doc = await _context.WorkflowSupportingDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.WorkflowSupportingDocumentId == docId && d.ServiceType == FireWorkflowConfig.ServiceType);
            if (doc == null) return null;

            var (officer, app) = await GetAuthorizedAsync(doc.ApplicationId);
            return officer == null || app == null ? null : doc;
        }

        [HttpGet]
        public async Task<IActionResult> PreviewStageDocument(int docId)
        {
            var doc = await GetAuthorizedStageDocumentAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = _encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStageDocument(int docId)
        {
            var doc = await GetAuthorizedStageDocumentAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = _encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }

        // ---------------- Applicant uploads + NOC ----------------

        /// <summary>Opens one of the applicant's uploaded documents (Fire department officers only).</summary>
        [HttpGet]
        public async Task<IActionResult> Document(int id, string column)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var def = FireFormMetadata.DocumentByColumn(column);
            if (def == null) return NotFound();

            var bytes = await _fireService.GetDocumentAsync(app.LoginId, def.Column);
            if (bytes == null) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(bytes, "application/octet-stream");
        }

        /// <summary>The Fire Safety Clearance certificate, for an approved application.</summary>
        [HttpGet]
        public async Task<IActionResult> Noc(int id)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            if (!string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "The Fire Safety Clearance is available only for approved applications." });

            var bytes = await _pdf.GenerateNocPdfAsync(app.LoginId);
            var fileName = $"Fire-NOC-{app.ApplicationCode?.Replace('/', '-') ?? app.Id.ToString()}.pdf";
            return File(bytes, "application/pdf", fileName);
        }
    }
}
