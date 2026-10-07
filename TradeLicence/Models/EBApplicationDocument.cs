using System;
using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// A citizen-uploaded document for an Electricity (EB) application, stored
    /// in the database instead of wwwroot/uploads. The bytes are AES-encrypted
    /// by IFileEncryptionService; FileIV is the per-file IV needed to decrypt.
    ///
    /// One row per (ApplicationNumber, DocumentType) — re-uploading a document
    /// replaces that row's content, so DocumentId stays stable. EBapplication's
    /// existing *Path columns (PhotoPath, AddressProofPath, ...) now hold this
    /// DocumentId (as text) instead of a "/uploads/xyz.jpg" disk path.
    /// </summary>
    public class EBApplicationDocument
    {
        [Key]
        public int DocumentId { get; set; }

        [Required, StringLength(100)]
        public string ApplicationNumber { get; set; } = string.Empty;

        // "Photo", "AddressProof", "IdentityProof", "TestReport", "SaleDeed", ...
        [Required, StringLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [StringLength(260)]
        public string? FileName { get; set; }

        // Always one of the safe types detected from the file's own bytes
        // (application/pdf, image/jpeg, image/png) — never the client's header.
        [StringLength(100)]
        public string? ContentType { get; set; }

        // Encrypted bytes
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public byte[] FileIV { get; set; } = Array.Empty<byte>();

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
