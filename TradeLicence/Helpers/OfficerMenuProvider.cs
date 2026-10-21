using TradeLicence.Models;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Single source of truth for officer nav menus. To add a new
    /// designation's menu, just add a new case below — no other file
    /// needs to change.
    ///
    /// Every Controller/Action referenced here now exists, so the rendered
    /// nav has no dead links:
    ///   - Officer: Index, All, Assigned, Forwarded, History, Approvals,
    ///     Inspections, InspectionSchedule, CreateUser, ExistingOfficers
    ///   - OfficerManagement: Index (redirects to Officer/ExistingOfficers),
    ///     Workflow (stage/designation overview)
    ///   - Reports: Index, DailyScrutiny, PendingScrutiny, VerificationSummary,
    ///     InspectionSummary, ApprovalSummary, WorkflowSummary
    ///
    /// _OfficerLayout still resolves each URL at render time and drops any
    /// item that doesn't, so a future typo here degrades gracefully instead
    /// of shipping a broken link.
    /// </summary>
    public static class OfficerMenuProvider
    {
        public static List<OfficerMenuItem> GetMenuForDesignation(string? designation)
        {
            // Every officer, regardless of designation, sees this.
            var menu = new List<OfficerMenuItem>
            {
                new() { Text = "Home", Controller = "Officer", Action = "Index", Icon = "fa-solid fa-house" }
            };

            switch (designation)
            {
                case "Admin":
                    // Stage-less Administrator. Fire NOC lets an admin approve a
                    // fire application directly (skipping the DEO -> Manager ->
                    // Inspection chain), so surface its queue here.
                    menu.Add(new() { Text = "Fire NOC Applications", Controller = "FireOfficer", Action = "AdminQueue", Icon = "fa-solid fa-fire-flame-curved" });
                    // "All Applications" not added separately — the Dashboard
                    // (Officer/Index) already shows every application.

                    // Example submenu: any item with Children renders as a
                    // dropdown in the nav. Controller/Action on the parent
                    // itself ("Administration" here) are left blank — clicking
                    // the parent just opens the dropdown, it doesn't navigate.
                    menu.Add(new()
                    {
                        Text = "Administration",
                        Icon = "fa-solid fa-gears",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Manage Officers", Controller = "OfficerManagement", Action = "Index", Icon = "fa-solid fa-users-gear" },
                            new() { Text = "Workflow Settings", Controller = "OfficerManagement", Action = "Workflow", Icon = "fa-solid fa-diagram-project" },
                            new() { Text = "Reports", Controller = "Reports", Action = "Index", Icon = "fa-solid fa-chart-pie" }
                        }
                    });
                    menu.Add(new() { Text = "All Applications", Controller = "Officer", Action = "All", Icon = "fa-solid fa-layer-group" });
                    menu.Add(new()
                    {
                        Text = "Officer Creation",
                        Icon = "fa-solid fa-user-plus",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Officer Creation", Controller = "Officer", Action = "CreateUser", Icon = "fa-solid fa-user-plus" },
                            new() { Text = "Existing Officers", Controller = "Officer", Action = "ExistingOfficers", Icon = "fa-solid fa-list-check" }
                        }
                    });
                    break;

                case "DEO":
                    // Stage 1 — Initial Scrutiny. Home (Officer/Index) already
                    // shows their pending queue, so these are the extra views:
                    // what they've already forwarded, and their own reports.
                    menu.Add(new() { Text = "Forwarded Applications", Controller = "Officer", Action = "Forwarded", Icon = "fa-solid fa-share-from-square" });
                    menu.Add(new()
                    {
                        Text = "Reports",
                        Icon = "fa-solid fa-chart-column",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Daily Scrutiny Report", Controller = "Reports", Action = "DailyScrutiny", Icon = "fa-solid fa-calendar-day" },
                            new() { Text = "Pending Applications Report", Controller = "Reports", Action = "PendingScrutiny", Icon = "fa-solid fa-hourglass-half" }
                        }
                    });
                    break;

                case "Manager":
                    // Stage 2 — Verification.
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned", Icon = "fa-solid fa-inbox" });
                    menu.Add(new() { Text = "Verification History", Controller = "Officer", Action = "History", Icon = "fa-solid fa-clock-rotate-left" });
                    menu.Add(new() { Text = "Reports", Controller = "Reports", Action = "VerificationSummary", Icon = "fa-solid fa-chart-column" });
                    break;

                case "Inspection Officer":
                    menu.Add(new() { Text = "Site Inspections", Controller = "Officer", Action = "Inspections", Icon = "fa-solid fa-clipboard-check" });
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned", Icon = "fa-solid fa-inbox" });
                    menu.Add(new() { Text = "Inspection Schedule", Controller = "Officer", Action = "InspectionSchedule", Icon = "fa-solid fa-calendar-check" });
                    menu.Add(new() { Text = "Reports", Controller = "Reports", Action = "InspectionSummary", Icon = "fa-solid fa-chart-column" });
                    break;

                case "GM":
                    // Stage 4 — final Approval.
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned", Icon = "fa-solid fa-inbox" });
                    menu.Add(new() { Text = "Final Approvals", Controller = "Officer", Action = "Approvals", Icon = "fa-solid fa-file-circle-check" });
                    menu.Add(new()
                    {
                        Text = "Reports",
                        Icon = "fa-solid fa-chart-column",
                        Children = new List<OfficerMenuItem>
                        {
                            new() { Text = "Approval Summary", Controller = "Reports", Action = "ApprovalSummary", Icon = "fa-solid fa-clipboard-check" },
                            new() { Text = "Overall Workflow Report", Controller = "Reports", Action = "WorkflowSummary", Icon = "fa-solid fa-diagram-project" }
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
