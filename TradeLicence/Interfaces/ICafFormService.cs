using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Models.Caf;

namespace TradeLicence.Interfaces
{
    public interface ICafFormService
    {
        /// <summary>True once the applicant has saved Step 1 at least once.</summary>
        Task<bool> BasicDetailsExistAsync(long loginId);

        /// <summary>
        /// Step numbers whose main row has been saved for this applicant — one batched query, used for the
        /// stepper's green ticks and for "resume where you left off".
        /// </summary>
        Task<HashSet<int>> GetSavedStepNumbersAsync(IEnumerable<CafStepDef> steps, long loginId);

        /// <summary>
        /// Submitted CAFs (caf_basic_details.statuss = 'S') as dashboard lines. Pass loginIds to fetch only those.
        /// Read-only helper for the officer dashboards — no new table.
        /// </summary>
        Task<List<CafSubmittedSummary>> GetSubmittedSummariesAsync(IEnumerable<long>? loginIds = null);

        /// <summary>
        /// Everything the Application Preview shows, for ANY applicant's CAF (the citizen's own preview and
        /// the officers' read-only view share this). Uses only the existing tables.
        /// </summary>
        /// <param name="stepKeys">
        /// Null = every step (the applicant's own preview and the Industry officer's full view). A receiving
        /// department's officer passes only the keys it may see (CafDepartmentSections.StepKeysFor), so the other
        /// departments' data is never even read.
        /// </param>
        Task<CafPreviewViewModel> LoadPreviewAsync(long loginId, IReadOnlyCollection<string>? stepKeys = null);

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
        /// <summary>caf_basic_details.statuss for this applicant: 'P' = pending (still editable), 'S' = submitted, null = nothing saved yet. Anything other than null/'P' means the form is locked.</summary>
        Task<string?> GetStatusAsync(long loginId);

        /// <summary>
        /// Marks the whole application Submitted (caf_basic_details.statuss = 'S'). Called from the
        /// Application Preview's Submit button. Only acts on a still-pending application, so a double
        /// click or a replayed request can't re-submit; returns false when nothing was updated.
        /// </summary>
        Task<bool> MarkSubmittedAsync(long loginId);

        /// <summary>
        /// Industry officer's "Return to Applicant": reopens a submitted CAF for editing (statuss 'S' -> 'P').
        /// Only acts on a submitted CAF; the applicant edits and submits again. Returns false when nothing changed.
        /// </summary>
        Task<bool> ReturnToApplicantAsync(long loginId);
    }
}
