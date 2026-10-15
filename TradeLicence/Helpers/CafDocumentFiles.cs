namespace TradeLicence.Helpers
{
    /// <summary>
    /// Shared by every controller that serves a CAF document (the applicant's own, the Industry officer's,
    /// the receiving department officer's). Documents are stored as raw bytes with no content type on record,
    /// so the type is read from the file's first bytes — never from a file name or from user input.
    /// </summary>
    public static class CafDocumentFiles
    {
        /// <summary>"application/pdf", "image/jpeg" or "image/png"; null if the bytes are none of those.</summary>
        public static string? SniffContentType(byte[] b)
        {
            if (b.Length >= 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46) return "application/pdf";               // %PDF
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            return null;
        }
    }
}
