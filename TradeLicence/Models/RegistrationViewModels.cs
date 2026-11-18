namespace TradeLicence.Models
{
    // ======================= Registration of Firms =======================
    public class FirmPartnerInput
    {
        public int Id { get; set; }                       // 0 = newly added row
        public string? Name { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public string? PermanentAddress { get; set; }
        public IFormFile? IdProofFile { get; set; }
        public IFormFile? SignatureFile { get; set; }

        // display only (filled by the server, never trusted on post)
        public int? IdProofDocumentId { get; set; }
        public int? SignatureDocumentId { get; set; }
    }

    public class FirmFormViewModel
    {
        public int Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public string Status { get; set; } = "Draft";
        public string? OfficerRemarks { get; set; }

        public string? ApplicantName { get; set; }
        public string? ApplicantAddress { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? ProposedFirmName { get; set; }
        public string? FirmName { get; set; }
        public string? PrincipalPlace { get; set; }
        public string? OtherPlace { get; set; }
        public string? Station { get; set; }
        public bool Declaration { get; set; }

        public List<FirmPartnerInput> Partners { get; set; } = new();

        public IFormFile? FormIFile { get; set; }
        public IFormFile? PartnershipDeedFile { get; set; }
        public IFormFile? AddressProofFile { get; set; }

        // display only
        public int? FormIDocumentId { get; set; }
        public int? PartnershipDeedDocumentId { get; set; }
        public int? AddressProofDocumentId { get; set; }
    }

    // ======================= Society Registration / Renewal =======================
    public class SocietyMemberInput
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public DateTime? DateOfAppointment { get; set; }
        public string? Address { get; set; }
        public IFormFile? IdProofFile { get; set; }
        public IFormFile? SignatureFile { get; set; }

        public int? IdProofDocumentId { get; set; }
        public int? SignatureDocumentId { get; set; }
    }

    public class SocietyFormViewModel
    {
        public int Id { get; set; }
        public string? ApplicationNumber { get; set; }
        public string Status { get; set; } = "Draft";
        public string? OfficerRemarks { get; set; }

        public string ApplicationType { get; set; } = "Registration";
        public string? ExistingRegistrationNumber { get; set; }
        public DateTime? LicenceExpiryDate { get; set; }

        public string? ApplicantName { get; set; }
        public string? ApplicantAddress { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? ApplicantDesignation { get; set; }
        public string? SocietyName { get; set; }
        public string? ProposedSocietyName { get; set; }
        public string? SocietyAddress { get; set; }
        public bool Declaration { get; set; }

        public List<SocietyMemberInput> Members { get; set; } = new();

        public IFormFile? MemorandumFile { get; set; }
        public IFormFile? RulesFile { get; set; }
        public IFormFile? PowerOfAutonomyFile { get; set; }
        public IFormFile? AddressProofFile { get; set; }
        public IFormFile? NameAvailabilityFile { get; set; }

        public int? MemorandumDocumentId { get; set; }
        public int? RulesDocumentId { get; set; }
        public int? PowerOfAutonomyDocumentId { get; set; }
        public int? AddressProofDocumentId { get; set; }
        public int? NameAvailabilityDocumentId { get; set; }
    }
}

namespace TradeLicence.Models
{
    /// <summary>Model for the shared _RegistrationUpload partial (one labelled PDF upload box).</summary>
    public class RegUploadField
    {
        public string Name { get; set; } = string.Empty;       // posted property name, e.g. "FormIFile"
        public string Label { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Accept { get; set; } = "pdf";            // "pdf" or "pdf,jpg,jpeg,png"
        public int? ExistingDocumentId { get; set; }
        public string ViewController { get; set; } = string.Empty; // controller that serves ViewDocument
    }
}
