namespace TradeLicence.Helpers
{
    using System;
    /// <summary>
    /// Single source of truth for the Fire NOC / Fire Safety Clearance service
    /// (legacy EODB: deptcode = 9, serviceid = 1017).
    ///
    /// The Fire service runs on the shared WorkflowEngineService, so its
    /// ServiceType is what tags WorkflowHistories / WorkflowSupportingDocuments /
    /// WorkflowPayments rows for fire applications — and Department is the
    /// Officer.Department value that Fire officers are created with (see
    /// Officer/CreateUser), which is how OfficerController.Index routes a fire
    /// officer to the fire queue.
    /// </summary>
    public static class FireWorkflowConfig
    {
        /// <summary>Officer.Department value every Fire department officer has.</summary>
        public const string Department = "Fire";

        /// <summary>ServiceType in the shared workflow tables for fire applications.</summary>
        public const string ServiceType = "FireClearance";

        /// <summary>Legacy service id ("Registration for Fire License").</summary>
        public const int ServiceId = 1017;

        /// <summary>Legacy department code for the Fire Service Department.</summary>
        public const short DeptCode = 9;

        /// <summary>Description shown on the acknowledgement / NOC.</summary>
        public const string ServiceDescription = "Registration for Fire License (Fire NOC)";
    }

    /// <summary>
    /// The one place that turns the raw status / stage strings stored on a Fire NOC
    /// into text a citizen or officer should actually read.
    ///
    /// Status and CurrentStage are kept in sync by the workflow engine: on a final
    /// decision CurrentStage becomes "Approved" / "Rejected" (see
    /// OfficerWorkflow.ApprovedStage / RejectedStage), so the stage is rendered
    /// from the status once the process is over instead of repeating the badge.
    /// </summary>
    public static class FireStatusDisplay
    {
        public static bool IsDecided(string? status) =>
            Eq(status, "Approved") || Eq(status, "Rejected");

        public static bool IsReturned(string? status) => Eq(status, "ReturnedToApplicant");

        /// <summary>Readable label for the status badge.</summary>
        public static string StatusLabel(string? status) => status switch
        {
            "Approved" => "Approved",
            "Rejected" => "Rejected",
            "ReturnedToApplicant" => "Sent back for correction",
            "Submitted" => "Submitted — in process",
            "Draft" => "Draft",
            _ => string.IsNullOrWhiteSpace(status) ? "—" : status
        };

        /// <summary>Bootstrap badge class matching <see cref="StatusLabel"/>.</summary>
        public static string StatusBadge(string? status) => status switch
        {
            "Approved" => "text-bg-success",
            "Rejected" => "text-bg-danger",
            "ReturnedToApplicant" => "text-bg-warning",
            "Submitted" => "text-bg-info",
            _ => "text-bg-secondary"
        };

        /// <summary>
        /// What the "Current Stage" row should say. Once the process is over the
        /// stored stage IS the decision, so describe the outcome rather than
        /// repeating the status badge.
        /// </summary>
        public static string StageLabel(string? status, string? stage) => status switch
        {
            "Approved" => "Process complete",
            "Rejected" => "Application closed",
            "ReturnedToApplicant" => "With you — correct and resubmit",
            _ => string.IsNullOrWhiteSpace(stage) ? "—" : stage
        };

        private static bool Eq(string? a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
