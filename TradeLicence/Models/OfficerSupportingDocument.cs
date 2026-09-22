using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    // A Verification or Inspection officer's own supporting file (e.g. a
    // verification report, site inspection photos) attached to an
    // application from the OFFICER side — not a citizen-uploaded document.
    // One row per (ApplicationId, Stage): re-uploading at the same stage
    // overwrites the previous file rather than adding a second row, so a
    // later-stage officer (e.g. Approval/GM) always sees exactly one file
    // per stage, whichever was uploaded last.
    public class OfficerSupportingDocument
    {
        [Key]
        public int OfficerSupportingDocumentId { get; set; }

        public int ApplicationId { get; set; }

        // "Verification" or "Inspection" — matches OfficerWorkflow.Stages.
        [Required]
        public string Stage { get; set; } = null!;

        public int UploadedByOfficerId { get; set; }

        [Required]
        public string FileName { get; set; } = null!;

        [Required]
        public string ContentType { get; set; } = null!;

        // Encrypted at rest via IFileEncryptionService, same as the
        // citizen-side TradeLicenceDocument (Step 5).
        public byte[] FileData { get; set; } = null!;
        public byte[] FileIV { get; set; } = null!;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        public virtual TradeLicenceApplication? Application { get; set; }
        public virtual Officer? UploadedByOfficer { get; set; }
    }
}
