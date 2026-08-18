using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    [Authorize(Roles = "Officer")]
    public class OfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITradeLicenceService _service;

        public OfficerController(ApplicationDbContext context, ITradeLicenceService service)
        {
            _context = context;
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var applications = await _context.TradeLicenceApplications
                .Where(a => a.Status == "Submitted")
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            ViewBag.Designation = User.FindFirst("Designation")?.Value;

            return View(applications);
        }

        // Read-only application detail view, kept entirely on the officer
        // side (its own layout, no citizen wizard) — reuses the same
        // _PreviewApplication partial the citizen wizard's Preview tab uses,
        // just with the Edit links turned off.
        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var model = await _service.GetApplicationPreviewAsync(id);
            if (model == null) return NotFound();

            model.ShowEditLinks = false;

            return View(model);
        }

        // Sends the application back to the applicant for correction —
        // status changes so it drops off this officer's "Submitted" queue,
        // and the remarks tell the applicant what needs fixing.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToApplicant(int id, string remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            application.Status = "ReturnedToApplicant";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = "Application returned to the applicant.";
            return RedirectToAction("Index");
        }

        // Escalates the application to the General Manager for further review.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToGM(int id, string? remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            application.Status = "ForwardedToGM";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = "Application forwarded to the General Manager.";
            return RedirectToAction("Index");
        }
    }
}
