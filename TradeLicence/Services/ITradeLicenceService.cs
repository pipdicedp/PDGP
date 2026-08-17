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
        Task<bool> UpdateCurrentStepAsync(int applicationId, int step);

        Task<List<TradeLicencePartner>> GetPartnersAsync(int applicationId);
        Task<TradeLicencePartner> AddPartnerAsync(int applicationId, string partnerName, string designation, string address);
        Task<TradeLicenceMachinery> AddMachineryAsync(int applicationId, string machineryName, int quantity, decimal horsePower);
        Task<bool> DeletePartnerAsync(int partnerId);

        // ---------------- Photographs (Step 4) ----------------
        Task SavePhotographsAsync(int applicationId, byte[]? applicantPhotoBytes, string? applicantPhotoContentType, byte[]? partnerPhotoBytes, string? partnerPhotoContentType);
        Task<(byte[] Bytes, string ContentType)?> GetDecryptedApplicantPhotoAsync(int applicationId);
        Task<(byte[] Bytes, string ContentType)?> GetDecryptedPartnerPhotoAsync(int applicationId);

        // ---------------- Documents (Step 5) ----------------
        Task<TradeLicenceDocument> SaveDocumentAsync(int applicationId, string documentName, string fileName, byte[] fileBytes, string contentType);
        Task<List<TradeLicenceDocument>> GetDocumentsAsync(int applicationId);
        Task<(byte[] Bytes, string ContentType, string FileName)?> GetDecryptedDocumentAsync(int documentId);
        Task<bool> DeleteDocumentAsync(int documentId);

        // ---------------- Shop Establishment Registration (Step 6) ----------------
        Task<ShopEstablishmentRegistration> SaveShopEstablishmentAsync(int applicationId, ShopEstablishmentRegistration input);
        Task<ShopEstablishmentRegistration?> GetShopEstablishmentAsync(int applicationId);
        Task<List<TradeLicenceMachinery>> GetMachineryAsync(int applicationId);

        // ---------------- Acknowledgement PDF ----------------
        Task<byte[]> GenerateAcknowledgementPdfAsync(int applicationId);

        // ---------------- Application Preview (shared by citizen wizard and officer view) ----------------
        Task<ApplicationPreviewViewModel?> GetApplicationPreviewAsync(int applicationId);
    }
}
