using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using WaterConnection.Data;

namespace TradeLicence.Services
{
    /// <summary>
    /// The officer's whole "register" — every application of the service(s)
    /// their department manages, whatever its status — plus the status
    /// breakdown shown as stat cards on the dashboard and the reports.
    ///
    /// Why this exists: the dashboard queue (OfficerController.Index) only
    /// lists what an officer can act on RIGHT NOW. The nav-menu pages
    /// (All / Assigned / Forwarded / History / Approvals / Inspections /
    /// Inspection Schedule) and the Reports pages need the completed
    /// (Approved/Rejected/Returned) rows as well, and they all need the same
    /// department routing — so that routing lives here once, mirroring
    /// OfficerController.Index's service-per-department branch order.
    /// </summary>
    public interface IOfficerRegisterService
    {
        /// <summary>Every application of the officer's service(s), newest first, any status.</summary>
        Task<List<OfficerQueueItem>> GetRegisterAsync(Officer officer);

        /// <summary>Status breakdown of the officer's whole register (dashboard / report stat cards).</summary>
        Task<OfficerQueueStats> GetStatsAsync(Officer officer);

        /// <summary>
        /// Register rows matching one filter — "all", "assigned", "forwarded",
        /// "history", "scrutiny", "verification", "inspections", "approvals",
        /// "approved", "rejected", "returned", "today".
        /// </summary>
        Task<List<OfficerQueueItem>> GetFilteredAsync(Officer officer, string kind);
    }

    public class OfficerRegisterService : IOfficerRegisterService
    {
        private readonly ApplicationDbContext _context;
        private readonly WaterApplicationDbContext _waterContext;
        private readonly ElectricityApplicationDbContext _electricityContext;
        private readonly ICafDepartmentProvider _cafDepartments;
        private readonly ICafFormService _cafService;

        public OfficerRegisterService(
            ApplicationDbContext context,
            WaterApplicationDbContext waterContext,
            ElectricityApplicationDbContext electricityContext,
            ICafDepartmentProvider cafDepartments,
            ICafFormService cafService)
        {
            _context = context;
            _waterContext = waterContext;
            _electricityContext = electricityContext;
            _cafDepartments = cafDepartments;
            _cafService = cafService;
        }

        // ---------------- Register ----------------

        public async Task<List<OfficerQueueItem>> GetRegisterAsync(Officer officer)
        {
            var items = new List<OfficerQueueItem>();
            var isAdmin = string.Equals(officer.Designation, "Admin", StringComparison.OrdinalIgnoreCase);

            // An Administrator covers every service this portal has officer
            // pages for, so their "All Applications" spans Trade Licence +
            // their own department's service.
            if (isAdmin) items.AddRange(await BuildTradeLicenceAsync());

            // Industry department officers work CAF through their own pages
            // (AccountController.OfficerLogin sends them to CafOfficer) — the
            // same branch OfficerController.Index takes.
            if (string.Equals(officer.Department, CafWorkflowConfig.IndustryDepartment, StringComparison.OrdinalIgnoreCase))
                return NewestFirst(items);

            if (string.Equals(officer.Department, "Water", StringComparison.OrdinalIgnoreCase))
            {
                items.AddRange(await BuildWaterAsync());
                items.AddRange(await BuildCafAsync(officer));
                return NewestFirst(items);
            }

            if (string.Equals(officer.Department, "Electricity", StringComparison.OrdinalIgnoreCase))
            {
                items.AddRange(await BuildElectricityAsync());
                items.AddRange(await BuildCafAsync(officer));
                return NewestFirst(items);
            }

            if (string.Equals(officer.Department, FireWorkflowConfig.Department, StringComparison.OrdinalIgnoreCase))
            {
                items.AddRange(await BuildFireAsync());
                return NewestFirst(items);
            }

            // Any other department CAF can be forwarded to — their register is
            // the CAFs forwarded to them (visible queue; there is no separate
            // "every CAF of this department" query in the shared engine).
            var cafDepartmentNames = await _cafDepartments.GetForwardableDepartmentsAsync();
            if (cafDepartmentNames.Any(d => string.Equals(d, officer.Department, StringComparison.OrdinalIgnoreCase)))
            {
                items.AddRange(await BuildCafAsync(officer));
                return NewestFirst(items);
            }

            // Trade Licence department (and anything unmapped) — the same
            // fallback OfficerController.Index lands on.
            if (!isAdmin) items.AddRange(await BuildTradeLicenceAsync());

            return NewestFirst(items);
        }

