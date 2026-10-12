using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Models.Caf;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// The receiving department's side of a forwarded CAF: full CAF preview + the standard 4-category
    /// officer workflow (Initial Scrutiny -> Verification -> Inspection -> Approval), run by the shared
    /// WorkflowEngineService on CafDepartmentApplication. Action names match what
    /// _OfficerWorkflowPanel posts to, same as WaterOfficer / ElectricityOfficer.
    ///
    /// An officer can only open and act on CAFs forwarded to THEIR OWN department.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class CafDepartmentOfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICafFormService _cafService;
        private readonly IFileEncryptionService _encryption;

        public CafDepartmentOfficerController(ApplicationDbContext context, ICafFormService cafService, IFileEncryptionService encryption)
        {
            _context = context;
            _cafService = cafService;
            _encryption = encryption;
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentOfficerId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : (int?)null;
        }

        // One engine per request, scoped to the department of the application being worked on:
        //  - officers offered in "Forward to" are limited to that department
        //  - there is no applicant-payment step for CAF, so Inspection doesn't wait for payment
        private WorkflowEngineService<CafDepartmentApplication> CreateEngine(string department) =>
            new WorkflowEngineService<CafDepartmentApplication>(
                _context, _context, CafWorkflowConfig.ServiceType, department, requirePaymentAtInspection: false);

        // The officer must belong to the SAME department the CAF was forwarded to.
        private async Task<(Officer? Officer, CafDepartmentApplication? App)> GetAuthorizedAsync(int id)
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return (null, null);

            var officer = await _context.Officers.FindAsync(officerId.Value);
            var app = await _context.CafDepartmentApplications.FindAsync(id);
            if (officer == null || app == null) return (null, null);

            if (!string.Equals(officer.Department, app.Department, StringComparison.OrdinalIgnoreCase))
                return (null, null);

            return (officer, app);
        }

        // The queue itself is built in OfficerController.Index (Department routes it there); this keeps
        // "Index" resolvable for _OfficerWorkflowPanel's "Back to Dashboard" link.
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        // ---------------- Full CAF preview + workflow panel ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var engine = CreateEngine(app.Department);
            var currentStage = app.CurrentStage;

            var forwardableByStage = await engine.GetForwardableOfficersAsync(currentStage);
            var (history, officerNames, revertTarget) = await engine.GetHistoryContextAsync(id, currentStage);
            var (stageDocuments, stageDocOfficerNames) = await engine.GetStageDocumentsAsync(id);

            var summary = (await _cafService.GetSubmittedSummariesAsync(new[] { app.LoginId })).FirstOrDefault();
            var preview = await _cafService.LoadPreviewAsync(app.LoginId);
            preview.AllowEdit = false;
            preview.Collapsible = true;

            var forwardedBy = await _context.Officers.FindAsync(app.ForwardedByOfficerId);

            ViewBag.CurrentStage = currentStage;
            ViewBag.IsFinalStage = engine.IsFinalStage(currentStage);
            ViewBag.ForwardableByStage = forwardableByStage;
            ViewBag.WorkflowHistory = history;
            ViewBag.OfficerNames = officerNames;
            ViewBag.RevertTarget = revertTarget;
            ViewBag.StageDocuments = stageDocuments;
            ViewBag.StageDocumentOfficerNames = stageDocOfficerNames;
            ViewBag.CanUploadStageDocument = currentStage == "Verification" || currentStage == "Inspection";
            ViewBag.ExistingStageDocumentForCurrentStage = stageDocuments.FirstOrDefault(d => d.Stage == currentStage);
            ViewBag.Payment = null;
            ViewBag.SubmittedDate = app.ForwardedDate;

            // CAF department workflows have no applicant-facing steps: hide payment + "Return to Applicant".
            ViewBag.HidePayment = true;
            ViewBag.HideReturnToApplicant = true;

            return View(new CafDepartmentApplicationVm
            {
                Application = app,
                Preview = preview,
                Summary = summary,
                ForwardedByName = forwardedBy?.FullName ?? forwardedBy?.Username
            });
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine(app.Department).ForwardToOfficerAsync(id, officerId, targetStage, remarks, officer.OfficerId);
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

            var result = await CreateEngine(app.Department).RevertToPreviousOfficerAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "revert";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Final decision (this department only) ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var result = await CreateEngine(app.Department).ApproveApplicationAsync(id, remarks);
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

            var result = await CreateEngine(app.Department).RejectApplicationAsync(id, remarks);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "reject";
            return RedirectToAction(nameof(Index));
        }

        // The panel names these two actions, but a CAF department workflow has neither an applicant to
        // return to (a submitted CAF is locked) nor an applicant payment — the buttons are hidden, and
        // these keep a hand-made request from doing anything.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReturnToApplicant(int id, string remarks) =>
            BadRequest(new { error = "A CAF can't be returned to the applicant." });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendPaymentRequest(int id) =>
            BadRequest(new { error = "There is no payment step for a CAF." });

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

            var doc = await CreateEngine(app.Department).UploadStageDocumentAsync(
                id, currentStage, officer.OfficerId, file.FileName, file.ContentType, cipherBytes, iv);

            return Json(new
            {
                docId = doc.WorkflowSupportingDocumentId,
                fileName = doc.FileName,
                contentType = doc.ContentType,
                uploadedDate = doc.UploadedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm tt")
            });
        }

        // A stage document is only readable by officers of the department its application was forwarded to.
        private async Task<WorkflowSupportingDocument?> GetAuthorizedStageDocumentAsync(int docId)
        {
            var doc = await _context.WorkflowSupportingDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.WorkflowSupportingDocumentId == docId && d.ServiceType == CafWorkflowConfig.ServiceType);
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
    }
}
