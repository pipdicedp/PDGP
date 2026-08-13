using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Interfaces;
using System.IO;

namespace TradeLicence.Controllers
{
    [Authorize]
    public class TradeLicenceController : Controller
    {
        private readonly Services.ITradeLicenceService _service;
        private readonly ApplicationDbContext _context;

        public TradeLicenceController(Services.ITradeLicenceService service, ApplicationDbContext context)
        {
            _service = service;
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

        // GET: /TradeLicence/Apply
        [HttpGet]
        public async Task<IActionResult> Apply(int? id)
        {
            TradeLicenceApplication model;

            if (id.HasValue)
            {
                var existing = await _service.GetApplicationAsync(id.Value);
                if (existing == null) return NotFound();
                model = existing;
            }
            else
            {
                model = new TradeLicenceApplication
                {
                    IsApplicationForTradeLicence = true,
                    IsRegistrationForShopsEstablishment = true,
                    DateOfCommencement = DateTime.Today,
                    CurrentStep = 1
                };
            }

            // Populated AFTER the model is loaded — Wards/Areas/Streets need the
            // existing MunicipalityId/WardId/AreaId to pre-select correctly for a
            // draft being reopened. Calling this before model existed meant those
            // three dropdowns always rendered empty for any existing application.
            await PopulateDropdownsAsync(model);

            // Tells the page/JS which tab to open on load — "application", "partners",
            // "machinery", "photo", "documents", "shops", or "confirm".
            ViewBag.StartTab = StepNumberToTabName(model.CurrentStep);

            return View(model);
        }

        private static string StepNumberToTabName(int step) => step switch
        {
            1 => "application",
            2 => "partners",
            3 => "machinery",
            4 => "photo",
            5 => "documents",
            6 => "shops",
            7 => "preview",
            8 => "confirm",
            _ => "application"
        };

        /// <summary>
        /// Called by each wizard step's "Next" button (Partners, Machinery, Photo,
        /// Documents, Shops) once that step's own client-side checks pass, so the
        /// database always reflects the furthest step the user has actually reached.
        /// This is what lets "Continue" from the dashboard reopen the correct tab
        /// instead of always starting at Application Details.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceStep(int applicationId, int step)
        {
            if (applicationId <= 0 || step < 1 || step > 8)
                return BadRequest(new { success = false, error = "Invalid applicationId or step." });

            var updated = await _service.UpdateCurrentStepAsync(applicationId, step);
            if (!updated) return NotFound();

            return Ok(new { success = true });
        }
        // POST: /TradeLicence/Apply  (Save Draft)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(TradeLicenceApplication model, List<int> selectedDocuments)
        {
            // Draft saves skip full validation so partially-filled forms can be stored
            ModelState.Clear();

            if (model.UserId == null)
            {
                model.UserId = GetCurrentUserId();
            }

            await _service.SaveDraftAsync(model, selectedDocuments);

            TempData["Message"] = "Draft saved successfully.";
            return RedirectToAction(nameof(Apply), new { id = model.ApplicationId });
        }

        /// <summary>
        /// AJAX endpoint called by the "Next" button on Application Details.
        /// Unlike SaveDraft, this DOES enforce full server-side [Required]/format
        /// validation (client-side jQuery validation is only a UX convenience —
        /// this is the real authority) — the wizard is only allowed to reveal
        /// Partners Details once the record actually passes validation AND is
        /// saved to the database.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveApplicationDetails(TradeLicenceApplication model, List<int> selectedDocuments)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value != null && kvp.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new { success = false, errors });
            }

            if (model.CurrentStep < 2) model.CurrentStep = 2;   // <-- ADD THIS LINE

            if (model.UserId == null)
            {
                model.UserId = GetCurrentUserId();
            }

            await _service.SaveDraftAsync(model, selectedDocuments);

            return Ok(new { success = true, applicationId = model.ApplicationId });
        }

        // POST: /TradeLicence/Apply (Final Submit — called via AJAX from the Confirm tab)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(TradeLicenceApplication model, List<int> selectedDocuments)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kvp => kvp.Value != null && kvp.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

