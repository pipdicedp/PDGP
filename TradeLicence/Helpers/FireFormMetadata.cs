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
                    new() { Name = "premisesname",  Label = "Premises Name",       Type = CafFieldType.Text,     MaxLength = 200, Section = "Premises Details", Required = true },
                    new() { Name = "address",       Label = "Premises Address",    Type = CafFieldType.TextArea, MaxLength = 400, Section = "Premises Details", Required = true },
                    new() { Name = "surveyno",      Label = "Premises Survey No.", Type = CafFieldType.Text,     MaxLength = 50,  Section = "Premises Details" },
                    new() { Name = "distcode",      Label = "Region",              Type = CafFieldType.Select,   MaxLength = 20,  Section = "Premises Details",
                            Options = new[] { "1|Puducherry", "2|Karaikal", "3|Mahe", "4|Yanam" }, Required = true },
                    new() { Name = "registrationtype", Label = "Registration / Renewal", Type = CafFieldType.Select, MaxLength = 20, Section = "Premises Details",
                            Options = new[] { "Registration", "Renewal" }, Required = true },
                    new() { Name = "purpose",       Label = "Purpose",             Type = CafFieldType.Text,     MaxLength = 200, Section = "Premises Details", Required = true },

                    new() { Name = "ownername",     Label = "Owner Name",          Type = CafFieldType.Text,     MaxLength = 150, Section = "Owner Details", Required = true },
                    new() { Name = "owneraddress",  Label = "Owner Address",       Type = CafFieldType.TextArea, MaxLength = 400, Section = "Owner Details" },
                    new() { Name = "mobileno",      Label = "Mobile No.",          Type = CafFieldType.Text,     MaxLength = 20,  Section = "Owner Details", Required = true },
                    new() { Name = "phno",          Label = "Land Line No.",       Type = CafFieldType.Text,     MaxLength = 20,  Section = "Owner Details" },
                    new() { Name = "email",         Label = "E-mail",              Type = CafFieldType.Email,    MaxLength = 150, Section = "Owner Details", Required = true },
                    new() { Name = "name",          Label = "Name of Applicant",   Type = CafFieldType.Text,     MaxLength = 150, Section = "Owner Details", Required = true },
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
                    new() { Name = "buildingclass",   Label = "Building Classification",       Type = CafFieldType.Text,     MaxLength = 50, Section = "Building Details", Required = true },
                    new() { Name = "floortype",       Label = "Type of Floor / Roof",          Type = CafFieldType.Select,   MaxLength = 50, Section = "Building Details", Options = new[] { "RCC", "Madras Terrace", "Tiled", "Thatched", "Others" }, Required = true },
                    new() { Name = "ht",              Label = "Height Including Stilt Floor (M)", Type = CafFieldType.Decimal, Section = "Building Details", Required = true },
                    new() { Name = "plotarea",        Label = "Plot Area (Sq.M)",              Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "plintharea",      Label = "Plinth Area (Sq.M)",            Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "nooffloor",       Label = "No. of Floors",                 Type = CafFieldType.Number,   Section = "Building Details", Required = true },
                    new() { Name = "areacovered",     Label = "Total Covered Area (Sq.M)",      Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "buildingstorage", Label = "Building Storage",              Type = CafFieldType.Text,     MaxLength = 200, Section = "Building Details", Required = true },
                    new() { Name = "yearofconstruction", Label = "Year of Construction",       Type = CafFieldType.Number,   Section = "Building Details" },
                    new() { Name = "typeofconstruction", Label = "Type of Construction",       Type = CafFieldType.Select,   MaxLength = 50, Section = "Building Details", Options = new[] { "Load Bearing", "RCC Frame Structure", "Steel Structure", "Composite", "Others" } },
                    new() { Name = "builtuparea",     Label = "Built Up Area (Sq.M)",           Type = CafFieldType.Decimal,  Section = "Building Details" },

                    new() { Name = "basementdetail",  Label = "Basement / Stilt Details",       Type = CafFieldType.TextArea, MaxLength = 400, Section = "Basement / Stilt" },
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

                    new() { Name = "businessnature", Label = "Nature of Business",                    Type = CafFieldType.Text,   MaxLength = 200, Section = "Business Details", Required = true },
                    new() { Name = "staff",          Label = "Total No. of Staff / Students / Workers", Type = CafFieldType.Text, MaxLength = 50,  Section = "Business Details", Required = true },
                    new() { Name = "hazard",         Label = "Degree of Hazard in Case of Industry as per NBC-2016 (Annexure-B)", Type = CafFieldType.Select, MaxLength = 20, Section = "Business Details",
                            Options = new[] { "Low Hazard", "Moderate Hazard", "High Hazard", "Not Applicable" }, Required = true },
                    new() { Name = "fireescape",     Label = "Details of Fire Escape and Internal Staircase, Lifts, Ramps, Escalator, Corridors, Exits", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Business Details" },
                    new() { Name = "canteen",        Label = "Details of Canteen / Cooking Facility with LPG Cylinders Available", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Business Details" },
                    new() { Name = "noofbed",        Label = "No. of Beds (Hospital / Clinic / Nursing Home)", Type = CafFieldType.Text, MaxLength = 50, Section = "Business Details" },
                    new() { Name = "noofrooms",      Label = "Total No. of Rooms (Lodging House)",    Type = CafFieldType.Text,   MaxLength = 50,  Section = "Business Details" },
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
                    new() { Name = "firefighting", Label = "Details of Fire Fighting Arrangement Available", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Fire Safety Arrangements", Required = true },
                    new() { Name = "watersource",  Label = "Details of Water Source",           Type = CafFieldType.Text, MaxLength = 200, Section = "Fire Safety Arrangements", Required = true },
                    new() { Name = "firesought",   Label = "Fire Clearance Sought For",         Type = CafFieldType.Select, MaxLength = 50, Section = "Fire Safety Arrangements", Options = new[] { "New|New Fire Clearance", "Renewal|Renewal of Fire Clearance" }, Required = true },
                    new() { Name = "fireextinguishers", Label = "Fire Extinguishers Provided", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Fire Fighting System", Options = YesNo },
                    new() { Name = "firehydrant", Label = "Fire Hydrant System", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Fire Fighting System", Options = YesNo },
                    new() { Name = "firesprinkler", Label = "Automatic Sprinkler System", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Fire Fighting System", Options = YesNo },
                    new() { Name = "firealarm", Label = "Fire Alarm System", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Fire Fighting System", Options = YesNo },
                    new() { Name = "emergencyexit", Label = "Emergency Exits Provided", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Means of Escape", Options = YesNo },
                    new() { Name = "fireexit", Label = "Fire Escape Staircase", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Means of Escape", Options = YesNo },
                    new() { Name = "lift", Label = "Lift / Elevator", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Details", Options = YesNo },
                    new() { Name = "generator", Label = "Generator / DG Set", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Details", Options = YesNo },
                    new() { Name = "transformer", Label = "Transformer", Type = CafFieldType.YesNo, MaxLength = 20, Section = "Other Details", Options = YesNo },
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

        /// <summary>Formats a stored value for read-only display (mirrors CafFormMetadata.DisplayValue).</summary>
        public static string? DisplayValue(CafField field, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            raw = raw.Trim();

            switch (field.Type)
            {
                case CafFieldType.Select:
                    var match = field.Options?.FirstOrDefault(o =>
                        string.Equals(o.Split('|')[0].Trim(), raw, System.StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(match))
                    {
                        var pipe = match.IndexOf('|');
                        return pipe >= 0 ? match[(pipe + 1)..].Trim() : match;
                    }
                    return raw;

                case CafFieldType.Decimal:
                    return decimal.TryParse(raw, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d)
                        ? d.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                        : raw;

                case CafFieldType.Date:
                    return System.DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt)
                        ? dt.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture)
                        : raw;

                default:
                    return raw;
            }
        }
    }
}
