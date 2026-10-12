using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models
{
    /// <summary>
    /// One CAF forwarded to one department. This is the unit the receiving department's
    /// 4-category officer workflow (Initial Scrutiny -> Verification -> Inspection -> Approval)
    /// runs on, so it implements IWorkflowApplication and the shared WorkflowEngineService
    /// works on it unchanged.
    ///
    /// A CAF only gets a row here once an Industry officer explicitly forwards it to that
    /// department — submitting a CAF does NOT create any rows. One row per (LoginId, Department).
    /// </summary>
    public class CafDepartmentApplication : IWorkflowApplication
    {
        public int Id { get; set; }

        // IWorkflowApplication needs "ApplicationId" — read-only passthrough of Id.
        [NotMapped]
        public int ApplicationId => Id;

        /// <summary>caf_basic_details.loginid — the applicant's CAF.</summary>
        public long LoginId { get; set; }

        /// <summary>The citizen's UserId (same value as LoginId; kept for IWorkflowApplication).</summary>
        public int? UserId { get; set; }

        [Required, StringLength(150)]
        public string Department { get; set; } = string.Empty;

        public int ForwardedByOfficerId { get; set; }
        public DateTime ForwardedDate { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string? ForwardRemarks { get; set; }

        // ---- IWorkflowApplication ----
        [StringLength(50)] public string Status { get; set; } = "Submitted";
        [StringLength(50)] public string CurrentStage { get; set; } = "Initial Scrutiny";
        public int? AssignedOfficerId { get; set; }
        [StringLength(20)] public string PaymentStatus { get; set; } = "NotRequested";
        public string? OfficerRemarks { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
