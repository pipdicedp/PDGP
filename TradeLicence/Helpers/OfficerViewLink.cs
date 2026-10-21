using Microsoft.AspNetCore.Mvc;
using TradeLicence.Models;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Maps an OfficerQueueItem to that service's own officer "View
    /// application" page. The shared register (OfficerRegisterService) is
    /// deliberately URL-free so it can be reused anywhere; whoever renders the
    /// rows turns them into links through here — currently OfficerController
    /// (nav-menu pages) and ReportsController (Reports pages).
    /// </summary>
    public static class OfficerViewLink
    {
        public static string Resolve(OfficerQueueItem item, IUrlHelper url) => item.ServiceType switch
        {
            "Trade Licence" => url.Action("ViewApplication", "Officer", new { id = item.ApplicationId }) ?? "#",
            "Fire NOC" => url.Action("ViewApplication", "FireOfficer", new { id = item.ApplicationId }) ?? "#",
            "Water" => url.Action("ViewApplication", "WaterOfficer", new { id = item.ApplicationId }) ?? "#",
            "Electricity" => url.Action("ViewApplication", "ElectricityOfficer", new { id = item.ApplicationId }) ?? "#",
            "CAF" => url.Action("ViewApplication", "CafDepartmentOfficer", new { id = item.ApplicationId }) ?? "#",
            _ => "#"
        };
    }
}
