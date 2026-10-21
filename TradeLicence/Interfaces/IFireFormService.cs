using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Models.Caf;

namespace TradeLicence.Interfaces
{
    /// <summary>
    /// Reads and writes the Fire NOC application tables
    /// (dbo.FireClearanceApplications / dbo.FireClearanceDocuments) with plain
    /// parameterized ADO.NET, exactly the way ICafFormService does for the CAF.
    ///
    /// The CAF form-descriptor models (CafField / CafStepDef / CafDocumentDef)
    /// are reused here on purpose: they describe "a metadata-driven form"
    /// generically (one table, a list of typed columns, optional child tables,
    /// optional document uploads) and carry nothing CAF-specific.
    /// </summary>
    public interface IFireFormService
    {
        /// <summary>True once the applicant has saved Step 1 at least once.</summary>
        Task<bool> BasicDetailsExistAsync(long loginId);

        /// <summary>Current values for every column of step.Fields, keyed by field name. Empty until saved.</summary>
        Task<Dictionary<string, string?>> LoadMainRowAsync(CafStepDef step, long loginId);

        /// <summary>Inserts the applicant's row on the first save, then updates it.</summary>
        Task SaveMainRowAsync(CafStepDef step, long loginId, Dictionary<string, string?> postedValues);

        /// <summary>Size in bytes of each document already uploaded (column name -> bytes).</summary>
        Task<Dictionary<string, long>> GetDocumentSizesAsync(long loginId);

        /// <summary>Upserts the given documents (column name -> file bytes).</summary>
        Task SaveDocumentsAsync(long loginId, Dictionary<string, byte[]> files);

        /// <summary>The stored bytes of one document, or null.</summary>
        Task<byte[]?> GetDocumentAsync(long loginId, string column);

        /// <summary>Marks the application Submitted and stamps the reference number.</summary>
        Task MarkSubmittedAsync(long loginId);

        /// <summary>Read-only view of everything the applicant has saved, for Preview and officer review.</summary>
        Task<CafPreviewViewModel> LoadPreviewAsync(long loginId);

        /// <summary>Convenience: the application row for a loginid, or null.</summary>
        Task<Models.FireClearanceApplication?> GetApplicationAsync(long loginId);
    }
}
