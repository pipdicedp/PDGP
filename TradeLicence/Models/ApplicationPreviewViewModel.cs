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

        // Resolved from the raw MunicipalityId/WardId/AreaId/StreetId on
        // Application, so the preview can show names instead of raw IDs.
        public string? MunicipalityName { get; set; }
        public string? WardName { get; set; }
        public string? AreaName { get; set; }
        public string? StreetName { get; set; }

        // True in the citizen wizard's Preview tab (lets them jump back to a
        // tab and edit). False for the officer's read-only view.
        public bool ShowEditLinks { get; set; } = true;
    }
}
