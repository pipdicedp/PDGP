using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Interfaces;

namespace TradeLicence.Services
{
    /// <summary>
    /// The "Forward to department" list, read from the department master (dbo.eodbdeptmast): every department
    /// with linedept = 1. Replaces StaticCafDepartmentProvider — only the one DI line in Program.cs changes.
    ///
    /// Each name still has to equal Officers.Department for that department's officers (case doesn't matter),
    /// because that is how a forwarded CAF reaches the right queue — see Database/CAF_CheckDepartmentOfficers.sql.
    ///
    /// Only deptname is selected. The table also holds merchantid / merchantkey; nothing here ever reads them.
    /// </summary>
    public class DbCafDepartmentProvider : ICafDepartmentProvider
    {
        private readonly ApplicationDbContext _context;

        public DbCafDepartmentProvider(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetForwardableDepartmentsAsync()
        {
            // CAST so it works whether linedept is bit, int or char — the master is tiny, so index use doesn't matter.
            // The result column must be called "Value" for a scalar query.
            var names = await _context.Database
                .SqlQueryRaw<string>("SELECT deptname AS [Value] FROM dbo.eodbdeptmast WHERE CAST(linedept AS varchar(10)) = '1'")
                .ToListAsync();

            return names
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())                                   // CHAR columns come back space-padded
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
