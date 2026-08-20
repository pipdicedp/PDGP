using TradeLicence.Models;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Single source of truth for officer nav menus. To add a new
    /// designation's menu, just add a new case below — no other file
    /// needs to change.
    ///
    /// NOTE: several controllers/actions referenced below don't exist yet —
    /// these links are wired for when they're built. Ask to have them built
    /// out and the routes here will already be correct:
    ///   - Officer: Forwarded, History, Assigned, Inspections,
    ///     InspectionSchedule, Approvals, All
    ///   - OfficerManagement: Index, Workflow
    ///   - Reports: Index, DailyScrutiny, PendingScrutiny,
    ///     VerificationSummary, InspectionSummary, ApprovalSummary,
    ///     WorkflowSummary
    /// </summary>
    public static class OfficerMenuProvider
    {
        public static List<OfficerMenuItem> GetMenuForDesignation(string? designation)
        {
            // Every officer, regardless of designation, sees this.
            var menu = new List<OfficerMenuItem>
            {
                new() { Text = "Home", Controller = "Officer", Action = "Index" }
            };

            switch (designation)
            {
                case "Admin":
                    // "All Applications" not added separately — the Dashboard
                    // (Officer/Index) already shows every application.

                    // Example submenu: any item with Children renders as a
                    // dropdown in the nav. Controller/Action on the parent
                    // itself ("Administration" here) are left blank — clicking
                    // the parent just opens the dropdown, it doesn't navigate.
                    menu.Add(new()
                    {
                        Text = "Administration",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Manage Officers", Controller = "OfficerManagement", Action = "Index" },
                            new() { Text = "Workflow Settings", Controller = "OfficerManagement", Action = "Workflow" },
                            new() { Text = "Reports", Controller = "Reports", Action = "Index" }
                        }
                    });
                    menu.Add(new() { Text = "All Applications", Controller = "Officer", Action = "All" });
                    menu.Add(new() { Text = "User Creation", Controller = "Officer", Action = "Inspections" });
                    break;

                case "DEO":
                    // Stage 1 — Initial Scrutiny. Home (Officer/Index) already
                    // shows their pending queue, so these are the extra views:
                    // what they've already forwarded, and their own reports.
                    menu.Add(new() { Text = "Forwarded Applications", Controller = "Officer", Action = "Forwarded" });
                    menu.Add(new()
                    {
                        Text = "Reports",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Daily Scrutiny Report", Controller = "Reports", Action = "DailyScrutiny" },
                            new() { Text = "Pending Applications Report", Controller = "Reports", Action = "PendingScrutiny" }
                        }
                    });
                    break;

                case "Manager":
                    // Stage 2 — Verification.
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned" });
                    menu.Add(new() { Text = "Verification History", Controller = "Officer", Action = "History" });
                    menu.Add(new() { Text = "Reports", Controller = "Reports", Action = "VerificationSummary" });
                    break;

                case "Inspection Officer":
                    menu.Add(new() { Text = "Site Inspections", Controller = "Officer", Action = "Inspections" });
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned" });
                    menu.Add(new() { Text = "Inspection Schedule", Controller = "Officer", Action = "InspectionSchedule" });
                    menu.Add(new() { Text = "Reports", Controller = "Reports", Action = "InspectionSummary" });
                    break;

                case "GM":
                    // Stage 4 — final Approval.
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned" });
                    menu.Add(new() { Text = "Final Approvals", Controller = "Officer", Action = "Approvals" });
                    menu.Add(new()
                    {
                        Text = "Reports",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Approval Summary", Controller = "Reports", Action = "ApprovalSummary" },
                            new() { Text = "Overall Workflow Report", Controller = "Reports", Action = "WorkflowSummary" }
                        }
                    });
                    break;

                default:
                    // Unrecognized/blank designation — safe minimal menu (Dashboard only).
                    break;
            }

            return menu;
        }
    }
}