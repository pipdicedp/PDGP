using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Models;
using static TradeLicence.Models.TradeLicenceApplication;

namespace TradeLicence.Interfaces
{
    public interface ITradeLicenceRepository
    {
        Task AddApplicationAsync(TradeLicenceApplication application);
        Task AddApplicationDocumentAsync(ApplicationDocument doc);
        Task<TradeLicenceApplication?> GetApplicationWithDocumentsAsync(int id);
        Task<List<Municipality>> GetMunicipalitiesAsync();
        Task<List<Ward>> GetWardsByMunicipalityAsync(int municipalityId);
        Task<List<Area>> GetAreasByWardAsync(int wardId);
        Task<List<Street>> GetStreetsByAreaAsync(int areaId);
        Task<List<DoorNumberLookup>> GetDoorNumbersByStreetAsync(int streetId);
        Task<List<DocumentChecklistItem>> GetDocumentChecklistAsync();

        Task AddPartnerAsync(TradeLicencePartner partner);
        Task<List<TradeLicencePartner>> GetPartnersAsync(int applicationId);
        Task<TradeLicencePartner?> GetPartnerAsync(int partnerId);
        void RemovePartner(TradeLicencePartner partner);

        Task<bool> UpdateCurrentStepAsync(int applicationId, int step);
        Task SaveChangesAsync();
    }
}
