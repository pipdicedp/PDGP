using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using static TradeLicence.Models.TradeLicenceApplication;

namespace TradeLicence.Services
{
    public class TradeLicenceService : ITradeLicenceService
    {
        private readonly ITradeLicenceRepository _repo;

        public TradeLicenceService(ITradeLicenceRepository repo)
        {
            _repo = repo;
        }

        public async Task SaveDraftAsync(TradeLicenceApplication model, List<int>? selectedDocuments)
        {
            model.Status = "Draft";

            if (model.ApplicationId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
            }

            if (string.IsNullOrEmpty(model.ApplicationNumber))
            {
                model.ApplicationNumber = await GenerateApplicationNumberAsync();   // <-- was the hardcoded TL/GUID line
            }

            await _repo.AddApplicationAsync(model);

            if (selectedDocuments != null)
            {
                foreach (var docId in selectedDocuments)
                {
                    await _repo.AddApplicationDocumentAsync(new ApplicationDocument
                    {
                        Application = model,
                        DocumentItemId = docId
                    });
                }
            }

            await _repo.SaveChangesAsync();
        }


        public async Task SubmitApplicationAsync(TradeLicenceApplication model, List<int>? selectedDocuments)
        {
            model.Status = "Submitted";

            if (model.ApplicationId == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
            }

            if (string.IsNullOrEmpty(model.ApplicationNumber))
            {
                model.ApplicationNumber = await GenerateApplicationNumberAsync();
            }

            await _repo.AddApplicationAsync(model);

            if (selectedDocuments != null)
            {
                foreach (var docId in selectedDocuments)
                {
                    await _repo.AddApplicationDocumentAsync(new ApplicationDocument
                    {
                        Application = model,
                        DocumentItemId = docId
                    });
                }
            }

            await _repo.SaveChangesAsync();
        }

        public async Task<TradeLicenceApplication?> GetApplicationAsync(int id)
        {
            return await _repo.GetApplicationWithDocumentsAsync(id);
        }

        // ---------------- Partners (Step 2) ----------------

        public async Task<List<TradeLicencePartner>> GetPartnersAsync(int applicationId)
        {
            return await _repo.GetPartnersAsync(applicationId);
        }

        public async Task<TradeLicencePartner> AddPartnerAsync(int applicationId, string partnerName, string designation, string address)
        {
            var partner = new TradeLicencePartner
            {
                ApplicationId = applicationId,
                PartnerName = partnerName.Trim(),
                Designation = designation.Trim(),
                Address = address.Trim()
            };
            await _repo.AddPartnerAsync(partner);
            await _repo.SaveChangesAsync();
            return partner;
        }

        public async Task<bool> DeletePartnerAsync(int partnerId)
        {
            var partner = await _repo.GetPartnerAsync(partnerId);
            if (partner == null) return false;
            _repo.RemovePartner(partner);
            await _repo.SaveChangesAsync();
            return true;
        }
        public async Task<bool> UpdateCurrentStepAsync(int applicationId, int step)
        {
            var result = await _repo.UpdateCurrentStepAsync(applicationId, step);
            if (result)
            {
                await _repo.SaveChangesAsync();
            }
            return result;
        }

        public async Task<TradeLicenceMachinery> AddMachineryAsync(int applicationId, string machineryName, int quantity, decimal horsePower)
        {
            var machinery = new TradeLicenceMachinery
            {
                ApplicationId = applicationId,
                MachineryName = machineryName.Trim(),
                Quantity = quantity,
                HorsePower = horsePower
            };
            await _repo.AddMachineryAsync(machinery);
            await _repo.SaveChangesAsync();
            return machinery;
        }
        private async Task<string> GenerateApplicationNumberAsync()
        {
            var datePart = DateTime.Now.ToString("ddMMyyyy");
            var sequenceNumber = await _repo.GetNextApplicationSequenceNumberAsync();
            return $"{datePart}{sequenceNumber:D2}";
        }

        public async Task<List<Municipality>> GetMunicipalitiesAsync() => await _repo.GetMunicipalitiesAsync();
        public async Task<List<Ward>> GetWardsAsync(int municipalityId) => await _repo.GetWardsByMunicipalityAsync(municipalityId);
        public async Task<List<Area>> GetAreasAsync(int wardId) => await _repo.GetAreasByWardAsync(wardId);
        public async Task<List<Street>> GetStreetsAsync(int areaId) => await _repo.GetStreetsByAreaAsync(areaId);
        public async Task<List<DoorNumberLookup>> GetDoorNumbersAsync(int streetId) => await _repo.GetDoorNumbersByStreetAsync(streetId);
        public async Task<List<DocumentChecklistItem>> GetDocumentChecklistAsync() => await _repo.GetDocumentChecklistAsync();
    }
}
