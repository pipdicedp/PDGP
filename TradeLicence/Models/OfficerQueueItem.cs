namespace TradeLicence.Models
{
    /// <summary>
    /// One row in the shared officer queue (OfficerController.Index -> SharedQueue.cshtml).
    /// Each service maps its own application entity into this before adding
    /// it to the list — the view itself never knows which service a row
    /// came from beyond the ServiceType label and the pre-built ViewUrl.
    /// </summary>
    public class OfficerQueueItem
    {
        public int ApplicationId { get; set; }
        public string ServiceType { get; set; } = string.Empty;   // "Water", "Electricity", ...
        public string DisplayName { get; set; } = string.Empty;   // applicant name — each service's own field, mapped here
        public string Contact { get; set; } = string.Empty;       // phone/mobile — each service's own field, mapped here
        public string CurrentStage { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string ViewUrl { get; set; } = string.Empty;        // pre-built link to that service's own ViewApplication page
    }
}
