using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Name Availability of SOCIETIES (citizen side). Saves each request as a row of
    /// dbo.SocietyNameAvailabilities ΓÇö its own table, separate from the Firm service.
    /// </summary>
    [Authorize]
    public class NameAvailabilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Sub-folder under wwwroot\uploads where this service's files are kept.
        private const string UploadFolder = "society";

        public NameAvailabilityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["EntityType"] = "Society";
            ViewData["Title"] = "Name Availability of Societies";
            return View("Create", new SocietyNameAvailability());
        }

        /// <summary>Tabular list of the Society name availability requests saved so far.</summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["EntityType"] = "Society";
            ViewData["Title"] = "Name Availability of Societies";

            var rows = await _context.SocietyNameAvailabilities
                .AsNoTracking()
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return View("~/Views/NameAvailability/Index.cshtml",
                rows.Cast<NameAvailabilityBase>().ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SocietyNameAvailability model)
        {
            ViewData["EntityType"] = "Society";
            ViewData["Title"] = "Name Availability of Societies";

            // NOC is mandatory only when the applicant says a NOC is attached.
            if (model.NocAttached == true && (model.NocFile == null || model.NocFile.Length == 0))
                ModelState.AddModelError("NocFile", "Please upload the NOC file");

            ValidateFile(model.IdentityProofFile, "IdentityProofFile");
            ValidateFile(model.SignatureProofFile, "SignatureProofFile");
            ValidateFile(model.NocFile, "NocFile");

            if (!ModelState.IsValid)
                return View("Create", model);

            model.IdentityProofPath = await SaveFile(model.IdentityProofFile);
            model.SignatureProofPath = await SaveFile(model.SignatureProofFile);
            model.NocPath = await SaveFile(model.NocFile);
            model.CreatedOn = DateTime.Now;

            _context.SocietyNameAvailabilities.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Name availability request for the Society saved successfully. Reference No: " + model.Id;
            return RedirectToAction(nameof(Create));
        }

        /// <summary>Only PDF, at most 1 MB (the service's stated rule).</summary>
        private void ValidateFile(IFormFile? file, string key)
        {
            if (file == null || file.Length == 0) return;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
                ModelState.AddModelError(key, "Only PDF file is allowed");
            else if (file.Length > 1024 * 1024)
                ModelState.AddModelError(key, "Maximum size of uploading file is 1MB");
        }

        private async Task<string?> SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var folder = Path.Combine(_env.WebRootPath, "uploads", "nameavailability", UploadFolder);
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/nameavailability/{UploadFolder}/{fileName}";
        }
    }
}
