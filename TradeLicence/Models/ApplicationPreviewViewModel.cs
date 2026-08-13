using TradeLicence.Models;

namespace TradeLicence.Models
{
    public class ApplicationPreviewViewModel
    {
        public TradeLicenceApplication Application { get; set; } = null!;
        public List<TradeLicencePartner> Partners { get; set; } = new();
        public List<TradeLicenceMachinery> Machinery { get; set; } = new();
        public List<TradeLicenceDocument> Documents { get; set; } = new();
        public ShopEstablishmentRegistration? ShopRegistration { get; set; }
    }
}
