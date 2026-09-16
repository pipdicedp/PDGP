using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// Generic version of OfficerSupportingDocument — one shared table for
    /// every service besides TradeLicence (which keeps its own
    /// OfficerSupportingDocuments table unchanged). ApplicationId is a soft
    /// reference (ServiceType + ApplicationId identify the application).
    /// </summary>
    public class WorkflowSupportingDocument
    {
        [Key] public int WorkflowSupportingDocumentId { get; set; }

        [Required, StringLength(50)]
        public string ServiceType { get; set; } = string.Empty;

        public int ApplicationId { get; set; }

        // Which stage this file was uploaded at — "one file per (ServiceType,
        // ApplicationId, Stage)", enforced via a unique index, same as
        // OfficerSupportingDocument's (ApplicationId, Stage) index.
        [Required, StringLength(50)]
        public string Stage { get; set; } = string.Empty;

        public int? UploadedByOfficerId { get; set; }
        public Officer? UploadedByOfficer { get; set; }

        [StringLength(260)]
        public string? FileName { get; set; }

        [StringLength(150)]
        public string? ContentType { get; set; }

        public byte[]? FileData { get; set; }
        public byte[]? FileIV { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