                return BadRequest(new { success = false, errors });
            }

            if (model.UserId == null)
            {
                model.UserId = GetCurrentUserId();
            }

            await _service.SubmitApplicationAsync(model, selectedDocuments);

            var pdfBytes = await _service.GenerateAcknowledgementPdfAsync(model.ApplicationId);
            var fileName = $"Acknowledgement_{model.ApplicationNumber ?? model.ApplicationId.ToString()}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var application = await _service.GetApplicationAsync(id);
            if (application == null) return NotFound();
            return View(application);
        }

        // Downloads the acknowledgement slip as a PDF directly — no print dialog.
        [HttpGet]
        public async Task<IActionResult> DownloadAcknowledgement(int id)
        {
            var application = await _service.GetApplicationAsync(id);
            if (application == null) return NotFound();

            var pdfBytes = await _service.GenerateAcknowledgementPdfAsync(id);
            var fileName = $"Acknowledgement_{application.ApplicationNumber ?? id.ToString()}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        // Called by the Confirm tab to fill in the read-only summary
        // (Applicant Name / Trade Name / Mobile Number) from saved data.
        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/GetApplicationSummary")]
        public async Task<IActionResult> GetApplicationSummary(int applicationId)
        {
            var application = await _service.GetApplicationAsync(applicationId);
            if (application == null) return NotFound();

            return Json(new
            {
                applicantName = application.ApplicantName,
                tradeName = application.NameAndStyleOfFactory,
                mobileNumber = application.MobileNumber
            });
        }

        // Renders a read-only summary of everything saved so far — all 6
        // earlier tabs, including photos and documents — for the Preview tab.
        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/PreviewApplication")]
        public async Task<IActionResult> PreviewApplication(int applicationId)
        {
            var application = await _service.GetApplicationAsync(applicationId);
            if (application == null) return NotFound();

            var model = new ApplicationPreviewViewModel
            {
                Application = application,
                Partners = await _service.GetPartnersAsync(applicationId),
                Machinery = await _service.GetMachineryAsync(applicationId),
                Documents = await _service.GetDocumentsAsync(applicationId),
                ShopRegistration = application.IsRegistrationForShopsEstablishment
                    ? await _service.GetShopEstablishmentAsync(applicationId)
                    : null
            };

            if (application.MunicipalityId.HasValue)
            {
                var municipalities = await _service.GetMunicipalitiesAsync();
                model.MunicipalityName = municipalities.FirstOrDefault(m => m.MunicipalityId == application.MunicipalityId)?.MunicipalityName;
            }
            if (application.WardId.HasValue && application.MunicipalityId.HasValue)
            {
                var wards = await _service.GetWardsAsync(application.MunicipalityId.Value);
                model.WardName = wards.FirstOrDefault(w => w.WardId == application.WardId)?.WardName;
            }
            if (application.AreaId.HasValue && application.WardId.HasValue)
            {
                var areas = await _service.GetAreasAsync(application.WardId.Value);
                model.AreaName = areas.FirstOrDefault(a => a.AreaId == application.AreaId)?.AreaName;
            }
            if (application.StreetId.HasValue && application.AreaId.HasValue)
            {
                var streets = await _service.GetStreetsAsync(application.AreaId.Value);
                model.StreetName = streets.FirstOrDefault(s => s.StreetId == application.StreetId)?.StreetName;
            }

            return PartialView("_PreviewApplication", model);
        }

        // ---------------- Cascading dropdown AJAX endpoints ----------------

        [HttpGet]
        public async Task<JsonResult> GetWards(int municipalityId)
        {
            var wards = (await _service.GetWardsAsync(municipalityId))
                .Select(w => new { w.WardId, w.WardName })
                .ToList();
            return Json(wards);
        }

        [HttpGet]
        public async Task<JsonResult> GetAreas(int wardId)
        {
            var areas = (await _service.GetAreasAsync(wardId))
                .Select(a => new { a.AreaId, a.AreaName })
                .ToList();
            return Json(areas);
        }

        [HttpGet]
        public async Task<JsonResult> GetStreets(int areaId)
        {
            var streets = (await _service.GetStreetsAsync(areaId))
                .Select(s => new { s.StreetId, s.StreetName })
                .ToList();
            return Json(streets);
        }

        [HttpGet]
        public async Task<JsonResult> GetDoorNumbers(int streetId)
        {
            var doors = (await _service.GetDoorNumbersAsync(streetId))
                .Select(d => new { d.DoorNumberId, d.DoorNumberValue })
                .ToList();
            return Json(doors);
        }

        // ---------------- Helpers ----------------

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var id) ? id : (int?)null;
        }

        private async Task PopulateDropdownsAsync(TradeLicenceApplication? model = null)
        {
            ViewBag.Municipalities = new SelectList(
                await _service.GetMunicipalitiesAsync(),
                "MunicipalityId", "MunicipalityName", model?.MunicipalityId);

            ViewBag.Wards = model?.MunicipalityId != null
                ? new SelectList(await _service.GetWardsAsync(model.MunicipalityId.Value), "WardId", "WardName", model.WardId)
                : new SelectList(Enumerable.Empty<Ward>(), "WardId", "WardName");

            ViewBag.Areas = model?.WardId != null
                ? new SelectList(await _service.GetAreasAsync(model.WardId.Value), "AreaId", "AreaName", model.AreaId)
                : new SelectList(Enumerable.Empty<Area>(), "AreaId", "AreaName");

            ViewBag.Streets = model?.AreaId != null
                ? new SelectList(await _service.GetStreetsAsync(model.AreaId.Value), "StreetId", "StreetName", model.StreetId)
                : new SelectList(Enumerable.Empty<Street>(), "StreetId", "StreetName");

            ViewBag.DocumentChecklist = await _service.GetDocumentChecklistAsync();

            ViewBag.OwnershipTypes = new SelectList(new[] { "Proprietary", "Partnership", "Firm" });
            ViewBag.LicencePeriods = new SelectList(new[] { "Annual", "Half-Yearly" });
            ViewBag.OtherParticularsList = new SelectList(new[] { "New", "Renewal", "Change of Ownership", "Additional Trade" });
            ViewBag.PurposeList = new SelectList(new[]
            {
                "Manufacturing", "Trading", "Service Provider", "Godown/Warehouse",
                "Hotel/Restaurant", "Clinic/Hospital", "Pharmacy", "Beauty Parlour", "Others"
            });
        }

        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/Get-Partners-Details")]
        public async Task<IActionResult> PartnersDetails(int applicationId)
        {
            ViewBag.ApplicationId = applicationId;
            var partners = await _service.GetPartnersAsync(applicationId);
            return View(partners);
        }

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/AddPartner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPartner(int applicationId, string partnerName, string designation, string address)
        {
            if (string.IsNullOrWhiteSpace(partnerName) || string.IsNullOrWhiteSpace(designation) || string.IsNullOrWhiteSpace(address))
                return BadRequest(new { error = "All fields are required." });

            var partner = await _service.AddPartnerAsync(applicationId, partnerName, designation, address);

            return Json(new { partner.PartnerId, partner.PartnerName, partner.Designation, partner.Address });
        }

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/DeletePartner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePartner(int partnerId)
        {
            var deleted = await _service.DeletePartnerAsync(partnerId);
            if (!deleted) return NotFound();
            return Ok();
        }

        /// <summary>
        /// Saves ALL partner rows currently in the grid in a single request,
        /// called by the "Save" button below the Partners grid. Each row was only
        /// held client-side until now — nothing hits the database until this runs.
        /// </summary>
        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/SaveAllPartners")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAllPartners([FromBody] SavePartnersRequest request)
        {
            if (request?.Partners == null || request.Partners.Count == 0)
                return BadRequest(new { error = "Please add at least one partner before saving." });

            //if (request.ApplicationId <= 0)
            //    return BadRequest(new { error = "Invalid application." });

            foreach (var p in request.Partners)
            {
                if (string.IsNullOrWhiteSpace(p.PartnerName) ||
                    string.IsNullOrWhiteSpace(p.Designation) ||
                    string.IsNullOrWhiteSpace(p.Address))
                {
                    continue; // skip any incomplete row defensively
                }

                await _service.AddPartnerAsync(request.ApplicationId, p.PartnerName, p.Designation, p.Address);
            }

            return Ok(new { success = true });
        }

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/SaveAllMachinery")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAllMachinery([FromBody] SaveMachineryRequest request)
        {
            if (request?.Machinery == null || request.Machinery.Count == 0)
                return BadRequest(new { error = "Please add at least one machinery item before saving." });

            //if (request.ApplicationId <= 0)
            //    return BadRequest(new { error = "Invalid application." });

            foreach (var m in request.Machinery)
            {
                if (string.IsNullOrWhiteSpace(m.MachineryName)) continue; // skip incomplete rows defensively

                await _service.AddMachineryAsync(request.ApplicationId, m.MachineryName, m.Quantity, m.HorsePower);
            }

            return Ok(new { success = true });
        }

        // ---------------- Photographs (Step 4) ----------------

        private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB per file

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/SavePhotographs")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePhotographs(int applicationId, IFormFile? applicantPhoto, IFormFile? partnerPhoto)
        {
            if (applicationId <= 0)
                return BadRequest(new { error = "Invalid application." });

            if (applicantPhoto == null && partnerPhoto == null)
                return BadRequest(new { error = "Please choose at least one photo to upload." });

            if ((applicantPhoto?.Length ?? 0) > MaxUploadBytes || (partnerPhoto?.Length ?? 0) > MaxUploadBytes)
                return BadRequest(new { error = "Each photo must be 5 MB or smaller." });

            byte[]? applicantBytes = null;
            string? applicantContentType = null;
            if (applicantPhoto != null && applicantPhoto.Length > 0)
            {
                using var ms = new MemoryStream();
                await applicantPhoto.CopyToAsync(ms);
                applicantBytes = ms.ToArray();
                applicantContentType = applicantPhoto.ContentType;
            }

            byte[]? partnerBytes = null;
            string? partnerContentType = null;
            if (partnerPhoto != null && partnerPhoto.Length > 0)
            {
                using var ms = new MemoryStream();
                await partnerPhoto.CopyToAsync(ms);
                partnerBytes = ms.ToArray();
                partnerContentType = partnerPhoto.ContentType;
            }

            await _service.SavePhotographsAsync(applicationId, applicantBytes, applicantContentType, partnerBytes, partnerContentType);

            return Ok(new { success = true });
        }

        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/ViewApplicantPhoto")]
        public async Task<IActionResult> ViewApplicantPhoto(int applicationId)
        {
            var result = await _service.GetDecryptedApplicantPhotoAsync(applicationId);
            if (result == null) return NotFound();
            return File(result.Value.Bytes, result.Value.ContentType);
        }

        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/ViewPartnerPhoto")]
        public async Task<IActionResult> ViewPartnerPhoto(int applicationId)
        {
            var result = await _service.GetDecryptedPartnerPhotoAsync(applicationId);
            if (result == null) return NotFound();
            return File(result.Value.Bytes, result.Value.ContentType);
        }

        // ---------------- Documents (Step 5) ----------------

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/SaveDocument")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDocument(int applicationId, string documentName, IFormFile file)
        {
            if (applicationId <= 0 || string.IsNullOrWhiteSpace(documentName))
                return BadRequest(new { error = "Invalid request." });

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Please choose a file to upload." });

            if (file.Length > MaxUploadBytes)
                return BadRequest(new { error = "File must be 5 MB or smaller." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var doc = await _service.SaveDocumentAsync(applicationId, documentName, file.FileName, bytes, file.ContentType);

            return Ok(new { success = true, documentId = doc.DocumentId });
        }

        [HttpGet]
        [Route("TradeLicence/NewLicence/Apply/ViewDocument")]
        public async Task<IActionResult> ViewDocument(int documentId)
        {
            var result = await _service.GetDecryptedDocumentAsync(documentId);
            if (result == null) return NotFound();
            // No fileName here on purpose — passing one sets
            // Content-Disposition: attachment, which forces a download instead
            // of letting the browser render it inline (used in a preview iframe).
            return File(result.Value.Bytes, result.Value.ContentType);
        }

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/DeleteDocument")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var deleted = await _service.DeleteDocumentAsync(documentId);
            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }

        // ---------------- Shop Establishment Registration (Step 6) ----------------

        [HttpPost]
        [Route("TradeLicence/NewLicence/Apply/SaveShopEstablishment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShopEstablishment([FromBody] ShopEstablishmentRegistration model)
        {
            if (model == null || model.ApplicationId <= 0)
                return BadRequest(new { error = "Invalid application." });

            var saved = await _service.SaveShopEstablishmentAsync(model.ApplicationId, model);

            return Ok(new { success = true, shopRegistrationId = saved.ShopRegistrationId });
        }
    }
}
