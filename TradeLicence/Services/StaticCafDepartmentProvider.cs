using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;

namespace TradeLicence.Services
{
    /// <summary>
    /// TEMPORARY department list. Replace with a master-table-backed ICafDepartmentProvider.
    /// The names must equal Officers.Department for that department's officers.
    /// </summary>
    public class StaticCafDepartmentProvider : ICafDepartmentProvider
    {
        private static readonly List<string> Departments = new()
        {
            CafWorkflowConfig.TownAndCountryPlanningDepartment,
            "Factory & Boiler",
            "Electricity",
            "Science & Technology",
            "Water",
            CafWorkflowConfig.PlanningAuthorityDepartment,
            CafWorkflowConfig.RevenueDepartment
        };

        public Task<List<string>> GetForwardableDepartmentsAsync() => Task.FromResult(new List<string>(Departments));
    }
}
