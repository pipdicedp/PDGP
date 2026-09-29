using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Generic version of TradeLicencePayment — one shared table for every
    /// service besides TradeLicence (which keeps its own TradeLicencePayments
    /// table unchanged). ApplicationId is a soft reference (ServiceType +
    /// ApplicationId identify the application).
    /// </summary>
    public class WorkflowPayment
    {
        [Key] public int WorkflowPaymentId { get; set; }

        [Required, StringLength(50)]
        public string ServiceType { get; set; } = string.Empty;

        public int ApplicationId { get; set; }

        public decimal PaymentAmount { get; set; }
        public decimal ExtraCharge { get; set; }
        public decimal TotalPaymentAmount { get; set; }

        // "Pending" -> "Paid"
        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime PaymentRequestedDate { get; set; } = DateTime.UtcNow;
        public DateTime? PaymentCompletedDate { get; set; }
    }
}
