using TradeLicence.Models;

namespace TradeLicence.Helpers
{
    /// <summary>
    /// Single source of truth for officer nav menus. To add a new
    /// designation's menu, just add a new case below — no other file
    /// needs to change.
    ///
    /// NOTE: OfficerDashboard / OfficerManagement / Reports controllers
    /// don't exist yet — these links are wired for when they're built.
    /// Ask to have them built out and the routes here will already be correct.
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
                            new() { Text = "Reports", Controller = "Reports", Action = "Index" }
                        }
                    });
                    menu.Add(new() { Text = "User Creation", Controller = "Officer", Action = "Inspections" });
                    break;

                case "Inspector":
                    menu.Add(new() { Text = "Site Inspections", Controller = "Officer", Action = "Inspections" });
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned" });
                    break;

                case "Clerk":
                    menu.Add(new() { Text = "Assigned Applications", Controller = "Officer", Action = "Assigned" });
                    break;

                default:
                    // Unrecognized/blank designation — safe minimal menu (Dashboard only).
                    break;
            }

            return menu;
        }
    }
}
