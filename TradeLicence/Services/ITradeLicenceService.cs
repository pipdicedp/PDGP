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

        // ---------------- Employer other than Manager ----------------
        Task<List<ShopEmployer>> GetEmployersAsync(int applicationId);
        Task<ShopEmployer> AddEmployerAsync(int applicationId, EmployerRowDto input);
        Task<bool> DeleteEmployerAsync(int employerRowId);

        // ---------------- Form IX, Part-A ----------------
        Task<List<ShopFormIXPartA>> GetFormIXPartAAsync(int applicationId);
        Task<ShopFormIXPartA> AddFormIXPartARowAsync(int applicationId, FormIXPartARowDto input);
        Task<bool> DeleteFormIXPartARowAsync(int partARowId);

        // ---------------- Form IX, Part-B ----------------
        Task<List<ShopFormIXPartB>> GetFormIXPartBAsync(int applicationId);
        Task<ShopFormIXPartB> AddFormIXPartBRowAsync(int applicationId, FormIXPartBRowDto input);
        Task<bool> DeleteFormIXPartBRowAsync(int partBRowId);

        // ---------------- Shop Establishment Annexure Documents ----------------
        Task<ShopAnnexureDocument> SaveAnnexureDocumentAsync(int applicationId, string documentName, string fileName, byte[] fileBytes, string contentType);
        Task<List<ShopAnnexureDocument>> GetAnnexureDocumentsAsync(int applicationId);
        Task<(byte[] Bytes, string ContentType, string FileName)?> GetDecryptedAnnexureDocumentAsync(int documentId);
        Task<bool> DeleteAnnexureDocumentAsync(int documentId);

        // ---------------- Acknowledgement PDF ----------------
        Task<byte[]> GenerateAcknowledgementPdfAsync(int applicationId);

        // ---------------- Trade Licence Certificate PDF (Approved applications only) ----------------
        Task<byte[]> GenerateCertificatePdfAsync(int applicationId);

        // ---------------- Certificate download tracking ----------------
        Task<bool> MarkCertificateDownloadedAsync(int applicationId);

        // ---------------- Application Preview (shared by citizen wizard and officer view) ----------------
        Task<ApplicationPreviewViewModel?> GetApplicationPreviewAsync(int applicationId);
    }
}
