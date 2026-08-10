using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WaterConnection.Data;
using WaterConnection.Models;

namespace WaterConnection.Controllers
{
    public class WaterConnectionController : Controller
    {
        private readonly WaterApplicationDbContext _context;

        public WaterConnectionController(WaterApplicationDbContext context)
        {
            _context = context;
        }

        // Landing page for the Water Connection module: choose to apply or check status.
        [HttpGet]
        public IActionResult Home()
        {
            return View();
        }

        // Display Form
        [HttpGet]
        public IActionResult Index()
        {
            var model = new WaterConnectionFormViewModel();
            PopulateDropdowns(model);
            return View(model);
        }

        // Save Form
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 26_214_400)] // 25 MB
        [RequestSizeLimit(26_214_400)]
        public async Task<IActionResult> Index(WaterConnectionFormViewModel model)
        {
            // Server-side PDF enforcement: client-side (wc-attachments.js) is the first
            // line of defense, but a request can always bypass JS, so re-check here against
            // the file's actual bytes before trusting anything.
            ValidatePdfUpload(model.NameAddressFile, nameof(model.NameAddressFile));
            ValidatePdfUpload(model.OwnershipFile, nameof(model.OwnershipFile));
            ValidatePdfUpload(model.OthersFile, nameof(model.OthersFile));
            ValidatePdfUpload(model.ContractorConsentFile, nameof(model.ContractorConsentFile));

            if (ModelState.IsValid)
            {
                var application = new WaterConnectionApplication
                {
                    Name = model.Name,
                    PowerOfAttorney = model.PowerOfAttorney,
                    FatherName = model.FatherName,
                    SpouseName = model.SpouseName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,

                    CommDoorNo = model.CommDoorNo,
                    CommAddress1 = model.CommAddress1,
                    CommAddress2 = model.CommAddress2,
                    CommCity = model.CommCity,

                    ConnDoorNo = model.ConnDoorNo,
                    ConnAddress1 = model.ConnAddress1,
                    ConnAddress2 = model.ConnAddress2,
                    ConnCity = model.ConnCity,

                    DeptCode = model.DeptCode!.Value,
                    SectionCode = model.SectionCode!.Value,
                    ContractorCode = model.ContractorCode,
                    AreaCode = model.AreaCode!.Value,
                    PurposeCode = model.PurposeCode!.Value,
                    NaVerifyCode = model.NaVerifyCode!.Value,
                    OwnFileCode = model.OwnFileCode!.Value,

                    ApplicationDate = DateTime.Now,
                    Status = "Submitted"
                };

                _context.WaterConnectionApplications.Add(application);
                await _context.SaveChangesAsync(); // populates application.ApplicationId

                var naDocText = await _context.NameAddressVerifications
                    .Where(x => x.NaVerifyCode == model.NaVerifyCode)
                    .Select(x => x.DocumentName)
                    .FirstOrDefaultAsync();

                var ownDocText = await _context.OwnershipVerifications
                    .Where(x => x.OwnFileCode == model.OwnFileCode)
                    .Select(x => x.DocumentName)
                    .FirstOrDefaultAsync();

                await AddDocument(application.ApplicationId, "NameAddress", naDocText, model.NameAddressFile);
                await AddDocument(application.ApplicationId, "Ownership", ownDocText, model.OwnershipFile);
                await AddDocument(application.ApplicationId, "Others", model.OthersDocument, model.OthersFile);
                await AddDocument(application.ApplicationId, "ContractorConsent", model.ContractorConsentDocument, model.ContractorConsentFile);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Application submitted successfully.";
                TempData["SuccessApplicationId"] = application.ApplicationId;
                return RedirectToAction("Index");
            }

            ViewBag.ValidationFailed = true;
            PopulateDropdowns(model);
            return View(model);
        }

        // Queue a document row for a newly created application; skipped if no file was chosen.
        // The file's bytes are read straight into the entity -- Application_Documents.File_Path
        // is varbinary(max), so this goes to the DB, not the local disk.
        private async Task AddDocument(int applicationId, string purpose, string? option, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return;

            byte[] content;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                content = memoryStream.ToArray();
            }

            _context.ApplicationDocuments.Add(new ApplicationDocument
            {
                ApplicationId = applicationId,
                DocumentPurpose = purpose,
                DocumentOption = option,
                FileContent = content,
                UploadedOn = DateTime.Now
            });
        }

        // View All Applications
        [HttpGet]
        public async Task<IActionResult> List()
        {
            var applications = await _context.WaterConnectionApplications
                .Include(a => a.Purpose)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            return View(applications);
        }

        // View Application Details
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.WaterConnectionApplications
                .Include(a => a.Department)
                .Include(a => a.Section)
                .Include(a => a.Contractor)
                .Include(a => a.Area)
                .Include(a => a.Purpose)
                .Include(a => a.NameAddressVerification)
                .Include(a => a.OwnershipVerification)
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(x => x.ApplicationId == id);

            if (application == null)
                return NotFound();

            return View(application);
        }

        // Delete Application
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _context.WaterConnectionApplications
                .FirstOrDefaultAsync(x => x.ApplicationId == id);

            if (application != null)
            {
                // Application_Documents rows (and their file bytes) cascade-delete
                // automatically via FK_Doc_Application -- nothing to clean up on disk.
                _context.WaterConnectionApplications.Remove(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("List");
        }

        // Check Application Status -- show the search form
        [HttpGet]
        public IActionResult CheckStatus()
        {
            return View(new WaterConnectionStatusViewModel());
        }

        // Check Application Status -- look up by Application_Id and show its Status
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckStatus(WaterConnectionStatusViewModel model)
        {
            model.Searched = true;
            model.Found = false;
            model.Status = null;
            model.ApplicantName = null;
            model.ApplicationDate = null;

            if (model.ApplicationId.HasValue)
            {
                var application = await _context.WaterConnectionApplications
                    .Where(a => a.ApplicationId == model.ApplicationId.Value)
                    .Select(a => new { a.Status, a.Name, a.ApplicationDate })
                    .FirstOrDefaultAsync();

                if (application != null)
                {
                    model.Found = true;
                    model.Status = application.Status;
                    model.ApplicantName = application.Name;
                    model.ApplicationDate = application.ApplicationDate;
                }
            }

            return View(model);
        }

        // Serve a document's bytes straight out of the database so it can be viewed/downloaded.
        // There's no column for the original filename/MIME type, so the content type is
        // detected from the bytes themselves (the schema only has File_Path).
        [HttpGet]
        public async Task<IActionResult> Document(int id, bool download = false)
        {
            var doc = await _context.ApplicationDocuments
                .FirstOrDefaultAsync(d => d.DocumentId == id);

            if (doc == null || doc.FileContent == null || doc.FileContent.Length == 0)
                return NotFound();

            var (contentType, extension) = DetectFileType(doc.FileContent);
            var baseName = string.IsNullOrWhiteSpace(doc.DocumentPurpose) ? "document" : doc.DocumentPurpose;
            var fileName = $"{baseName}-{doc.DocumentId}{extension}";

            // The 3-arg File(bytes, contentType, fileDownloadName) overload always sends
            // Content-Disposition: attachment, which forces a download even inside an
            // <iframe> preview. Setting the header ourselves as "inline" lets the browser
            // render PDFs/images in place; ?download=true still forces a real download
            // (e.g. for a "Download" button) via "attachment" instead.
            var disposition = download ? "attachment" : "inline";
            Response.Headers.Append("Content-Disposition", $"{disposition}; filename=\"{fileName}\"");

            return File(doc.FileContent, contentType);
        }

        // Identifies common document/image formats by their byte signature ("magic numbers"),
        // since the DB only stores raw bytes -- no filename or MIME type column exists to read from.
        private static (string ContentType, string Extension) DetectFileType(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return ("application/pdf", ".pdf");

            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return ("image/jpeg", ".jpg");

            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                return ("image/png", ".png");

            if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38
                && (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
                return ("image/gif", ".gif");

            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                return ("image/bmp", ".bmp");

            return ("application/octet-stream", string.Empty);
        }

        // Adds a ModelState error if a mandatory upload isn't actually a PDF. A missing
        // file is left to the [Required] attribute -- this only judges files that were
        // provided.
        private void ValidatePdfUpload(IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0)
                return;

            if (!IsPdfFile(file))
            {
                ModelState.AddModelError(fieldName, "Only PDF files are allowed.");
            }
        }

        // Checks both the extension and the file's own byte signature ("%PDF") -- the
        // extension alone can be renamed, so the header is what actually decides this.
        private static bool IsPdfFile(IFormFile file)
        {
            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var stream = file.OpenReadStream();
                var header = new byte[4];
                var bytesRead = stream.Read(header, 0, header.Length);
                stream.Position = 0; // rewind so AddDocument can still read the full file afterward

                return bytesRead == 4
                    && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46; // "%PDF"
            }
            catch
            {
                return false;
            }
        }

        // ---- Cascading dropdown endpoints (Section -> Contractor -> Area follow the DB's FK chain) ----

        [HttpGet]
        public async Task<JsonResult> GetSections(int? deptCode)
        {
            var query = _context.Sections.AsQueryable();
            if (deptCode.HasValue)
                query = query.Where(s => s.DeptCode == deptCode.Value);

            var sections = await query
                .OrderBy(s => s.SectionName)
                .Select(s => new { value = s.SectionCode, text = s.SectionName })
                .ToListAsync();

            return Json(sections);
        }

        [HttpGet]
        public async Task<JsonResult> GetContractors(int? sectionCode)
        {
            var query = _context.Contractors.AsQueryable();
            if (sectionCode.HasValue)
                query = query.Where(c => c.SectionCode == sectionCode.Value);

            var contractors = await query
                .OrderBy(c => c.ContractorName)
                .Select(c => new { value = c.ContractorCode, text = c.ContractorName })
                .ToListAsync();

            return Json(contractors);
        }

        [HttpGet]
        public async Task<JsonResult> GetAreas(int? contractorCode)
        {
            var query = _context.Areas.AsQueryable();
            if (contractorCode.HasValue)
                query = query.Where(a => a.ContractorCode == contractorCode.Value);

            var areas = await query
                .OrderBy(a => a.AreaName)
                .Select(a => new { value = a.AreaCode, text = a.AreaName })
                .ToListAsync();

            return Json(areas);
        }

        // Loads every dropdown the form needs, in full (no filtering) so the form
        // works the same way it did before -- the cascading endpoints above are
        // an optional enhancement the front-end JS can call on top of this.
        private void PopulateDropdowns(WaterConnectionFormViewModel model)
        {
            model.Departments = _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => new SelectListItem { Value = d.DeptCode.ToString(), Text = d.DepartmentName })
                .ToList();

            model.Sections = _context.Sections
                .OrderBy(s => s.SectionName)
                .Select(s => new SelectListItem { Value = s.SectionCode.ToString(), Text = s.SectionName })
                .ToList();

            model.Contractors = _context.Contractors
                .OrderBy(c => c.ContractorName)
                .Select(c => new SelectListItem { Value = c.ContractorCode.ToString(), Text = c.ContractorName })
                .ToList();

            model.Areas = _context.Areas
                .OrderBy(a => a.AreaName)
                .Select(a => new SelectListItem { Value = a.AreaCode.ToString(), Text = a.AreaName })
                .ToList();

            model.Purposes = _context.Purposes
                .OrderBy(p => p.PurposeName)
                .Select(p => new SelectListItem { Value = p.PurposeCode.ToString(), Text = p.PurposeName })
                .ToList();

            model.NameAddressVerifications = _context.NameAddressVerifications
                .OrderBy(x => x.DocumentName)
                .Select(x => new SelectListItem { Value = x.NaVerifyCode.ToString(), Text = x.DocumentName })
                .ToList();

            model.OwnershipVerifications = _context.OwnershipVerifications
                .OrderBy(x => x.DocumentName)
                .Select(x => new SelectListItem { Value = x.OwnFileCode.ToString(), Text = x.DocumentName })
                .ToList();
        }
    }
}
