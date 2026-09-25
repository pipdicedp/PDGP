using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    // ASSUMPTION: ElectricityApplicationDbContext lives in namespace
    // TradeLicence.Data — inferred from the migration Designer file's
    // "using TradeLicence.Data;" line, since that file wasn't shared
    // directly. If it's actually elsewhere, just fix this one using line.
    //
    // Same shape as WaterOfficerController — all Forward/Revert/Approve/
    // Reject/Return/Payment logic lives in WorkflowEngineService<EBapplication>,
    // shared with every other service.
    [Authorize(Roles = "Officer")]
    public class ElectricityOfficerController : Controller
    {
        private const string ServiceType = "Electricity";

        private readonly ElectricityApplicationDbContext _electricityContext;
        private readonly ApplicationDbContext _sharedContext;
        private readonly WorkflowEngineService<EBapplication> _engine;

        public ElectricityOfficerController(ElectricityApplicationDbContext electricityContext, ApplicationDbContext sharedContext)
        {
            _electricityContext = electricityContext;
            _sharedContext = sharedContext;
            _engine = new WorkflowEngineService<EBapplication>(electricityContext, sharedContext, ServiceType);
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentOfficerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        // Only officers whose own Department is "Electricity" can work
        // this queue — same Officers table/login as every other service.
        private async Task<Officer?> GetCurrentElectricityOfficerAsync()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return null;

            var officer = await _sharedContext.Officers.FindAsync(officerId.Value);
            if (officer == null || officer.Department != "Electricity") return null;
            return officer;
        }

        // ---------------- Dashboard ----------------
        // The actual queue is built in OfficerController.Index (Department
        // routes it there) — this just keeps "Index" resolvable so
        // _OfficerWorkflowPanel's "Back to Dashboard" link (Url.Action("Index"),
        // no controller specified) still works from this controller too.

        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        // ---------------- Application detail + workflow panel ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var application = await _electricityContext.EBapplications
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null) return NotFound();

            var currentStage = application.CurrentStage;

            var forwardableByStage = await _engine.GetForwardableOfficersAsync(currentStage);
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
            ViewBag.SubmittedDate = application.CreatedDate ?? DateTime.UtcNow;

            return View(application);
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
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
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.RevertToPreviousOfficerAsync(id, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "revert";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Final Approval ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
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
            var officer = await GetCurrentElectricityOfficerAsync();
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
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.ReturnToApplicantAsync(id, remarks);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "return";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Inspection-stage payment ----------------

        // Fixed amounts — adjust here if Electricity's fee differs.
        private const decimal PaymentAmount = 1000m;
        private const decimal ExtraCharge = 500m;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPaymentRequest(int id)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.SendPaymentRequestAsync(id, PaymentAmount, ExtraCharge);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "payment";
            return RedirectToAction(nameof(ViewApplication), new { id });
        }

        // ---------------- Officer-uploaded stage documents (Verification/Inspection) ----------------
        // Uses the shared, encrypted-in-DB WorkflowSupportingDocuments table —
        // same as Water — even though Electricity's own CITIZEN documents are
        // stored as disk paths (a different, older pattern). These two are
        // unrelated: this is only for files an OFFICER uploads during review.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDocument(int id, IFormFile file, [FromServices] IFileEncryptionService encryption)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            var application = await _electricityContext.EBapplications.FindAsync(id);
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
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStageDocument(int docId, [FromServices] IFileEncryptionService encryption)
        {
            var officer = await GetCurrentElectricityOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }
    }
}
