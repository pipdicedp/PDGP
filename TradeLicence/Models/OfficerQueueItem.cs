namespace TradeLicence.Models
{
    /// <summary>
    /// One row in the shared officer queue (OfficerController.Index -> SharedQueue.cshtml).
    /// Each service maps its own application entity into this before adding
    /// it to the list — the view itself never knows which service a row
    /// came from beyond the ServiceType label and the pre-built ViewUrl.
    /// </summary>
    public class OfficerQueueItem
    {
        public int ApplicationId { get; set; }
        public string ServiceType { get; set; } = string.Empty;   // "Water", "Electricity", ...
        public string DisplayName { get; set; } = string.Empty;   // applicant name — each service's own field, mapped here
        public string Contact { get; set; } = string.Empty;       // phone/mobile — each service's own field, mapped here
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string ViewUrl { get; set; } = string.Empty;        // pre-built link to that service's own ViewApplication page

        // Null = unassigned, sitting at CurrentStage's designation for anyone to
        // pick up (same meaning as IWorkflowApplication.AssignedOfficerId).
        // Only the menu pages (Assigned/Forwarded/History/...) read this — the
        // dashboard queue is already filtered by the engine/controller.
        public int? AssignedOfficerId { get; set; }

        // Which row of WorkflowHistories belongs to this application.
        // Engine services set their service type ("Water", "Electricity",
        // "FireClearance", "CAF"); Trade Licence leaves this null because its
        // history lives in ApplicationWorkflowHistories instead (see
        // OfficerController's touched-by-me lookups).
        public string? HistoryServiceType { get; set; }
    }

    /// <summary>
    /// Status breakdown shown as the stat cards on the officer dashboard and
    /// on the menu pages (All / Assigned / Forwarded / ...). Computed from the
    /// officer's full register — not from the visible queue — so completed
    /// (Approved/Rejected) applications are counted too.
    /// </summary>
    public class OfficerQueueStats
    {
        public int Total { get; set; }
        public int Draft { get; set; }
        // Submitted but nobody has picked it up yet.
        public int Pending { get; set; }
        // Submitted and already past Initial Scrutiny.
        public int InProgress { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        // Sent back to the citizen for correction.
        public int Returned { get; set; }
    }
}
