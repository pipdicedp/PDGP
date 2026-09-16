using System;

namespace TradeLicence.Models
{
    /// <summary>
    /// Implemented by every service's application entity (TradeLicenceApplication,
    /// WaterConnectionApplication, and future services) so WorkflowEngineService
    /// can Forward/Revert/Approve/Reject/ReturnToApplicant/RequestPayment against
    /// any of them without service-specific code.
    ///
    /// Deliberately minimal — only the fields the workflow itself touches.
    /// Everything else (service-specific columns like meter number, trade
    /// name, etc.) stays out of this contract and lives only on the concrete
    /// entity, read via the service's own preview logic.
    /// </summary>
    public interface IWorkflowApplication
    {
        int ApplicationId { get; }
        int? UserId { get; set; }

        // "Draft" / "Submitted" / "ReturnedToApplicant" / "Approved" / "Rejected"
        string Status { get; set; }

        // One of OfficerWorkflow.Stages — "Initial Scrutiny" by default.
        string CurrentStage { get; set; }

        // Null = unassigned, sitting at CurrentStage's designation for anyone to pick up.
        int? AssignedOfficerId { get; set; }

        // "NotRequested" / "Pending" / "Paid" — services that don't use the
        // payment step can just leave this at "NotRequested" forever.
        string PaymentStatus { get; set; }

        string? OfficerRemarks { get; set; }
        DateTime? ModifiedDate { get; set; }
    }
}
