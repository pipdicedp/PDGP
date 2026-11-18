using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models
{
    /// <summary>Registration of Firms under the Indian Partnership Act 1932.</summary>
    public class FirmApplication : IWorkflowApplication
    {
        public int Id { get; set; }

        [NotMapped]
        public int ApplicationId => Id;

        public int? UserId { get; set; }

        [StringLength(50)] public string? ApplicationNumber { get; set; }

        // 1. Applicant
        [StringLength(150)] public string? ApplicantName { get; set; }
        [StringLength(500)] public string? ApplicantAddress { get; set; }
        [StringLength(15)] public string? MobileNumber { get; set; }
        [StringLength(150)] public string? Email { get; set; }

        // 2 / 3. Firm names
        [StringLength(200)] public string? ProposedFirmName { get; set; }
        [StringLength(200)] public string? FirmName { get; set; }

        // 4. Place of business
        [StringLength(500)] public string? PrincipalPlace { get; set; }
        [StringLength(500)] public string? OtherPlace { get; set; }

        // 5. Station
        [StringLength(150)] public string? Station { get; set; }

        // Uploaded documents -> FirmApplicationDocuments.DocumentId
        public int? FormIDocumentId { get; set; }
        public int? PartnershipDeedDocumentId { get; set; }
        public int? AddressProofDocumentId { get; set; }

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

        public List<FirmPartner> Partners { get; set; } = new();
    }

    public class FirmPartner
    {
        public int Id { get; set; }
        public int FirmApplicationId { get; set; }
        public FirmApplication? Application { get; set; }

        [StringLength(150)] public string Name { get; set; } = string.Empty;
        public DateTime? DateOfJoining { get; set; }
        [StringLength(500)] public string? PermanentAddress { get; set; }

        public int? IdProofDocumentId { get; set; }
        public int? SignatureDocumentId { get; set; }
    }

    /// <summary>
    /// Citizen-uploaded document, AES-encrypted in the database (same approach as
    /// EBApplicationDocument). Re-uploading replaces the row content so DocumentId stays stable.
    /// DocumentType: FormI, PartnershipDeed, AddressProof, PartnerIdProof, PartnerSignature.
    /// </summary>
    public class FirmApplicationDocument
    {
        [Key] public int DocumentId { get; set; }
        public int FirmApplicationId { get; set; }

        [Required, StringLength(50)] public string DocumentType { get; set; } = string.Empty;
        [StringLength(260)] public string? FileName { get; set; }
        [StringLength(100)] public string? ContentType { get; set; }

        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public byte[] FileIV { get; set; } = Array.Empty<byte>();
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
