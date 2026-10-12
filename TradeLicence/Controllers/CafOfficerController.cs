using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Models.Caf;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Industry department officers: review submitted Common Application Forms and forward each
    /// one to the department(s) they choose. A CAF enters a department's workflow only through
    /// Forward() below — submitting a CAF does not put it in any department's queue by itself.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class CafOfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICafFormService _cafService;
        private readonly ICafDepartmentProvider _departments;

        public CafOfficerController(ApplicationDbContext context, ICafFormService cafService, ICafDepartmentProvider departments)
        {
            _context = context;
            _cafService = cafService;
            _departments = departments;
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentOfficerId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : (int?)null;
        }

        // Only officers whose Department is the Industry department can use this controller.
        private async Task<Officer?> GetCurrentIndustryOfficerAsync()
        {
            var officerId = GetCurrentOfficerId();
            if (officerId == null) return null;

            var officer = await _context.Officers.FindAsync(officerId.Value);
            if (officer == null) return null;

            return string.Equals(officer.Department, CafWorkflowConfig.IndustryDepartment, StringComparison.OrdinalIgnoreCase)
                ? officer
                : null;
        }

        // ---------------- Dashboard ----------------

        [HttpGet]
        public async Task<IActionResult> Index(string? tab)
        {
            var officer = await GetCurrentIndustryOfficerAsync();
            if (officer == null) return Forbid();

            var submitted = await _cafService.GetSubmittedSummariesAsync();
            var loginIds = submitted.Select(s => s.LoginId).ToList();

            var forwards = loginIds.Count == 0
                ? new List<CafDepartmentApplication>()
                : await _context.CafDepartmentApplications
                    .AsNoTracking()
                    .Where(f => loginIds.Contains(f.LoginId))
                    .OrderBy(f => f.ForwardedDate)
                    .ToListAsync();

            var forwardsByLogin = forwards
                .GroupBy(f => f.LoginId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var items = submitted.Select(s => new CafIndustryQueueItem
            {
                Summary = s,
                Forwards = forwardsByLogin.TryGetValue(s.LoginId, out var list) ? list : new List<CafDepartmentApplication>()
            }).ToList();

            var vm = new CafIndustryDashboardVm
            {
                Tab = string.Equals(tab, "forwarded", StringComparison.OrdinalIgnoreCase) ? "forwarded" : "pending",
                Pending = items.Where(i => i.Forwards.Count == 0).ToList(),
                Forwarded = items.Where(i => i.Forwards.Count > 0).ToList()
            };

            ViewBag.Designation = officer.Designation;
            ViewBag.Department = officer.Department;
            return View(vm);
        }

        // ---------------- Full CAF preview + Officer Workflow ----------------

        [HttpGet]
        public async Task<IActionResult> ViewApplication(long id)
        {
            var officer = await GetCurrentIndustryOfficerAsync();
            if (officer == null) return Forbid();

            // only a submitted CAF can be reviewed
            var summary = (await _cafService.GetSubmittedSummariesAsync(new[] { id })).FirstOrDefault();
            if (summary == null) return NotFound();

            var preview = await _cafService.LoadPreviewAsync(id);
            preview.AllowEdit = false;
            preview.Collapsible = true;

            var forwards = await _context.CafDepartmentApplications
                .AsNoTracking()
                .Where(f => f.LoginId == id)
                .OrderBy(f => f.ForwardedDate)
                .ToListAsync();

            var officerIds = forwards
                .SelectMany(f => new int?[] { f.ForwardedByOfficerId, f.AssignedOfficerId })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList();

            var officerNames = officerIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Officers
                    .Where(o => officerIds.Contains(o.OfficerId))
                    .ToDictionaryAsync(o => o.OfficerId, o => o.FullName ?? o.Username);

            var allDepartments = await _departments.GetForwardableDepartmentsAsync();
            var available = allDepartments
                .Where(d => !forwards.Any(f => string.Equals(f.Department, d, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return View(new CafIndustryApplicationVm
            {
                Summary = summary,
                Preview = preview,
                Forwards = forwards,
                OfficerNames = officerNames,
                AvailableDepartments = available
            });
        }

        // ---------------- Forward to department(s) ----------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Forward(long id, List<string>? departments, string? remarks)
        {
            var officer = await GetCurrentIndustryOfficerAsync();
            if (officer == null) return Forbid();

            var summary = (await _cafService.GetSubmittedSummariesAsync(new[] { id })).FirstOrDefault();
            if (summary == null) return NotFound();

            var selected = (departments ?? new List<string>())
                .Select(d => d?.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(d => d!)
                .ToList();

            if (selected.Count == 0)
            {
                TempData["CafOfficerError"] = "Please select at least one department to forward the application to.";
                return RedirectToAction(nameof(ViewApplication), new { id });
            }

            // only departments from the dropdown list can be chosen — never trust the posted names
            var allowed = await _departments.GetForwardableDepartmentsAsync();
            var canonical = new List<string>();
            foreach (var name in selected)
            {
                var match = allowed.FirstOrDefault(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    TempData["CafOfficerError"] = $"'{name}' is not a department this application can be forwarded to.";
                    return RedirectToAction(nameof(ViewApplication), new { id });
                }
                canonical.Add(match);
            }

            var already = await _context.CafDepartmentApplications
                .Where(f => f.LoginId == id)
                .Select(f => f.Department)
                .ToListAsync();

            var duplicates = canonical
                .Where(d => already.Any(a => string.Equals(a, d, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (duplicates.Count > 0)
            {
                TempData["CafOfficerError"] = "Already forwarded to: " + string.Join(", ", duplicates) + ".";
                return RedirectToAction(nameof(ViewApplication), new { id });
            }

            var now = DateTime.UtcNow;
            foreach (var department in canonical)
            {
                _context.CafDepartmentApplications.Add(new CafDepartmentApplication
                {
                    LoginId = id,
                    UserId = id <= int.MaxValue ? (int?)id : null,
                    Department = department,
                    ForwardedByOfficerId = officer.OfficerId,
                    ForwardedDate = now,
                    ForwardRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
                    Status = "Submitted",
                    // starts at the first of the 4 stages, unassigned — it appears in that
                    // department's first-stage officers' queue and the workflow begins there
                    CurrentStage = OfficerWorkflow.Stages[0],
                    PaymentStatus = "NotRequested"
                });
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // e.g. two officers forwarded to the same department at the same moment
                TempData["CafOfficerError"] = "Could not forward the application — it may already have been forwarded. Please review and try again.";
                return RedirectToAction(nameof(ViewApplication), new { id });
            }

            TempData["OfficerActionMessage"] = "Application forwarded to " + string.Join(", ", canonical) + ".";
            TempData["OfficerActionType"] = "forward";
            return RedirectToAction(nameof(Index), new { tab = "forwarded" });
        }
    }
}
