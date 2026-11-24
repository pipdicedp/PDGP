using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Officer side for Form 20A / 21A. Thin on purpose - Forward / Revert / Approve / Reject / Return /
    /// Payment all live in the shared WorkflowEngineService (same engine Water and Electricity use), so the
    /// DEO -> Manager -> Inspection Officer -> GM flow is identical to the other services.
    ///
    /// Beyond what Water does, every action also checks that the acting officer belongs to this form's
    /// department, the application is still "Submitted", and the officer is the one the application is
    /// currently with (right designation for the stage, and the assigned officer if one is assigned).
    /// </summary>
    [Authorize(Roles = "Officer")]
    public abstract class DrugLicenceOfficerControllerBase<TApp> : Controller where TApp : DrugLicenceApplicationBase, new()
    {
        private const string ViewName = "~/Views/DrugLicence/OfficerViewApplication.cshtml";

        protected readonly DrugsApplicationDbContext Ctx;
        protected readonly ApplicationDbContext Shared;
        protected readonly DrugFormDefinition Form;
        protected readonly DrugLicenceFormService<TApp> Svc;
        protected readonly WorkflowEngineService<TApp> Engine;
        protected readonly IFileEncryptionService Encryption;

        protected DrugLicenceOfficerControllerBase(DrugFormDefinition form, DrugsApplicationDbContext ctx,
            ApplicationDbContext shared, IFileEncryptionService encryption)
        {
            Form = form;
            Ctx = ctx;
            Shared = shared;
            Encryption = encryption;
            Svc = new DrugLicenceFormService<TApp>(ctx, encryption, form);
            Engine = new WorkflowEngineService<TApp>(ctx, shared, form.ServiceType);
        }

        // ---------------- helpers ----------------

        private async Task<Officer?> GetOfficerAsync()
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var officerId)) return null;
            var officer = await Shared.Officers.FindAsync(officerId);
            if (officer == null || officer.IsLocked) return null;
            return string.Equals(officer.Department, Form.Department, StringComparison.OrdinalIgnoreCase) ? officer : null;
        }

        // Returns an error result if this officer may not act on the application right now.
        private async Task<(Officer? Officer, IActionResult? Denied)> AuthorizeActionAsync(int applicationId)
        {
            var officer = await GetOfficerAsync();
            if (officer == null) return (null, Forbid());

            var app = await Ctx.Set<TApp>().AsNoTracking().FirstOrDefaultAsync(a => a.ApplicationId == applicationId);
            if (app == null) return (null, NotFound());

            if (app.Status != "Submitted")
                return (null, BadRequest(new { error = "This application is no longer awaiting officer action." }));

            var expectedDesignation = OfficerWorkflow.StageToDesignation.GetValueOrDefault(app.CurrentStage);
            var rightDesignation = expectedDesignation != null && officer.Designation == expectedDesignation;
            var rightPerson = app.AssignedOfficerId == null || app.AssignedOfficerId == officer.OfficerId;
            if (!rightDesignation || !rightPerson)
                return (null, BadRequest(new { error = "This application is not with you at the moment." }));

            return (officer, null);
        }

        private IActionResult Done(WorkflowActionResult result, string type, IActionResult onSuccess)
        {
            if (!result.Success) return BadRequest(new { error = result.Error });
            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = type;
            return onSuccess;
        }

        // ---------------- queue ----------------

        // The queue itself is built in OfficerController.Index (routed by Department); this keeps "Index"
        // resolvable for _OfficerWorkflowPanel's "Back to Dashboard" link.
        [HttpGet]
        public IActionResult Index() => RedirectToAction("Index", "Officer");

        // ---------------- application detail + workflow panel ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var officer = await GetOfficerAsync();
            if (officer == null) return Forbid();

            var app = await Ctx.Set<TApp>().FirstOrDefaultAsync(a => a.ApplicationId == id);
            if (app == null) return NotFound();

            var currentStage = app.CurrentStage;
            var (history, officerNames, revertTarget) = await Engine.GetHistoryContextAsync(id, currentStage);
            var (stageDocuments, stageDocOfficerNames) = await Engine.GetStageDocumentsAsync(id);

            ViewBag.Form = Form;
            ViewBag.Pharmacists = await Svc.GetPharmacistsAsync(id);
            ViewBag.UploadedDocuments = await Svc.GetDocumentInfosAsync(id);

            // ---- what _OfficerWorkflowPanel expects ----
            ViewBag.CurrentStage = currentStage;
            ViewBag.IsFinalStage = Engine.IsFinalStage(currentStage);
            ViewBag.ForwardableByStage = await Engine.GetForwardableOfficersAsync(currentStage);
            ViewBag.WorkflowHistory = history;
            ViewBag.OfficerNames = officerNames;
            ViewBag.RevertTarget = revertTarget;
            ViewBag.StageDocuments = stageDocuments;
            ViewBag.StageDocumentOfficerNames = stageDocOfficerNames;
            ViewBag.CanUploadStageDocument = currentStage == "Verification" || currentStage == "Inspection";
            ViewBag.ExistingStageDocumentForCurrentStage = stageDocuments.FirstOrDefault(d => d.Stage == currentStage);
            ViewBag.Payment = await Engine.GetPaymentAsync(id);
            ViewBag.SubmittedDate = app.SubmittedDate ?? app.CreatedDate;

            return View(ViewName, app);
        }

        // ---------------- forward / revert ----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string? remarks)
        {
            var (officer, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.ForwardToOfficerAsync(id, officerId, targetStage, remarks, officer!.OfficerId);
            return Done(result, "forward", RedirectToAction(nameof(Index)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertToPreviousOfficer(int id, string? remarks)
        {
            var (officer, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.RevertToPreviousOfficerAsync(id, remarks, officer!.OfficerId);
            return Done(result, "revert", RedirectToAction(nameof(Index)));
        }

        // ---------------- final decision / return ----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var (_, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.ApproveApplicationAsync(id, remarks);
            return Done(result, "approve", RedirectToAction(nameof(Index)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int id, string? remarks)
        {
            var (_, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.RejectApplicationAsync(id, remarks);
            return Done(result, "reject", RedirectToAction(nameof(Index)));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToApplicant(int id, string? remarks)
        {
            var (officer, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.ReturnToApplicantAsync(id, remarks, officer!.OfficerId);
            return Done(result, "return", RedirectToAction(nameof(Index)));
        }

        // ---------------- inspection-stage payment ----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendPaymentRequest(int id)
        {
            var (_, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;
            var result = await Engine.SendPaymentRequestAsync(id, DrugLicenceConfig.PaymentAmount, DrugLicenceConfig.ExtraCharge);
            return Done(result, "payment", RedirectToAction(nameof(ViewApplication), new { id }));
        }

        // ---------------- officer's own stage document (Verification / Inspection) ----------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadStageDocument(int id, IFormFile file)
        {
            var (officer, denied) = await AuthorizeActionAsync(id);
            if (denied != null) return denied;

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            var app = await Ctx.Set<TApp>().AsNoTracking().FirstAsync(a => a.ApplicationId == id);
            if (app.CurrentStage != "Verification" && app.CurrentStage != "Inspection")
                return BadRequest(new { error = "A supporting document can only be uploaded at the Verification or Inspection stage." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var (cipher, iv) = Encryption.Encrypt(ms.ToArray());

            var doc = await Engine.UploadStageDocumentAsync(id, app.CurrentStage, officer!.OfficerId,
                file.FileName, file.ContentType, cipher, iv);

            return Json(new
            {
                docId = doc.WorkflowSupportingDocumentId,
                fileName = doc.FileName,
                contentType = doc.ContentType,
                uploadedDate = doc.UploadedDate.ToLocalTime().ToString("dd-MM-yyyy hh:mm tt")
            });
        }

        // Stage documents are shared in one table by ServiceType, so check the document belongs to THIS form's service.
        private async Task<WorkflowSupportingDocument?> GetOwnStageDocumentAsync(int docId)
        {
            var doc = await Engine.GetStageDocumentByIdAsync(docId);
            return doc != null && doc.ServiceType == Form.ServiceType && doc.FileData != null && doc.FileIV != null ? doc : null;
        }

        [HttpGet]
        public async Task<IActionResult> PreviewStageDocument(int docId)
        {
            if (await GetOfficerAsync() == null) return Forbid();
            var doc = await GetOwnStageDocumentAsync(docId);
            if (doc == null) return NotFound();
            return File(Encryption.Decrypt(doc.FileData!, doc.FileIV!), doc.ContentType ?? "application/octet-stream");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStageDocument(int docId)
        {
            if (await GetOfficerAsync() == null) return Forbid();
            var doc = await GetOwnStageDocumentAsync(docId);
            if (doc == null) return NotFound();
            return File(Encryption.Decrypt(doc.FileData!, doc.FileIV!), doc.ContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }

        // ---------------- citizen-uploaded PDFs, for the reviewing officer ----------------

        [HttpGet]
        public async Task<IActionResult> OfficerDocument(int docId, bool download = false)
        {
            if (await GetOfficerAsync() == null) return Forbid();

            var doc = await Svc.GetDocumentBytesAsync(docId);
            if (doc == null) return NotFound();

            var disposition = download ? "attachment" : "inline";
            Response.Headers.Append("Content-Disposition", $"{disposition}; filename=\"{doc.Value.FileName.Replace("\"", "")}\"");
            return File(doc.Value.Bytes, "application/pdf");
        }
    }
}
