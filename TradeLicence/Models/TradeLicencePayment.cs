using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Inspection-stage payment (Application Payment + Extra Charge), one
    /// row per application. Requested by the Inspection officer via
    /// OfficerController.SendPaymentRequest — the applicant's own payment
    /// flow (once built) is what should flip PaymentStatus to "Paid".
    /// TradeLicenceApplication.PaymentStatus mirrors this row's status so
    /// ForwardToOfficer can gate on it without an extra join/query.
    /// </summary>
    public class TradeLicencePayment
    {
        [Key] public int PaymentId { get; set; }

        public int ApplicationId { get; set; }

        public decimal PaymentAmount { get; set; }
        public decimal ExtraCharge { get; set; }
        public decimal TotalPaymentAmount { get; set; }

        // "Pending" -> "Paid"
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime PaymentRequestedDate { get; set; } = DateTime.UtcNow;
        public DateTime? PaymentCompletedDate { get; set; }
    }
}
