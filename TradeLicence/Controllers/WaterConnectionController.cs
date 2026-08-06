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
        private readonly IWebHostEnvironment _environment;

        public WaterConnectionController(WaterApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        public async Task<IActionResult> Index(WaterConnectionFormViewModel model)
        {
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

                AddDocument(application.ApplicationId, "NameAddress", naDocText, model.NameAddressFile);
                AddDocument(application.ApplicationId, "Ownership", ownDocText, model.OwnershipFile);
                AddDocument(application.ApplicationId, "Others", model.OthersDocument, model.OthersFile);
                AddDocument(application.ApplicationId, "ContractorConsent", model.ContractorConsentDocument, model.ContractorConsentFile);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Application submitted successfully.";
                return RedirectToAction("Index");
            }

            PopulateDropdowns(model);
            return View(model);
        }

        // Queue a document row for a newly created application; skipped if no file was chosen.
        private void AddDocument(int applicationId, string purpose, string? option, IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return;

            _context.ApplicationDocuments.Add(new ApplicationDocument
            {
                ApplicationId = applicationId,
                DocumentPurpose = purpose,
                DocumentOption = option,
                IsRequired = true,
                FilePath = UploadFile(file),
                UploadedOn = DateTime.Now
            });
        }

        // Upload File
        private string UploadFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return uniqueFileName;
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
                .Include(a => a.Documents)
                .FirstOrDefaultAsync(x => x.ApplicationId == id);

            if (application != null)
            {
                // Remove uploaded files from disk (DB rows cascade automatically via FK_Doc_Application)
                if (application.Documents != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    foreach (var doc in application.Documents)
                    {
                        if (string.IsNullOrWhiteSpace(doc.FilePath))
                            continue;

                        var fullPath = Path.Combine(uploadsFolder, doc.FilePath);
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                }

                _context.WaterConnectionApplications.Remove(application);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("List");
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
