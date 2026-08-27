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
        Task AddMachineryAsync(TradeLicenceMachinery machinery);
        Task<int> GetNextApplicationSequenceNumberAsync();
        void RemovePartner(TradeLicencePartner partner);

        Task<bool> UpdateCurrentStepAsync(int applicationId, int step);
        Task SaveChangesAsync();

        Task<TradeLicencePhotograph?> GetPhotographByApplicationIdAsync(int applicationId);
        Task AddPhotographAsync(TradeLicencePhotograph photograph);

        Task<TradeLicenceDocument?> GetDocumentByApplicationAndNameAsync(int applicationId, string documentName);
        Task<TradeLicenceDocument?> GetDocumentByIdAsync(int documentId);
        Task<List<TradeLicenceDocument>> GetDocumentsByApplicationIdAsync(int applicationId);
        Task AddDocumentAsync(TradeLicenceDocument document);
        void RemoveDocument(TradeLicenceDocument document);
        Task<ShopEstablishmentRegistration?> GetShopRegistrationByApplicationIdAsync(int applicationId);
        Task AddShopRegistrationAsync(ShopEstablishmentRegistration registration);
        Task<List<TradeLicenceMachinery>> GetMachineryByApplicationIdAsync(int applicationId);

        // ---------------- Employer other than Manager ----------------
        Task AddEmployerAsync(ShopEmployer employer);
        Task<List<ShopEmployer>> GetEmployersByApplicationIdAsync(int applicationId);
        Task<ShopEmployer?> GetEmployerAsync(int employerRowId);
        void RemoveEmployer(ShopEmployer employer);

        // ---------------- Form IX, Part-A ----------------
        Task AddFormIXPartARowAsync(ShopFormIXPartA row);
        Task<List<ShopFormIXPartA>> GetFormIXPartAByApplicationIdAsync(int applicationId);
        Task<ShopFormIXPartA?> GetFormIXPartARowAsync(int partARowId);
        void RemoveFormIXPartARow(ShopFormIXPartA row);

        // ---------------- Form IX, Part-B ----------------
        Task AddFormIXPartBRowAsync(ShopFormIXPartB row);
        Task<List<ShopFormIXPartB>> GetFormIXPartBByApplicationIdAsync(int applicationId);
        Task<ShopFormIXPartB?> GetFormIXPartBRowAsync(int partBRowId);
        void RemoveFormIXPartBRow(ShopFormIXPartB row);

        // ---------------- Shop Establishment Annexure Documents ----------------
        Task<ShopAnnexureDocument?> GetAnnexureDocumentByApplicationAndNameAsync(int applicationId, string documentName);
        Task<ShopAnnexureDocument?> GetAnnexureDocumentByIdAsync(int documentId);
        Task<List<ShopAnnexureDocument>> GetAnnexureDocumentsByApplicationIdAsync(int applicationId);
        Task AddAnnexureDocumentAsync(ShopAnnexureDocument document);
        void RemoveAnnexureDocument(ShopAnnexureDocument document);
    }
}
