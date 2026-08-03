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

        public bool IsRequired { get; set; } = true;

        // Stored (unique) file name inside wwwroot/uploads
        [StringLength(400)]
        public string? FilePath { get; set; }

        public DateTime UploadedOn { get; set; } = DateTime.Now;
    }
}
