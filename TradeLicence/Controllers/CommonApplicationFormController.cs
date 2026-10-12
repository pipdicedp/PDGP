using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Models.Caf;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// The Common Application Form (CAF): a 5-step wizard, one step per
    /// dbo.caf_* table (see CafFormMetadata). Each step is its own page —
    /// "Save &amp; Next" really does save that step's table before moving on,
    /// as asked for, rather than only saving at the very end.
    ///
    /// Route is the literal "common_application_form" the dashboard link
    /// points at (see _UserLayout.cshtml), not the default
    /// /CommonApplicationForm/... MVC route.
    ///
    /// loginid (the primary key of every CAF table) is the citizen's own
    /// UserId — the same id AccountController puts in the login cookie — so
    /// each citizen has exactly one CAF application tied to their account.
    /// If your deployment instead wants one CAF loginid to be independent of
    /// the citizen's UserId (e.g. so one citizen could start several CAF
    /// applications), that needs a separate "application list" screen in
    /// front of this controller; ask and I'll add it.
    /// </summary>
    [Authorize]
    [Route("common_application_form")]
    public class CommonApplicationFormController : Controller
    {
        private readonly ICafFormService _service;
        private readonly ILogger<CommonApplicationFormController> _logger;
        private readonly ApplicationDbContext _context;

        public CommonApplicationFormController(ICafFormService service, ILogger<CommonApplicationFormController> logger, ApplicationDbContext context)
        {
            _service = service;
            _logger = logger;
            _context = context;
        }

        private long? GetLoginId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(raw, out var id) ? id : null;
        }

        // /common_application_form (the menu link) = "resume where you left off": jump to the first
        // step that hasn't been saved yet, or to the Application Preview once every step is saved,
        // or to the preview (read-only) if the form was already submitted.
        [HttpGet("")]
        public async Task<IActionResult> Resume()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (await IsSubmittedAsync(loginId.Value)) return RedirectToAction("TrackStatus");

            var saved = await _service.GetSavedStepNumbersAsync(CafFormMetadata.Steps, loginId.Value);
            var next = CafFormMetadata.Steps
                .OrderBy(s => s.Number)
                .FirstOrDefault(s => !saved.Contains(s.Number));

            if (next == null) return RedirectToAction("Preview");

            if (next.Number > CafFormMetadata.MinStep)
                TempData["CafMessage"] = $"Welcome back! Continuing from {next.Title}, where you left off.";

            return RedirectToAction("Step", new { step = next.Number });
        }

        [HttpGet("{step:int}")]
        public async Task<IActionResult> Step(int step)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            // Once submitted the form is read-only — send the applicant to the Application Preview instead.
            if (await IsSubmittedAsync(loginId.Value)) return RedirectToAction("Preview");

            var stepDef = CafFormMetadata.ByNumber(step);
            if (stepDef == null) return NotFound();

            var basicExists = await _service.BasicDetailsExistAsync(loginId.Value);
            if (!basicExists && stepDef.Key != "basic")
            {
                TempData["CafMessage"] = "Please save Basic Details first.";
                return RedirectToAction("Step", new { step = CafFormMetadata.MinStep });
            }

            var vm = await BuildViewModelAsync(stepDef, loginId.Value, basicExists);

            if (TempData["CafMessage"] is string msg) vm.SuccessMessage = msg;

            return View("Step", vm);
        }

        [HttpPost("{step:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStep(int step, [FromForm] IFormCollection form)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            // Once submitted the form is read-only — send the applicant to the Application Preview instead.
            if (await IsSubmittedAsync(loginId.Value)) return RedirectToAction("Preview");

            var stepDef = CafFormMetadata.ByNumber(step);
            if (stepDef == null) return NotFound();

            var basicExists = await _service.BasicDetailsExistAsync(loginId.Value);
            if (!basicExists && stepDef.Key != "basic")
            {
                TempData["CafMessage"] = "Please save Basic Details first.";
                return RedirectToAction("Step", new { step = CafFormMetadata.MinStep });
            }

            var posted = stepDef.Fields.ToDictionary(f => f.Name, f => (string?)form[f.Name].ToString());
            try
            {
                await _service.SaveMainRowAsync(stepDef, loginId.Value, posted);
            }
            catch (SqlException ex)
            {
                // Log the real error for the developer; show the applicant a readable message
                // on the same step with everything they typed still in the form.
                _logger.LogError(ex, "CAF save failed on step {Step} ({Table}) for login {LoginId}: SQL error {Number}",
                    step, stepDef.TableName, loginId.Value, ex.Number);

                var errVm = await BuildViewModelAsync(stepDef, loginId.Value, basicExists);
                errVm.Values = posted;
                errVm.ErrorMessage = $"Could not save {stepDef.Title} (database error {ex.Number}). Please check the values entered and try again.";
                return View("Step", errVm);
            }

            // The last data step no longer submits — it hands over to the Application Preview,
            // where the applicant reviews everything and submits explicitly.
            var isLastStep = step == CafFormMetadata.MaxStep;
            if (isLastStep)
            {
                TempData["CafMessage"] = $"{stepDef.Title} saved. Please review your application before submitting.";
                return RedirectToAction("Preview");
            }

            TempData["CafMessage"] = $"{stepDef.Title} saved.";
            return RedirectToAction("Step", new { step = step + 1 });
        }

        // ---------------- Sub-table rows (Add / Delete) ----------------

        [HttpPost("subrow/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSubRow(int step, string subKey, [FromForm] IFormCollection form)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (await IsSubmittedAsync(loginId.Value))
                return BadRequest(new { error = "This application has already been submitted and can no longer be changed." });

            var stepDef = CafFormMetadata.ByNumber(step);
            var subDef = stepDef?.SubTables.FirstOrDefault(s => s.Key == subKey);
            if (subDef == null) return NotFound();

            if (!await _service.BasicDetailsExistAsync(loginId.Value))
                return BadRequest(new { error = "Please save Basic Details first." });

            var hasAnyValue = subDef.Fields.Any(f => !string.IsNullOrWhiteSpace(form[f.Name].ToString()));
            if (!hasAnyValue)
                return BadRequest(new { error = "Please fill in at least one field before adding." });

            var posted = subDef.Fields.ToDictionary(f => f.Name, f => (string?)form[f.Name].ToString());
            var row = await _service.AddSubRowAsync(subDef, loginId.Value, posted);

            return Ok(new { success = true, id = row.Id, values = row.Values });
        }

        [HttpPost("subrow/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubRow(int step, string subKey, long id)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (await IsSubmittedAsync(loginId.Value))
                return BadRequest(new { error = "This application has already been submitted and can no longer be changed." });

            var stepDef = CafFormMetadata.ByNumber(step);
            var subDef = stepDef?.SubTables.FirstOrDefault(s => s.Key == subKey);
            if (subDef == null) return NotFound();

            var removed = await _service.DeleteSubRowAsync(subDef, loginId.Value, id);
            return removed ? Ok(new { success = true }) : NotFound(new { success = false });
        }

        // ---------------- Application Preview (6th tab) + Submit ----------------

        [HttpGet("preview")]
        public async Task<IActionResult> Preview()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var basicExists = await _service.BasicDetailsExistAsync(loginId.Value);
            if (!basicExists)
            {
                TempData["CafMessage"] = "Please save Basic Details first.";
                return RedirectToAction("Step", new { step = CafFormMetadata.MinStep });
            }

            var vm = await BuildPreviewAsync(loginId.Value, basicExists);
            // submitted applicants follow progress on the separate Track Status page; the preview only
            // carries the card when the Industry department returned the form for correction
            vm.ApplicantStatus = vm.IsSubmitted ? null : await BuildApplicantStatusAsync(loginId.Value, false);

            if (TempData["CafMessage"] is string msg) vm.SuccessMessage = msg;
            if (TempData["CafError"] is string err) vm.ErrorMessage = err;
            vm.JustSubmitted = TempData["CafSubmitted"] is true;

            return View("Preview", vm);
        }

        // ---------------- Track Status (separate page) ----------------

        [HttpGet("status")]
        public async Task<IActionResult> TrackStatus()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (!await _service.BasicDetailsExistAsync(loginId.Value))
            {
                TempData["CafMessage"] = "Please save Basic Details first.";
                return RedirectToAction("Step", new { step = CafFormMetadata.MinStep });
            }

            var isSubmitted = await IsSubmittedAsync(loginId.Value);

            return View("TrackStatus", new CafTrackStatusVm
            {
                LoginId = loginId.Value,
                IsSubmitted = isSubmitted,
                Summary = isSubmitted
                    ? (await _service.GetSubmittedSummariesAsync(new[] { loginId.Value })).FirstOrDefault()
                    : null,
                Status = await BuildApplicantStatusAsync(loginId.Value, isSubmitted)
            });
        }

        [HttpPost("submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (!await _service.BasicDetailsExistAsync(loginId.Value))
            {
                TempData["CafMessage"] = "Please save Basic Details first.";
                return RedirectToAction("Step", new { step = CafFormMetadata.MinStep });
            }

            var preview = await BuildPreviewAsync(loginId.Value, true);

            if (preview.IsSubmitted)
            {
                TempData["CafMessage"] = "This application has already been submitted.";
                return RedirectToAction("Preview");
            }

            // Server-side guard: the stepper lets an applicant jump straight to the preview, so make
            // sure every section was actually saved (not just that the button was enabled).
            if (!preview.IsComplete)
            {
                TempData["CafError"] = "Please complete and save these sections before submitting: "
                                       + string.Join(", ", preview.IncompleteSections) + ".";
                return RedirectToAction("Preview");
            }

            var submitted = await _service.MarkSubmittedAsync(loginId.Value);
            if (!submitted)
            {
                TempData["CafError"] = "We could not submit your application. Please try again.";
                return RedirectToAction("Preview");
            }

            // a CAF the Industry officer returned is back under review once it is submitted again
            await ResetReviewOnResubmitAsync(loginId.Value);

            TempData["CafSubmitted"] = true;
            return RedirectToAction("Preview");
        }

        // ---------------- helpers ----------------

        // statuss: null / 'P' = still being filled in; anything else (e.g. 'S') = submitted and locked.
        private async Task<bool> IsSubmittedAsync(long loginId)
        {
            var status = await _service.GetStatusAsync(loginId);
            return !string.IsNullOrEmpty(status) && status != "P";
        }

        // "Where is my CAF?" — what the Industry department and the forwarded departments have done so far.
        // Shows the department, never the individual officer.
        private async Task<CafApplicantStatusVm?> BuildApplicantStatusAsync(long loginId, bool isSubmitted)
        {
            var review = await _context.CafIndustryReviews.AsNoTracking().FirstOrDefaultAsync(r => r.LoginId == loginId);

            if (!isSubmitted)
            {
                // not submitted: only worth a card if the Industry department sent it back
                if (review?.Status != "Returned") return null;

                return new CafApplicantStatusVm
                {
                    Tone = "warning",
                    Headline = "Returned for correction",
                    Detail = "The Industry Department has returned your application. Please make the corrections (use Edit on a section), then submit it again.",
                    ReviewStatus = "Returned",
                    Returned = true,
                    DecisionDate = review.LastActionDate,
                    Remarks = review.Remarks
                };
            }

            var forwards = await _context.CafDepartmentApplications
                .AsNoTracking()
                .Where(f => f.LoginId == loginId)
                .OrderBy(f => f.ForwardedDate)
                .ToListAsync();

            var vm = new CafApplicantStatusVm
            {
                ReviewStatus = review?.Status ?? "Pending",
                ViewedDate = review?.FirstViewedDate,
                DecisionDate = review?.LastActionDate,
                Forwards = forwards
            };

            switch (vm.ReviewStatus)
            {
                case "Approved":
                    vm.Tone = "success";
                    vm.Headline = "Approved";
                    vm.Detail = "Your application has been approved by the Industry Department.";
                    vm.Remarks = review?.Remarks;
                    break;
                case "Rejected":
                    vm.Tone = "danger";
                    vm.Headline = "Rejected";
                    vm.Detail = "Your application has been rejected by the Industry Department.";
                    vm.Remarks = review?.Remarks;
                    break;
                case "Forwarded":
                    vm.Tone = "info";
                    vm.Headline = "Forwarded to departments";
                    vm.Detail = $"The Industry Department has forwarded your application to {forwards.Count} department(s) for processing. Their progress is shown below.";
                    break;
                case "Viewed":
                    vm.Tone = "info";
                    vm.Headline = "Under review";
                    vm.Detail = "An Industry Department officer has opened your application and is reviewing it.";
                    break;
                default:
                    vm.Tone = "info";
                    vm.Headline = "Submitted";
                    vm.Detail = "Your application has been submitted and is waiting for the Industry Department to review it.";
                    break;
            }

            return vm;
        }

        private async Task ResetReviewOnResubmitAsync(long loginId)
        {
            var review = await _context.CafIndustryReviews.FirstOrDefaultAsync(r => r.LoginId == loginId);
            if (review == null || review.Status != "Returned") return;

            review.Status = "Pending";
            review.FirstViewedDate = null;
            review.FirstViewedByOfficerId = null;
            review.Remarks = null;
            await _context.SaveChangesAsync();
        }

        // The same loader the officers' read-only CAF view uses (ICafFormService.LoadPreviewAsync).
        private async Task<CafPreviewViewModel> BuildPreviewAsync(long loginId, bool basicExists)
        {
            var vm = await _service.LoadPreviewAsync(loginId);
            vm.BasicDetailsExist = basicExists;
            return vm;
        }

        private async Task<CafStepViewModel> BuildViewModelAsync(CafStepDef stepDef, long loginId, bool basicExists)
        {
            var vm = new CafStepViewModel
            {
                AllSteps = CafFormMetadata.Steps,
                Current = stepDef,
                BasicDetailsExist = basicExists,
                Values = await _service.LoadMainRowAsync(stepDef, loginId),
                SavedSteps = await _service.GetSavedStepNumbersAsync(CafFormMetadata.Steps, loginId)
            };

            foreach (var sub in stepDef.SubTables)
            {
                vm.SubTables.Add(new CafSubTableVm
                {
                    Def = sub,
                    Rows = await _service.GetSubRowsAsync(sub, loginId)
                });
            }

            return vm;
        }
    }
}
