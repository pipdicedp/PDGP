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

        // An application is only actionable while it is "Submitted". Once it has
        // been approved, rejected or returned to the applicant it is out of the
        // officers' hands, and every mutating action below refuses it — this is
        // what stops an approved NOC being pushed back into the workflow.
        private static string? ActionBlockedReason(FireClearanceApplication app) =>
            string.Equals(app.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                ? null
                : $"This application is '{app.Status}' and can no longer be actioned.";

        private static bool IsDecided(FireClearanceApplication app) =>
            string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(app.Status, "Rejected", StringComparison.OrdinalIgnoreCase);

        // The inspector is the Fire department officer mapped to the Inspection
        // stage. They can only submit a site-inspection report — not forward,
        // approve, reject or return the application.
        private static bool IsInspector(Officer officer) =>
            string.Equals(officer.Designation, "Inspection Officer", StringComparison.OrdinalIgnoreCase)
            && string.Equals(officer.Department, FireWorkflowConfig.Department, StringComparison.OrdinalIgnoreCase);

        // Final decisions belong to the Fire NOC Administrator (and, if one is
        // ever created, the GM). The inspector only reports back.
        private static bool CanTakeFinalDecision(Officer officer) =>
            IsAdministrator(officer)
            || string.Equals(officer.Designation, "GM", StringComparison.OrdinalIgnoreCase);

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
        /// Fire NOC Administrator: list applications that can be forwarded to
        /// the Inspector for site inspection. Forwarding is moved out of the
        /// "Fire NOC Inspection" card into this dedicated menu action.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ForwardToInspector()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return Forbid();

            var officer = await _context.Officers.FindAsync(officerId.Value);
            if (officer == null || !IsAdministrator(officer)) return Forbid();

            var engine = CreateEngine();
            var inspectionIndex = OfficerWorkflow.StageIndex("Inspection");

            var apps = await _context.FireClearanceApplications
                .Where(a => a.Status == null || a.Status != "Draft")
                .Where(a => !string.Equals(a.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(a.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            var items = apps
                .Where(a => OfficerWorkflow.StageIndex(a.CurrentStage) >= 0
                         && OfficerWorkflow.StageIndex(a.CurrentStage) < inspectionIndex)
                .Select(a => new OfficerQueueItem
                {
                    ApplicationId = a.Id,
                    ServiceType = "Fire NOC",
                    HistoryServiceType = FireWorkflowConfig.ServiceType,
                    DisplayName = a.Name ?? "-",
                    Contact = a.MobileNo ?? "-",
                    CurrentStage = a.CurrentStage,
                    Status = a.Status,
                    AssignedOfficerId = a.AssignedOfficerId,
                    SubmittedDate = a.SubmittedDate ?? a.ModifiedDate ?? DateTime.UtcNow,
                    ViewUrl = Url.Action("ForwardToInspectorForm", "FireOfficer", new { id = a.Id })!
                }).ToList();

            ViewBag.Designation = officer.Designation;
            ViewBag.Department = officer.Department;
            ViewBag.QueueTitle = "Forward to Inspector";
            ViewBag.QueueSubtitle = "Applications ready to be forwarded to the Fire NOC Inspector for site inspection.";
            return View("~/Views/Officer/SharedQueue.cshtml", items);
        }

        [HttpGet]
        public async Task<IActionResult> ForwardToInspectorForm(int id)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();
            if (!IsAdministrator(officer)) return Forbid();

            var engine = CreateEngine();
            var currentStage = app.CurrentStage;
            var inspectionIndex = OfficerWorkflow.StageIndex("Inspection");
            var currentIndex = OfficerWorkflow.StageIndex(currentStage);

            if (currentIndex >= inspectionIndex || string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(app.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(ViewApplication), new { id });

            var forwardableByStage = await engine.GetForwardableOfficersAsync(currentStage);
            var inspectors = forwardableByStage
                .Where(x => string.Equals(x.Stage, "Inspection", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Officer)
                .GroupBy(o => o.OfficerId)
                .Select(g => g.First())
                .ToList();

            ViewBag.Inspectors = inspectors;
            return View(app);
        }

        /// <summary>
        /// Administrator's Fire NOC queue: shows all fire applications so the
        /// admin can track pending, returned, approved and rejected ones in one
        /// place. Submitted apps can be approved directly from here.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AdminQueue()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return Forbid();

            var officer = await _context.Officers.FindAsync(officerId.Value);
            if (officer == null || !IsAdministrator(officer)) return Forbid();

            var apps = await _context.FireClearanceApplications
                // Drafts are the applicant's private work-in-progress; they only
                // enter the officer's world when the applicant submits them.
                .Where(a => a.Status == null || a.Status != "Draft")
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            var items = apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.Id,
                ServiceType = "Fire NOC",
                HistoryServiceType = FireWorkflowConfig.ServiceType,
                DisplayName = a.Name ?? "-",
                Contact = a.MobileNo ?? "-",
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                AssignedOfficerId = a.AssignedOfficerId,
                SubmittedDate = a.SubmittedDate ?? a.ModifiedDate ?? DateTime.UtcNow,
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
            var isInspector = IsInspector(officer);

            // Fire inspectors available for the administrator to forward to.
            // Only the Inspection stage is relevant here.
            var inspectors = forwardableByStage
                .Where(x => string.Equals(x.Stage, "Inspection", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Officer)
                .GroupBy(o => o.OfficerId)
                .Select(g => g.First())
                .ToList();

            var inspectionHistory = history.LastOrDefault(h =>
                string.Equals(h.ActionType, "Inspection", StringComparison.OrdinalIgnoreCase));
            var inspectionDocument = stageDocuments.LastOrDefault(d =>
                string.Equals(d.Stage, "Inspection", StringComparison.OrdinalIgnoreCase));

            var inspectionOfficerName = inspectionHistory?.FromOfficerId is int inspectorId
                && officerNames.TryGetValue(inspectorId, out var inspectorName)
                    ? inspectorName
                    : null;

            var inspectionIndex = OfficerWorkflow.StageIndex("Inspection");
            var currentIndex = OfficerWorkflow.StageIndex(currentStage);
            var canForwardToInspector = isAdmin
                && string.Equals(app.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                && !IsDecided(app)
                && currentIndex >= 0
                && currentIndex < inspectionIndex
                && inspectors.Count > 0;

            var canSubmitInspection = isInspector
                && string.Equals(app.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                && string.Equals(currentStage, "Inspection", StringComparison.OrdinalIgnoreCase)
                && (app.AssignedOfficerId == null || app.AssignedOfficerId == officer.OfficerId);

            var inspectionAssignedToOther = isInspector
                && string.Equals(app.Status, "Submitted", StringComparison.OrdinalIgnoreCase)
                && string.Equals(currentStage, "Inspection", StringComparison.OrdinalIgnoreCase)
                && app.AssignedOfficerId != null
                && app.AssignedOfficerId != officer.OfficerId;

            ViewBag.CurrentStage = currentStage;
            // An Administrator may approve at any stage, so for them the
            // "Final Approval" (Approve/Reject) panel is always available.
            ViewBag.IsFinalStage = engine.IsFinalStage(currentStage) || isAdmin;
            ViewBag.AdminOverride = isAdmin;
            // Approved / Rejected = final. The workflow panel then shows a
            // read-only decision card instead of action buttons.
            ViewBag.IsDecided = IsDecided(app);
            ViewBag.DecisionStatus = app.Status;
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

            // Fire-specific inspection card (administrator -> inspector -> administrator).
            ViewBag.Inspectors = inspectors;
            ViewBag.CanForwardToInspector = canForwardToInspector;
            ViewBag.IsInspector = isInspector;
            ViewBag.CanSubmitInspection = canSubmitInspection;
            ViewBag.InspectionAssignedToOther = inspectionAssignedToOther;
            ViewBag.InspectionHistory = inspectionHistory;
            ViewBag.InspectionDocument = inspectionDocument;
            ViewBag.InspectionOfficerName = inspectionOfficerName;
            // The inspector gets the dedicated Site Inspection form above, so the
            // shared panel's generic forward/upload/final buttons are hidden.
            ViewBag.HideForwardPanel = isInspector;
            ViewBag.HideFinalActions = isInspector;
            ViewBag.HideReturnToApplicant = isInspector;
            ViewBag.IsFireNocAdmin = isAdmin;
            ViewBag.CanForwardToInspectorFromPanel = canForwardToInspector;

            return View(new FireApplicationVm
            {
                Application = app,
                Preview = preview,
                DocumentSizes = await _fireService.GetDocumentSizesAsync(app.LoginId),
                DocumentTypes = await _fireService.GetDocumentTypesAsync(app.LoginId)
            });
        }

        // ---------------- Forward / Revert ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            if (IsInspector(officer))
                return BadRequest(new { error = "Inspectors submit the site-inspection report instead of forwarding the application." });

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

            var result = await CreateEngine().ForwardToOfficerAsync(id, officerId, targetStage, remarks, officer.OfficerId);
            if (!result.Success) return BadRequest(new { error = result.Error });

            TempData["OfficerActionMessage"] = result.Message;
            TempData["OfficerActionType"] = "forward";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// The Inspector's completion step: records the site-inspection remarks,
        /// stores the signed inspection report as the Inspection-stage document,
        /// and hands the application back to the Fire NOC Administrator for the
        /// final decision. The inspection itself is a Fire-NOC-only action, so it
        /// lives here rather than in the shared workflow engine.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInspectionReport(int id, string? remarks, IFormFile? report)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            if (!IsInspector(officer)) return Forbid();

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

            if (!string.Equals(app.CurrentStage, "Inspection", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "The application is not at the inspection stage." });

            if (app.AssignedOfficerId != null && app.AssignedOfficerId != officer.OfficerId)
                return BadRequest(new { error = "This inspection is assigned to another officer." });

            remarks = remarks?.Trim();
            if (string.IsNullOrWhiteSpace(remarks))
                return BadRequest(new { error = "Please enter the inspection remarks." });

            if (remarks.Length > 1000)
                return BadRequest(new { error = "Inspection remarks cannot exceed 1000 characters." });

            if (report == null || report.Length == 0)
                return BadRequest(new { error = "Please attach the inspection report." });

            const long maxReportBytes = 5 * 1024 * 1024;
            if (report.Length > maxReportBytes)
                return BadRequest(new { error = "The inspection report must be 5 MB or smaller." });

            using var reportStream = new MemoryStream();
            await report.CopyToAsync(reportStream);
            var reportBytes = reportStream.ToArray();

            // Never trust the browser-supplied content type: store/serve only
            // what the file's own signature identifies as PDF, JPEG or PNG.
            var detected = EBDocumentHelper.DetectFile(reportBytes);
            if (detected == null)
                return BadRequest(new { error = "The inspection report must be a PDF, JPG or PNG file." });

            // Send the completed inspection back to the administrator who
            // forwarded it to this inspector; fall back to any active Fire NOC
            // Administrator if the previous officer wasn't one.
            var forwardToInspector = await _context.WorkflowHistories
                .AsNoTracking()
                .Where(h => h.ServiceType == FireWorkflowConfig.ServiceType
                    && h.ApplicationId == id
                    && h.ActionType == "Forward"
                    && h.ToStage == "Inspection")
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefaultAsync();

            Officer? recipient = null;
            if (forwardToInspector?.FromOfficerId != null)
            {
                var previousOfficer = await _context.Officers.FindAsync(forwardToInspector.FromOfficerId.Value);
                if (previousOfficer != null && IsAdministrator(previousOfficer) && !previousOfficer.IsLocked)
                    recipient = previousOfficer;
            }

            recipient ??= await _context.Officers
                .Where(o => o.Department == FireWorkflowConfig.Department
                    && o.Designation == "Admin"
                    && !o.IsLocked)
                .OrderBy(o => o.OfficerId)
                .FirstOrDefaultAsync();

            if (recipient == null)
                return BadRequest(new { error = "No active Fire NOC Administrator is available to receive the inspection report." });

            var fileName = Path.GetFileName(report.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "Inspection-Report" + detected.Value.Extension;
            if (fileName.Length > 255)
                fileName = fileName[..255];

            // Keep the stage document, the application's new stage/assignee and
            // the audit row together: either the inspector's report is fully
            // recorded, or nothing changes.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var (cipherBytes, iv) = _encryption.Encrypt(reportBytes);
            await CreateEngine().UploadStageDocumentAsync(
                id, "Inspection", officer.OfficerId,
                fileName, detected.Value.ContentType, cipherBytes, iv);

            app.CurrentStage = "Approval";
            app.AssignedOfficerId = recipient.OfficerId;
            app.OfficerRemarks = remarks;
            app.ModifiedDate = DateTime.UtcNow;

            _context.WorkflowHistories.Add(new WorkflowHistory
            {
                ServiceType = FireWorkflowConfig.ServiceType,
                ApplicationId = id,
                FromOfficerId = officer.OfficerId,
                FromStage = "Inspection",
                ToOfficerId = recipient.OfficerId,
                ToStage = "Approval",
                ActionType = "Inspection",
                Remarks = remarks,
                ActionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["OfficerActionMessage"] =
                $"Inspection report submitted to {recipient.FullName}. The application is now with the administrator for the final decision.";
            TempData["OfficerActionType"] = "inspection";
            return RedirectToAction(nameof(ViewApplication), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertToPreviousOfficer(int id, string remarks)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            if (IsInspector(officer))
                return BadRequest(new { error = "Inspectors submit the site-inspection report instead of reverting the application." });

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

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

            if (!CanTakeFinalDecision(officer)) return Forbid();

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

            var result = await CreateEngine().ApproveApplicationAsync(
                id, remarks,
                allowAnyStage: IsAdministrator(officer),
                terminalStage: OfficerWorkflow.ApprovedStage,
                actingOfficerId: officer.OfficerId,
                recordHistory: true);
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

            if (!CanTakeFinalDecision(officer)) return Forbid();

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

            var result = await CreateEngine().RejectApplicationAsync(
                id, remarks, IsAdministrator(officer),
                terminalStage: OfficerWorkflow.RejectedStage,
                actingOfficerId: officer.OfficerId,
                recordHistory: true);
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

            if (!CanTakeFinalDecision(officer)) return Forbid();

            var blocked = ActionBlockedReason(app);
            if (blocked != null) return BadRequest(new { error = blocked });

            var result = await CreateEngine().ReturnToApplicantAsync(id, remarks, officer.OfficerId, recordHistory: true);
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

            if (IsInspector(officer))
                return BadRequest(new { error = "Use the Site Inspection form to submit your report and remarks." });

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

            // The content type comes from the file's own signature, never from the
            // upload header, so an "image" can never be served back as HTML. Images
            // render inline in the officer's browser; PDFs open in the new tab.
            var type = EBDocumentHelper.DetectFile(bytes)?.ContentType ?? "application/octet-stream";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(bytes, type);
        }

        /// <summary>Same document as above, but as a download with a sensible file name.</summary>
        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id, string column)
        {
            var (officer, app) = await GetAuthorizedAsync(id);
            if (officer == null || app == null) return Forbid();

            var def = FireFormMetadata.DocumentByColumn(column);
            if (def == null) return NotFound();

            var bytes = await _fireService.GetDocumentAsync(app.LoginId, def.Column);
            if (bytes == null) return NotFound();

            var detected = EBDocumentHelper.DetectFile(bytes);
            var fileName = $"Fire-{app.ApplicationCode?.Replace('/', '-') ?? app.Id.ToString()}-{def.Column}{detected?.Extension ?? ""}";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(bytes, detected?.ContentType ?? "application/octet-stream", fileName);
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
