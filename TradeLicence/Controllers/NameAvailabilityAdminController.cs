using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Name Availability administrator module.
    ///
    /// One officer account (Username: Society / Designation:
    /// "Name Availability Admin") reviews BOTH services from a single login:
    ///   - Name Availability of Society   -> dbo.SocietyNameAvailabilities
    ///   - Name Availability of Firms     -> dbo.FirmNameAvailabilities
    ///
    /// Each applicant submits up to five proposed names. The administrator
    /// opens a request, approves exactly ONE of those names (or rejects the
    /// request) and the decision is written back to the same row, where the
    /// applicant sees it on their "Submitted Requests" list.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class NameAvailabilityAdminController : Controller
    {
        /// <summary>Designation claim of the account that may use this module.</summary>
        public const string AdminDesignation = "Name Availability Admin";

        private readonly ApplicationDbContext _context;

        public NameAvailabilityAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsNameAvailabilityAdmin =>
            string.Equals(User.FindFirst("Designation")?.Value, AdminDesignation, StringComparison.OrdinalIgnoreCase);

        // ---------------- Dashboard ----------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            ViewData["Title"] = "Name Availability - Admin";

            ViewBag.SocietyTotal = await _context.SocietyNameAvailabilities.CountAsync();
            ViewBag.SocietyPending = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Pending");
            ViewBag.SocietyApproved = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Approved");
            ViewBag.SocietyRejected = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Rejected");

            ViewBag.FirmTotal = await _context.FirmNameAvailabilities.CountAsync();
            ViewBag.FirmPending = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Pending");
            ViewBag.FirmApproved = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Approved");
            ViewBag.FirmRejected = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Rejected");

            return View();
        }

        // ---------------- Queues (one per service, same login) ----------------

        [HttpGet]
        public async Task<IActionResult> Society(string status = "Pending")
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            ViewData["Title"] = "Name Availability of Societies";
            ViewData["EntityType"] = "Society";
            ViewData["StatusFilter"] = status;

            var query = _context.SocietyNameAvailabilities.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.Status == status);

            var rows = await query.OrderByDescending(r => r.Id).ToListAsync();

            ViewBag.Total = await _context.SocietyNameAvailabilities.CountAsync();
            ViewBag.Pending = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Pending");
            ViewBag.Approved = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Approved");
            ViewBag.Rejected = await _context.SocietyNameAvailabilities.CountAsync(r => r.Status == "Rejected");

            return View("Queue", rows.Cast<NameAvailabilityBase>().ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Firms(string status = "Pending")
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            ViewData["Title"] = "Name Availability of Firms";
            ViewData["EntityType"] = "Firm";
            ViewData["StatusFilter"] = status;

            var query = _context.FirmNameAvailabilities.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.Status == status);

            var rows = await query.OrderByDescending(r => r.Id).ToListAsync();

            ViewBag.Total = await _context.FirmNameAvailabilities.CountAsync();
            ViewBag.Pending = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Pending");
            ViewBag.Approved = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Approved");
            ViewBag.Rejected = await _context.FirmNameAvailabilities.CountAsync(r => r.Status == "Rejected");

            return View("Queue", rows.Cast<NameAvailabilityBase>().ToList());
        }

        // ---------------- Review / decision ----------------

        [HttpGet]
        public async Task<IActionResult> Review(string type, int id)
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            var row = await FindRowAsync(type, id);
            if (row == null) return NotFound();

            PrepareReviewViewData(type);
            return View(row);
        }

        /// <summary>Approve exactly one of the applicant's proposed names.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string type, int id, string? selectedName, string? remarks)
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            var row = await FindRowAsync(type, id);
            if (row == null) return NotFound();

            var match = row.ProposedNames
                .FirstOrDefault(n => string.Equals(n, (selectedName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                TempData["Error"] = "Please select one of the proposed names to approve.";
                return RedirectToAction(nameof(Review), new { type, id });
            }

            row.Status = "Approved";
            row.ApprovedName = match;
            row.AdminRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
            row.ApprovedBy = User.Identity?.Name;
            row.ApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Name \"{match}\" approved and sent to the applicant.";
            return RedirectToAction(string.Equals(type, "Firm", StringComparison.OrdinalIgnoreCase) ? nameof(Firms) : nameof(Society));
        }

        /// <summary>Reject the request (remarks required so the applicant knows why).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string type, int id, string? remarks)
        {
            if (!IsNameAvailabilityAdmin) return Forbid();

            var row = await FindRowAsync(type, id);
            if (row == null) return NotFound();

            if (string.IsNullOrWhiteSpace(remarks))
            {
                TempData["Error"] = "Please enter remarks before rejecting the request.";
                return RedirectToAction(nameof(Review), new { type, id });
            }

            row.Status = "Rejected";
            row.ApprovedName = null;
            row.AdminRemarks = remarks.Trim();
            row.ApprovedBy = User.Identity?.Name;
            row.ApprovedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Request rejected and the remarks were sent to the applicant.";
            return RedirectToAction(string.Equals(type, "Firm", StringComparison.OrdinalIgnoreCase) ? nameof(Firms) : nameof(Society));
        }

        // ---------------- helpers ----------------

        private async Task<NameAvailabilityBase?> FindRowAsync(string? type, int id)
        {
            if (string.Equals(type, "Firm", StringComparison.OrdinalIgnoreCase))
                return await _context.FirmNameAvailabilities.FirstOrDefaultAsync(r => r.Id == id);

            if (string.Equals(type, "Society", StringComparison.OrdinalIgnoreCase))
                return await _context.SocietyNameAvailabilities.FirstOrDefaultAsync(r => r.Id == id);

            return null;
        }

        private void PrepareReviewViewData(string? type)
        {
            var isFirm = string.Equals(type, "Firm", StringComparison.OrdinalIgnoreCase);
            ViewData["EntityType"] = isFirm ? "Firm" : "Society";
            ViewData["EntityPlural"] = isFirm ? "Firms" : "Societies";
            ViewData["Title"] = isFirm ? "Name Availability of Firms" : "Name Availability of Societies";
        }
    }
}
