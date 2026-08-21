using System;
using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// One row per Forward or Revert action on an application, in order.
    /// This is what powers:
    ///   - the "Forwarded by ..." history shown at the top of the officer's
    ///     ViewApplication page (the full DEO -> Manager -> Inspection ->
    ///     GM chain, with remarks, is just this table filtered by
    ///     ApplicationId and ordered by ActionDate)
    ///   - "Revert to previous officer" — reverting looks up the most
    ///     recent Forward entry that brought the application to its
    ///     CURRENT stage, and sends it back to that entry's FromOfficerId.
    /// TradeLicenceApplication.OfficerRemarks/CurrentStage/AssignedOfficerId
    /// still hold the CURRENT state — this table is the trail of how it
    /// got there.
    /// </summary>
    public class ApplicationWorkflowHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int ApplicationId { get; set; }

        // Null for the very first entry if you ever want to log "citizen
        // submitted" as a row — not currently done, history starts at the
        // first Forward from Initial Scrutiny.
        public int? FromOfficerId { get; set; }
        public string FromStage { get; set; } = string.Empty;

        public int? ToOfficerId { get; set; }
        public string ToStage { get; set; } = string.Empty;

        // "Forward" or "Revert"
        public string ActionType { get; set; } = "Forward";

        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; } = DateTime.UtcNow;

        public virtual TradeLicenceApplication? Application { get; set; }
        public virtual Officer? FromOfficer { get; set; }
        public virtual Officer? ToOfficer { get; set; }
    }
}
