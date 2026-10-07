using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
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

        public CommonApplicationFormController(ICafFormService service, ILogger<CommonApplicationFormController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private long? GetLoginId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(raw, out var id) ? id : null;
        }

        [HttpGet("")]
        [HttpGet("{step:int}")]
        public async Task<IActionResult> Step(int step = 1)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

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

            var isLastStep = step == CafFormMetadata.MaxStep;
            if (isLastStep)
            {
                await _service.MarkSubmittedAsync(loginId.Value);
                TempData["CafMessage"] = "Common Application Form submitted successfully.";
                return RedirectToAction("Index", "Dashboard");
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

            var stepDef = CafFormMetadata.ByNumber(step);
            var subDef = stepDef?.SubTables.FirstOrDefault(s => s.Key == subKey);
            if (subDef == null) return NotFound();

            var removed = await _service.DeleteSubRowAsync(subDef, loginId.Value, id);
            return removed ? Ok(new { success = true }) : NotFound(new { success = false });
        }

        // ---------------- helpers ----------------

        private async Task<CafStepViewModel> BuildViewModelAsync(CafStepDef stepDef, long loginId, bool basicExists)
        {
            var vm = new CafStepViewModel
            {
                AllSteps = CafFormMetadata.Steps,
                Current = stepDef,
                BasicDetailsExist = basicExists,
                Values = await _service.LoadMainRowAsync(stepDef, loginId)
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
