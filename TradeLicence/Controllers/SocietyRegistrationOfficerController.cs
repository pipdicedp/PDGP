using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    // Officer side of "Registration / Renewal of Society". Same shape as ElectricityOfficerController - all
    // Forward / Revert / Approve / Reject / Return / Payment logic lives in the shared
    // WorkflowEngineService<SocietyApplication>. Only officers whose Department is
    // "Society Registration" (RegistrationWorkflowConfig.SocietyDepartment) can work this queue.
    [Authorize(Roles = "Officer")]
    public class SocietyRegistrationOfficerController : Controller
    {
        private const string ServiceType = RegistrationWorkflowConfig.SocietyServiceType;

        private readonly SocietyRegistrationDbContext _appContext;
        private readonly ApplicationDbContext _sharedContext;
        private readonly WorkflowEngineService<SocietyApplication> _engine;

        public SocietyRegistrationOfficerController(SocietyRegistrationDbContext appContext, ApplicationDbContext sharedContext)
        {
            _appContext = appContext;
            _sharedContext = sharedContext;
            _engine = new WorkflowEngineService<SocietyApplication>(appContext, sharedContext, ServiceType);
        }

        private int? GetCurrentOfficerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        private async Task<Officer?> GetCurrentServiceOfficerAsync()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return null;

            var officer = await _sharedContext.Officers.FindAsync(officerId.Value);
            if (officer == null || !RegistrationWorkflowConfig.IsSocietyOfficer(officer.Department)) return null;
            return officer;
        }

        // The queue itself is built in OfficerController.Index; this keeps "Index" resolvable
        // for _OfficerWorkflowPanel's "Back to Dashboard" link (Url.Action("Index")).
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var application = await _appContext.SocietyApplications
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            var currentStage = application.CurrentStage;

            var forwardableByStage = await _engine.GetForwardableOfficersAsync(currentStage, officer.OfficerId);
            var (history, officerNames, revertTarget) = await _engine.GetHistoryContextAsync(id, currentStage);
            var (stageDocuments, stageDocOfficerNames) = await _engine.GetStageDocumentsAsync(id);
            var payment = await _engine.GetPaymentAsync(id);

            ViewBag.CurrentStage = currentStage;
            ViewBag.IsFinalStage = _engine.IsFinalStage(currentStage);
            ViewBag.ForwardableByStage = forwardableByStage;
            ViewBag.WorkflowHistory = history;
            ViewBag.OfficerNames = officerNames;
            ViewBag.RevertTarget = revertTarget;
            ViewBag.StageDocuments = stageDocuments;
            ViewBag.StageDocumentOfficerNames = stageDocOfficerNames;
            ViewBag.CanUploadStageDocument = currentStage == "Verification" || currentStage == "Inspection";
            ViewBag.ExistingStageDocumentForCurrentStage = stageDocuments.FirstOrDefault(d => d.Stage == currentStage);
            ViewBag.Payment = payment;
            ViewBag.SubmittedDate = application.SubmittedDate ?? application.CreatedDate ?? DateTime.UtcNow;

            // names of the citizen's uploaded documents, for the shared details partial
            ViewBag.DocNames = await _appContext.SocietyApplicationDocuments.AsNoTracking()
                .Where(d => d.SocietyApplicationId == id)
                .ToDictionaryAsync(d => d.DocumentId, d => d.FileName ?? d.DocumentType);

            return View(application);
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.ForwardToOfficerAsync(id, officerId, targetStage, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "forward";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertToPreviousOfficer(int id, string remarks)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.RevertToPreviousOfficerAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "revert";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Final approval / reject / return ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.ApproveApplicationAsync(id, remarks);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "approve";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string remarks)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.RejectApplicationAsync(id, remarks);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "reject";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToApplicant(int id, string remarks)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.ReturnToApplicantAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "return";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Inspection-stage payment ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPaymentRequest(int id)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.SendPaymentRequestAsync(id,
                RegistrationWorkflowConfig.SocietyFee, RegistrationWorkflowConfig.SocietyExtraCharge);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "payment";
            return RedirectToAction(nameof(ViewApplication), new { id });
        }

        // ---------------- Officer-uploaded stage documents (Verification / Inspection) ----------------
        // Shared, encrypted WorkflowSupportingDocuments table - same as Water / Electricity.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDocument(int id, IFormFile file, [FromServices] IFileEncryptionService encryption)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            var application = await _appContext.SocietyApplications.FindAsync(id);
            if (application == null) return NotFound();

            var currentStage = application.CurrentStage;
            if (currentStage != "Verification" && currentStage != "Inspection")
                return BadRequest(new { error = "A supporting document can only be uploaded at the Verification or Inspection stage." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var (cipherBytes, iv) = encryption.Encrypt(ms.ToArray());

            var doc = await _engine.UploadStageDocumentAsync(
                id, currentStage, officer.OfficerId, file.FileName, file.ContentType, cipherBytes, iv);

            return Json(new
            {
                docId = doc.WorkflowSupportingDocumentId,
                fileName = doc.FileName,
                contentType = doc.ContentType,
                uploadedDate = doc.UploadedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm tt")
            });
        }

        [HttpGet]
        public async Task<IActionResult> PreviewStageDocument(int docId, [FromServices] IFileEncryptionService encryption)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc == null || doc.ServiceType != ServiceType || doc.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStageDocument(int docId, [FromServices] IFileEncryptionService encryption)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc == null || doc.ServiceType != ServiceType || doc.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }

        // ---------------- Citizen-uploaded documents (encrypted in the database) ----------------
        // Officers aren't the owner, so no owner check - but a department officer is required.
        [HttpGet]
        public async Task<IActionResult> OfficerDocument(int documentId, [FromServices] IFileEncryptionService encryption, bool download = false)
        {
            var officer = await GetCurrentServiceOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _appContext.SocietyApplicationDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DocumentId == documentId);
            if (doc == null || doc.FileData.Length == 0) return NotFound();

            var bytes = encryption.Decrypt(doc.FileData, doc.FileIV);

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers.Append("Content-Disposition",
                $"{(download ? "attachment" : "inline")}; filename=\"{RegistrationFileHelper.SafeFileName(doc.DocumentType, doc.DocumentId, doc.ContentType)}\"");

            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }
    }
}
