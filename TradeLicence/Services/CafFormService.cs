using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Interfaces;
using TradeLicence.Models.Caf;

namespace TradeLicence.Services
{
    /// <summary>
    /// Reads and writes the Common Application Form tables (dbo.caf_*) with
    /// plain parameterized ADO.NET instead of EF Core entities.
    ///
    /// Why not EF: CafFormMetadata drives the form dynamically from a column
    /// list (label/type per field), so every table here is addressed by
    /// column name at runtime rather than through a fixed C# class per
    /// table. EF entities would need one class + one DbSet + one
    /// OnModelCreating block per table for no benefit over this, and three
    /// of the tables (dbo.caf_towncountry_table, caf_factoryboiler_table,
    /// caf_electricity_table, caf_sciencetech_table) have no primary key in
    /// the supplied schema at all, which EF requires. This service uses the
    /// ApplicationDbContext purely to borrow its already-configured
    /// SqlConnection (same NewEODB database, same connection string) — it
    /// does not touch the EF change tracker.
    ///
    /// Every sub-table (the five dbo.caf_*_sub_table* tables) must have an
    /// identity "Id" primary key for AddSubRowAsync/DeleteSubRowAsync to
    /// address one row at a time — see Database/CAF_SchemaExtras.sql, which
    /// must be run once before this feature is used.
    /// </summary>
    public class CafFormService : ICafFormService
    {
        private readonly ApplicationDbContext _context;

        public CafFormService(ApplicationDbContext context)
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

        // ---------------- sub-table key self-check ----------------

        private static readonly string[] SubTableNames =
        {
            "caf_towncountry_sub_table1",
            "caf_factoryboiler_sub_table1",
            "caf_factoryboiler_sub_table2",
            "caf_factoryboiler_sub_table3",
            "caf_sciencetech_sub_table1"
        };

        private static volatile bool _subTableKeysChecked;
        private static readonly SemaphoreSlim _subTableKeysLock = new(1, 1);

        /// <summary>
        /// The Add/Delete-row feature needs an identity [Id] primary key on every
        /// dbo.caf_*_sub_table*. The original CREATE TABLE script has none, and
        /// without it every query that touches these tables fails with
        /// "Invalid column name 'Id'". This applies the same additive, idempotent
        /// change as Database/CAF_SchemaExtras.sql (part 1) once per app start, so
        /// a database where that script was never run still works. Needs ALTER
        /// permission for the connection-string user; if that is missing, run the
        /// script by hand.
        /// </summary>
        private static async Task EnsureSubTableKeysAsync(SqlConnection conn)
        {
            if (_subTableKeysChecked) return;
            await _subTableKeysLock.WaitAsync();
            try
            {
                if (_subTableKeysChecked) return;

                foreach (var t in SubTableNames)
                {
                    // Two separate commands: the PK statement can't be compiled in the
                    // same batch as the ALTER that creates the column it refers to.
                    using (var addCol = new SqlCommand(
                        $"IF COL_LENGTH('dbo.{t}', 'Id') IS NULL ALTER TABLE dbo.[{t}] ADD [Id] INT IDENTITY(1,1) NOT NULL;", conn))
                    {
                        await addCol.ExecuteNonQueryAsync();
                    }

                    using (var addPk = new SqlCommand(
                        $"IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('dbo.{t}') AND type = 'PK') " +
                        $"ALTER TABLE dbo.[{t}] ADD CONSTRAINT [PK_{t}_Id] PRIMARY KEY CLUSTERED ([Id]);", conn))
                    {
                        await addPk.ExecuteNonQueryAsync();
                    }
                }

                _subTableKeysChecked = true;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException(
                    "The CAF sub-tables are missing their identity [Id] key and the database user could not add it " +
                    "(" + ex.Message + "). Run Database/CAF_SchemaExtras.sql once against NewEODB.", ex);
            }
            finally
            {
                _subTableKeysLock.Release();
            }
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
                    // Select options may be encoded "value|label" (see unitcategory) — store only the value.
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
            new("@loginid", SqlDbType.Decimal) { Precision = 18, Scale = 0, Value = loginId };

        private static SqlParameter AppCodeParam(long loginId) =>
            new("@appcode", SqlDbType.Decimal) { Precision = 18, Scale = 0, Value = loginId };

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

        // ---------------- main (one row per applicant) tables ----------------

        public async Task<bool> BasicDetailsExistAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.caf_basic_details WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                var count = (int)(await cmd.ExecuteScalarAsync())!;
                return count > 0;
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<List<CafSubmittedSummary>> GetSubmittedSummariesAsync(IEnumerable<long>? loginIds = null)
        {
            var result = new List<CafSubmittedSummary>();
            var idList = loginIds?.Distinct().ToList();
            if (idList != null && idList.Count == 0) return result;

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand();
                cmd.Connection = conn;

                var sql = "SELECT [loginid], [indname], [promotername], [mobileno], [appliedon] " +
                          "FROM dbo.caf_basic_details WHERE [statuss] = 'S'";

                if (idList != null)
                {
                    var names = new List<string>();
                    for (var i = 0; i < idList.Count; i++)
                    {
                        var name = "@id" + i;
                        names.Add(name);
                        cmd.Parameters.Add(new SqlParameter(name, System.Data.SqlDbType.BigInt) { Value = idList[i] });
                    }
                    sql += " AND [loginid] IN (" + string.Join(",", names) + ")";
                }

                cmd.CommandText = sql + " ORDER BY [appliedon] DESC, [loginid] DESC";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string? mobile = null;
                    if (!reader.IsDBNull(3))
                    {
                        var raw = reader.GetValue(3);
                        mobile = decimal.TryParse(Convert.ToString(raw, System.Globalization.CultureInfo.InvariantCulture),
                                     System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var m)
                            ? m.ToString("0", System.Globalization.CultureInfo.InvariantCulture)
                            : Convert.ToString(raw);
                    }

                    result.Add(new CafSubmittedSummary
                    {
                        LoginId = Convert.ToInt64(reader.GetValue(0)),
                        UnitName = reader.IsDBNull(1) ? null : reader.GetString(1),
                        PromoterName = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Mobile = mobile,
                        AppliedOn = reader.IsDBNull(4) ? null : Convert.ToDateTime(reader.GetValue(4))
                    });
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }

            return result;
        }

