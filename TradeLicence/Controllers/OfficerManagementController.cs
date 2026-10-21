using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Controllers
{
    /// <summary>
    /// Administration pages behind the Admin's "Administration" menu:
    /// "Manage Officers" and "Workflow Settings". Both are admin-only.
    /// </summary>
    [Authorize(Roles = "Officer")]
    public class OfficerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OfficerManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentOfficerId()
        {
            var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        private async Task<bool> IsAdminAsync()
        {
            var me = await _context.Officers.FindAsync(GetCurrentOfficerId());
            return me != null && string.Equals(me.Designation, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        // "Manage Officers" is really the Existing Officers table (create /
        // activate / deactivate / delete). Keep the menu route and send the
        // admin there — one source of truth for officer administration.
        [HttpGet]
        public IActionResult Index() => RedirectToAction("ExistingOfficers", "Officer");

        // Read-only overview of the 4-stage workflow and who staffs each stage.
        // The mapping itself lives in Models/OfficerWorkflow.cs (code, not DB),
        // so this page reflects it rather than editing it.
        [HttpGet]
        public async Task<IActionResult> Workflow()
        {
            if (!await IsAdminAsync()) return Forbid();

            var officers = await _context.Officers
                .OrderBy(o => o.Designation)
                .ThenBy(o => o.Username)
                .ToListAsync();

            static OfficerWorkflowOfficer ToRow(Officer o) => new()
            {
                Username = o.Username,
                FullName = o.FullName,
                Department = o.Department,
                IsLocked = o.IsLocked
            };

            var model = new OfficerWorkflowViewModel
            {
                TotalOfficers = officers.Count,
                Stages = OfficerWorkflow.Stages
                    .Select(stage =>
                    {
                        OfficerWorkflow.StageToDesignation.TryGetValue(stage, out var designation);
                        return new OfficerWorkflowStage
                        {
                            Stage = stage,
                            Designation = designation ?? "—",
                            Officers = officers
                                .Where(o => string.Equals(o.Designation, designation, StringComparison.OrdinalIgnoreCase))
                                .Select(ToRow)
                                .ToList()
                        };
                    })
                    .ToList(),
                AdminOfficers = officers
                    .Where(o => string.Equals(o.Designation, "Admin", StringComparison.OrdinalIgnoreCase))
                    .Select(ToRow)
                    .ToList()
            };

            return View(model);
        }
    }
}
