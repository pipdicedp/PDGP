using System.Collections.Generic;

namespace TradeLicence.Models.Caf
{
    /// <summary>
    /// One step of the Common Application Form as it appears on the read-only
    /// Preview screen: the step definition plus whatever the applicant has
    /// saved for it so far.
    /// </summary>
    public class CafPreviewSection
    {
        public CafStepDef Step { get; set; } = null!;

        /// <summary>True once the step's main row (or, for documents, any upload) exists.</summary>
        public bool HasRow { get; set; }

        /// <summary>Saved values of the step's main row, keyed by CafField.Name (empty until saved).</summary>
        public Dictionary<string, string?> Values { get; set; } = new();

        /// <summary>Each child table of the step together with its saved rows.</summary>
        public List<CafSubTableVm> SubTables { get; set; } = new();
    }

    /// <summary>
    /// Everything the read-only Preview page needs for one applicant. Built by
    /// CafFormService.LoadPreviewAsync and shared by the citizen Preview page
    /// and the officer "View Application" pages.
    /// </summary>
    public class CafPreviewViewModel
    {
        public List<CafStepDef> AllSteps { get; set; } = new();

        public List<CafPreviewSection> Sections { get; set; } = new();

        /// <summary>True once Step 1 (Basic Details) has ever been saved.</summary>
        public bool BasicDetailsExist { get; set; }

        /// <summary>True when every step has been saved at least once, so submission is allowed.</summary>
        public bool IsComplete { get; set; }

        /// <summary>True once the applicant has submitted (caf_basic_details.statuss = 'S').</summary>
        public bool IsSubmitted { get; set; }

        /// <summary>Step numbers whose main row has been saved — drives the stepper's green ticks.</summary>
        public HashSet<int> SavedSteps { get; set; } = new();

        /// <summary>Set right after the applicant clicks Submit, so the page can confirm it once.</summary>
        public bool JustSubmitted { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>Citizen preview: show per-section "Edit" links. Officer views set this false.</summary>
        public bool AllowEdit { get; set; }

        /// <summary>Officer views show each section collapsed with expand/collapse controls.</summary>
        public bool Collapsible { get; set; }
    }

    /// <summary>
    /// The left-hand wizard stepper, shared by the six Step pages and the
    /// Preview page (see Views/CommonApplicationForm/_CafStepper.cshtml).
    /// </summary>
    public class CafStepperVm
    {
        public List<CafStepDef> AllSteps { get; set; } = new();

        /// <summary>The step being viewed; CafFormMetadata.PreviewStep while on the Preview page.</summary>
        public int CurrentNumber { get; set; }

        public bool BasicDetailsExist { get; set; }

        public bool IsSubmitted { get; set; }

        /// <summary>Step numbers already saved, so the stepper can mark them done.</summary>
        public HashSet<int> SavedSteps { get; set; } = new();
    }
}
