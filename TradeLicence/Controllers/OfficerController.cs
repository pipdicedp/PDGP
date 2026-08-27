using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Services;

namespace TradeLicence.Controllers
{
    [Authorize(Roles = "Officer")]
    public class OfficerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITradeLicenceService _service;
        private readonly PasswordHasher<Officer> _officerPasswordHasher = new();

        public OfficerController(ApplicationDbContext context, ITradeLicenceService service)
        {
            _context = context;
            _service = service;
        }

        // The officer's own id is set as ClaimTypes.NameIdentifier at login
        // (see AccountController.OfficerLogin) — NOT a separate "OfficerId"
        // claim, so we read the standard claim type here.
        private int GetCurrentOfficerId()
        {
            var raw = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        // Only the Admin designation (e.g. tladmin) may create officer
        // accounts — every officer has Role "Officer" (see
        // AccountController.OfficerLogin), so Designation is what actually
        // tells Admin apart from DEO/Manager/Inspection Officer/GM here.
        private bool IsCurrentOfficerAdmin() =>
            User.FindFirst("Designation")?.Value == "Admin";

        public async Task<IActionResult> Index()
        {
            var currentDesignation = User.FindFirst("Designation")?.Value;
            var currentOfficerId = GetCurrentOfficerId();

            // A Designation can cover more than one stage (e.g. "Manager"
            // covers both Verification and Inspection) — see OfficerWorkflow.cs.
            var myStages = OfficerWorkflow.StagesForDesignation(currentDesignation);

            // Visible to this officer if either:
            //  - it's sitting unassigned at one of their stages (anyone with
            //    that designation can pick it up), or
            //  - it's specifically been forwarded to them by name.
            var applications = await _context.TradeLicenceApplications
                .Where(a => a.Status == "Submitted" &&
                    (
                        (a.AssignedOfficerId == null && myStages.Contains(a.CurrentStage))
                        || a.AssignedOfficerId == currentOfficerId
                    ))
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            ViewBag.Designation = currentDesignation;

            return View(applications);
        }

        // Read-only application detail view, kept entirely on the officer
        // side (its own layout, no citizen wizard) — reuses the same
        // _PreviewApplication partial the citizen wizard's Preview tab uses,
        // just with the Edit links turned off.
        [HttpGet]
        public async Task<IActionResult> ViewApplication(int id)
        {
            var model = await _service.GetApplicationPreviewAsync(id);
            if (model == null) return NotFound();

            model.ShowEditLinks = false;

            var application = await _context.TradeLicenceApplications.FindAsync(id);
            var currentStage = application?.CurrentStage ?? OfficerWorkflow.Stages[0];
            var currentIndex = OfficerWorkflow.StageIndex(currentStage);

            // Officers can only forward FORWARD through the stage order
            // (Initial Scrutiny -> Verification -> Inspection -> Approval),
            // never back a step — so only later stages are offered here.
            var laterStages = OfficerWorkflow.Stages.Skip(currentIndex + 1).ToList();

            // One entry per (stage, officer) — a Manager appears once under
            // "Verification" and again under "Inspection", since the same
            // person can be the target of either stage.
            var forwardableByStage = new List<(string Stage, Officer Officer)>();
            foreach (var stage in laterStages)
            {
                var designation = OfficerWorkflow.StageToDesignation[stage];
                var officers = await _context.Officers
                    .Where(o => o.Designation == designation && !o.IsLocked)
                    .OrderBy(o => o.FullName)
                    .ToListAsync();

                forwardableByStage.AddRange(officers.Select(o => (stage, o)));
            }

            ViewBag.CurrentStage = currentStage;
            ViewBag.IsFinalStage = laterStages.Count == 0;
            ViewBag.ForwardableByStage = forwardableByStage;

            // Full audit trail — every Forward/Revert so far, oldest first.
            // Powers the "Forwarded by ..." timeline at the top of the page
            // (and for GM, this naturally shows the whole DEO -> Manager ->
            // Inspection Officer chain since it's just this list).
            var history = await _context.ApplicationWorkflowHistories
                .Where(h => h.ApplicationId == id)
                .OrderBy(h => h.ActionDate)
                .ToListAsync();

            // Officer names for display — one query instead of N+1 per history row.
            var officerIds = history
                .SelectMany(h => new[] { h.FromOfficerId, h.ToOfficerId })
                .Where(x => x.HasValue).Select(x => x!.Value)
                .Distinct().ToList();
            var officerNames = await _context.Officers
                .Where(o => officerIds.Contains(o.OfficerId))
                .ToDictionaryAsync(o => o.OfficerId, o => o.FullName ?? o.Username);

            // Revert target — the most recent Forward entry that landed the
            // application at its CURRENT stage tells us who to send it back
            // to. Nothing to revert to at Initial Scrutiny (the first stage).
            var revertTarget = history
                .Where(h => h.ActionType == "Forward" && h.ToStage == currentStage)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefault();

            ViewBag.WorkflowHistory = history;
            ViewBag.OfficerNames = officerNames;
            ViewBag.RevertTarget = revertTarget;

            return View(model);
        }

        // Sends the application back to the applicant for correction —
        // status changes so it drops off this officer's "Submitted" queue,
        // and the remarks tell the applicant what needs fixing.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnToApplicant(int id, string remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            application.Status = "ReturnedToApplicant";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = "Application returned to the applicant successfully.";
            TempData["OfficerActionType"] = "return";
            return RedirectToAction("Index");
        }