        private static List<OfficerQueueItem> NewestFirst(List<OfficerQueueItem> items) =>
            items.OrderByDescending(i => i.SubmittedDate).ThenByDescending(i => i.ApplicationId).ToList();

        private async Task<List<OfficerQueueItem>> BuildTradeLicenceAsync()
        {
            var apps = await _context.TradeLicenceApplications
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.ApplicationId,
                ServiceType = "Trade Licence",
                // Trade Licence keeps its own history table, so it has no
                // WorkflowHistories.ServiceType (null keys the two apart).
                HistoryServiceType = null,
                DisplayName = a.ApplicantName ?? "-",
                Contact = a.MobileNumber ?? "-",
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                AssignedOfficerId = a.AssignedOfficerId,
                SubmittedDate = a.CreatedDate
            }).ToList();
        }

        private async Task<List<OfficerQueueItem>> BuildFireAsync()
        {
            var apps = await _context.FireClearanceApplications
                // Unsubmitted drafts stay out of the officer register.
                .Where(a => a.Status == null || a.Status != "Draft")
                .OrderByDescending(a => a.Id)
                .ToListAsync();

            return apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.Id,
                ServiceType = "Fire NOC",
                HistoryServiceType = FireWorkflowConfig.ServiceType,
                DisplayName = a.Name ?? "-",
                Contact = a.MobileNo ?? "-",
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                AssignedOfficerId = a.AssignedOfficerId,
                SubmittedDate = a.SubmittedDate ?? a.ModifiedDate ?? DateTime.UtcNow
            }).ToList();
        }

        private async Task<List<OfficerQueueItem>> BuildWaterAsync()
        {
            var apps = await _waterContext.WaterConnectionApplications
                .OrderByDescending(a => a.ApplicationId)
                .ToListAsync();

            return apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.ApplicationId,
                ServiceType = "Water",
                HistoryServiceType = "Water",
                DisplayName = a.Name,
                Contact = a.PhoneNumber,
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                AssignedOfficerId = a.AssignedOfficerId,
                SubmittedDate = a.ApplicationDate
            }).ToList();
        }

        private async Task<List<OfficerQueueItem>> BuildElectricityAsync()
        {
            var apps = await _electricityContext.EBapplications
                .OrderByDescending(a => a.ApplicationId)
                .ToListAsync();

            return apps.Select(a => new OfficerQueueItem
            {
                ApplicationId = a.ApplicationId,
                ServiceType = "Electricity",
                HistoryServiceType = "Electricity",
                DisplayName = a.ApplicantName ?? "-",
                Contact = a.MobileNumber ?? "-",
                CurrentStage = a.CurrentStage,
                Status = a.Status,
                AssignedOfficerId = a.AssignedOfficerId,
                SubmittedDate = a.CreatedDate ?? DateTime.UtcNow
            }).ToList();
        }

        // Same visibility rule and display-name lookup OfficerController's
        // dashboard queue uses for CAFs forwarded to a department.
        private async Task<List<OfficerQueueItem>> BuildCafAsync(Officer officer)
        {
            var engine = new WorkflowEngineService<CafDepartmentApplication>(
                _context, _context, CafWorkflowConfig.ServiceType);

            var apps = (await engine.GetOfficerQueueAsync(officer.Designation, officer.OfficerId))
                .Where(a => string.Equals(a.Department, officer.Department, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (apps.Count == 0) return new List<OfficerQueueItem>();

            var summaries = (await _cafService.GetSubmittedSummariesAsync(apps.Select(a => a.LoginId)))
                .ToDictionary(s => s.LoginId);

            return apps.Select(a =>
            {
                summaries.TryGetValue(a.LoginId, out var s);
                return new OfficerQueueItem
                {
                    ApplicationId = a.Id,
                    ServiceType = "CAF",
                    HistoryServiceType = CafWorkflowConfig.ServiceType,
                    DisplayName = s?.UnitName ?? s?.PromoterName ?? "-",
                    Contact = s?.Mobile ?? "-",
                    CurrentStage = a.CurrentStage,
                    Status = a.Status,
                    AssignedOfficerId = a.AssignedOfficerId,
                    SubmittedDate = a.ForwardedDate
                };
            }).ToList();
        }

        // ---------------- Stats ----------------

        public async Task<OfficerQueueStats> GetStatsAsync(Officer officer) =>
            ComputeStats(await GetRegisterAsync(officer));

        /// <summary>
        /// Status breakdown used by the dashboard/report stat cards. "Submitted"
        /// is split by stage: still at Initial Scrutiny = Pending, anywhere
        /// later = In Progress (the same distinction the citizen-side status
        /// page makes).
        /// </summary>
        public static OfficerQueueStats ComputeStats(IEnumerable<OfficerQueueItem> items)
        {
            var stats = new OfficerQueueStats();

            foreach (var item in items)
            {
                stats.Total++;

                var status = item.Status ?? string.Empty;
                if (Is(status, "Draft")) { stats.Draft++; }
                else if (Is(status, "Approved")) { stats.Approved++; }
                else if (Is(status, "Rejected")) { stats.Rejected++; }
                else if (Is(status, "ReturnedToApplicant")) { stats.Returned++; }
                else if (Is(status, "Submitted"))
                {
                    if (Is(item.CurrentStage, "Initial Scrutiny")) stats.Pending++;
                    else stats.InProgress++;
                }
            }

            return stats;
        }

        // ---------------- Filtered views ----------------

        public async Task<List<OfficerQueueItem>> GetFilteredAsync(Officer officer, string kind)
        {
            var register = await GetRegisterAsync(officer);

            IEnumerable<OfficerQueueItem> rows = (kind ?? "all").Trim().ToLowerInvariant() switch
            {
                "assigned" => register.Where(i => i.AssignedOfficerId == officer.OfficerId),
                "scrutiny" => register.Where(i => Is(i.Status, "Submitted") && Is(i.CurrentStage, "Initial Scrutiny")),
                "verification" => register.Where(i => Is(i.Status, "Submitted") && Is(i.CurrentStage, "Verification")),
                "inspections" => register.Where(i => Is(i.Status, "Submitted") && Is(i.CurrentStage, "Inspection")),
                "approvals" => register.Where(i => Is(i.Status, "Submitted") && Is(i.CurrentStage, "Approval")),
                "approved" => register.Where(i => Is(i.Status, "Approved")),
                "rejected" => register.Where(i => Is(i.Status, "Rejected")),
                "returned" => register.Where(i => Is(i.Status, "ReturnedToApplicant")),
                "today" => register.Where(i => i.SubmittedDate.Date == DateTime.Today),
                "forwarded" or "history" => await BuildHandledAsync(officer, register, fromMeOnly: kind == "forwarded"),
                _ => register
            };

            // An inspection schedule reads best oldest-first ("what's next"),
            // everything else newest-first.
            var list = (kind ?? "").Trim().ToLowerInvariant() == "inspections"
                ? rows.OrderBy(i => i.SubmittedDate).ToList()
                : rows.OrderByDescending(i => i.SubmittedDate).ThenByDescending(i => i.ApplicationId).ToList();

            return list;
        }

        // Rows this officer has handled, across BOTH history tables: Trade
        // Licence uses ApplicationWorkflowHistories (keyed by application id
        // alone), every engine service uses WorkflowHistories (keyed by
        // service type + application id).
        private async Task<List<OfficerQueueItem>> BuildHandledAsync(
            Officer officer, List<OfficerQueueItem> register, bool fromMeOnly)
        {
            var keys = new HashSet<(string? Service, int Id)>();

            var tradeLicenceIds = await _context.ApplicationWorkflowHistories
                .Where(h => h.FromOfficerId == officer.OfficerId
                    || (!fromMeOnly && h.ToOfficerId == officer.OfficerId))
                .Select(h => h.ApplicationId)
                .Distinct()
                .ToListAsync();

            foreach (var id in tradeLicenceIds) keys.Add((null, id));

            var sharedRows = await _context.WorkflowHistories
                .Where(h => h.FromOfficerId == officer.OfficerId
                    || (!fromMeOnly && h.ToOfficerId == officer.OfficerId))
                .Select(h => new { h.ServiceType, h.ApplicationId })
                .Distinct()
                .ToListAsync();

            foreach (var row in sharedRows) keys.Add((row.ServiceType, row.ApplicationId));

            return register
                .Where(i => keys.Contains((i.HistoryServiceType, i.ApplicationId)))
                .ToList();
        }

        private static bool Is(string? value, string expected) =>
            string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
