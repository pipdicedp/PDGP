using System.Collections.Generic;

namespace TradeLicence.Models.Caf
{
    /// <summary>
    /// One step of the Common Application Form as it appears on the read-only
    /// Preview screen: the step definition plus whatever the applicant has
    /// saved for it so far.
    /// Kept from the master branch; the shared CafPreviewViewModel and
    /// CafStepperVm come from CafModels.cs so there is only one definition of each.
    /// </summary>
    public class CafPreviewSection
    {
        public CafStepDef Step { get; set; } = null!;

        /// <summary>True once the step's main row (or, for documents, any upload) exists.</summary>
        public bool HasRow { get; set; }

        /// <summary>Saved values of the step's main row, keyed by CafField.Name (empty until saved).</summary>
        public Dictionary<string, string?> Values { get; set; } = new();

        /// <summary>Each child table of the step together with its saved rows.</summary>
        public List<CafSubTableVm> SubTables { get; set; } = new();
    }
}
