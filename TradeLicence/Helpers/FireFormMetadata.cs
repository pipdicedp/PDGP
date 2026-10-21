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
            // STEP 1 — Applicant & Owner Details
            // ============================================================
            new CafStepDef
            {
                Number = 1,
                Key = "applicant",
                Title = "Applicant & Owner Details",
                TableName = "FireClearanceApplications",
                Fields = new List<CafField>
                {
                    new() { Name = "name",         Label = "Name of Applicant",   Type = CafFieldType.Text,     MaxLength = 150, Section = "Applicant Details", Required = true },
                    new() { Name = "address",      Label = "Address of Applicant", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Applicant Details", Required = true },
                    new() { Name = "email",        Label = "Email Address",       Type = CafFieldType.Email,    MaxLength = 150, Section = "Applicant Details", Required = true },
                    new() { Name = "mobileno",     Label = "Mobile Number",       Type = CafFieldType.Text,     MaxLength = 20,  Section = "Applicant Details", Required = true },
                    new() { Name = "phno",         Label = "Phone Number",        Type = CafFieldType.Text,     MaxLength = 20,  Section = "Applicant Details" },

                    new() { Name = "ownername",    Label = "Name of Owner",       Type = CafFieldType.Text,     MaxLength = 150, Section = "Owner Details", Required = true },
                    new() { Name = "owneraddress", Label = "Address of Owner",    Type = CafFieldType.TextArea, MaxLength = 400, Section = "Owner Details" },
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
                    new() { Name = "buildingclass",   Label = "Building Class",            Type = CafFieldType.Text,     MaxLength = 50, Section = "Building Details", Required = true },
                    new() { Name = "floortype",       Label = "Type of Floor / Roof",      Type = CafFieldType.Select,   MaxLength = 50, Section = "Building Details", Options = new[] { "RCC", "Madras Terrace", "Tiled", "Thatched", "Others" }, Required = true },
                    new() { Name = "ht",              Label = "Height of Building (Mtr)",  Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "plotarea",         Label = "Plot Area (Sq.Mtr)",        Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "plintharea",      Label = "Plinth Area (Sq.Mtr)",      Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "nooffloor",       Label = "Number of Floors",          Type = CafFieldType.Number,   Section = "Building Details", Required = true },
                    new() { Name = "areacovered",     Label = "Area Covered (Sq.Mtr)",     Type = CafFieldType.Decimal,  Section = "Building Details", Required = true },
                    new() { Name = "buildingstorage", Label = "Building / Storage Description", Type = CafFieldType.Text, MaxLength = 200, Section = "Building Details", Required = true },

                    new() { Name = "basementdetail",  Label = "Basement Details",          Type = CafFieldType.TextArea, MaxLength = 400, Section = "Basement Details" },
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
                    new() { Name = "occupencydetail",    Label = "Occupancy Details",          Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details", Required = true },
                    new() { Name = "muloccupencydetail", Label = "Multiple Occupancy Details", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details" },
                    new() { Name = "setbackdetail",      Label = "Setback Details",            Type = CafFieldType.TextArea, MaxLength = 400, Section = "Occupancy Details" },
                    new() { Name = "purpose",            Label = "Purpose of Building",        Type = CafFieldType.Text,     MaxLength = 200, Section = "Occupancy Details", Required = true },

                    new() { Name = "businessnature", Label = "Nature of Business / Trade", Type = CafFieldType.Text,   MaxLength = 200, Section = "Business Details", Required = true },
                    new() { Name = "staff",          Label = "Number of Staff",            Type = CafFieldType.Text,   MaxLength = 50,  Section = "Business Details", Required = true },
                    new() { Name = "hazard",         Label = "Hazardous Process Involved", Type = CafFieldType.YesNo,  MaxLength = 20,  Section = "Business Details", Options = YesNo, Required = true },
                    new() { Name = "fireescape",     Label = "Fire Escape Provided",       Type = CafFieldType.YesNo,  MaxLength = 20,  Section = "Business Details", Options = YesNo, Required = true },
                    new() { Name = "canteen",        Label = "Canteen Facility Available", Type = CafFieldType.YesNo,  MaxLength = 20,  Section = "Business Details", Options = YesNo },
                    new() { Name = "noofbed",        Label = "Number of Beds",             Type = CafFieldType.Text,   MaxLength = 50,  Section = "Business Details" },
                    new() { Name = "noofrooms",      Label = "Number of Rooms",            Type = CafFieldType.Text,   MaxLength = 50,  Section = "Business Details" },
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
                    new() { Name = "surveyno",     Label = "Survey / R.S. No.",     Type = CafFieldType.Text, MaxLength = 50, Section = "Site Details", Required = true },
                    new() { Name = "distcode",     Label = "District",              Type = CafFieldType.Select, MaxLength = 20, Section = "Site Details",
                            Options = new[] { "1|Puducherry", "2|Karaikal", "3|Mahe", "4|Yanam" }, Required = true },

                    new() { Name = "firefighting", Label = "Fire Fighting Arrangements", Type = CafFieldType.TextArea, MaxLength = 400, Section = "Fire Safety Arrangements", Required = true },
                    new() { Name = "watersource",  Label = "Water Source for Fire Fighting", Type = CafFieldType.Text, MaxLength = 200, Section = "Fire Safety Arrangements", Required = true },
                    new() { Name = "firesought",   Label = "Fire Safety Licence Sought For", Type = CafFieldType.Select, MaxLength = 50, Section = "Fire Safety Arrangements", Options = new[] { "New|New Licence", "Renewal|Renewal of Licence" }, Required = true },
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
                    new() { Column = "buildingdoc",   Label = "Building Plan", Required = true },
                    new() { Column = "landdoc",       Label = "Land / Ownership Document", Required = true },
                    new() { Column = "firedoc",       Label = "Fire Certificate" },
                    new() { Column = "commodoc",      Label = "Commodities / Goods Details" },
                    new() { Column = "statutorydoc",  Label = "Statutory Approval" },
                    new() { Column = "insurancedoc",  Label = "Insurance Document" },
                    new() { Column = "othersdoc",     Label = "Other Document" },
                    new() { Column = "schooldoc",     Label = "School / Institution Certificate" },
                    new() { Column = "stabilitydoc",  Label = "Structural Stability Certificate" },
                    new() { Column = "strengthdoc",   Label = "Structural Strength Certificate" },
                    new() { Column = "firesafetydoc", Label = "Fire Safety Certificate" }
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