        // Forwards the application to a specific officer AT A SPECIFIC STAGE
        // (Initial Scrutiny -> Verification -> Inspection -> Approval).
        // The stage is taken from the form (not derived from the officer's
        // Designation) because one designation — "Manager" — covers two
        // stages, so Designation alone can't tell them apart.
        // Replaces the old fixed "Forward to GM" action.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForwardToOfficer(int id, int officerId, string targetStage, string remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            var officer = await _context.Officers.FindAsync(officerId);
            if (officer == null)
                return BadRequest(new { error = "Please choose a valid officer to forward to." });

            var expectedDesignation = OfficerWorkflow.StageToDesignation.GetValueOrDefault(targetStage);
            if (expectedDesignation == null || officer.Designation != expectedDesignation)
                return BadRequest(new { error = "That officer doesn't match the selected stage." });

            var currentIndex = OfficerWorkflow.StageIndex(application.CurrentStage);
            var targetIndex = OfficerWorkflow.StageIndex(targetStage);

            if (targetIndex <= currentIndex)
                return BadRequest(new { error = "You can only forward to a later stage, not the current or an earlier one." });

            var fromStage = application.CurrentStage;
            var fromOfficerId = application.AssignedOfficerId;

            application.AssignedOfficerId = officer.OfficerId;
            application.CurrentStage = targetStage;
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            _context.ApplicationWorkflowHistories.Add(new ApplicationWorkflowHistory
            {
                ApplicationId = application.ApplicationId,
                FromOfficerId = fromOfficerId ?? GetCurrentOfficerId(),
                FromStage = fromStage,
                ToOfficerId = officer.OfficerId,
                ToStage = targetStage,
                ActionType = "Forward",
                Remarks = remarks,
                ActionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = $"Application successfully forwarded to {officer.FullName} ({targetStage}).";
            TempData["OfficerActionType"] = "forward";
            return RedirectToAction("Index");
        }

        // Sends the application back one step to whichever officer most
        // recently forwarded it to the current stage — GM reverts to the
        // Inspection Officer who sent it to them, Inspection Officer
        // reverts to the Manager, Manager reverts to the DEO. Not offered
        // at Initial Scrutiny (nothing before it to revert to).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevertToPreviousOfficer(int id, string remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            var revertTarget = await _context.ApplicationWorkflowHistories
                .Where(h => h.ApplicationId == id && h.ActionType == "Forward" && h.ToStage == application.CurrentStage)
                .OrderByDescending(h => h.ActionDate)
                .FirstOrDefaultAsync();

            if (revertTarget == null || revertTarget.FromOfficerId == null)
                return BadRequest(new { error = "There's no earlier officer to revert this application to." });

            var previousOfficer = await _context.Officers.FindAsync(revertTarget.FromOfficerId.Value);
            if (previousOfficer == null)
                return BadRequest(new { error = "The previous officer's account could not be found." });

            var fromStage = application.CurrentStage;
            var fromOfficerId = application.AssignedOfficerId;

            application.AssignedOfficerId = previousOfficer.OfficerId;
            application.CurrentStage = revertTarget.FromStage;
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            _context.ApplicationWorkflowHistories.Add(new ApplicationWorkflowHistory
            {
                ApplicationId = application.ApplicationId,
                FromOfficerId = fromOfficerId ?? GetCurrentOfficerId(),
                FromStage = fromStage,
                ToOfficerId = previousOfficer.OfficerId,
                ToStage = revertTarget.FromStage,
                ActionType = "Revert",
                Remarks = remarks,
                ActionDate = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = $"Application reverted to {previousOfficer.FullName} ({revertTarget.FromStage}).";
            TempData["OfficerActionType"] = "revert";
            return RedirectToAction("Index");
        }

        // Final sign-off — only valid once the application has reached the
        // last stage (Approval / GM). Sets Status to "Approved", which is
        // what drives citizen-side actions like "Download Certificate" —
        // check GenerateAcknowledgementPdfAsync / the citizen dashboard's
        // Download Certificate condition if you gate that on Status too.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int id, string? remarks)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(id);
            if (application == null) return NotFound();

            var lastStage = OfficerWorkflow.Stages[^1]; // "Approval"
            if (application.CurrentStage != lastStage)
                return BadRequest(new { error = "This application hasn't reached the final approval stage yet." });

            application.Status = "Approved";
            application.OfficerRemarks = remarks;
            application.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] = "Application approved successfully.";
            TempData["OfficerActionType"] = "approve";
            return RedirectToAction("Index");
        }

