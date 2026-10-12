using System;
using System.Collections.Generic;

namespace TradeLicence.Models.Caf
{
    /// <summary>A submitted CAF as one line for the officer dashboards (from caf_basic_details).</summary>
    public class CafSubmittedSummary
    {
        public long LoginId { get; set; }
        public string? UnitName { get; set; }
        public string? PromoterName { get; set; }
        public string? Mobile { get; set; }
        public DateTime? AppliedOn { get; set; }
    }

    public class CafIndustryQueueItem
    {
        public CafSubmittedSummary Summary { get; set; } = null!;
        public List<CafDepartmentApplication> Forwards { get; set; } = new();
    }

    public class CafIndustryDashboardVm
    {
        public string Tab { get; set; } = "pending";
        public List<CafIndustryQueueItem> Pending { get; set; } = new();
        public List<CafIndustryQueueItem> Forwarded { get; set; } = new();
    }

    /// <summary>Industry officer's "View" page: full CAF preview + forwarding.</summary>
    public class CafIndustryApplicationVm
    {
        public CafSubmittedSummary Summary { get; set; } = null!;
        public CafPreviewViewModel Preview { get; set; } = null!;
        public List<CafDepartmentApplication> Forwards { get; set; } = new();
        public Dictionary<int, string> OfficerNames { get; set; } = new();
        public List<string> AvailableDepartments { get; set; } = new();
    }

    /// <summary>Receiving department's "View" page: full CAF preview + the 4-category workflow panel.</summary>
    public class CafDepartmentApplicationVm
    {
        public CafDepartmentApplication Application { get; set; } = null!;
        public CafPreviewViewModel Preview { get; set; } = null!;
        public CafSubmittedSummary? Summary { get; set; }
        public string? ForwardedByName { get; set; }
    }
}
