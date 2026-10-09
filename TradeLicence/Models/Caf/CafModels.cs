using System.Collections.Generic;
using System.Linq;

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
    }

    /// <summary>One already-saved sub-table row, ready for the mini-table — Id plus the display fields.</summary>
    public class CafSubRowVm
    {
        public long Id { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new();
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

        /// <summary>Step numbers whose main row is already saved — drives the stepper's green ticks.</summary>
        public HashSet<int> SavedSteps { get; set; } = new();

        /// <summary>False only before Step 1 (Basic Details) has ever been saved — later steps redirect back to Step 1 until then.</summary>
        public bool BasicDetailsExist { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>Data the shared stepper partial (_CafStepper.cshtml) needs — works for the 5 data steps and the Preview tab.</summary>
    public class CafStepperVm
    {
        public List<CafStepDef> AllSteps { get; set; } = new();

        /// <summary>1-5 for a data-entry step, CafFormMetadata.PreviewStep (6) on the Application Preview tab.</summary>
        public int CurrentNumber { get; set; }

        public bool BasicDetailsExist { get; set; }
        public bool IsSubmitted { get; set; }

        /// <summary>Steps whose data is saved. Only these show as green/done; unsaved steps stay grey.</summary>
        public HashSet<int> SavedSteps { get; set; } = new();
    }

    /// <summary>One step's saved data for the Application Preview: the main row plus its sub-table rows.</summary>
    public class CafPreviewSectionVm
    {
        public CafStepDef Step { get; set; } = null!;

        /// <summary>Saved values keyed by CafField.Name. Empty when the applicant never saved this step.</summary>
        public Dictionary<string, string?> Values { get; set; } = new();

        /// <summary>True once the step's main row exists in the database (LoadMainRowAsync returns nothing otherwise).</summary>
        public bool HasRow { get; set; }

        public List<CafSubTableVm> SubTables { get; set; } = new();
    }

    /// <summary>Everything the Application Preview (6th tab) needs.</summary>
    public class CafPreviewViewModel
    {
        public List<CafStepDef> AllSteps { get; set; } = new();
        public List<CafPreviewSectionVm> Sections { get; set; } = new();

        public bool BasicDetailsExist { get; set; }

        /// <summary>statuss on caf_basic_details is no longer 'P' (pending) — the form is read-only.</summary>
        public bool IsSubmitted { get; set; }

        /// <summary>True only for the applicant's own preview before submission: shows the per-section Edit links. Officers' read-only views leave it false.</summary>
        public bool AllowEdit { get; set; }

        /// <summary>Officers' views: every section starts collapsed and opens when its heading is clicked. The applicant's own preview leaves it false (all sections open).</summary>
        public bool Collapsible { get; set; }

        /// <summary>Set for the single request right after a successful submit, to show the success alert.</summary>
        public bool JustSubmitted { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Every step has a saved main row, so the application can be submitted.</summary>
        public bool IsComplete => Sections.Count > 0 && Sections.All(s => s.HasRow);

        /// <summary>Step numbers that have a saved main row (same meaning as CafStepViewModel.SavedSteps).</summary>
        public HashSet<int> SavedSteps => Sections.Where(s => s.HasRow).Select(s => s.Step.Number).ToHashSet();

        public List<string> IncompleteSections =>
            Sections.Where(s => !s.HasRow).Select(s => s.Step.Title).ToList();
    }
}
