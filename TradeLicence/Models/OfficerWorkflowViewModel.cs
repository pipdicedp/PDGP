using System.Collections.Generic;

namespace TradeLicence.Models
{
    /// <summary>
    /// Backs Views/OfficerManagement/Workflow.cshtml — a read-only picture of
    /// the fixed 4-stage workflow (Models/OfficerWorkflow.cs) and which officer
    /// designation staffs each stage.
    /// </summary>
    public class OfficerWorkflowViewModel
    {
        public List<OfficerWorkflowStage> Stages { get; set; } = new();
        public List<OfficerWorkflowOfficer> AdminOfficers { get; set; } = new();
        public int TotalOfficers { get; set; }
    }

    public class OfficerWorkflowStage
    {
        public string Stage { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public List<OfficerWorkflowOfficer> Officers { get; set; } = new();
    }

    public class OfficerWorkflowOfficer
    {
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Department { get; set; }
        public bool IsLocked { get; set; }
    }
}
