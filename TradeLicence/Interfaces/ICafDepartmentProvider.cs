using System.Collections.Generic;
using System.Threading.Tasks;

namespace TradeLicence.Interfaces
{
    /// <summary>
    /// Where the "forward to department" dropdown gets its list from. Each name must match the
    /// Officers.Department value of that department's officers exactly — that is how a forwarded
    /// CAF reaches the right queue.
    ///
    /// Today: StaticCafDepartmentProvider (a fixed list). When the department master table is
    /// ready, write a second implementation that reads it and change the single DI line in
    /// Program.cs — no controller or view changes needed.
    /// </summary>
    public interface ICafDepartmentProvider
    {
        Task<List<string>> GetForwardableDepartmentsAsync();
    }
}
