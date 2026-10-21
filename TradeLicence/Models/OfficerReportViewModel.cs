using System;
using System.Collections.Generic;

namespace TradeLicence.Models
{
    /// <summary>One row of a report's breakdown table (status / stage / service + count).</summary>
    public class ReportCount
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// Backs Views/Reports/Index.cshtml — the one report page every
    /// Reports/* menu item renders (only the title, subtitle and the filtered
    /// detail list change between them). Stats always describe the officer's
    /// whole register; Rows are the rows that report is actually about.
    /// </summary>
    public class OfficerReportViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;

        public string OfficerName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; }

        public OfficerQueueStats Stats { get; set; } = new();
        public List<OfficerQueueItem> Rows { get; set; } = new();

        public List<ReportCount> ByStatus { get; set; } = new();
        public List<ReportCount> ByStage { get; set; } = new();
        public List<ReportCount> ByService { get; set; } = new();
    }
}