        public async Task<CafPreviewViewModel> LoadPreviewAsync(long loginId)
        {
            var status = await GetStatusAsync(loginId);
            var steps = TradeLicence.Helpers.CafFormMetadata.Steps;

            var vm = new CafPreviewViewModel
            {
                AllSteps = steps,
                BasicDetailsExist = await BasicDetailsExistAsync(loginId),
                IsSubmitted = !string.IsNullOrEmpty(status) && status != "P"
            };
            vm.AllowEdit = !vm.IsSubmitted;

            foreach (var step in steps)
            {
                var section = new CafPreviewSectionVm
                {
                    Step = step,
                    Values = await LoadMainRowAsync(step, loginId)
                };
                section.HasRow = section.Values.Count > 0;

                foreach (var sub in step.SubTables)
                {
                    section.SubTables.Add(new CafSubTableVm
                    {
                        Def = sub,
                        Rows = await GetSubRowsAsync(sub, loginId)
                    });
                }

                vm.Sections.Add(section);
            }

            return vm;
        }

        public async Task<HashSet<int>> GetSavedStepNumbersAsync(IEnumerable<CafStepDef> steps, long loginId)
        {
            var saved = new HashSet<int>();
            var stepList = steps.ToList();
            if (stepList.Count == 0) return saved;

            // One round trip: "SELECT 1 WHERE EXISTS (...) UNION ALL SELECT 2 WHERE EXISTS (...) ..."
            // Table names come from CafFormMetadata (constants), never from user input.
            var selects = stepList.Select(s =>
                $"SELECT {s.Number} AS [StepNo] WHERE EXISTS (SELECT 1 FROM dbo.[{s.TableName}] WHERE [loginid] = @loginid)");

            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand(string.Join(" UNION ALL ", selects), conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    saved.Add(Convert.ToInt32(reader[0]));
            }
            finally { CloseIfWeOpenedIt(conn, opened); }

            return saved;
        }

