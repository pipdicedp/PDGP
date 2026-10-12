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
    /// Industry department officers: review submitted Common Application Forms and
    ///   - Forward each one to the department(s) they choose,
    ///   - Approve or Reject it,
    ///   - or Return it to the applicant for correction.
    /// A CAF enters a department's workflow only through Forward() — submitting a CAF does not
    /// put it in any department's queue by itself. Every step is recorded in CafIndustryReviews so
    /// the applicant can follow it on their Application Preview page.
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

        private static bool IsFinal(CafIndustryReview? review) =>
            review?.Status == "Approved" || review?.Status == "Rejected";

        // First time an Industry officer opens a (re)submitted CAF -> "Viewed", which the applicant can see.
        private async Task MarkViewedAsync(long loginId, int officerId)
        {
            var review = await _context.CafIndustryReviews.FirstOrDefaultAsync(r => r.LoginId == loginId);
            var now = DateTime.UtcNow;

            if (review == null)
            {
                _context.CafIndustryReviews.Add(new CafIndustryReview
                {
                    LoginId = loginId,
                    Status = "Viewed",
                    FirstViewedDate = now,
                    FirstViewedByOfficerId = officerId
                });
            }
            else if (review.Status == "Pending")
            {
                review.Status = "Viewed";
                review.FirstViewedDate = now;
                review.FirstViewedByOfficerId = officerId;
            }
            else
            {
                return; // already viewed / forwarded / decided
            }

            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException) { _context.ChangeTracker.Clear(); } // another officer opened it at the same moment
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

            var reviews = loginIds.Count == 0
                ? new List<CafIndustryReview>()
                : await _context.CafIndustryReviews
                    .AsNoTracking()
                    .Where(r => loginIds.Contains(r.LoginId))
                    .ToListAsync();

            var forwardsByLogin = forwards.GroupBy(f => f.LoginId).ToDictionary(g => g.Key, g => g.ToList());
            var reviewByLogin = reviews.ToDictionary(r => r.LoginId);

            var items = submitted.Select(s => new CafIndustryQueueItem
            {
                Summary = s,
                Forwards = forwardsByLogin.TryGetValue(s.LoginId, out var list) ? list : new List<CafDepartmentApplication>(),
                Review = reviewByLogin.TryGetValue(s.LoginId, out var review) ? review : null
            }).ToList();

            var normalizedTab = (tab ?? "").ToLowerInvariant() switch
            {
                "forwarded" => "forwarded",
                "decided" => "decided",
                _ => "pending"
            };

            var vm = new CafIndustryDashboardVm
            {
                Tab = normalizedTab,
                Decided = items.Where(i => IsFinal(i.Review)).ToList(),
                Forwarded = items.Where(i => !IsFinal(i.Review) && i.Forwards.Count > 0).ToList(),
                Pending = items.Where(i => !IsFinal(i.Review) && i.Forwards.Count == 0).ToList()
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

            await MarkViewedAsync(id, officer.OfficerId);

            var preview = await _cafService.LoadPreviewAsync(id);
            preview.AllowEdit = false;
            preview.Collapsible = true;

            var review = await _context.CafIndustryReviews.AsNoTracking().FirstOrDefaultAsync(r => r.LoginId == id);

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
                AvailableDepartments = available,
                Review = review
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

            var review = await _context.CafIndustryReviews.FirstOrDefaultAsync(r => r.LoginId == id);
            if (IsFinal(review))
            {
                TempData["CafOfficerError"] = $"This application has already been {review!.Status.ToLowerInvariant()}.";
                return RedirectToAction(nameof(ViewApplication), new { id });
            }

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

            // what the applicant sees: "Forwarded to ..."
            if (review == null)
            {
                review = new CafIndustryReview { LoginId = id, FirstViewedDate = now, FirstViewedByOfficerId = officer.OfficerId };
                _context.CafIndustryReviews.Add(review);
            }
            review.Status = "Forwarded";
            review.LastActionDate = now;
            review.LastActionByOfficerId = officer.OfficerId;

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

        // ---------------- Approve / Reject / Return to Applicant ----------------
        //
        // Rules (the page disables the buttons the same way; this is the real check):
        //  - Approve / Reject: only once no forwarded department is still working on it.
        //  - Return to Applicant: only before the CAF has been forwarded anywhere — returning it
        //    changes the form the departments are reviewing.
        //  - Reject / Return need remarks: the applicant sees them.
        //  - Approved / Rejected is final.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(long id, string? decision, string? remarks)
        {
            var officer = await GetCurrentIndustryOfficerAsync();
            if (officer == null) return Forbid();

            var summary = (await _cafService.GetSubmittedSummariesAsync(new[] { id })).FirstOrDefault();
            if (summary == null) return NotFound();

            decision = (decision ?? "").Trim().ToLowerInvariant();
            if (decision != "approve" && decision != "reject" && decision != "return")
                return BadRequest(new { error = "Unknown action." });

            remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
            if (remarks != null && remarks.Length > 1000) remarks = remarks[..1000];

            IActionResult Fail(string message)
            {
                TempData["CafOfficerError"] = message;
                return RedirectToAction(nameof(ViewApplication), new { id });
            }

            if ((decision == "reject" || decision == "return") && remarks == null)
                return Fail("Please enter remarks — the applicant will see them.");

            var review = await _context.CafIndustryReviews.FirstOrDefaultAsync(r => r.LoginId == id);
            if (IsFinal(review))
                return Fail($"This application has already been {review!.Status.ToLowerInvariant()}.");

            var forwards = await _context.CafDepartmentApplications
                .AsNoTracking()
                .Where(f => f.LoginId == id)
                .ToListAsync();

            if (decision == "return")
            {
                if (forwards.Count > 0)
                    return Fail("This application has already been forwarded to a department, so it can no longer be returned to the applicant.");
            }
            else if (forwards.Any(f => f.Status == "Submitted"))
            {
                return Fail("A forwarded department is still processing this application. Approve or Reject becomes available once every forwarded department has finished.");
            }

            var now = DateTime.UtcNow;
            if (review == null)
            {
                review = new CafIndustryReview { LoginId = id, FirstViewedDate = now, FirstViewedByOfficerId = officer.OfficerId };
                _context.CafIndustryReviews.Add(review);
            }

            if (decision == "return")
            {
                // reopen the form for editing (statuss S -> P) before recording the decision
                if (!await _cafService.ReturnToApplicantAsync(id))
                    return Fail("Could not return the application — it may have already been changed. Please refresh and try again.");
            }

            review.Status = decision == "approve" ? "Approved" : decision == "reject" ? "Rejected" : "Returned";
            review.LastActionDate = now;
            review.LastActionByOfficerId = officer.OfficerId;
            review.Remarks = remarks;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Fail("Could not save the decision. Please try again.");
            }

            switch (decision)
            {
                case "approve":
                    TempData["OfficerActionMessage"] = "Application approved.";
                    TempData["OfficerActionType"] = "approve";
                    return RedirectToAction(nameof(Index), new { tab = "decided" });
                case "reject":
                    TempData["OfficerActionMessage"] = "Application rejected.";
                    TempData["OfficerActionType"] = "reject";
                    return RedirectToAction(nameof(Index), new { tab = "decided" });
                default:
                    TempData["OfficerActionMessage"] = "Application returned to the applicant for correction.";
                    TempData["OfficerActionType"] = "return";
                    return RedirectToAction(nameof(Index), new { tab = "pending" });
            }
        }
    }
}
