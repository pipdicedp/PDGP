using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Generic version of ApplicationWorkflowHistory — one shared table for
    /// every service instead of one table per service. ApplicationId is a
    /// "soft" reference (no FK) since it points at a different physical
    /// table depending on ServiceType; ServiceType + ApplicationId together
    /// identify the application. FromOfficerId/ToOfficerId DO have a real FK
    /// to Officers since that table is genuinely shared across all services.
    ///
    /// TradeLicence keeps using its own ApplicationWorkflowHistories table
    /// unchanged — this table is for every OTHER service going forward
    /// (Water first, then Electricity, Transport, ...).
    /// </summary>
    public class WorkflowHistory
    {
        [Key] public int HistoryId { get; set; }

        [Required, StringLength(50)]
        public string ServiceType { get; set; } = string.Empty; // "Water", "Electricity", ...

        public int ApplicationId { get; set; }

        public int? FromOfficerId { get; set; }
        public Officer? FromOfficer { get; set; }

        [StringLength(50)]
        public string? FromStage { get; set; }

        public int? ToOfficerId { get; set; }
        public Officer? ToOfficer { get; set; }

        [StringLength(50)]
        public string? ToStage { get; set; }

        // "Forward" / "Revert"
        [Required, StringLength(20)]
        public string ActionType { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    }
}
