using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class TradeLicenceDocument
    {
        [Key]
        public int DocumentId { get; set; }

        public int ApplicationId { get; set; }

        /// <summary>The checklist label, e.g. "Aadhaar Copy", "Property Tax Receipt", "Building Plan".</summary>
        [Required]
        public string DocumentName { get; set; } = string.Empty;

        /// <summary>Original uploaded file name, kept for display and for the download's file name.</summary>
        public string? FileName { get; set; }

        // Kept for backward compatibility — no longer written to. Documents are
        // now stored encrypted, directly in the database.
        public string? FilePath { get; set; }

        public byte[]? DocumentData { get; set; }
        public byte[]? DocumentIV { get; set; }
        public string? DocumentContentType { get; set; }

        public DateTime? UploadedDate { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}
