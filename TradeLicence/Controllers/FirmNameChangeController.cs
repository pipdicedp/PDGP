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
    /// Firms - Request for Name/Business Place/Partners (Name Change of Firms).
    /// Every submitted request is saved as a row of dbo.FirmNameChangeRequest
    /// (columns: RequestId, IsNameChange, IsAddressChange, IsPartnership, IsOthers,
    /// FormFileName, FormFilePath, FormContentType, FormFileSizeKB, ApplicationNo,
    /// ChallanNo, ChallanDate, Amount, Remarks, RequestStatus, CreatedBy, CreatedOn,
    /// ModifiedBy, ModifiedOn, IsActive). The uploaded Change Request Form (FORM II)
    /// is stored under wwwroot\uploads\firmnamechange\.
    /// </summary>
    [Authorize]
    public class FirmNameChangeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const string UploadFolder = "firmnamechange";

        // Max upload size: 1 MB (same rule as the other name services).
        private const long MaxFileBytes = 1024 * 1024;

        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        public FirmNameChangeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Name Change of Firms";
            return View(new FirmNameChangeRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FirmNameChangeRequest model)
        {
            ViewData["Title"] = "Name Change of Firms";

            // "Requested Change For" is mandatory: at least one option.
            if (!model.IsNameChange && !model.IsAddressChange && !model.IsPartnership && !model.IsOthers)
                ModelState.AddModelError(string.Empty, "Please select the change requested");

            ValidateFile(model.ChangeRequestFormFile, "ChangeRequestFormFile");

            if (!ModelState.IsValid)
                return View(model);

            var saved = await SaveFile(model.ChangeRequestFormFile);

            model.FormFileName = saved.FileName;
            model.FormFilePath = saved.RelativePath;
            model.FormContentType = saved.ContentType;
            model.FormFileSizeKB = saved.SizeKB;
            model.Remarks ??= string.Empty;
            model.RequestStatus = "Submitted";
            model.CreatedBy = User.Identity?.Name;
            model.CreatedOn = DateTime.Now;
            model.IsActive = true;

            _context.FirmNameChangeRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Name Change of Firms request saved successfully. Reference No: " + model.RequestId;
            return RedirectToAction(nameof(Create));
        }

        /// <summary>Tabular list of the Name Change of Firms requests saved so far.</summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Name Change of Firms";

            var rows = await _context.FirmNameChangeRequests
                .AsNoTracking()
                .OrderByDescending(r => r.RequestId)
                .ToListAsync();

            return View(rows);
        }

        /// <summary>PDF or image (JPG/PNG), at most 1 MB.</summary>
        private void ValidateFile(IFormFile? file, string key)
        {
            if (file == null || file.Length == 0) return;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                ModelState.AddModelError(key, "Only PDF, JPG or PNG file is allowed");
            else if (file.Length > MaxFileBytes)
                ModelState.AddModelError(key, "Maximum size of uploading file is 1MB");
        }

        private async Task<(string FileName, string RelativePath, string ContentType, int SizeKB)> SaveFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return (string.Empty, string.Empty, string.Empty, 0);

            var folder = Path.Combine(_env.WebRootPath, "uploads", UploadFolder);
            Directory.CreateDirectory(folder);

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            return (file.FileName,
                    $"/uploads/{UploadFolder}/{fileName}",
                    contentType,
                    (int)Math.Ceiling(file.Length / 1024.0));
        }
    }
}
