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

        /// <summary>
        /// Officers.Department value for the Puducherry Planning Authority's officers. A CAF can be
        /// forwarded to it; on approval the applicant can download the Planning Authority NOC.
        /// </summary>
        public const string PlanningAuthorityDepartment = "Puducherry Planning Authority";

        /// <summary>
        /// Officers.Department value for the Revenue department's officers. A CAF can be forwarded
        /// to it; on approval the applicant can download the Revenue and Disaster Management NOC.
        /// </summary>
        public const string RevenueDepartment = "Revenue";

        /// <summary>
        /// Officers.Department value for the Town &amp; Country Planning (TCP) officers. Their officers' Department
        /// column must equal this text exactly, or forwarded CAFs will not reach them and "Forward To" will
        /// say "No officers found". On approval the applicant can download the TCP NOC.
        /// </summary>
        public const string TownAndCountryPlanningDepartment = "Town & Country Planning";
    }
}
