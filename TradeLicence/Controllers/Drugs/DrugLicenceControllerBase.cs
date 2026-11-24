using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Models;
using TradeLicence.Models.Drugs;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Citizen side of the Restricted Drug Licence services. Written ONCE; Form20AController and
    /// Form21AController only say which form (entity + DrugFormDefinition) they are.
    ///
    ///   GET  Apply            blank form (or Apply?id=N to continue a draft / correct a returned application)
    ///   POST Apply            "Save as Draft" or "Submit" (actionButton)
    ///   GET  MyApplications   track status, pay, download approval
    ///   GET  Details          read-only view of one application
    ///   GET  ViewDocument     open an uploaded PDF
    ///   POST PayNow           pay the amount the Inspection officer requested (test payment, like Water/Electricity)
    ///   GET  DownloadApproval sample approval PDF once the GM has approved
    /// </summary>
    [Authorize]
    public abstract class DrugLicenceControllerBase<TApp> : Controller where TApp : DrugLicenceApplicationBase, new()
    {
        private const string ViewApply = "~/Views/DrugLicence/Apply.cshtml";
        private const string ViewList = "~/Views/DrugLicence/MyApplications.cshtml";
        private const string ViewDetails = "~/Views/DrugLicence/Details.cshtml";

        protected readonly DrugsApplicationDbContext Ctx;
        protected readonly ApplicationDbContext Shared;
        protected readonly DrugFormDefinition Form;
        protected readonly DrugLicenceFormService<TApp> Svc;
        protected readonly WorkflowEngineService<TApp> Engine;

        protected DrugLicenceControllerBase(DrugFormDefinition form, DrugsApplicationDbContext ctx,
            ApplicationDbContext shared, IFileEncryptionService encryption)
        {
            Form = form;
            Ctx = ctx;
            Shared = shared;
            Svc = new DrugLicenceFormService<TApp>(ctx, encryption, form);
            Engine = new WorkflowEngineService<TApp>(ctx, shared, form.ServiceType);
        }

        // Officers sign in with the same cookie and also put THEIR id in NameIdentifier, so an officer with
        // OfficerId = 1 would otherwise be treated as citizen UserId = 1 (same guard as ElectricityController).
        private int? CurrentUserId()
        {
            if (User.IsInRole("Officer")) return null;
            return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
        }

        // ---------------------------------------------------------------- Apply

        [HttpGet]
        public async Task<IActionResult> Apply(int? id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            TApp? app = null;
            if (id.HasValue)
            {
                app = await Svc.GetOwnedAsync(id.Value, userId.Value);
                if (app == null) return NotFound();

                if (!DrugLicenceFormService<TApp>.IsEditable(app))
                {
                    TempData["Error"] = "This application has already been submitted and can't be edited.";
                    return RedirectToAction(nameof(Details), new { id = app.ApplicationId });
                }
            }

            return View(ViewApply, await Svc.BuildViewModelAsync(app));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(DrugLicenceFormViewModel model, string? actionButton)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            TApp? existing = null;
            if (model.ApplicationId > 0)
            {
                existing = await Svc.GetOwnedAsync(model.ApplicationId, userId.Value);
                if (existing == null) return NotFound();
                if (!DrugLicenceFormService<TApp>.IsEditable(existing))
                {
                    TempData["Error"] = "This application has already been submitted and can't be edited.";
                    return RedirectToAction(nameof(Details), new { id = existing.ApplicationId });
                }
            }

            // Always persist what the applicant typed/uploaded first, so a failed Submit never loses their work.
            var (app, fileErrors) = await Svc.SaveAsync(model, existing, userId.Value, Request.Form.Files);

            var submit = string.Equals(actionButton, "submit", StringComparison.OrdinalIgnoreCase);

            if (!submit)
            {
                TempData["Message"] = fileErrors.Count == 0
                    ? "Draft saved. You can come back and continue any time."
                    : "Draft saved, but some files were not accepted: " + string.Join(" ", fileErrors);
                return RedirectToAction(nameof(Apply), new { id = app.ApplicationId });
            }

            var errors = await Svc.ValidateForSubmitAsync(model, app);
            foreach (var (field, message) in errors) ModelState.AddModelError(field, message);
            foreach (var message in fileErrors) ModelState.AddModelError(string.Empty, message);

            if (!ModelState.IsValid)
            {
                model.ApplicationId = app.ApplicationId;
                await Svc.FillDisplayDataAsync(model, app);
                ViewBag.SubmitFailed = true;
                return View(ViewApply, model);
            }

            if (app.Status == "ReturnedToApplicant")
            {
                var result = await Engine.ResubmitAsync(app.ApplicationId, userId);
                TempData[result.Success ? "Message" : "Error"] = result.Success ? result.Message : result.Error;
            }
            else
            {
                await Svc.SubmitAsync(app);
                TempData["Message"] = $"Application {app.ApplicationNumber} submitted successfully.";
            }

            return RedirectToAction(nameof(MyApplications));
        }

        // ---------------------------------------------------------------- list / details

        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            var apps = await Ctx.Set<TApp>()
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            var ids = apps.Select(a => a.ApplicationId).ToList();
            var payments = await Shared.WorkflowPayments
                .Where(p => p.ServiceType == Form.ServiceType && ids.Contains(p.ApplicationId))
                .ToDictionaryAsync(p => p.ApplicationId);

            return View(ViewList, new DrugLicenceListViewModel
            {
                Form = Form,
                Applications = apps.Cast<DrugLicenceApplicationBase>().ToList(),
                Payments = payments
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            var app = await Svc.GetOwnedAsync(id, userId.Value);
            if (app == null) return NotFound();

            return View(ViewDetails, new DrugLicenceDetailsViewModel
            {
                Form = Form,
                Application = app,
                Pharmacists = await Svc.GetPharmacistsAsync(id),
                Documents = await Svc.GetDocumentInfosAsync(id),
                Payment = await Engine.GetPaymentAsync(id)
            });
        }

        // ---------------------------------------------------------------- documents

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int docId, bool download = false)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            var appId = await Svc.GetDocumentApplicationIdAsync(docId);
            if (appId == null) return NotFound();

            // ownership: the document must belong to one of THIS citizen's applications
            if (await Svc.GetOwnedAsync(appId.Value, userId.Value) == null) return NotFound();

            var doc = await Svc.GetDocumentBytesAsync(docId);
            if (doc == null) return NotFound();

            var disposition = download ? "attachment" : "inline";
            Response.Headers.Append("Content-Disposition", $"{disposition}; filename=\"{doc.Value.FileName.Replace("\"", "")}\"");
            return File(doc.Value.Bytes, "application/pdf");
        }

        // ---------------------------------------------------------------- payment

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayNow(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            var result = await Engine.PayNowAsync(id, userId);
            TempData[result.Success ? "Message" : "Error"] = result.Success ? result.Message : result.Error;
            return RedirectToAction(nameof(MyApplications));
        }

        // ---------------------------------------------------------------- approval download

        // SAMPLE pdf for now - see DrugLicencePdfBuilder.
        [HttpGet]
        public async Task<IActionResult> DownloadApproval(int id)
        {
            var userId = CurrentUserId();
            if (userId == null) return Forbid();

            var app = await Svc.GetOwnedAsync(id, userId.Value);
            if (app == null) return NotFound();

            if (app.Status != "Approved")
            {
                TempData["Error"] = "The approval can be downloaded only after the application has been approved.";
                return RedirectToAction(nameof(MyApplications));
            }

            var pdf = DrugLicencePdfBuilder.Build(Form, app, await Svc.GetPharmacistsAsync(id), app.ModifiedDate ?? DateTime.UtcNow);
            var safeNumber = (app.ApplicationNumber ?? $"Form{Form.Code}-{id}").Replace('/', '-');
            return File(pdf, "application/pdf", $"{safeNumber}-Approval.pdf");
        }
    }
}
