namespace TradeLicence.Helpers
{
    /// <summary>
    /// One place for the two new services' workflow settings.
    ///
    /// Officers of these services are created from Officer > Officer Creation with the
    /// Department text below (spelling must match; comparison ignores upper/lower case).
    /// Designations are the same 4 as every other service: DEO -> Manager ->
    /// Inspection Officer -> GM (see OfficerWorkflow.cs).
    /// </summary>
    public static class RegistrationWorkflowConfig
    {
        // ---- Registration of Firms (Indian Partnership Act 1932) ----
        public const string FirmServiceType = "Firms";
        public const string FirmsDepartment = "Registration of Firms";

        // ---- Registration / Renewal of Society (Societies Registration Act 1860) ----
        public const string SocietyServiceType = "Society";
        public const string SocietyDepartment = "Society Registration";

        // Fee requested from the applicant at the Inspection stage (same step as Electricity / Water).
        // Placeholder values - change to the real fee schedule.
        public const decimal FirmFee = 1000m;
        public const decimal FirmExtraCharge = 500m;
        public const decimal SocietyFee = 1000m;
        public const decimal SocietyExtraCharge = 500m;

        public static bool IsFirmsOfficer(string? department) =>
            string.Equals(department, FirmsDepartment, StringComparison.OrdinalIgnoreCase);

        public static bool IsSocietyOfficer(string? department) =>
            string.Equals(department, SocietyDepartment, StringComparison.OrdinalIgnoreCase);
    }
}
