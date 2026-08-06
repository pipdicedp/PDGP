namespace WaterConnection.Models
{
    // Backs the Check Application Status page. Read-only lookup -- not persisted.
    public class WaterConnectionStatusViewModel
    {
        public int? ApplicationId { get; set; }

        // True once a search has been submitted (vs. the initial blank form)
        public bool Searched { get; set; }

        public bool Found { get; set; }

        public string? Status { get; set; }
        public string? ApplicantName { get; set; }
        public DateTime? ApplicationDate { get; set; }
    }
}