        // ---------------- Admin: Officer (User) Creation ----------------
        // Admin-only "User Creation" page. Creates a row in the Officers
        // table. The Designation picked here is exactly the value
        // ForwardToOfficer/ViewApplication filter Officers by (see
        // OfficerWorkflow.StageToDesignation) — so e.g. a new officer
        // created with Designation "Manager" shows up in the Verification
        // stage's "Forward To" dropdown immediately, with no other change
        // needed anywhere else in the app.
        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!IsCurrentOfficerAdmin()) return Forbid();

            ViewBag.Designations = OfficerWorkflow.AllDesignations;
            return View(new OfficerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(OfficerCreateViewModel model)
        {
            if (!IsCurrentOfficerAdmin()) return Forbid();

            ViewBag.Designations = OfficerWorkflow.AllDesignations;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usernameTaken = await _context.Officers.AnyAsync(o => o.Username == model.Username);
            if (usernameTaken)
            {
                ModelState.AddModelError(nameof(model.Username), "This username is already taken.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var emailTaken = await _context.Officers.AnyAsync(o => o.Email == model.Email);
                if (emailTaken)
                {
                    ModelState.AddModelError(nameof(model.Email), "This email is already registered.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var newOfficer = new Officer
            {
                Username = model.Username,
                FullName = model.FullName,
                Department = model.Department,
                Designation = model.Designation,
                Email = model.Email,
                IsLocked = model.IsLocked,
                FailedLoginAttempts = 0,
                LastLoginDate = null,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = User.Identity?.Name
            };
            // Same PasswordHasher<Officer> approach AccountController uses to
            // verify logins — never store the typed password as-is.
            newOfficer.PasswordHash = _officerPasswordHasher.HashPassword(newOfficer, model.Password);

            _context.Officers.Add(newOfficer);
            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] =
                $"Officer account '{newOfficer.Username}' created successfully as {newOfficer.Designation}.";
            TempData["OfficerActionType"] = "created";

            return RedirectToAction("CreateUser");
        }

        // Read-only list of every officer account, with an Active/Inactive
        // toggle and a delete option per row. Separate page from CreateUser
        // so the creation form isn't cluttered with the full list every time.
        [HttpGet]
        public async Task<IActionResult> ExistingOfficers()
        {
            if (!IsCurrentOfficerAdmin()) return Forbid();

            var officers = await _context.Officers
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return View(officers);
        }

        // Flips Officer.IsLocked — the same flag AccountController.OfficerLogin
        // checks before verifying a password, so "Inactive" here means the
        // account genuinely can't log in, not just a cosmetic label.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleOfficerStatus(int officerId)
        {
            if (!IsCurrentOfficerAdmin()) return Forbid();

            var officer = await _context.Officers.FindAsync(officerId);
            if (officer == null) return NotFound();

            // Guard against an admin locking themselves out mid-session —
            // there's no other way back in if they're the only Admin account.
            if (officerId == GetCurrentOfficerId() && !officer.IsLocked)
            {
                TempData["OfficerActionMessage"] = "You can't set your own account to Inactive while logged in.";
                TempData["OfficerActionType"] = "return";
                return RedirectToAction("ExistingOfficers");
            }

            officer.IsLocked = !officer.IsLocked;

            // Unlocking without resetting this would leave them one or two
            // bad logins away from being auto-locked again immediately —
            // see AccountController.OfficerLogin's MaxFailedAttempts check.
            if (!officer.IsLocked)
            {
                officer.FailedLoginAttempts = 0;
            }

            await _context.SaveChangesAsync();

            TempData["OfficerActionMessage"] =
                $"'{officer.Username}' is now {(officer.IsLocked ? "Inactive" : "Active")}.";
            TempData["OfficerActionType"] = "toggled";

            return RedirectToAction("ExistingOfficers");
        }

        // Permanently removes an officer account. Officers.OfficerId is
        // referenced by ApplicationWorkflowHistories.FromOfficerId/ToOfficerId
        // with no cascade or set-null configured (see the Database script) —
        // deleting an officer who has ever forwarded/received an application
        // would violate that FK, so that case is caught and turned into a
        // friendly message rather than a raw SQL error. Locking (via
        // ToggleOfficerStatus) is the fallback for those accounts.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOfficer(int officerId)
        {
            if (!IsCurrentOfficerAdmin()) return Forbid();

            var officer = await _context.Officers.FindAsync(officerId);
            if (officer == null) return NotFound();

            if (officerId == GetCurrentOfficerId())
            {
                TempData["OfficerActionMessage"] = "You can't delete your own account while logged in.";
                TempData["OfficerActionType"] = "return";
                return RedirectToAction("ExistingOfficers");
            }

            var username = officer.Username;

            try
            {
                _context.Officers.Remove(officer);
                await _context.SaveChangesAsync();

                TempData["OfficerActionMessage"] = $"Officer account '{username}' deleted.";
                TempData["OfficerActionType"] = "deleted";
            }
            catch (DbUpdateException)
            {
                TempData["OfficerActionMessage"] =
                    $"'{username}' can't be deleted — they appear in the workflow history of one or more applications. Set them to Inactive instead.";
                TempData["OfficerActionType"] = "return";
            }

            return RedirectToAction("ExistingOfficers");
        }
    }
}
