using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Models.Caf;

namespace TradeLicence.Interfaces
{
    public interface ICafFormService
    {
        /// <summary>True once the applicant has saved Step 1 at least once.</summary>
        Task<bool> BasicDetailsExistAsync(long loginId);

        /// <summary>Current values for every column of step.TableName for this applicant (empty dictionary if the row doesn't exist yet).</summary>
        Task<Dictionary<string, string?>> LoadMainRowAsync(CafStepDef step, long loginId);

        /// <summary>Insert-or-update the one row (keyed by loginid) that backs this step's main table.</summary>
        Task SaveMainRowAsync(CafStepDef step, long loginId, Dictionary<string, string?> postedValues);

        /// <summary>All saved rows of one sub-table for this applicant, in the order they were added.</summary>
        Task<List<CafSubRowVm>> GetSubRowsAsync(CafSubTableDef def, long loginId);

        /// <summary>Inserts one sub-table row and returns it back (with its new Id) so the UI can show it immediately.</summary>
        Task<CafSubRowVm> AddSubRowAsync(CafSubTableDef def, long loginId, Dictionary<string, string?> postedValues);

        /// <summary>Deletes one sub-table row, scoped to this applicant so one citizen can't delete another's row.</summary>
        Task<bool> DeleteSubRowAsync(CafSubTableDef def, long loginId, long rowId);

        /// <summary>Size in bytes of each document already uploaded (column name -> bytes). Columns with no file are absent.</summary>
        Task<Dictionary<string, long>> GetDocumentSizesAsync(long loginId);

        /// <summary>Saves the given documents (column name -> file bytes). Columns not in the dictionary keep whatever was uploaded before.</summary>
        Task SaveDocumentsAsync(long loginId, Dictionary<string, byte[]> files);

        /// <summary>The stored bytes of one document, or null if none was uploaded.</summary>
        Task<byte[]?> GetDocumentAsync(long loginId, string column);

        /// <summary>Marks the whole application Submitted (caf_basic_details.statuss = 'S') once the final step is saved.</summary>
        Task MarkSubmittedAsync(long loginId);
    }
}
