using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TradeLicence.Data;
using TradeLicence.Helpers;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using TradeLicence.Models.Caf;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
  
    [Authorize]
    [Route("fire_clearance")]
    public class FireClearanceController : Controller
    {
        private readonly IFireFormService _service;
        private readonly IFireNocPdfService _pdf;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FireClearanceController> _logger;

        public FireClearanceController(
            IFireFormService service,
            IFireNocPdfService pdf,
            ApplicationDbContext context,
            ILogger<FireClearanceController> logger)
        {
            _service = service;
            _pdf = pdf;
            _context = context;
            _logger = logger;
        }

        // ---------------- helpers ----------------

        private long? GetLoginId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(raw, out var id) ? id : (long?)null;
        }

        private WorkflowEngineService<FireClearanceApplication> CreateEngine() =>
            new WorkflowEngineService<FireClearanceApplication>(
                _context, _context, FireWorkflowConfig.ServiceType, FireWorkflowConfig.Department,
                requirePaymentAtInspection: false);

        /// <summary>Draft (including "no row yet") and ReturnedToApplicant are editable; everything else is read-only.</summary>
        private static bool IsEditable(string? status) =>
            string.IsNullOrEmpty(status)
            || string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "ReturnedToApplicant", StringComparison.OrdinalIgnoreCase);

        // ---------------- status page ----------------

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (!await _service.BasicDetailsExistAsync(loginId.Value))
                return RedirectToAction("Step", new { step = FireFormMetadata.MinStep });

            var app = await _service.GetApplicationAsync(loginId.Value);
            var preview = await _service.LoadPreviewAsync(loginId.Value);
            preview.AllowEdit = IsEditable(app?.Status);
            preview.Collapsible = false;

            if (TempData["FireMessage"] is string msg) preview.SuccessMessage = msg;

            return View(new FireApplicationVm
            {
                Application = app!,
                Preview = preview,
                DocumentSizes = await _service.GetDocumentSizesAsync(loginId.Value)
            });
        }

        // ---------------- wizard ----------------

        [HttpGet("step/{step:int}")]
        public async Task<IActionResult> Step(int step)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var stepDef = FireFormMetadata.ByNumber(step);
            if (stepDef == null) return NotFound();

            var basicExists = await _service.BasicDetailsExistAsync(loginId.Value);
            var app = await _service.GetApplicationAsync(loginId.Value);

            // Once the application has left the applicant's hands, the wizard is read-only.
            if (basicExists && !IsEditable(app?.Status))
                return RedirectToAction("Index");

            if (!basicExists && stepDef.Key != "applicant")
            {
                TempData["FireMessage"] = "Please save the Applicant Details first.";
                return RedirectToAction("Step", new { step = FireFormMetadata.MinStep });
            }

            var vm = await BuildViewModelAsync(stepDef, loginId.Value, basicExists);
            if (TempData["FireMessage"] is string msg) vm.SuccessMessage = msg;

            return View("Step", vm);
        }

        [HttpPost("step/{step:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStep(int step, [FromForm] IFormCollection form)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var stepDef = FireFormMetadata.ByNumber(step);
            if (stepDef == null) return NotFound();

            var basicExists = await _service.BasicDetailsExistAsync(loginId.Value);
            if (!basicExists && stepDef.Key != "applicant")
            {
                TempData["FireMessage"] = "Please save the Applicant Details first.";
                return RedirectToAction("Step", new { step = FireFormMetadata.MinStep });
            }

            // A submitted / approved / rejected application is read-only. The GET
            // action already guards this, but without the guard here a direct POST
            // (or a stale form) could re-save the last step — and MarkSubmittedAsync
            // would then silently flip an approved NOC's status back to "Submitted".
            var existing = await _service.GetApplicationAsync(loginId.Value);
            if (!IsEditable(existing?.Status))
                return RedirectToAction("Index");

            if (stepDef.Key == "documents")
                return await SaveDocumentsStepAsync(stepDef, loginId.Value, form, basicExists);

            var posted = stepDef.Fields.ToDictionary(f => f.Name, f => (string?)form[f.Name].ToString());

            var missing = new List<string>();
            foreach (var f in stepDef.Fields.Where(x => x.Required))
            {
                var v = posted.TryGetValue(f.Name, out var sv) ? sv : null;
                if (string.IsNullOrWhiteSpace(v))
                    missing.Add(f.Label);
            }
            if (missing.Count > 0)
            {
                var errVm = await BuildViewModelAsync(stepDef, loginId.Value, basicExists);
                errVm.Values = posted;
                errVm.ErrorMessage = "Please fill all mandatory fields: " + string.Join(", ", missing);
                return View("Step", errVm);
            }

            try
            {
                await _service.SaveMainRowAsync(stepDef, loginId.Value, posted);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Fire NOC save failed on step {Step} ({Table}) for login {LoginId}: SQL error {Number}",
                    step, FireFormMetadata.MainTable, loginId.Value, ex.Number);

                var errVm = await BuildViewModelAsync(stepDef, loginId.Value, basicExists);
                errVm.Values = posted;
                errVm.ErrorMessage = $"Could not save {stepDef.Title} (database error {ex.Number}). Please check the values entered and try again.";
                return View("Step", errVm);
            }

            if (step == FireFormMetadata.MaxStep)
            {
                await _service.MarkSubmittedAsync(loginId.Value);
                TempData["FireMessage"] = "Fire NOC application submitted successfully.";
                return RedirectToAction("Index");
            }

            TempData["FireMessage"] = $"{stepDef.Title} saved.";
            return RedirectToAction("Step", new { step = step + 1 });
        }

        // ---------------- Preview & Submit ----------------

        [HttpGet("preview")]
        public async Task<IActionResult> Preview()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var app = await _service.GetApplicationAsync(loginId.Value);
            var vm = await _service.LoadPreviewAsync(loginId.Value);
            vm.AllowEdit = IsEditable(app?.Status);
            vm.Collapsible = false;

            if (TempData["FireJustSubmitted"] is string just && just == "1") vm.JustSubmitted = true;
            if (TempData["FireMessage"] is string msg) vm.SuccessMessage = msg;

            ViewBag.DocumentSizes = await _service.GetDocumentSizesAsync(loginId.Value);
            return View("Preview", vm);
        }

        [HttpPost("submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var app = await _service.GetApplicationAsync(loginId.Value);
            if (!IsEditable(app?.Status)) return RedirectToAction("Index");

            var vm = await _service.LoadPreviewAsync(loginId.Value);

            if (!vm.IsComplete)
            {
                vm.ErrorMessage = "Please save every section before submitting the application.";
                vm.AllowEdit = true;
                ViewBag.DocumentSizes = await _service.GetDocumentSizesAsync(loginId.Value);
                return View("Preview", vm);
            }

            await _service.MarkSubmittedAsync(loginId.Value);
            var updated = await _service.GetApplicationAsync(loginId.Value);
            TempData["FireMessage"] = "Fire NOC application submitted successfully." +
                (updated?.ApplicationCode is string code ? $" Reference No: {code}." : "");

            return RedirectToAction("Index");
        }

        /// <summary>Applicant fixed what the officer asked for and sends it straight back to them.</summary>
        [HttpPost("resubmit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resubmit()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var app = await _service.GetApplicationAsync(loginId.Value);
            if (app == null) return NotFound();

            if (!string.Equals(app.Status, "ReturnedToApplicant", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index");

            var result = await CreateEngine().ResubmitAsync(app.Id, (int)loginId.Value, recordHistory: true);
            TempData["FireMessage"] = result.Success
                ? "Application resubmitted to the officer successfully."
                : (result.Error ?? "The application could not be resubmitted.");

            return RedirectToAction("Index");
        }

        // ---------------- Documents ----------------

        private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        /// <summary>Content type from the file's first bytes (not its name), or null if it isn't a PDF/JPG/PNG.</summary>
        private static string? SniffContentType(byte[] b)
        {
            if (b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46) return "application/pdf";               // %PDF
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            return null;
        }

        private async Task<IActionResult> SaveDocumentsStepAsync(CafStepDef stepDef, long loginId, IFormCollection form, bool basicExists)
        {
            var toSave = new Dictionary<string, byte[]>();
            var problems = new List<string>();

            foreach (var doc in stepDef.Documents)
            {
                var file = form.Files.GetFile(doc.Column);
                if (file == null || file.Length == 0) continue;

                if (file.Length > FireFormMetadata.MaxDocumentBytes)
                {
                    problems.Add($"{doc.Label}: file is larger than 5 MB.");
                    continue;
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedDocumentExtensions.Contains(ext))
                {
                    problems.Add($"{doc.Label}: only PDF, JPG or PNG files are accepted.");
                    continue;
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                if (SniffContentType(bytes) == null)
                {
                    problems.Add($"{doc.Label}: the file is not a valid PDF, JPG or PNG.");
                    continue;
                }

                toSave[doc.Column] = bytes;
            }

            try
            {
                if (toSave.Count > 0)
                    await _service.SaveDocumentsAsync(loginId, toSave);

                var onFile = await _service.GetDocumentSizesAsync(loginId);
                foreach (var doc in stepDef.Documents.Where(d => d.Required && !onFile.ContainsKey(d.Column)))
                    problems.Add($"{doc.Label}: this document is required.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Fire NOC document save failed for login {LoginId}: SQL error {Number}", loginId, ex.Number);
                problems.Add($"Could not save the documents (database error {ex.Number}). Please try again.");
            }

            if (problems.Count > 0)
            {
                var errVm = await BuildViewModelAsync(stepDef, loginId, basicExists);
                if (toSave.Count > 0)
                    errVm.SuccessMessage = $"{toSave.Count} document(s) uploaded.";
                errVm.ErrorMessage = "The application was not submitted. " + string.Join(" ", problems);
                return View("Step", errVm);
            }

            await _service.MarkSubmittedAsync(loginId);
            var updated = await _service.GetApplicationAsync(loginId);
            TempData["FireMessage"] = "Fire NOC application submitted successfully." +
                (updated?.ApplicationCode is string code ? $" Reference No: {code}." : "");

            return RedirectToAction("Index");
        }

        /// <summary>Opens one of the applicant's own uploaded documents. loginid comes from the cookie, never from the URL.</summary>
        [HttpGet("document/{column}")]
        public async Task<IActionResult> Document(string column)
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var def = FireFormMetadata.DocumentByColumn(column);
            if (def == null) return NotFound();

            var bytes = await _service.GetDocumentAsync(loginId.Value, def.Column);
            if (bytes == null) return NotFound();

            var contentType = SniffContentType(bytes) ?? "application/octet-stream";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(bytes, contentType);
        }

        // ---------------- PDFs ----------------

        [HttpGet("acknowledgement")]
        public async Task<IActionResult> Acknowledgement()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            if (!await _service.BasicDetailsExistAsync(loginId.Value))
                return NotFound();

            var bytes = await _pdf.GenerateAcknowledgementPdfAsync(loginId.Value);
            return File(bytes, "application/pdf", "Fire-NOC-Acknowledgement.pdf");
        }

        [HttpGet("noc")]
        public async Task<IActionResult> Noc()
        {
            if (User.IsInRole("Officer")) return Forbid();

            var loginId = GetLoginId();
            if (loginId == null) return Unauthorized();

            var app = await _service.GetApplicationAsync(loginId.Value);
            if (app == null || !string.Equals(app.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                return NotFound();

            var bytes = await _pdf.GenerateNocPdfAsync(loginId.Value);
            var fileName = $"Fire-NOC-{app.ApplicationCode?.Replace('/', '-') ?? app.Id.ToString()}.pdf";
            return File(bytes, "application/pdf", fileName);
        }

        // ---------------- helpers ----------------

        private async Task<CafStepViewModel> BuildViewModelAsync(CafStepDef stepDef, long loginId, bool basicExists)
        {
            var vm = new CafStepViewModel
            {
                AllSteps = FireFormMetadata.Steps,
                Current = stepDef,
                BasicDetailsExist = basicExists,
                Values = stepDef.Documents.Count > 0
                    ? new Dictionary<string, string?>()
                    : await _service.LoadMainRowAsync(stepDef, loginId)
            };

            if (stepDef.Documents.Count > 0)
            {
                var sizes = await _service.GetDocumentSizesAsync(loginId);
                foreach (var doc in stepDef.Documents)
                {
                    vm.Documents.Add(new CafDocumentVm
                    {
                        Def = doc,
                        SizeBytes = sizes.TryGetValue(doc.Column, out var len) ? len : null
                    });
                }
            }

            return vm;
        }
    }
}
