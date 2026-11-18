using TradeLicence.Services;

namespace TradeLicence.Services
{
    /// <summary>
    /// Shared upload rules for the Firms and Society registration forms:
    /// max 1 MB, type detected from the file's own bytes (never the browser header).
    /// </summary>
    public static class RegistrationFileHelper
    {
        public const long MaxBytes = 1_048_576; // 1 MB, same limit the form shows to the citizen

        /// <summary>
        /// Reads and validates an uploaded file. pdfOnly = true for the scanned-document
        /// uploads; partner / member ID proof and signature also allow JPG / PNG.
        /// </summary>
        public static async Task<(byte[]? Bytes, string? ContentType, string? Error)> ReadAsync(
            IFormFile file, string label, bool pdfOnly)
        {
            if (file.Length == 0)
                return (null, null, $"{label}: the selected file is empty.");

            if (file.Length > MaxBytes)
                return (null, null, $"{label}: file size must be 1 MB or less.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            var detected = EBDocumentHelper.DetectFile(bytes);
            if (detected == null)
                return (null, null, $"{label}: only PDF{(pdfOnly ? "" : ", JPG or PNG")} files are allowed.");

            if (pdfOnly && detected.Value.ContentType != "application/pdf")
                return (null, null, $"{label}: only PDF files are allowed.");

            return (bytes, detected.Value.ContentType, null);
        }

        public static string CleanFileName(string? name)
        {
            var n = Path.GetFileName(name ?? "document");
            return n.Length > 250 ? n[^250..] : n;
        }

        // Generated, never taken from the upload - safe to put in a header.
        public static string SafeFileName(string documentType, int documentId, string? contentType) =>
            $"{documentType}-{documentId}{EBDocumentHelper.ExtensionFor(contentType)}";
    }
}
