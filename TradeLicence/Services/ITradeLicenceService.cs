using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Models;

namespace TradeLicence.Services
{
    public interface ITradeLicenceService
    {
        Task SaveDraftAsync(TradeLicenceApplication model, List<int>? selectedDocuments);
        Task SubmitApplicationAsync(TradeLicenceApplication model, List<int>? selectedDocuments);
        Task<TradeLicenceApplication?> GetApplicationAsync(int id);
        Task<List<Municipality>> GetMunicipalitiesAsync();
        Task<List<Ward>> GetWardsAsync(int municipalityId);
        Task<List<Area>> GetAreasAsync(int wardId);
        Task<List<Street>> GetStreetsAsync(int areaId);
        Task<List<DoorNumberLookup>> GetDoorNumbersAsync(int streetId);
        Task<List<DocumentChecklistItem>> GetDocumentChecklistAsync();

        Task<List<TradeLicencePartner>> GetPartnersAsync(int applicationId);
        Task<TradeLicencePartner> AddPartnerAsync(int applicationId, string partnerName, string designation, string address);
        Task<bool> DeletePartnerAsync(int partnerId);
    }
}
