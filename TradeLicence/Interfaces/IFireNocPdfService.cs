using System.Threading.Tasks;

namespace TradeLicence.Interfaces
{
    /// <summary>
    /// QuestPDF output for the Fire NOC service: the acknowledgement slip the
    /// applicant receives on submission, and the Fire Safety Clearance (NOC)
    /// once the application has been approved.
    /// </summary>
    public interface IFireNocPdfService
    {
        /// <summary>Acknowledgement slip for the applicant's submitted fire application.</summary>
        Task<byte[]> GenerateAcknowledgementPdfAsync(long loginId);

        /// <summary>The Fire Safety Clearance certificate — only valid once Status is "Approved".</summary>
        Task<byte[]> GenerateNocPdfAsync(long loginId);
    }
}
