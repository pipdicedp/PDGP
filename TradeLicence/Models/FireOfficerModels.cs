using System.Collections.Generic;
using TradeLicence.Models.Caf;

namespace TradeLicence.Models
{
    /// <summary>
    /// A Fire NOC application together with its read-only form rendering.
    /// Used by both the citizen's status page and the Fire officer's
    /// "View Application" page.
    /// </summary>
    public class FireApplicationVm
    {
        public FireClearanceApplication Application { get; set; } = null!;

        /// <summary>Read-only rendering of every wizard step (reuses the CAF preview model).</summary>
        public CafPreviewViewModel Preview { get; set; } = null!;

        /// <summary>Uploaded document sizes in bytes, keyed by document column name.</summary>
        public Dictionary<string, long> DocumentSizes { get; set; } = new();
    }
}
