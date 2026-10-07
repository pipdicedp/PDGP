using System.Globalization;
using TradeLicence.Models;

namespace TradeLicence.Services
{
    /// <summary>
    /// Small shared helpers for EB documents stored in the database.
    /// </summary>
    public static class EBDocumentHelper
    {
        /// <summary>
        /// EBapplication's *Path columns hold either a DocumentId ("45", new
        /// uploads stored in the database) or a legacy disk path
        /// ("/uploads/abc.jpg", applications submitted before the switch).
        /// Returns true (and the id) only for the former.
        /// </summary>
        public static bool TryGetDocumentId(string? reference, out int documentId)
        {
            documentId = 0;
            if (string.IsNullOrWhiteSpace(reference)) return false;
            return int.TryParse(reference.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out documentId);
        }

        /// <summary>
        /// Identifies PDF / JPEG / PNG by byte signature. Anything else returns
        /// null. The result — not the browser-supplied Content-Type — is what
        /// gets stored and served, so an uploaded "pdf" can never be served
        /// back as HTML.
        /// </summary>
        public static (string ContentType, string Extension)? DetectFile(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return ("application/pdf", ".pdf");

            if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return ("image/jpeg", ".jpg");

            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47
                && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
                return ("image/png", ".png");

            return null;
        }

        public static string ExtensionFor(string? contentType) => contentType switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => ""
        };

        // Generated, never taken from the upload — safe to drop into a header.
        public static string SafeFileName(EBApplicationDocument doc) =>
            $"{doc.DocumentType}-{doc.DocumentId}{ExtensionFor(doc.ContentType)}";
    }
}
