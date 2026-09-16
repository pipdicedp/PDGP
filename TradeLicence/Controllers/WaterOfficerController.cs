using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Services;
using WaterConnection.Data;
using WaterConnection.Models;

namespace WaterConnection.Controllers
{
    // Deliberately thin — all Forward/Revert/Approve/Reject/Return/Payment
    // business logic lives in WorkflowEngineService<WaterConnectionApplication>
    // (TradeLicence.Services), shared with every other service. This
    // controller's job is just: auth/claims, fetching Water-specific preview
    // data, and translating engine results into HTTP responses.
    [Authorize(Roles = "Officer")]
    public class WaterOfficerController : Controller
    {
        private const string ServiceType = "Water";

        private readonly WaterApplicationDbContext _waterContext;
        private readonly ApplicationDbContext _sharedContext;
        private readonly IFileEncryptionService _encryption;
        private readonly WorkflowEngineService<WaterConnectionApplication> _engine;

        public WaterOfficerController(WaterApplicationDbContext waterContext, ApplicationDbContext sharedContext, IFileEncryptionService encryption)
        {
            _waterContext = waterContext;
            _sharedContext = sharedContext;
            _encryption = encryption;
            _engine = new WorkflowEngineService<WaterConnectionApplication>(waterContext, sharedContext, ServiceType);
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentOfficerId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : (int?)null;
        }

        private string? GetCurrentDesignation() => User.FindFirst("Designation")?.Value;

        // Only officers whose own Department is "Water" can work this
        // queue — same Officers table/login as every other service, but an
        // officer only ever sees the department they belong to.
        private async Task<Officer?> GetCurrentWaterOfficerAsync()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return null;

            var officer = await _sharedContext.Officers.FindAsync(officerId.Value);
            if (officer == null || officer.Department != "Water") return null;
            return officer;
        }

        // ---------------- Dashboard ----------------
        // The actual queue is built in OfficerController.Index (Department
        // routes it there) — this just keeps "Index" resolvable so
        // _OfficerWorkflowPanel's "Back to Dashboard" link (Url.Action("Index"),
        // no controller specified — resolves to whichever controller renders
        // it) still works from this controller too.

        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        // ---------------- Application detail + workflow panel ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var application = await _waterContext.WaterConnectionApplications
                .Include(a => a.Purpose)
                .Include(a => a.Department)
                .Include(a => a.Section)
                .Include(a => a.Contractor)
                .Include(a => a.Area)
                .Include(a => a.NameAddressVerification)
                .Include(a => a.OwnershipVerification)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

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
            ViewBag.SubmittedDate = application.ApplicationDate;

            return View(application);
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var officer = await GetCurrentWaterOfficerAsync();
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
            var officer = await GetCurrentWaterOfficerAsync();
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
            var officer = await GetCurrentWaterOfficerAsync();
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
            var officer = await GetCurrentWaterOfficerAsync();
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
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.ReturnToApplicantAsync(id, remarks);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "return";
            return RedirectToAction(nameof(Index));
        }

        // ---------------- Inspection-stage payment ----------------

        // Fixed amounts — adjust here if Water's fee differs from TradeLicence's.
        private const decimal PaymentAmount = 1000m;
        private const decimal ExtraCharge = 500m;

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPaymentRequest(int id)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var result = await _engine.SendPaymentRequestAsync(id, PaymentAmount, ExtraCharge);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "payment";
            return RedirectToAction(nameof(ViewApplication), new { id });
        }

        // ---------------- Stage document upload/preview/download ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDocument(int id, IFormFile file)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            var application = await _waterContext.WaterConnectionApplications.FindAsync(id);
            if (application == null) return NotFound();

            var currentStage = application.CurrentStage;
            if (currentStage != "Verification" && currentStage != "Inspection")
                return BadRequest(new { error = "A supporting document can only be uploaded at the Verification or Inspection stage." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var (cipherBytes, iv) = _encryption.Encrypt(ms.ToArray());

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
        public async Task<IActionResult> PreviewStageDocument(int docId)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = _encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStageDocument(int docId)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _engine.GetStageDocumentByIdAsync(docId);
            if (doc?.FileData == null || doc.FileIV == null) return NotFound();

            var bytes = _encryption.Decrypt(doc.FileData, doc.FileIV);
            return File(bytes, doc.ContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }

        // ---------------- Citizen-uploaded documents (unencrypted, per ApplicationDocument.cs) ----------------
        // Same byte-signature detection as WaterConnectionController.Document,
        // but WITHOUT the ownership check — an officer reviewing the
        // application isn't its owner, so that check would wrongly block them.
        [HttpGet]
        public async Task<IActionResult> OfficerDocument(int id, bool download = false)
        {
            var officer = await GetCurrentWaterOfficerAsync();
            if (officer == null) return Forbid();

            var doc = await _waterContext.ApplicationDocuments.FirstOrDefaultAsync(d => d.DocumentId == id);
            if (doc == null || doc.FileContent == null || doc.FileContent.Length == 0)
                return NotFound();

            var (contentType, extension) = DetectFileType(doc.FileContent);
            var baseName = string.IsNullOrWhiteSpace(doc.DocumentPurpose) ? "document" : doc.DocumentPurpose;
            var fileName = $"{baseName}-{doc.DocumentId}{extension}";

            var disposition = download ? "attachment" : "inline";
            Response.Headers.Append("Content-Disposition", $"{disposition}; filename=\"{fileName}\"");

            return File(doc.FileContent, contentType);
        }

        // Identifies common document/image formats by their byte signature
        // ("magic numbers") — no filename/MIME column exists to read from.
        private static (string ContentType, string Extension) DetectFileType(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return ("application/pdf", ".pdf");

            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return ("image/jpeg", ".jpg");

            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                return ("image/png", ".png");

            if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38
                && (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
                return ("image/gif", ".gif");

            return ("application/octet-stream", "");
        }
    }
}
