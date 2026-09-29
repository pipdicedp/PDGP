using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Services
{
    /// <summary>
    /// Return type for every mutating action below — mirrors what
    /// OfficerController used to build directly (BadRequest / TempData
    /// message), so a thin controller just forwards Success/Error/Message
    /// into a BadRequest / TempData without re-deriving anything.
    /// </summary>
    public class WorkflowActionResult
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Message { get; init; }

        public static WorkflowActionResult Ok(string message) => new() { Success = true, Message = message };
        public static WorkflowActionResult Fail(string error) => new() { Success = false, Error = error };
    }

    /// <summary>
    /// The shared Officer Workflow engine — Forward / Revert / Approve /
    /// Reject / Return-to-Applicant / Payment, written ONCE and reused by
    /// every service (Water first, then Electricity, Transport, ...).
    ///
    /// TradeLicence keeps its own hand-written OfficerController — it is
    /// NOT migrated to this engine, so nothing about its behavior changes.
    /// This class is for every service built AFTER TradeLicence.
    ///
    /// TApp is that service's application entity (e.g. WaterConnectionApplication),
    /// which must implement IWorkflowApplication. appContext is THAT service's
    /// own DbContext (the one TApp is registered in) — sharedContext is always
    /// TradeLicence.Data.ApplicationDbContext, since Officers and the generic
    /// WorkflowHistories/WorkflowSupportingDocuments/WorkflowPayments tables
    /// live there regardless of which service is calling.
    /// </summary>
    public class WorkflowEngineService<TApp> where TApp : class, TradeLicence.Models.IWorkflowApplication
    {
        private readonly DbContext _appContext;
        private readonly ApplicationDbContext _sharedContext;
        private readonly string _serviceType;

        public WorkflowEngineService(DbContext appContext, ApplicationDbContext sharedContext, string serviceType)
        {
            _appContext = appContext;
            _sharedContext = sharedContext;
            _serviceType = serviceType;
        }

        private DbSet<TApp> Applications => _appContext.Set<TApp>();

        // ---------------- Reads ----------------

        // Same visibility rule as OfficerController.Index: unassigned at one
        // of this officer's stages, OR specifically assigned to them by name.
        public async Task<List<TApp>> GetOfficerQueueAsync(string? designation, int officerId)
        {
            var myStages = TradeLicence.Models.OfficerWorkflow.StagesForDesignation(designation);

            return await Applications
                .Where(a => a.Status == "Submitted" &&
                    (
                        (a.AssignedOfficerId == null && myStages.Contains(a.CurrentStage))
                        || a.AssignedOfficerId == officerId
                    ))
                .OrderByDescending(a => a.Id)
                .ToListAsync();
        }

        public async Task<TApp?> GetApplicationAsync(int id) => await Applications.FindAsync(id);

        // One entry per (stage, officer) for every stage AFTER the current
        // one — officers can only forward forward, never skip backward.
        public async Task<List<(string Stage, TradeLicence.Models.Officer Officer)>> GetForwardableOfficersAsync(string currentStage)
        {
            var currentIndex = TradeLicence.Models.OfficerWorkflow.StageIndex(currentStage);
            var laterStages = TradeLicence.Models.OfficerWorkflow.Stages.Skip(currentIndex + 1).ToList();

            var result = new List<(string, TradeLicence.Models.Officer)>();
            foreach (var stage in laterStages)
            {
                var designation = TradeLicence.Models.OfficerWorkflow.StageToDesignation[stage];
                var officers = await _sharedContext.Officers
                    .Where(o => o.Designation == designation && !o.IsLocked)
                    .OrderBy(o => o.FullName)
                    .ToListAsync();

                result.AddRange(officers.Select(o => (stage, o)));
            }
            return result;
        }

        public bool IsFinalStage(string currentStage) =>
            TradeLicence.Models.OfficerWorkflow.StageIndex(currentStage) == TradeLicence.Models.OfficerWorkflow.Stages.Length - 1;

        // Full audit trail (oldest first) + officer display names + the
        // "revert to" target (most recent Forward that landed it here).
        public async Task<(List<WorkflowHistory> History, Dictionary<int, string> OfficerNames, WorkflowHistory? RevertTarget)>
            GetHistoryContextAsync(int applicationId, string currentStage)
        {
            var history = await _sharedContext.WorkflowHistories
                .Where(h => h.ServiceType == _serviceType && h.ApplicationId == applicationId)
                .OrderBy(h => h.ActionDate)
                .ToListAsync();

            var officerIds = history
                .SelectMany(h => new[] { h.FromOfficerId, h.ToOfficerId })
                .Where(x => x.HasValue).Select(x => x!.Value)
                .Distinct().ToList();
            var officerNames = await _sharedContext.Officers
                .Where(o => officerIds.Contains(o.OfficerId))
                .ToDictionaryAsync(o => o.OfficerId, o => o.FullName ?? o.Username);

            var revertTarget = history
                .Where(h => h.ActionType == "Forward" && h.ToStage == currentStage)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            return (history, officerNames, revertTarget);
        }

        // One row per stage at most, oldest-uploaded first + uploader names.
        public async Task<(List<WorkflowSupportingDocument> Docs, Dictionary<int, string> OfficerNames)>GetStageDocumentsAsync(int applicationId)
        {
            var docs = await _sharedContext.WorkflowSupportingDocuments
                .Where(d => d.ServiceType == _serviceType && d.ApplicationId == applicationId)
                .OrderBy(d => d.UploadedDate)
                .ToListAsync();

            var officerIds = docs.Select(d => d.UploadedByOfficerId).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var officerNames = await _sharedContext.Officers
                .Where(o => officerIds.Contains(o.OfficerId))
                .ToDictionaryAsync(o => o.OfficerId, o => o.FullName ?? o.Username);

            return (docs, officerNames);
        }

        public async Task<WorkflowPayment?> GetPaymentAsync(int applicationId) =>
            await _sharedContext.WorkflowPayments
                .FirstOrDefaultAsync(p => p.ServiceType == _serviceType && p.ApplicationId == applicationId);

        public async Task<WorkflowSupportingDocument?> GetStageDocumentByIdAsync(int docId) =>
            await _sharedContext.WorkflowSupportingDocuments.FindAsync(docId);

        // ---------------- Mutations ----------------

        public async Task<WorkflowActionResult> ForwardToOfficerAsync(int applicationId, int officerId, string targetStage, string? remarks, int actingOfficerId)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            var officer = await _sharedContext.Officers.FindAsync(officerId);
            if (officer == null) return WorkflowActionResult.Fail("Please choose a valid officer to forward to.");

            var expectedDesignation = TradeLicence.Models.OfficerWorkflow.StageToDesignation.GetValueOrDefault(targetStage);
            if (expectedDesignation == null || officer.Designation != expectedDesignation)
                return WorkflowActionResult.Fail("That officer doesn't match the selected stage.");

            var currentIndex = TradeLicence.Models.OfficerWorkflow.StageIndex(application.CurrentStage);
            var targetIndex = TradeLicence.Models.OfficerWorkflow.StageIndex(targetStage);
            if (targetIndex <= currentIndex)
                return WorkflowActionResult.Fail("You can only forward to a later stage, not the current or an earlier one.");

            if (application.CurrentStage == "Inspection" && application.PaymentStatus != "Paid")
                return WorkflowActionResult.Fail("This application can't be forwarded until the payment has been received.");

            var fromStage = application.CurrentStage;
            var fromOfficerId = application.AssignedOfficerId;

            application.AssignedOfficerId = officer.OfficerId;
            application.CurrentStage = targetStage;
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            _sharedContext.WorkflowHistories.Add(new WorkflowHistory
            {
                ServiceType = _serviceType,
                ApplicationId = applicationId,
                FromOfficerId = fromOfficerId ?? actingOfficerId,
                FromStage = fromStage,
                ToOfficerId = officer.OfficerId,
                ToStage = targetStage,
                ActionType = "Forward",
                Remarks = remarks,
                ActionDate = DateTime.UtcNow
            });

            await _appContext.SaveChangesAsync();
            await _sharedContext.SaveChangesAsync();

            return WorkflowActionResult.Ok($"Application successfully forwarded to {officer.FullName} ({targetStage}).");
        }

        public async Task<WorkflowActionResult> RevertToPreviousOfficerAsync(int applicationId, string? remarks, int actingOfficerId)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            if (application.PaymentStatus == "Pending")
                return WorkflowActionResult.Fail("This application can't be reverted while a payment is pending with the applicant.");

            var revertTarget = await _sharedContext.WorkflowHistories
                .Where(h => h.ServiceType == _serviceType && h.ApplicationId == applicationId
                    && h.ActionType == "Forward" && h.ToStage == application.CurrentStage)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefaultAsync();

            if (revertTarget == null || revertTarget.FromOfficerId == null)
                return WorkflowActionResult.Fail("There's no earlier officer to revert this application to.");

            var previousOfficer = await _sharedContext.Officers.FindAsync(revertTarget.FromOfficerId.Value);
            if (previousOfficer == null) return WorkflowActionResult.Fail("The previous officer's account could not be found.");

            var fromStage = application.CurrentStage;
            var fromOfficerId = application.AssignedOfficerId;

            application.AssignedOfficerId = previousOfficer.OfficerId;
            application.CurrentStage = revertTarget.FromStage ?? TradeLicence.Models.OfficerWorkflow.Stages[0];
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            _sharedContext.WorkflowHistories.Add(new WorkflowHistory
            {
                ServiceType = _serviceType,
                ApplicationId = applicationId,
                FromOfficerId = fromOfficerId ?? actingOfficerId,
                FromStage = fromStage,
                ToOfficerId = previousOfficer.OfficerId,
                ToStage = revertTarget.FromStage,
                ActionType = "Revert",
                Remarks = remarks,
                ActionDate = DateTime.UtcNow
            });

            await _appContext.SaveChangesAsync();
            await _sharedContext.SaveChangesAsync();

            return WorkflowActionResult.Ok($"Application reverted to {previousOfficer.FullName} ({revertTarget.FromStage}).");
        }

        public async Task<WorkflowActionResult> ApproveApplicationAsync(int applicationId, string? remarks)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            var lastStage = TradeLicence.Models.OfficerWorkflow.Stages[^1];
            if (application.CurrentStage != lastStage)
                return WorkflowActionResult.Fail("This application hasn't reached the final approval stage yet.");

            application.Status = "Approved";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _appContext.SaveChangesAsync();
            return WorkflowActionResult.Ok("Application approved successfully.");
        }

        public async Task<WorkflowActionResult> RejectApplicationAsync(int applicationId, string? remarks)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            var lastStage = TradeLicence.Models.OfficerWorkflow.Stages[^1];
            if (application.CurrentStage != lastStage)
                return WorkflowActionResult.Fail("This application hasn't reached the final approval stage yet.");

            application.Status = "Rejected";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _appContext.SaveChangesAsync();
            return WorkflowActionResult.Ok("Application rejected.");
        }

        public async Task<WorkflowActionResult> ReturnToApplicantAsync(int applicationId, string? remarks)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            if (application.PaymentStatus == "Pending")
                return WorkflowActionResult.Fail("This application can't be returned to the applicant while a payment is pending.");

            application.Status = "ReturnedToApplicant";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _appContext.SaveChangesAsync();
            return WorkflowActionResult.Ok("Application returned to the applicant successfully.");
        }

        // Officer-initiated — fixed amounts are the caller's decision (each
        // service may charge differently); this just persists whatever the
        // controller passes in.
        public async Task<WorkflowActionResult> SendPaymentRequestAsync(int applicationId, decimal paymentAmount, decimal extraCharge)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            if (application.CurrentStage != "Inspection")
                return WorkflowActionResult.Fail("Payment can only be requested at the Inspection stage.");

            var payment = await _sharedContext.WorkflowPayments
                .FirstOrDefaultAsync(p => p.ServiceType == _serviceType && p.ApplicationId == applicationId);

            if (payment == null)
            {
                payment = new WorkflowPayment { ServiceType = _serviceType, ApplicationId = applicationId };
                _sharedContext.WorkflowPayments.Add(payment);
            }

            payment.PaymentAmount = paymentAmount;
            payment.ExtraCharge = extraCharge;
            payment.TotalPaymentAmount = paymentAmount + extraCharge;
            payment.PaymentStatus = "Pending";
            payment.PaymentRequestedDate = DateTime.UtcNow;
            payment.PaymentCompletedDate = null;

            application.PaymentStatus = "Pending";
            application.ModifiedDate = DateTime.UtcNow;

            await _appContext.SaveChangesAsync();
            await _sharedContext.SaveChangesAsync();

            return WorkflowActionResult.Ok("Payment request sent to the applicant.");
        }

        // Citizen-initiated (temporary test action, same as TradeLicence's
        // PayNow, until a real payment gateway replaces this).
        public async Task<WorkflowActionResult> PayNowAsync(int applicationId, int? requestingUserId)
        {
            var application = await Applications.FindAsync(applicationId);
            if (application == null) return WorkflowActionResult.Fail("Application not found.");

            if (application.UserId != requestingUserId) return WorkflowActionResult.Fail("You don't have permission to pay for this application.");

            var payment = await _sharedContext.WorkflowPayments
                .FirstOrDefaultAsync(p => p.ServiceType == _serviceType && p.ApplicationId == applicationId);

            if (payment == null || payment.PaymentStatus != "Pending" || application.PaymentStatus != "Pending")
                return WorkflowActionResult.Fail("There's no pending payment for this application.");

            payment.PaymentStatus = "Paid";
            payment.PaymentCompletedDate = DateTime.UtcNow;

            application.PaymentStatus = "Paid";
            application.ModifiedDate = DateTime.UtcNow;

            await _appContext.SaveChangesAsync();
            await _sharedContext.SaveChangesAsync();

            return WorkflowActionResult.Ok("Payment successful! Your application has been sent back to the officer for forwarding.");
        }

        // Encryption stays the controller's concern (IFileEncryptionService
        // is already DI-registered and service-agnostic) — this just
        // upserts the row with whatever ciphertext/IV it's handed.
        public async Task<WorkflowSupportingDocument> UploadStageDocumentAsync(
            int applicationId, string currentStage, int officerId,
            string fileName, string contentType, byte[] cipherData, byte[] iv)
        {
            var existing = await _sharedContext.WorkflowSupportingDocuments
                .FirstOrDefaultAsync(d => d.ServiceType == _serviceType && d.ApplicationId == applicationId && d.Stage == currentStage);

            if (existing == null)
            {
                existing = new WorkflowSupportingDocument { ServiceType = _serviceType, ApplicationId = applicationId, Stage = currentStage };
                _sharedContext.WorkflowSupportingDocuments.Add(existing);
            }

            existing.UploadedByOfficerId = officerId;
            existing.FileName = fileName;
            existing.ContentType = contentType;
            existing.FileData = cipherData;
            existing.FileIV = iv;
            existing.UploadedDate = DateTime.UtcNow;

            await _sharedContext.SaveChangesAsync();
            return existing;
        }
    }
}
