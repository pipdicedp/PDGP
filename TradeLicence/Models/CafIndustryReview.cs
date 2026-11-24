using System;
using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// The Industry department's own review state for a submitted CAF — one row per CAF (LoginId).
    /// It exists so the applicant can see where their CAF stands ("viewed", "forwarded",
    /// "approved"...), and so the Industry officer's Approve / Reject / Return decisions have
    /// somewhere to live. caf_basic_details.statuss stays what it was (P = editable, S = submitted
    /// and locked); this table never replaces it.
    ///
    /// Status: Pending (no officer has opened it since it was (re)submitted) -> Viewed ->
    /// Forwarded -> Approved / Rejected, or Returned (sent back to the applicant for correction).
    /// A row is created the first time an Industry officer opens the CAF.
    /// </summary>
    public class CafIndustryReview
    {
        public int Id { get; set; }

        /// <summary>caf_basic_details.loginid — one review row per CAF.</summary>
        public long LoginId { get; set; }

        [Required, StringLength(20)]
        public string Status { get; set; } = "Viewed";

        public DateTime? FirstViewedDate { get; set; }
        public int? FirstViewedByOfficerId { get; set; }

        public DateTime? LastActionDate { get; set; }
        public int? LastActionByOfficerId { get; set; }

        /// <summary>Remarks given with the latest Approve / Reject / Return — shown to the applicant.</summary>
        [StringLength(1000)]
        public string? Remarks { get; set; }
    }
}
