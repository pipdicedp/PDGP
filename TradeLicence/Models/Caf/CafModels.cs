using System.Collections.Generic;

namespace TradeLicence.Models.Caf
{
    /// <summary>How a CafField should be rendered and how its posted value should be parsed.</summary>
    public enum CafFieldType
    {
        Text,
        TextArea,
        Number,     // whole numbers -> SqlDbType.BigInt
        Decimal,    // NUMERIC/FLOAT columns -> SqlDbType.Decimal
        Date,
        Select,     // free-text column, but offered as a dropdown of sensible values
        YesNo,      // free-text column storing "Yes"/"No"
        Email
    }

    /// <summary>
    /// One column of a CAF table, described well enough to (a) render an
    /// input for it and (b) read/write it with plain parameterized ADO.NET —
    /// see CafFormService. "Name" must match the real database column name
    /// exactly (case doesn't matter to SQL Server, but keep it exact for
    /// readability).
    /// </summary>
    public class CafField
    {
        public string Name { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public CafFieldType Type { get; init; } = CafFieldType.Text;
        public string Section { get; init; } = string.Empty;
        public int? MaxLength { get; init; }
        public string[]? Options { get; init; }

        /// <summary>HTML input "step" attribute for Decimal fields (default "0.01").</summary>
        public string DecimalStep { get; init; } = "0.01";
    }

    /// <summary>
    /// A repeatable child table shown as an "Add row" mini-form + mini-table
    /// inside a step (e.g. Land Requirement Details under Town & Country).
    /// Requires the table to have an identity "Id" primary key — see
    /// Database/CAF_SchemaExtras.sql — because rows must be individually
    /// addressable (added one at a time, deletable one at a time).
    /// </summary>
    public class CafSubTableDef
    {
        public string Key { get; init; } = string.Empty;
        public string TableName { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public List<CafField> Fields { get; init; } = new();
    }

    /// <summary>
    /// One uploadable document = one varbinary(max) column of dbo.caf_doc_table.
    /// Column must match the database column name exactly.
    /// </summary>
    public class CafDocumentDef
    {
        public string Column { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;

        /// <summary>When true the applicant cannot submit until this document is uploaded.</summary>
        public bool Required { get; init; }
    }

    /// <summary>
    /// One step of the Common Application Form = one main database table
    /// (one row per applicant, keyed by loginid) plus zero or more
    /// CafSubTableDef child tables (many rows per applicant).
    /// </summary>
    public class CafStepDef
    {
        public int Number { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string TableName { get; init; } = string.Empty;
        public List<CafField> Fields { get; init; } = new();
        public List<CafSubTableDef> SubTables { get; init; } = new();

        /// <summary>Only used by the documents step: the file uploads shown instead of Fields.</summary>
        public List<CafDocumentDef> Documents { get; init; } = new();
    }

    /// <summary>One already-saved sub-table row, ready for the mini-table — Id plus the display fields.</summary>
    public class CafSubRowVm
    {
        public long Id { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new();
    }

    /// <summary>A document slot plus whether the applicant has already uploaded a file for it.</summary>
    public class CafDocumentVm
    {
        public CafDocumentDef Def { get; set; } = null!;
        public long? SizeBytes { get; set; }
        public bool HasFile => SizeBytes.HasValue && SizeBytes.Value > 0;
    }

    public class CafSubTableVm
    {
        public CafSubTableDef Def { get; set; } = null!;
        public List<CafSubRowVm> Rows { get; set; } = new();
    }

    /// <summary>Everything the Step view needs to render one step of the wizard.</summary>
    public class CafStepViewModel
    {
        public List<CafStepDef> AllSteps { get; set; } = new();
        public CafStepDef Current { get; set; } = null!;

        /// <summary>Current saved values for Current.Fields, keyed by CafField.Name. Missing/blank if not saved yet.</summary>
        public Dictionary<string, string?> Values { get; set; } = new();

        public List<CafSubTableVm> SubTables { get; set; } = new();

        /// <summary>Document slots for the documents step (empty on every other step).</summary>
        public List<CafDocumentVm> Documents { get; set; } = new();

        /// <summary>False only before Step 1 (Basic Details) has ever been saved — later steps redirect back to Step 1 until then.</summary>
        public bool BasicDetailsExist { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
