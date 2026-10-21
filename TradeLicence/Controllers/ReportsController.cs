using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// The officer portal's Reports section — one page per Reports/* item in
    /// OfficerMenuProvider. Every action renders the same view
    /// (Views/Reports/Index.cshtml); only the report title, the description
    /// and which slice of the register the detail table lists change.
    ///
    /// All the data comes from IOfficerRegisterService, so a report can never
    /// show an application the officer's own dashboard wouldn't show either.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOfficerRegisterService _register;

        public ReportsController(ApplicationDbContext context, IOfficerRegisterService register)
        {
            _context = context;
            _register = register;
        }

        // The officer's own id is set as ClaimTypes.NameIdentifier at officer
        // login (see AccountController.OfficerLogin).
        private int GetCurrentOfficerId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        [HttpGet]
        public Task<IActionResult> Index() =>
            ReportAsync("all", "Reports",
                "Your complete application portfolio — every stage and status at a glance.");

        [HttpGet]
        public Task<IActionResult> DailyScrutiny() =>
            ReportAsync("today", "Daily Scrutiny Report",
                "Applications that reached your desk today.");

        [HttpGet]
        public Task<IActionResult> PendingScrutiny() =>
            ReportAsync("scrutiny", "Pending Applications Report",
                "Applications still awaiting initial scrutiny.");

        [HttpGet]
        public Task<IActionResult> VerificationSummary() =>
            ReportAsync("verification", "Verification Summary",
                "Applications currently at the Verification stage.");

        [HttpGet]
        public Task<IActionResult> InspectionSummary() =>
            ReportAsync("inspections", "Inspection Summary",
                "Applications currently at the Inspection stage.");

        [HttpGet]
        public Task<IActionResult> ApprovalSummary() =>
            ReportAsync("approved", "Approval Summary",
                "Applications that have been approved.");

        [HttpGet]
        public Task<IActionResult> WorkflowSummary() =>
            ReportAsync("all", "Overall Workflow Report",
                "Stage-by-stage view of every application in your service.");

        private async Task<IActionResult> ReportAsync(string kind, string title, string subtitle)
        {
            var officer = await _context.Officers.FindAsync(GetCurrentOfficerId());
            if (officer == null) return Forbid();

            var register = await _register.GetRegisterAsync(officer);
            var rows = await _register.GetFilteredAsync(officer, kind);

            foreach (var row in rows)
                row.ViewUrl = OfficerViewLink.Resolve(row, Url);

            var model = new OfficerReportViewModel
            {
                Title = title,
                Subtitle = subtitle,
                OfficerName = officer.FullName ?? officer.Username,
                Designation = officer.Designation ?? string.Empty,
                Department = officer.Department ?? string.Empty,
                GeneratedOn = DateTime.Now,
                Stats = OfficerRegisterService.ComputeStats(register),
                Rows = rows,
                ByStatus = CountBy(register, i => string.IsNullOrWhiteSpace(i.Status) ? "Unknown" : i.Status),
                ByStage = CountBy(register, i => string.IsNullOrWhiteSpace(i.CurrentStage) ? "—" : i.CurrentStage),
                ByService = CountBy(register, i => string.IsNullOrWhiteSpace(i.ServiceType) ? "—" : i.ServiceType)
            };

            return View("Index", model);
        }

        private static List<ReportCount> CountBy(
            IEnumerable<OfficerQueueItem> items, Func<OfficerQueueItem, string> keySelector) =>
            items
                .GroupBy(keySelector)
                .Select(g => new ReportCount { Label = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Label)
                .ToList();
    }
}
