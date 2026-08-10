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

            var myApplications = userId == null
                ? new List<TradeLicenceApplication>()
                : await _context.TradeLicenceApplications
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedDate)
                    .ToListAsync();

            return View(myApplications);
        }
    }
}
