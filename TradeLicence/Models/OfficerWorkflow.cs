using System.Collections.Generic;
using System.Linq;

namespace TradeLicence.Models
{
    /// <summary>
    /// Maps the 4 fixed workflow stages (what TradeLicenceApplication.CurrentStage
    /// holds) to the real Officer.Designation values in your Officers table.
    ///
    /// EDIT THIS FILE — not the controller — if your designation names
    /// change or you add/remove a stage. Everything else reads from here.
    ///
    /// Current mapping (confirmed): DEO handles Initial Scrutiny, Manager
    /// handles Verification, "Inspection Officer" handles Inspection, GM
    /// handles Approval. Admin is not part of the workflow.
    /// </summary>
    public static class OfficerWorkflow
    {
        public static readonly string[] Stages =
        {
            "Initial Scrutiny", "Verification", "Inspection", "Approval"
        };

        // Stage -> the Officer.Designation value allowed to act on it.
        public static readonly Dictionary<string, string> StageToDesignation = new()
        {
            { "Initial Scrutiny", "DEO" },
            { "Verification",     "Manager" },
            { "Inspection",       "Inspection Officer" },
            { "Approval",         "GM" }
        };

        // Every Designation value the app actually recognizes: the ones
        // wired into the workflow above, plus "Admin" (which sits outside
        // the 4 stages — see the class comment). Used to build the
        // Designation dropdown on Officer/CreateUser, so a newly created
        // officer's Designation is guaranteed to match what
        // ForwardToOfficer/ViewApplication filter Officers by — no extra
        // wiring needed for them to show up in the right "Forward To" list.
        public static readonly string[] AllDesignations =
            new[] { "Admin" }.Concat(StageToDesignation.Values.Distinct()).ToArray();

        // Reverse lookup — a Designation ("Manager") can cover more than one
        // stage, so this returns a list, not a single stage.
        public static List<string> StagesForDesignation(string? designation)
        {
            if (string.IsNullOrEmpty(designation)) return new List<string>();
            return StageToDesignation
                .Where(kv => kv.Value == designation)
                .Select(kv => kv.Key)
                .ToList();
        }

        public static int StageIndex(string stage) => System.Array.IndexOf(Stages, stage);
    }
}
