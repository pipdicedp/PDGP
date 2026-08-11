using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class ShopEstablishmentRegistration
    {
        [Key]
        public int ShopRegistrationId { get; set; }

        public int ApplicationId { get; set; }

        public string? ApplicantName { get; set; }
        public string? ShopOrEstablishmentName { get; set; }
        public string? RegistrationPeriod { get; set; }
        public string? TypeOfEstablishment { get; set; }
        public string? MobileNumber { get; set; }
        public string? EmailId { get; set; }

        // ---- Shop Address ----
        public string? ShopAddressLine1 { get; set; }
        public string? ShopAddressLine2 { get; set; }
        public string? ShopDistrictRegion { get; set; }
        public string? ShopCommune { get; set; }
        public string? ShopPinCode { get; set; }

        // ---- Communication Address ----
        public string? CommAddressLine1 { get; set; }
        public string? CommAddressLine2 { get; set; }
        public string? CommDistrictRegion { get; set; }
        public string? CommCommune { get; set; }
        public string? CommPinCode { get; set; }

        // ---- Employee Details ----
        public int? MaxEmployeesProposed { get; set; }
        public int? MaleEmployees { get; set; }
        public int? FemaleEmployees { get; set; }
        public int? TransgenderEmployees { get; set; }
        public int TotalEmployees { get; set; }

        // ---- Manager Details ----
        public string? ManagerFullName { get; set; }
        public string? ManagerAddressLine1 { get; set; }
        public string? ManagerAddressLine2 { get; set; }
        public string? ManagerCountry { get; set; }
        public string? ManagerState { get; set; }
        public string? ManagerDistrict { get; set; }
        public string? ManagerPostalZipCode { get; set; }
        public string? ManagerMobileNumber { get; set; }

        // ---- Migrant Workers ----
        public int? MigrantWorkersDirect { get; set; }
        public int? MigrantWorkersThroughContractor { get; set; }

        // ---- Payment Details ----
        public string? DateOfPaymentOfWages { get; set; }
        public string? FormIXNote { get; set; }
        public decimal? AmountPaid { get; set; }
        public string? GrasReferenceNumber { get; set; }
        public DateTime? DateOfPayment { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}