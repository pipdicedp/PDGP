using System;

namespace TradeLicence.Models
{
    /// <summary>
    /// A unified status row for the citizen's "Application Status Tracking" page,
    /// combining Trade Licence and Fire NOC (and extensible for other services).
    /// </summary>
    public class ApplicationStatusItem
    {
        public string Service { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public int InternalId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public DateTime DateOfApplication { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CurrentStage { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public int TimeElapsedDays => (DateTime.Now.Date - DateOfApplication.Date).Days;
        public string ViewUrl { get; set; } = string.Empty;
        public bool CanContinue { get; set; }
        public bool CanPayNow { get; set; }
        public bool CanResubmit { get; set; }
    }
}