namespace TradeLicence.Models.Drugs
{
    /// <summary>
    /// Everything that differs between the two services (Form 20A / Form 21A) in one place.
    /// The controllers, service, views and workflow are written ONCE and driven by this.
    /// </summary>
    public sealed class DrugFormDefinition
    {
        /// <summary>"20A" / "21A" - stored in DrugLicencePharmacists.FormType / DrugLicenceDocuments.FormType.</summary>
        public required string Code { get; init; }

        /// <summary>Card / page title.</summary>
        public required string Title { get; init; }

        /// <summary>WorkflowHistories / WorkflowPayments / WorkflowSupportingDocuments.ServiceType (max 50 chars).</summary>
        public required string ServiceType { get; init; }

        /// <summary>Officers.Department value. Must match the seeded officers EXACTLY (see Database/DrugLicence_Form20A_21A.sql).</summary>
        public required string Department { get; init; }

        /// <summary>EODB service id from the portal URL (informational).</summary>
        public required int ServiceId { get; init; }

        /// <summary>Citizen-side controller name, e.g. "Form20A".</summary>
        public required string Controller { get; init; }

        /// <summary>Officer-side controller name, e.g. "Form20AOfficer".</summary>
        public required string OfficerController { get; init; }
    }

    public sealed record DrugDocumentDef(string Key, string Label, bool AlwaysRequired);

    public sealed record DrugOption(string Value, string Label);

    public static class DrugLicenceConfig
    {
        // Department text used in Officers.Department. Change here AND in the SQL seed script together.
        public const string Form20ADepartment = "Drugs - Form 20A";
        public const string Form21ADepartment = "Drugs - Form 21A";

        public static readonly DrugFormDefinition Form20A = new()
        {
            Code = "20A",
            Title = "Application for Fresh/Retention of Restricted Drug License in Form 20A",
            ServiceType = "Drugs20A",
            Department = Form20ADepartment,
            ServiceId = 1064,
            Controller = "Form20A",
            OfficerController = "Form20AOfficer"
        };

        public static readonly DrugFormDefinition Form21A = new()
        {
            Code = "21A",
            Title = "Application for Fresh/Retention of Restricted Drug License in Form 21A",
            ServiceType = "Drugs21A",
            Department = Form21ADepartment,
            ServiceId = 1065,
            Controller = "Form21A",
            OfficerController = "Form21AOfficer"
        };

        public static readonly DrugFormDefinition[] All = { Form21A, Form20A };

        public static DrugFormDefinition? ForDepartment(string? department) =>
            All.FirstOrDefault(f => string.Equals(f.Department, department, StringComparison.OrdinalIgnoreCase));

        // ---- Inspection-stage payment (placeholder amounts - same idea as WaterOfficerController) ----
        public const decimal PaymentAmount = 1000m;
        public const decimal ExtraCharge = 500m;

        // ---- Uploads: the sample form says "Only allowed PDF file and Maximum size of uploading file is 1MB" ----
        public const long MaxFileBytes = 1_048_576;

        // Order = order on the form. "Cold storage" proof is required only when Cold Storage = Yes,
        // "Others" is optional (everything else is always required).
        public static readonly DrugDocumentDef[] Documents =
        {
            new("ProprietorDetails", "Details of Proprietor or Partners or Managing Director", true),
            new("Affidavit",         "Affidavit under section 34 of Drugs and Cosmetics Act,1940 / Special declaration of Registered Pharmacist", true),
            new("PharmacistDetails", "Details of Pharmacist / Competent", true),
            new("PremisesPlan",      "Plan of the premises by the Certified Engineer", true),
            new("PremisesOwnership", "Premises Ownership document / Rental lease / Receipt", true),
            new("ColdStorageProof",  "Cold storage purchase / Working condition proof", false),
            new("Others",            "Others", false),
        };

        public static readonly DrugOption[] ScheduleOptions =
        {
            new("CC1ExcludingX",    "Drugs Specified in Schedule C,C(1) excluding X"),
            new("OtherThanCC1AndX", "Drugs other than Specified in Schedule C,C(1) and X"),
            new("X",                "Drugs Specified in Schedule X"),
            new("C1ExcludingX",     "Drugs Specified in Schedule C(1) excluding X"),
            new("NotApplicable",    "Not Applicable"),
        };

        // Property name on the application entity -> label. Order = order on the form.
        public static readonly DrugOption[] StorageOptions =
        {
            new("AluminiumSections", "Aluminium Sections"),
            new("Drawers",           "Drawers"),
            new("LockAndKey",        "Lock and Key"),
            new("Racks",             "Racks"),
            new("GlassSections",     "Glass Sections"),
            new("Pellets",           "Pellets"),
            new("WoodenFurniture",   "Wooden Furniture"),
            new("AnyOther",          "Any Other"),
        };

        public static readonly string[] SexOptions = { "Male", "Female", "Other" };

        // Used only when DropdownMasters has no rows for the category (the SQL script seeds them).
        public static readonly string[] DefaultConstitutionTypes =
            { "Proprietorship", "Partnership", "Private Limited Company", "Public Limited Company", "Limited Liability Partnership", "Society / Trust", "Others" };
        public static readonly string[] DefaultDistricts = { "Puducherry", "Karaikal", "Mahe", "Yanam" };
        public static readonly string[] DefaultCeilingTypes = { "RCC", "Asbestos Sheet", "Tiled", "Metal Sheet", "Others" };

        public const string CatConstitution = "DrugConstitutionType";
        public const string CatDistrict = "DrugDistrict";
        public const string CatCeiling = "DrugCeilingType";
    }
}
