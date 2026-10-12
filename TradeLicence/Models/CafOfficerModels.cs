using System;
using System.Collections.Generic;
using System.Linq;

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
        public CafIndustryReview? Review { get; set; }
    }

    public class CafIndustryDashboardVm
    {
        public string Tab { get; set; } = "pending";
        public List<CafIndustryQueueItem> Pending { get; set; } = new();
        public List<CafIndustryQueueItem> Forwarded { get; set; } = new();
        public List<CafIndustryQueueItem> Decided { get; set; } = new();
    }

    /// <summary>Industry officer's "View" page: full CAF preview + forwarding.</summary>
    public class CafIndustryApplicationVm
    {
        public CafSubmittedSummary Summary { get; set; } = null!;
        public CafPreviewViewModel Preview { get; set; } = null!;
        public List<CafDepartmentApplication> Forwards { get; set; } = new();
        public Dictionary<int, string> OfficerNames { get; set; } = new();
        public List<string> AvailableDepartments { get; set; } = new();

        public CafIndustryReview? Review { get; set; }

        /// <summary>Approved or Rejected — the Industry officer's decision is final and the actions are hidden.</summary>
        public bool IsDecided => Review?.Status == "Approved" || Review?.Status == "Rejected";

        /// <summary>A forwarded department is still working on it.</summary>
        public bool DepartmentsInProgress => Forwards.Any(f => f.Status == "Submitted");

        public bool CanForward => !IsDecided && AvailableDepartments.Count > 0;

        // Approve / Reject wait until every forwarded department has finished (Approved or Rejected).
        public bool CanDecide => !IsDecided && !DepartmentsInProgress;

        // Returning changes the form under the departments' feet, so it is only possible before forwarding.
        public bool CanReturn => !IsDecided && Forwards.Count == 0;
    }

    /// <summary>Receiving department's "View" page: full CAF preview + the 4-category workflow panel.</summary>
    public class CafDepartmentApplicationVm
    {
        public CafDepartmentApplication Application { get; set; } = null!;
        public CafPreviewViewModel Preview { get; set; } = null!;
        public CafSubmittedSummary? Summary { get; set; }
        public string? ForwardedByName { get; set; }
    }

    /// <summary>The applicant's "where is my CAF" card on the Application Preview page.</summary>
    public class CafApplicantStatusVm
    {
        /// <summary>info / success / danger / warning — picks the card colour.</summary>
        public string Tone { get; set; } = "info";
        public string Headline { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;

        /// <summary>Pending / Viewed / Forwarded / Approved / Rejected / Returned</summary>
        public string ReviewStatus { get; set; } = "Pending";

        public bool Returned { get; set; }
        public DateTime? ViewedDate { get; set; }
        public DateTime? DecisionDate { get; set; }
        public string? Remarks { get; set; }

        /// <summary>One entry per department the CAF was forwarded to, with that department's own stage / status.</summary>
        public List<CafDepartmentApplication> Forwards { get; set; } = new();
    }

    /// <summary>Per-department progress cards (stage tracker) — used by the applicant's status page and the Industry officer's workflow.</summary>
    public class CafDeptProgressVm
    {
        public List<CafDepartmentApplication> Forwards { get; set; } = new();

        /// <summary>Industry officer's view: also show who forwarded it and the forwarding remarks. The applicant never sees officer names.</summary>
        public bool ShowInternal { get; set; }

        public Dictionary<int, string> OfficerNames { get; set; } = new();
    }

    /// <summary>The applicant's separate "Track Status" page.</summary>
    public class CafTrackStatusVm
    {
        public long LoginId { get; set; }
        public bool IsSubmitted { get; set; }
        public CafSubmittedSummary? Summary { get; set; }
        public CafApplicantStatusVm? Status { get; set; }
    }
}
