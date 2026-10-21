namespace TradeLicence.Helpers
{
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
}
