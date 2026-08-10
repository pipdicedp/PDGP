using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models
{
    // Maps to dbo.Application_Documents
    // One row per uploaded file for an application (Name/Address proof, Ownership proof, etc.)
    public class ApplicationDocument
    {
        [Key]
        public int DocumentId { get; set; }

        public int ApplicationId { get; set; }
        public WaterConnectionApplication? Application { get; set; }

        // Category of the document, e.g. "NameAddress", "Ownership", "Others", "ContractorConsent"
        [Required]
        [StringLength(100)]
        public string DocumentPurpose { get; set; } = string.Empty;

        // The selected document type/label at the time of upload (e.g. "Aadhaar Card or Voter ID")
        [StringLength(200)]
        public string? DocumentOption { get; set; }


        // File_Path is varbinary(max) in the DB — holds the actual file bytes,
        // not a disk path. Property is named FileContent in code to avoid confusion,
        // but is mapped to the File_Path column (see WaterApplicationDbContext).
        // No separate column exists for the original filename/MIME type, so the
        // content type is detected from the bytes themselves when serving the file
        // back out (see WaterConnectionController.Document).
        public byte[]? FileContent { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;
    }
}
