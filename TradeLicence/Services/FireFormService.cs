using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Models.Caf;

namespace TradeLicence.Services
{
    /// <summary>
    /// Reads and writes the Fire NOC application tables
    /// (dbo.FireClearanceApplications / dbo.FireClearanceDocuments) with plain
    /// parameterized ADO.NET, exactly the way CafFormService does for the CAF.
    ///
    /// Unlike the CAF — where every step is its own table — every data-entry
    /// step here writes the same row, keyed by [loginid] (the citizen's UserId).
    /// The step's field list decides which columns that save touches, so
    /// "Save &amp; Next" on step 2 only updates step 2's columns.
    ///
    /// The ApplicationDbContext is borrowed purely for its already-configured
    /// SqlConnection (same NewEODB database, same connection string); this
    /// service does not touch the EF change tracker.
    /// </summary>
    public class FireFormService : IFireFormService
    {
        private readonly ApplicationDbContext _context;

        public FireFormService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------- connection helper ----------------

        private async Task<(SqlConnection conn, bool weOpenedIt)> GetOpenConnectionAsync()
        {
            var conn = (SqlConnection)_context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Open) return (conn, false);
            await conn.OpenAsync();
            return (conn, true);
        }

        private static void CloseIfWeOpenedIt(SqlConnection conn, bool weOpenedIt)
        {
            if (weOpenedIt) conn.Close();
        }

        // ---------------- value <-> SqlParameter conversion ----------------

        private static object BuildParamRawValue(CafField f, string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return DBNull.Value;
            raw = raw.Trim();

            switch (f.Type)
            {
                case CafFieldType.Number:
                    return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                        ? l : (object)DBNull.Value;

                case CafFieldType.Decimal:
                    return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
                        ? d : (object)DBNull.Value;

                case CafFieldType.Date:
                    return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        ? dt.Date : (object)DBNull.Value;

                default:
                    // Select options may be encoded "value|label" — store only the value.
                    var pipe = raw.IndexOf('|');
                    var value = pipe >= 0 ? raw[..pipe] : raw;
                    if (f.MaxLength.HasValue && value.Length > f.MaxLength.Value)
                        value = value[..f.MaxLength.Value];
                    return value;
            }
        }

        private static SqlParameter MakeParam(string name, CafField f, string? raw)
        {
            var value = BuildParamRawValue(f, raw);
            var p = new SqlParameter(name, value);
            if (f.Type == CafFieldType.Decimal)
            {
                // Without an explicit scale, SqlParameter uses scale 0 and silently
                // rounds 12.55 to 13. Match the NUMERIC(18,2) columns.
                p.SqlDbType = SqlDbType.Decimal;
                p.Precision = 18;
                p.Scale = 2;
            }
            return p;
        }

        private static SqlParameter LoginIdParam(long loginId) =>
            new("@loginid", SqlDbType.BigInt) { Value = loginId };

