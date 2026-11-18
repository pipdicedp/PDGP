using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models
{
    /// <summary>Registration / Renewal of Society under the Societies Registration Act 1860.</summary>
    public class SocietyApplication : IWorkflowApplication
    {
        public int Id { get; set; }

        [NotMapped]
        public int ApplicationId => Id;

        public int? UserId { get; set; }

        [StringLength(50)] public string? ApplicationNumber { get; set; }

        // "Registration" or "Renewal"
        [StringLength(20)] public string ApplicationType { get; set; } = "Registration";
        // Renewal only
        [StringLength(100)] public string? ExistingRegistrationNumber { get; set; }
        public DateTime? LicenceExpiryDate { get; set; }

        // 1. Applicant
        [StringLength(150)] public string? ApplicantName { get; set; }
        [StringLength(500)] public string? ApplicantAddress { get; set; }
        [StringLength(15)] public string? MobileNumber { get; set; }
        [StringLength(150)] public string? Email { get; set; }
        [StringLength(150)] public string? ApplicantDesignation { get; set; }

        // 2 / 3 / 4. Society
        [StringLength(200)] public string? SocietyName { get; set; }
        [StringLength(200)] public string? ProposedSocietyName { get; set; }
        [StringLength(500)] public string? SocietyAddress { get; set; }

        // Uploaded documents -> SocietyApplicationDocuments.DocumentId
        public int? MemorandumDocumentId { get; set; }
        public int? RulesDocumentId { get; set; }
        public int? PowerOfAutonomyDocumentId { get; set; }
        public int? AddressProofDocumentId { get; set; }
        public int? NameAvailabilityDocumentId { get; set; }

        public bool DeclarationAccepted { get; set; }

        // ---- Workflow (IWorkflowApplication) ----
        [StringLength(30)] public string Status { get; set; } = "Draft";
        [StringLength(50)] public string CurrentStage { get; set; } = "Initial Scrutiny";
        public int? AssignedOfficerId { get; set; }
        [StringLength(20)] public string PaymentStatus { get; set; } = "NotRequested";
        [StringLength(1000)] public string? OfficerRemarks { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public List<SocietyMember> Members { get; set; } = new();
    }

    public class SocietyMember
    {
        public int Id { get; set; }
        public int SocietyApplicationId { get; set; }
        public SocietyApplication? Application { get; set; }

        [StringLength(150)] public string Name { get; set; } = string.Empty;
        [StringLength(150)] public string? Designation { get; set; }
        public DateTime? DateOfAppointment { get; set; }
        [StringLength(500)] public string? Address { get; set; }

        public int? IdProofDocumentId { get; set; }
        public int? SignatureDocumentId { get; set; }
    }

    /// <summary>
    /// DocumentType: Memorandum, Rules, PowerOfAutonomy, AddressProof, NameAvailability,
    /// MemberIdProof, MemberSignature.
    /// </summary>
    public class SocietyApplicationDocument
    {
        [Key] public int DocumentId { get; set; }
        public int SocietyApplicationId { get; set; }

        [Required, StringLength(50)] public string DocumentType { get; set; } = string.Empty;
        [StringLength(260)] public string? FileName { get; set; }
        [StringLength(100)] public string? ContentType { get; set; }

        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public byte[] FileIV { get; set; } = Array.Empty<byte>();
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
