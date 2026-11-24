using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models.Drugs
{
    /// <summary>
    /// One Pharmacist / Competent person. An application can have any number of them.
    /// Shared table for both forms - FormType + ApplicationId identify the parent
    /// (soft reference, same idea as WorkflowHistory.ServiceType + ApplicationId).
    /// </summary>
    public class DrugLicencePharmacist
    {
        [Key] public int PharmacistId { get; set; }

        [Required, StringLength(10)] public string FormType { get; set; } = string.Empty;
        public int ApplicationId { get; set; }

        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [StringLength(10)] public string? Sex { get; set; }
        [StringLength(150)] public string? Qualification { get; set; }
        [StringLength(50)] public string? Experience { get; set; }
        [StringLength(50)] public string? CouncilNo { get; set; }
        public DateTime? ValidityDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? YearOfPassing { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// An uploaded PDF, stored as bytes in the database (AES-encrypted with IFileEncryptionService, IV alongside -
    /// the same way Firm/Society/Electricity documents and officer stage documents are stored).
    /// One row per (FormType, ApplicationId, DocumentType): re-uploading replaces the previous file.
    /// </summary>
    public class DrugLicenceDocument
    {
        [Key] public int DocumentId { get; set; }

        [Required, StringLength(10)] public string FormType { get; set; } = string.Empty;
        public int ApplicationId { get; set; }

        [Required, StringLength(50)] public string DocumentType { get; set; } = string.Empty;
        [StringLength(260)] public string? FileName { get; set; }
        [StringLength(100)] public string? ContentType { get; set; }

        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public byte[] FileIV { get; set; } = Array.Empty<byte>();

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