        private static string? ReadCell(SqlDataReader reader, string column)
        {
            var idx = reader.GetOrdinal(column);
            if (reader.IsDBNull(idx)) return null;

            var value = reader.GetValue(idx);
            return value switch
            {
                DateTime dt => dt.ToString("yyyy-MM-dd"),
                decimal dec => dec.ToString(CultureInfo.InvariantCulture),
                double dbl => dbl.ToString(CultureInfo.InvariantCulture),
                float flt => flt.ToString(CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        // ---------------- main (one row per applicant) ----------------

        public async Task<bool> BasicDetailsExistAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand(
                    $"SELECT COUNT(1) FROM dbo.[{FireFormMetadata.MainTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                var count = (int)(await cmd.ExecuteScalarAsync())!;
                return count > 0;
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<Dictionary<string, string?>> LoadMainRowAsync(CafStepDef step, long loginId)
        {
            var result = new Dictionary<string, string?>();
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand(
                    $"SELECT * FROM dbo.[{FireFormMetadata.MainTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    foreach (var f in step.Fields)
                        result[f.Name] = ReadCell(reader, f.Name);
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
            return result;
        }

        public async Task SaveMainRowAsync(CafStepDef step, long loginId, Dictionary<string, string?> postedValues)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                bool exists;
                using (var existsCmd = new SqlCommand(
                    $"SELECT COUNT(1) FROM dbo.[{FireFormMetadata.MainTable}] WHERE [loginid] = @loginid", conn))
                {
                    existsCmd.Parameters.Add(LoginIdParam(loginId));
                    exists = (int)(await existsCmd.ExecuteScalarAsync())! > 0;
                }

                if (exists)
                {
                    var setClauses = step.Fields.Select((f, i) => $"[{f.Name}] = @f{i}");
                    var sql = $"UPDATE dbo.[{FireFormMetadata.MainTable}] SET {string.Join(", ", setClauses)}, " +
                              "[modifieddate] = GETDATE() WHERE [loginid] = @loginid";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.Add(LoginIdParam(loginId));
                    for (int i = 0; i < step.Fields.Count; i++)
                        cmd.Parameters.Add(MakeParam($"@f{i}", step.Fields[i], postedValues.GetValueOrDefault(step.Fields[i].Name)));

                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var columns = new List<string> { "[loginid]" };
                    var paramNames = new List<string> { "@loginid" };

                    // The very first save (step 1) also stamps the workflow /
                    // service fields the applicant never edits directly.
                    if (step.Key == "applicant")
                    {
                        columns.AddRange(new[]
                        {
                            "[status]", "[currentstage]", "[paymentstatus]", "[userid]",
                            "[serviceid]", "[servicedes]", "[deptcode]", "[apptype]", "[appliedon]"
                        });
                        paramNames.AddRange(new[]
                        {
                            "@status", "@currentstage", "@paymentstatus", "@userid",
                            "@serviceid", "@servicedes", "@deptcode", "@apptype", "@appliedon"
                        });
                    }

                    for (int i = 0; i < step.Fields.Count; i++)
                    {
                        columns.Add($"[{step.Fields[i].Name}]");
                        paramNames.Add($"@f{i}");
                    }

                    // Every save stamps "last saved", which the draft summary on
                    // the applicant's status page shows.
                    columns.Add("[modifieddate]");
                    paramNames.Add("GETDATE()");

                    var sql = $"INSERT INTO dbo.[{FireFormMetadata.MainTable}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.Add(LoginIdParam(loginId));
                    if (step.Key == "applicant")
                    {
                        cmd.Parameters.Add(new SqlParameter("@status", SqlDbType.VarChar, 30) { Value = "Draft" });
                        cmd.Parameters.Add(new SqlParameter("@currentstage", SqlDbType.VarChar, 50) { Value = "Initial Scrutiny" });
                        cmd.Parameters.Add(new SqlParameter("@paymentstatus", SqlDbType.VarChar, 30) { Value = "NotRequested" });
                        cmd.Parameters.Add(new SqlParameter("@userid", SqlDbType.Int) { Value = (int)loginId });
                        cmd.Parameters.Add(new SqlParameter("@serviceid", SqlDbType.Int) { Value = FireWorkflowConfig.ServiceId });
                        cmd.Parameters.Add(new SqlParameter("@servicedes", SqlDbType.NVarChar, 150) { Value = FireWorkflowConfig.ServiceDescription });
                        cmd.Parameters.Add(new SqlParameter("@deptcode", SqlDbType.SmallInt) { Value = FireWorkflowConfig.DeptCode });
                        cmd.Parameters.Add(new SqlParameter("@apptype", SqlDbType.NVarChar, 20) { Value = "FIRE" });
                        cmd.Parameters.Add(new SqlParameter("@appliedon", SqlDbType.Date) { Value = DateTime.Today });
                    }
                    for (int i = 0; i < step.Fields.Count; i++)
                        cmd.Parameters.Add(MakeParam($"@f{i}", step.Fields[i], postedValues.GetValueOrDefault(step.Fields[i].Name)));

                    await cmd.ExecuteNonQueryAsync();
                }

                // dbo.FireClearanceApplications.[name] is the denormalised title the
                // officer queues and lists show. The department's form has no separate
                // applicant-name field — its "name" column *is* the premises name — so
                // keep the display column in step with what the applicant typed.
                if (step.Key == "applicant")
                {
                    using var syncName = new SqlCommand(
                        $"UPDATE dbo.[{FireFormMetadata.MainTable}] " +
                        "SET [name] = LEFT(COALESCE(NULLIF(LTRIM(RTRIM([premisesname])), ''), [name]), 150) " +
                        "WHERE [loginid] = @loginid",
                        conn);
                    syncName.Parameters.Add(LoginIdParam(loginId));
                    await syncName.ExecuteNonQueryAsync();
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        // ---------------- documents (dbo.FireClearanceDocuments) ----------------

        public async Task<Dictionary<string, long>> GetDocumentSizesAsync(long loginId)
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var cols = FireFormMetadata.AllDocuments.Select(d => d.Column).ToList();
            if (cols.Count == 0) return result;

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                var select = string.Join(", ", cols.Select(c => $"DATALENGTH([{c}]) AS [{c}]"));
                using var cmd = new SqlCommand(
                    $"SELECT {select} FROM dbo.[{FireFormMetadata.DocumentTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    foreach (var c in cols)
                    {
                        var idx = reader.GetOrdinal(c);
                        if (!reader.IsDBNull(idx))
                        {
                            var length = Convert.ToInt64(reader.GetValue(idx));
                            if (length > 0) result[c] = length;
                        }
                    }
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
            return result;
        }

        public async Task<Dictionary<string, string>> GetDocumentTypesAsync(long loginId)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var cols = FireFormMetadata.AllDocuments.Select(d => d.Column).ToList();
            if (cols.Count == 0) return result;

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                // Only the first 8 bytes are needed to recognise PDF / JPEG / PNG.
                var select = string.Join(", ", cols.Select(c => $"SUBSTRING([{c}], 1, 8) AS [{c}]"));
                using var cmd = new SqlCommand(
                    $"SELECT {select} FROM dbo.[{FireFormMetadata.DocumentTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    foreach (var c in cols)
                    {
                        var idx = reader.GetOrdinal(c);
                        if (reader.IsDBNull(idx)) continue;

                        var head = (byte[])reader.GetValue(idx);
                        var detected = EBDocumentHelper.DetectFile(head);
                        if (detected != null) result[c] = detected.Value.ContentType;
                    }
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
            return result;
        }

        public async Task SaveDocumentsAsync(long loginId, Dictionary<string, byte[]> files)
        {
            var cols = files.Keys
                .Select(c => FireFormMetadata.DocumentByColumn(c)?.Column)
                .Where(c => c != null)
                .Select(c => c!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (cols.Count == 0) return;

            SqlParameter FileParam(string col) =>
                new("@f_" + col, SqlDbType.VarBinary, -1)
                {
                    Value = files.First(kv => string.Equals(kv.Key, col, StringComparison.OrdinalIgnoreCase)).Value
                };

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                // 1) Row already exists -> update just the uploaded columns.
                var setList = string.Join(", ", cols.Select(c => $"[{c}] = @f_{c}"));
                using (var upd = new SqlCommand(
                    $"UPDATE dbo.[{FireFormMetadata.DocumentTable}] SET {setList}, [uploadedon] = GETDATE() WHERE [loginid] = @loginid", conn))
                {
                    foreach (var c in cols) upd.Parameters.Add(FileParam(c));
                    upd.Parameters.Add(LoginIdParam(loginId));

                    if (await upd.ExecuteNonQueryAsync() > 0) return;
                }

                // 2) First upload -> insert the row.
                var colList = string.Join(", ", cols.Select(c => $"[{c}]"));
                var valList = string.Join(", ", cols.Select(c => $"@f_{c}"));
                using var ins = new SqlCommand(
                    $"INSERT INTO dbo.[{FireFormMetadata.DocumentTable}] ([loginid], {colList}, [uploadedon]) VALUES (@loginid, {valList}, GETDATE())", conn);
                foreach (var c in cols) ins.Parameters.Add(FileParam(c));
                ins.Parameters.Add(LoginIdParam(loginId));
                await ins.ExecuteNonQueryAsync();
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<byte[]?> GetDocumentAsync(long loginId, string column)
        {
            var def = FireFormMetadata.DocumentByColumn(column);
            if (def == null) return null;

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand(
                    $"SELECT [{def.Column}] FROM dbo.[{FireFormMetadata.DocumentTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                var value = await cmd.ExecuteScalarAsync();
                return value as byte[];
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        // ---------------- submit ----------------

        public async Task MarkSubmittedAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                // A human-readable reference (FIRE/YYYY/NNNNN) is stamped the first
                // time the application is submitted, from the identity Id.
                using var cmd = new SqlCommand(
                    "UPDATE dbo.[" + FireFormMetadata.MainTable + "] " +
                    "SET [status] = 'Submitted', " +
                    "    [submitteddate] = COALESCE([submitteddate], GETDATE()), " +
                    "    [appliedon] = COALESCE([appliedon], CAST(GETDATE() AS date)), " +
                    "    [appcode] = COALESCE([appcode], 'FIRE/' + CAST(YEAR(GETDATE()) AS varchar(4)) + '/' + RIGHT('00000' + CAST([Id] AS varchar(10)), 5)) " +
                    "WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                await cmd.ExecuteNonQueryAsync();
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        // ---------------- read-only preview ----------------

        public async Task<CafPreviewViewModel> LoadPreviewAsync(long loginId)
        {
            var vm = new CafPreviewViewModel
            {
                AllSteps = FireFormMetadata.Steps,
                BasicDetailsExist = await BasicDetailsExistAsync(loginId),
                AllowEdit = true,
                Collapsible = false
            };

            var docSizes = await GetDocumentSizesAsync(loginId);

            foreach (var step in FireFormMetadata.Steps)
            {
                var section = new CafPreviewSection { Step = step };

                if (step.Key == "documents")
                {
                    section.HasRow = docSizes.Count > 0;
                }
                else
                {
                    section.Values = await LoadMainRowAsync(step, loginId);
                    section.HasRow = section.Values.Count > 0;
                }

                vm.Sections.Add(section);
                if (section.HasRow) vm.SavedSteps.Add(step.Number);
            }

            vm.IsComplete = vm.Sections.Count > 0 && vm.Sections.All(s => s.HasRow);
            vm.IsSubmitted = await IsSubmittedAsync(loginId);

            return vm;
        }

        public async Task<FireClearanceApplication?> GetApplicationAsync(long loginId) =>
            await _context.FireClearanceApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.LoginId == loginId);

        private async Task<bool> IsSubmittedAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand(
                    $"SELECT [status] FROM dbo.[{FireFormMetadata.MainTable}] WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                var value = await cmd.ExecuteScalarAsync();
                // Anything other than Draft means the citizen has submitted at least once.
                return value is string s && !string.Equals(s.Trim(), "Draft", StringComparison.OrdinalIgnoreCase);
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }
    }
}
