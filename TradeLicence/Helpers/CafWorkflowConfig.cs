namespace TradeLicence.Helpers
{
    /// <summary>
    /// Fixed settings for the CAF officer workflow. Edit here, not in the controllers.
    /// </summary>
    public static class CafWorkflowConfig
    {
        /// <summary>
        /// The Officers.Department value of the Industry department officers who review
        /// submitted CAFs and forward them. Must match the Officers table exactly.
        /// </summary>
        public const string IndustryDepartment = "Industry";

        /// <summary>
        /// ServiceType used in WorkflowHistories / WorkflowPayments / WorkflowSupportingDocuments
        /// for CAF department workflows. CafDepartmentApplications.Id is unique across all
        /// departments, so one service type is enough.
        /// </summary>
        public const string ServiceType = "CAF";
    }
}
