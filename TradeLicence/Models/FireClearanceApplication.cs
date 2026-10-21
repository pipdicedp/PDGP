using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models
{
    /// <summary>
    /// One Fire NOC / Fire Safety Clearance application (legacy table
    /// "fireclearance", deptcode = 9, serviceid = 1017).
    ///
    /// Unlike the CAF — where the applicant's data lives in one table and the
    /// department's workflow row in another — a fire application is a single
    /// row: the applicant fills in the form (steps 1-4) and the officer then
    /// runs the shared 4-stage workflow on that same row, so it implements
    /// IWorkflowApplication and WorkflowEngineService operates on it directly.
    ///
    /// Only the key / workflow / summary columns are mapped here. The remaining
    /// applicant columns are read and written by name at runtime through
    /// FireFormService + FireFormMetadata (same metadata-driven approach as the
    /// CAF), so they deliberately do not appear as properties.
    /// </summary>
    public class FireClearanceApplication : IWorkflowApplication
    {
        public int Id { get; set; }

        // IWorkflowApplication needs "ApplicationId" — read-only passthrough of Id.
        [NotMapped]
        public int ApplicationId => Id;

        /// <summary>The citizen's own UserId (AccountController writes it under ClaimTypes.NameIdentifier).</summary>
        public long LoginId { get; set; }

        public int? UserId { get; set; }

        /// <summary>Human-readable reference, e.g. FIRE/2026/00042, stamped on submit.</summary>
        [StringLength(30)]
        public string? ApplicationCode { get; set; }

        // ---- Denormalised applicant summary, for the officer queue ----

        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(20)]
        public string? MobileNo { get; set; }

        public short? DistCode { get; set; }

        // ---- Service stamps ----

        public int? ServiceId { get; set; }

        [StringLength(150)]
        public string? ServiceDes { get; set; }

        public short? DeptCode { get; set; }

        // ---- IWorkflowApplication ----

        [StringLength(50)]
        public string Status { get; set; } = "Draft";

        [StringLength(50)]
        public string CurrentStage { get; set; } = "Initial Scrutiny";

        public int? AssignedOfficerId { get; set; }

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "NotRequested";

        [StringLength(1000)]
        public string? OfficerRemarks { get; set; }

        public DateTime? ModifiedDate { get; set; }

        /// <summary>When the applicant submitted (the officer queue orders on this).</summary>
        public DateTime? SubmittedDate { get; set; }
    }
}
