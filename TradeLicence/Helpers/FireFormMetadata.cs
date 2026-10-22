using System.Collections.Generic;
using System.Linq;
using TradeLicence.Models.Caf;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Single source of truth for the Fire NOC application form (legacy
    /// "fireclearance" sheet, deptcode 9 / serviceid 1017).
    ///
    /// Mirrors the CAF's metadata approach exactly: every step is one page of
    /// the wizard, every field is one column of dbo.FireClearanceApplications,
    /// and the last step is the document upload step (dbo.FireClearanceDocuments).
    /// Columns the application sets itself — status, currentstage, appcode,
    /// appliedon, serviceid, servicedes, deptcode, the officer-workflow columns
    /// and OfficerRemarks — are deliberately NOT fields here.
    ///
    /// The CAF form-descriptor models are reused (CafField / CafStepDef / ...)
    /// because they describe a metadata-driven form generically, not anything
    /// CAF-specific.
    /// </summary>
    public static class FireFormMetadata
    {
        private static readonly string[] YesNo = { "Yes", "No" };

        // ------------------------------------------------------------------
        // Option lists taken verbatim from the department's existing
        // Fire Safety Clearance application (Sector.FiresaftyClearance), so a
        // value picked here means the same thing it does in the old system.
        // ------------------------------------------------------------------

        private static readonly string[] BuildingClassifications =
        {
            "Residential", "Educational", "Institutional", "Assembly", "Business",
            "Mercantile", "Industrial", "Storage", "Hazardous", "Hospital/Clinic", "DRDM"
        };

        private static readonly string[] BusinessNatures =
        {
            "Operational", "Manufacturing", "Trade", "Storage", "Others"
        };

        private static readonly string[] HazardDegrees = { "Low", "Moderate", "High" };

        private static readonly string[] FireClearanceSoughtFor =
        {
            "Proposed Building",
            "Occupancy",
            "Trade License",
            "Arm License",
            "Petroleum products storage or outlet",
            "Renewal of Recognition",
            "Affiliation",
            "Fire Accident certificate",
            "Chemical storage",
            "Explosives",
            "Crackers",
            "Clinical activities",
            "Cinema hall",
            "Building"
        };

        private static readonly string[] FireFightingSystems =
        {
            "Fire Extinguisher",
            "Hose Reel System",
            "Downcomer",
            "Wet riser System",
            "Hydrant System",
            "Sprinkler System",
            "Detection and Alarm System",
            "Fire Pumps",
            "Others"
        };

        /// <summary>Stored as F / R, exactly like the legacy form's apptype radio pair.</summary>
        private static readonly string[] RegistrationRenewal = { "F|Registration", "R|Renewal" };

        private static readonly string[] Regions =
        {
            "1|Puducherry", "2|Karaikkal", "3|Mahe", "4|Yanam"
        };

        /// <summary>The one table every data-entry step writes to.</summary>
        public const string MainTable = "FireClearanceApplications";

        /// <summary>Uploaded-document table.</summary>
        public const string DocumentTable = "FireClearanceDocuments";

        public static readonly List<CafStepDef> Steps = new()
        {
            // ============================================================
            // STEP 1 — Premises & Owner Details
            // Mirrors page 1 of the department's printed form:
            // Premises Name / Owner Name / Mobile / Region / Registration or
            // Renewal / addresses / land line / e-mail / purpose / survey no.
            // ============================================================
            new CafStepDef
            {
                Number = 1,
                Key = "applicant",
                Title = "Premises & Owner Details",
                TableName = "FireClearanceApplications",
                Fields = new List<CafField>
                {
                    new() { Name = "premisesname",  Label = "Premises Name",       Type = CafFieldType.Text,     MaxLength = 200, Section = "Particulars of the Premises", Required = true },
                    new() { Name = "address",       Label = "Premises Address",    Type = CafFieldType.TextArea, MaxLength = 400, Section = "Particulars of the Premises", Required = true },
                    new() { Name = "surveyno",      Label = "Premises Survey No.", Type = CafFieldType.Text,     MaxLength = 50,  Section = "Particulars of the Premises" },
                    new() { Name = "distcode",      Label = "Region",              Type = CafFieldType.Select,   MaxLength = 20,  Section = "Particulars of the Premises",
                            Options = Regions, Required = true },
                    new() { Name = "registrationtype", Label = "Application Type", Type = CafFieldType.Radio,   MaxLength = 20,  Section = "Particulars of the Premises",
                            Options = RegistrationRenewal, Required = true },
                    new() { Name = "purpose",       Label = "Purpose",             Type = CafFieldType.TextArea, MaxLength = 200, Section = "Particulars of the Premises", Required = true },

                    new() { Name = "ownername",     Label = "Owner Name",          Type = CafFieldType.Text,     MaxLength = 150, Section = "Particulars of the Owner", Required = true },
                    new() { Name = "owneraddress",  Label = "Owner Address",       Type = CafFieldType.TextArea, MaxLength = 400, Section = "Particulars of the Owner" },
                    new() { Name = "mobileno",      Label = "Mobile No.",          Type = CafFieldType.Text,     MaxLength = 10,  Section = "Particulars of the Owner", Required = true },
                    new() { Name = "phno",          Label = "Land Line No.",       Type = CafFieldType.Text,     MaxLength = 20,  Section = "Particulars of the Owner" },
                    new() { Name = "email",         Label = "E-mail",              Type = CafFieldType.Email,    MaxLength = 150, Section = "Particulars of the Owner", Required = true },
                }
            },

            // ============================================================
            // STEP 2 — Building Details
            // ============================================================
            new CafStepDef
            {
                Number = 2,
                Key = "building",
                Title = "Building Details",
                TableName = "FireClearanceApplications",
                Fields = new List<CafField>
                {
                    new() { Name = "buildingclass",   Label = "Building Classification",       Type = CafFieldType.Select,   MaxLength = 50, Section = "Particulars of the Building", Options = BuildingClassifications, Required = true },
                    new() { Name = "floortype",       Label = "Type of Floor / Roof",          Type = CafFieldType.Select,   MaxLength = 50, Section = "Particulars of the Building", Options = new[] { "RCC", "Madras Terrace", "Tiled", "Thatched", "Others" }, Required = true },
                    new() { Name = "ht",              Label = "Height Including Stilt Floor (M)", Type = CafFieldType.Decimal, Section = "Particulars of the Building", Required = true },
                    new() { Name = "plotarea",        Label = "Plot Area (Sq.M)",              Type = CafFieldType.Decimal,  Section = "Particulars of the Building", Required = true },
                    new() { Name = "plintharea",      Label = "Plinth Area (Sq.M)",            Type = CafFieldType.Decimal,  Section = "Particulars of the Building", Required = true },
                    new() { Name = "nooffloor",       Label = "No. of Floors",                 Type = CafFieldType.Number,   Section = "Particulars of the Building", Required = true },
                    new() { Name = "areacovered",     Label = "Total Covered Area (Sq.M)",      Type = CafFieldType.Decimal,  Section = "Particulars of the Building", Required = true },
                    new() { Name = "buildingstorage", Label = "Building Storage",              Type = CafFieldType.TextArea, MaxLength = 200, Section = "Particulars of the Building", Required = true },
                    new() { Name = "yearofconstruction", Label = "Year of Construction",       Type = CafFieldType.Number,   Section = "Particulars of the Building" },
                    new() { Name = "typeofconstruction", Label = "Type of Construction",       Type = CafFieldType.Select,   MaxLength = 50, Section = "Particulars of the Building", Options = new[] { "Load Bearing", "RCC Frame Structure", "Steel Structure", "Composite", "Others" } },
                    new() { Name = "builtuparea",     Label = "Built Up Area (Sq.M)",           Type = CafFieldType.Decimal,  Section = "Particulars of the Building" },

                    new() { Name = "basementdetail",  Label = "Basement / Stilt Details",       Type = CafFieldType.TextArea, MaxLength = 400, Section = "Basement / Stilt Details" },
                }
            },

            // ============================================================
            // STEP 3 — Occupancy & Business Details
            // ============================================================
            new CafStepDef
            {
                Number = 3,
                Key = "occupancy",
                Title = "Occupancy & Business Details",
                TableName = "FireClearanceApplications",
                Fields = new List<CafField>
                {
                    new() { Name = "occupencydetail",    Label = "Floor-wise Occupancy Details",           Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details", Required = true },
                    new() { Name = "muloccupencydetail", Label = "Details in Case of Multiple Occupancy",  Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details" },
                    new() { Name = "setbackdetail",      Label = "Setback Details (All Sides) for Each Building / Block", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details" },

                    new() { Name = "businessnature", Label = "Nature of Business",                    Type = CafFieldType.Select, MaxLength = 200, Section = "Business and Hazard Details", Options = BusinessNatures, Required = true },
                    new() { Name = "staff",          Label = "Total No. of Staff / Student / Workers in the Building", Type = CafFieldType.TextArea, MaxLength = 200, Section = "Business and Hazard Details", Required = true },
                    new() { Name = "hazard",         Label = "Degree of Hazard in Case of Industry as per NBC-2016 (Annexure-B)", Type = CafFieldType.Select, MaxLength = 20, Section = "Business and Hazard Details",
                            Options = HazardDegrees, Required = true },
                    new() { Name = "fireescape",     Label = "Details of Fire Escape and Internal Staircase, Lifts, Ramps, Escalator, Corridors, Exits", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Business and Hazard Details" },
                    new() { Name = "canteen",        Label = "Details of Canteen / Cooking Facility with LPG Cylinders Available", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Business and Hazard Details" },
                    new() { Name = "noofbed",        Label = "No. of Beds (Hospital / Clinic / Nursing Home)", Type = CafFieldType.Text, MaxLength = 50, Section = "Business and Hazard Details" },
                    new() { Name = "noofrooms",      Label = "Total No. of Rooms (Lodging House)",    Type = CafFieldType.Text,   MaxLength = 50,  Section = "Business and Hazard Details" },
                }
            },

            // ============================================================
            // STEP 4 — Site & Fire Safety
            // ============================================================
            new CafStepDef
            {
                Number = 4,
                Key = "site",
                Title = "Site & Fire Safety",
                TableName = "FireClearanceApplications",
                Fields = new List<CafField>
                {
                    new() { Name = "firefighting", Label = "Fire Fighting Arrangements Available", Type = CafFieldType.MultiSelect, MaxLength = 400, Section = "Fire Safety Arrangements",
                            Options = FireFightingSystems, Required = true },
                    new() { Name = "watersource",  Label = "Details of Water Source",           Type = CafFieldType.TextArea, MaxLength = 200, Section = "Fire Safety Arrangements", Required = true },
                    new() { Name = "firesought",   Label = "Fire Clearance Sought For",         Type = CafFieldType.Select, MaxLength = 50, Section = "Fire Safety Arrangements", Options = FireClearanceSoughtFor, Required = true },
                    new() { Name = "emergencyexit", Label = "Emergency Exits Provided", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Means of Escape", Options = YesNo },
                    new() { Name = "fireexit", Label = "Fire Escape Staircase", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Means of Escape", Options = YesNo },
                    new() { Name = "lift", Label = "Lift / Elevator", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Installations", Options = YesNo },
                    new() { Name = "generator", Label = "Generator / DG Set", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Installations", Options = YesNo },
                    new() { Name = "transformer", Label = "Transformer", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Installations", Options = YesNo },
                    new() { Name = "licenseno", Label = "Previous Fire Licence No.", Type = CafFieldType.Text, MaxLength = 100, Section = "Previous Licence Details" },
                    new() { Name = "licensedate", Label = "Previous Licence Date", Type = CafFieldType.Date, Section = "Previous Licence Details" },
                }
            },

            // ============================================================
            // STEP 5 — Upload Documents (dbo.FireClearanceDocuments)
            // One varbinary(max) column per document; no Fields.
            // ============================================================
            new CafStepDef
            {
                Number = 5,
                Key = "documents",
                Title = "Upload Documents",
                TableName = "FireClearanceDocuments",
                Documents = new List<CafDocumentDef>
                {
                    // ---- building / ownership ----
                    new() { Column = "buildingdoc",     Label = "Approved Building Plan for All Floors and Dimensions", Required = true, Group = "Building & Ownership" },
                    new() { Column = "landdoc",         Label = "Land Document with Self-Attestation (Compulsory)",     Required = true, Group = "Building & Ownership" },
                    new() { Column = "siteplancdoc",    Label = "Site Plan / Layout Plan",          Group = "Building & Ownership" },
                    new() { Column = "taxreceiptdoc",   Label = "Property Tax Receipt",             Group = "Building & Ownership" },
                    new() { Column = "idproofdoc",      Label = "ID Proof of Applicant / Owner",    Group = "Building & Ownership" },

                    // ---- fire safety ----
                    new() { Column = "firedoc",         Label = "Fire Certificate Previously Obtained", Group = "Fire Safety" },
                    new() { Column = "firesafetydoc",   Label = "Fire Safety Certificate",          Group = "Fire Safety" },
                    new() { Column = "fireauditreportdoc", Label = "Third Party Fire Safety Audit Report", Group = "Fire Safety" },
                    new() { Column = "firetrainingdoc", Label = "Fire Drill / Training Record",     Group = "Fire Safety" },
                    new() { Column = "stabilitydoc",    Label = "Building Stability Certificate",   Group = "Fire Safety" },
                    new() { Column = "strengthdoc",     Label = "Structural Strength Certificate",  Group = "Fire Safety" },

                    // ---- occupancy / statutory approvals ----
                    new() { Column = "occupancycertdoc", Label = "Occupancy Certificate",           Group = "Occupancy & Statutory Approvals" },
                    new() { Column = "statutorydoc",    Label = "Statutory Licence Obtained for Carrying On Business", Group = "Occupancy & Statutory Approvals" },
                    new() { Column = "otherauthoritydoc", Label = "NOC from Other Authorities",      Group = "Occupancy & Statutory Approvals" },

                    // ---- installations ----
                    new() { Column = "electricalnocdoc", Label = "Electrical Inspector NOC",        Group = "Installations" },
                    new() { Column = "liftcertdoc",     Label = "Lift / Elevator Inspection Certificate", Group = "Installations" },

                    // ---- nature of premises ----
                    new() { Column = "commodoc",        Label = "Commodities / Business Already Licensed with Quantities", Group = "Nature of Premises" },
                    new() { Column = "hazardlicencedoc", Label = "Hazardous Material Licence",      Group = "Nature of Premises" },
                    new() { Column = "schooldoc",       Label = "Last Year School Registration",    Group = "Nature of Premises" },
                    new() { Column = "staffstrengthdoc", Label = "Staff and Student Strength Details", Group = "Nature of Premises" },
                    new() { Column = "insurancedoc",    Label = "Valid Insurance Policy / Vehicle Registration Certificate", Group = "Nature of Premises" },

                    // ---- photographs (image uploads) ----
                    new() { Column = "sitephotodoc",    Label = "Photographs of the Building / Site", Group = "Photographs" },

                    // ---- anything else ----
                    new() { Column = "othersdoc",       Label = "Any Other Related Documents",      Group = "Other" }
                }
            }
        };

        /// <summary>Largest single document accepted, in bytes (5 MB).</summary>
        public const int MaxDocumentBytes = 5 * 1024 * 1024;

        /// <summary>Every document column of dbo.FireClearanceDocuments.</summary>
        public static IEnumerable<CafDocumentDef> AllDocuments =>
            Steps.SelectMany(s => s.Documents);

        public static CafStepDef? ByNumber(int number) =>
            Steps.FirstOrDefault(s => s.Number == number);

        public static CafDocumentDef? DocumentByColumn(string column) =>
            Steps.SelectMany(s => s.Documents)
                 .FirstOrDefault(d => string.Equals(d.Column, column, System.StringComparison.OrdinalIgnoreCase));

        public static int MinStep => Steps.Min(s => s.Number);
        public static int MaxStep => Steps.Max(s => s.Number);

        /// <summary>Step number of the synthetic Preview tab (one past the last real step).</summary>
        public static int PreviewStep => MaxStep + 1;

        public const string PreviewTitle = "Preview & Submit";

        /// <summary>
        /// Formats a stored value for read-only display. Both forms describe
        /// their fields with the same CafField model, so the CAF implementation
        /// is the single source of truth for this.
        /// </summary>
        public static string? DisplayValue(CafField field, string? raw) =>
            CafFormMetadata.DisplayValue(field, raw);
    }
}
