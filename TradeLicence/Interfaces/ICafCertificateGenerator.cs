using TradeLicence.Services;

namespace TradeLicence.Interfaces
{
    /// <summary>
    /// Turns an approved CAF into the certificate PDF. The ONLY thing the CAF module knows about
    /// the certificate's look is this interface, so the final template can be swapped by writing a
    /// new implementation and changing the one registration line in Program.cs
    /// (see TradeLicenceStyleCafCertificateGenerator for the current, temporary one).
    /// </summary>
    public interface ICafCertificateGenerator
    {
        byte[] Generate(CafCertificateData data);
    }
}
