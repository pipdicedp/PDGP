using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;

namespace TradeLicence.Controllers
{
    [Authorize(Roles = "Officer")]
    public class OfficerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfficerController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
