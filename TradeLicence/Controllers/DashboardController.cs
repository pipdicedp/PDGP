using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Dashboard/SSOLandingPage
        // This is what a citizen now sees immediately after a successful login,
        // in place of the old Application-Status-Tracking landing spot. It hosts
        // the left-hand "Services" hamburger menu (EODB website + Other Services).
        [HttpGet]
        public IActionResult SSOLandingPage()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.TryParse(userIdClaim, out var id) ? id : (int?)null;

            var myApplications = userId == null
                ? new List<TradeLicenceApplication>()
                : await _context.TradeLicenceApplications
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

            ViewBag.MyApplications = myApplications;

            // No strongly-typed Model needed — your Index.cshtml doesn't use @model,
            // it hardcodes the 7 dashboard cards directly in the markup.
            return View();
        }

        // GET: /Dashboard/Status
        // Powers the new Screenshot(112)-style "Application Status Tracking" page —
        // status summary cards + searchable table, same colour theme as Index.
        [HttpGet]
        public async Task<IActionResult> Status()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.TryParse(userIdClaim, out var id) ? id : (int?)null;
            var loginIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var loginId = long.TryParse(loginIdClaim, out var lid) ? lid : (long?)null;

            var items = new List<ApplicationStatusItem>();

            if (userId.HasValue)
            {
                var tradeApps = await _context.TradeLicenceApplications
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

                foreach (var a in tradeApps)
                {
                    var isDraft = string.Equals(a.Status, "Draft", StringComparison.OrdinalIgnoreCase);
                    var isReturned = string.Equals(a.Status, "ReturnedToApplicant", StringComparison.OrdinalIgnoreCase);
                    items.Add(new ApplicationStatusItem
                    {
                        Service = "Trade Licence",
                        ApplicationId = a.ApplicationNumber ?? a.ApplicationId.ToString(),
                        InternalId = a.ApplicationId,
                        ApplicantName = a.ApplicantName ?? string.Empty,
                        Purpose = a.PurposeOfLicence ?? "Trade Licence",
                        DateOfApplication = a.CreatedDate,
                        Status = a.Status ?? string.Empty,
                        CurrentStage = a.CurrentStage ?? string.Empty,
                        PaymentStatus = a.PaymentStatus ?? string.Empty,
                        CanContinue = isDraft || isReturned,
                        CanPayNow = string.Equals(a.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase) && !isDraft,
                        CanResubmit = isReturned,
                        ViewUrl = Url.Action("Confirmation", "TradeLicence", new { id = a.ApplicationId }) ?? string.Empty
                    });
                }
            }

            if (loginId.HasValue)
            {
                var fireApps = await _context.FireClearanceApplications
                    .Where(a => a.LoginId == loginId)
                    .OrderByDescending(a => a.SubmittedDate ?? DateTime.MinValue)
                    .ToListAsync();

                foreach (var a in fireApps)
                {
                    var isDraft = string.Equals(a.Status, "Draft", StringComparison.OrdinalIgnoreCase);
                    var isReturned = string.Equals(a.Status, "ReturnedToApplicant", StringComparison.OrdinalIgnoreCase);
                    var appId = string.IsNullOrEmpty(a.ApplicationCode) ? a.Id.ToString() : a.ApplicationCode;
                    items.Add(new ApplicationStatusItem
                    {
                        Service = "Fire NOC",
                        ApplicationId = appId,
                        InternalId = a.Id,
                        ApplicantName = a.Name ?? string.Empty,
                        Purpose = "Fire Safety Clearance",
                        DateOfApplication = a.SubmittedDate?.Date ?? DateTime.Now.Date,
                        Status = a.Status ?? string.Empty,
                        CurrentStage = a.CurrentStage ?? string.Empty,
                        PaymentStatus = a.PaymentStatus ?? string.Empty,
                        CanContinue = isDraft || isReturned,
                        CanPayNow = false,
                        CanResubmit = isReturned,
                        ViewUrl = Url.Action("Index", "FireClearance") ?? string.Empty
                    });
                }
            }

            items = items.OrderByDescending(i => i.DateOfApplication).ToList();
            return View(items);
        }
    }
}