        public async Task<Dictionary<string, string?>> LoadMainRowAsync(CafStepDef step, long loginId)
        {
            var result = new Dictionary<string, string?>();
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand($"SELECT * FROM dbo.[{step.TableName}] WHERE [loginid] = @loginid", conn);
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
                using (var existsCmd = new SqlCommand($"SELECT COUNT(1) FROM dbo.[{step.TableName}] WHERE [loginid] = @loginid", conn))
                {
                    existsCmd.Parameters.Add(LoginIdParam(loginId));
                    exists = (int)(await existsCmd.ExecuteScalarAsync())! > 0;
                }

                if (exists)
                {
                    var setClauses = step.Fields.Select((f, i) => $"[{f.Name}] = @f{i}");
                    var sql = $"UPDATE dbo.[{step.TableName}] SET {string.Join(", ", setClauses)} WHERE [loginid] = @loginid";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.Add(LoginIdParam(loginId));
                    for (int i = 0; i < step.Fields.Count; i++)
                        cmd.Parameters.Add(MakeParam($"@f{i}", step.Fields[i], postedValues.GetValueOrDefault(step.Fields[i].Name)));

                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    var columns = new List<string> { "[loginid]", "[appcode]" };
                    var paramNames = new List<string> { "@loginid", "@appcode" };

                    // First-ever save of Step 1 also stamps the workflow fields the
                    // applicant never edits directly: apptype/statuss/appliedon.
                    if (step.Key == "basic")
                    {
                        columns.AddRange(new[] { "[apptype]", "[statuss]", "[appliedon]" });
                        paramNames.AddRange(new[] { "@apptype", "@statuss", "@appliedon" });
                    }

                    for (int i = 0; i < step.Fields.Count; i++)
                    {
                        columns.Add($"[{step.Fields[i].Name}]");
                        paramNames.Add($"@f{i}");
                    }

                    var sql = $"INSERT INTO dbo.[{step.TableName}] ({string.Join(", ", columns)}) VALUES ({string.Join(", ", paramNames)})";

                    using var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.Add(LoginIdParam(loginId));
                    cmd.Parameters.Add(AppCodeParam(loginId));
                    if (step.Key == "basic")
                    {
                        cmd.Parameters.Add(new SqlParameter("@apptype", SqlDbType.NChar, 10) { Value = "CAF" });
                        cmd.Parameters.Add(new SqlParameter("@statuss", SqlDbType.Char, 1) { Value = "P" });
                        cmd.Parameters.Add(new SqlParameter("@appliedon", SqlDbType.Date) { Value = DateTime.Today });
                    }
                    for (int i = 0; i < step.Fields.Count; i++)
                        cmd.Parameters.Add(MakeParam($"@f{i}", step.Fields[i], postedValues.GetValueOrDefault(step.Fields[i].Name)));

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        // ---------------- sub-tables (many rows per applicant) ----------------

        public async Task<List<CafSubRowVm>> GetSubRowsAsync(CafSubTableDef def, long loginId)
        {
            var rows = new List<CafSubRowVm>();
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                await EnsureSubTableKeysAsync(conn);
                using var cmd = new SqlCommand($"SELECT * FROM dbo.[{def.TableName}] WHERE [loginid] = @loginid ORDER BY [Id]", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new CafSubRowVm { Id = Convert.ToInt64(reader["Id"]) };
                    foreach (var f in def.Fields)
                        row.Values[f.Name] = ReadCell(reader, f.Name);
                    rows.Add(row);
                }
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
            return rows;
        }

        public async Task<CafSubRowVm> AddSubRowAsync(CafSubTableDef def, long loginId, Dictionary<string, string?> postedValues)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                await EnsureSubTableKeysAsync(conn);
                var columns = new List<string> { "[loginid]", "[appcode]" };
                var paramNames = new List<string> { "@loginid", "@appcode" };
                for (int i = 0; i < def.Fields.Count; i++)
                {
                    columns.Add($"[{def.Fields[i].Name}]");
                    paramNames.Add($"@f{i}");
                }

                var sql = $"INSERT INTO dbo.[{def.TableName}] ({string.Join(", ", columns)}) OUTPUT INSERTED.[Id] VALUES ({string.Join(", ", paramNames)})";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                cmd.Parameters.Add(AppCodeParam(loginId));
                for (int i = 0; i < def.Fields.Count; i++)
                    cmd.Parameters.Add(MakeParam($"@f{i}", def.Fields[i], postedValues.GetValueOrDefault(def.Fields[i].Name)));

                var newId = (int)(await cmd.ExecuteScalarAsync())!;

                var row = new CafSubRowVm { Id = newId };
                foreach (var f in def.Fields)
                    row.Values[f.Name] = postedValues.GetValueOrDefault(f.Name);
                return row;
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<bool> DeleteSubRowAsync(CafSubTableDef def, long loginId, long rowId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                await EnsureSubTableKeysAsync(conn);
                using var cmd = new SqlCommand($"DELETE FROM dbo.[{def.TableName}] WHERE [Id] = @id AND [loginid] = @loginid", conn);
                cmd.Parameters.Add(new SqlParameter("@id", rowId));
                cmd.Parameters.Add(LoginIdParam(loginId));
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<string?> GetStatusAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                using var cmd = new SqlCommand("SELECT [statuss] FROM dbo.caf_basic_details WHERE [loginid] = @loginid", conn);
                cmd.Parameters.Add(LoginIdParam(loginId));

                var result = await cmd.ExecuteScalarAsync();
                if (result == null || result == DBNull.Value) return null;
                return result.ToString()?.Trim();
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }

        public async Task<bool> MarkSubmittedAsync(long loginId)
        {
            var (conn, opened) = await GetOpenConnectionAsync();
            try
            {
                // statuss: 'P' (pending) is set on the first save of Step 1. Only a still-pending
                // application can be submitted — a second call affects 0 rows and returns false.
                using var cmd = new SqlCommand(
                    "UPDATE dbo.caf_basic_details SET [statuss] = 'S', [appliedon] = COALESCE([appliedon], GETDATE()) " +
                    "WHERE [loginid] = @loginid AND ([statuss] IS NULL OR [statuss] = 'P')",
                    conn);
                cmd.Parameters.Add(LoginIdParam(loginId));
                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
            finally { CloseIfWeOpenedIt(conn, opened); }
        }
    }
}
