using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Controllers
{
    public class TradeLicenceController : Controller
    {
        private readonly Services.ITradeLicenceService _service;

        public TradeLicenceController(Services.ITradeLicenceService service)
        {
            _service = service;
        }

        // GET: /TradeLicence/Apply
        [HttpGet]
        public async Task<IActionResult> Apply(int? id)
        {
            await PopulateDropdownsAsync();

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
            7 => "confirm",
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
            if (applicationId <= 0 || step < 1 || step > 7)
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

            await _service.SaveDraftAsync(model, selectedDocuments);

            TempData["Message"] = "Draft saved successfully.";
            return RedirectToAction(nameof(Apply));
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

            await _service.SaveDraftAsync(model, selectedDocuments);

            return Ok(new { success = true, applicationId = model.ApplicationId });
        }

        // POST: /TradeLicence/Apply (Submit / Next step)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(TradeLicenceApplication model, List<int> selectedDocuments)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model);
                return View(model);
            }

            await _service.SubmitApplicationAsync(model, selectedDocuments);

            // Step 1 of 7 -> proceed to Partners Details in a real flow
            return RedirectToAction(nameof(Confirmation), new { id = model.ApplicationId });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var application = await _service.GetApplicationAsync(id);
            if (application == null) return NotFound();
            return View(application);
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
    }
}
