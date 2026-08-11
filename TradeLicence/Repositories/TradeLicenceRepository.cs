using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeLicence.Data;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using static TradeLicence.Models.TradeLicenceApplication;

namespace TradeLicence.Repositories
{
    public class TradeLicenceRepository : RepositoryBase, ITradeLicenceRepository
    {
        public TradeLicenceRepository(ApplicationDbContext context) : base(context) { }

        public async Task AddApplicationAsync(TradeLicenceApplication application)
        {
            if (application.ApplicationId == 0)
            {
                // Brand new application — let the DB generate the identity value.
                await _context.TradeLicenceApplications.AddAsync(application);
                return;
            }

            // Existing draft being re-saved (e.g. re-visiting Application Details,
            // or final Submit on an application created earlier). Calling AddAsync()
            // here would try to INSERT the already-assigned ApplicationId explicitly,
            // which SQL Server rejects for an identity column. Load the tracked
            // entity instead and copy the incoming values onto it — this produces
            // a proper UPDATE using the existing row.
            var existing = await _context.TradeLicenceApplications.FindAsync(application.ApplicationId);
            if (existing == null)
            {
                // Shouldn't normally happen, but don't silently drop the save.
                await _context.TradeLicenceApplications.AddAsync(application);
                return;
            }

            var originalCreatedDate = existing.CreatedDate;
            var originalUserId = existing.UserId;

            _context.Entry(existing).CurrentValues.SetValues(application);

            // The wizard form doesn't round-trip every field on every step —
            // never let a re-save clobber these with a blank/default value.
            existing.CreatedDate = originalCreatedDate;
            if (application.UserId == null)
            {
                existing.UserId = originalUserId;
            }
        }

        public async Task AddApplicationDocumentAsync(ApplicationDocument doc)
        {
            await _context.ApplicationDocuments.AddAsync(doc);
        }

        public async Task<TradeLicenceApplication?> GetApplicationWithDocumentsAsync(int id)
        {
            return await _context.TradeLicenceApplications
                .Include(a => a.ApplicationDocuments)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);
        }

        public async Task<List<Municipality>> GetMunicipalitiesAsync()
        {
            return await _context.Municipalities.OrderBy(m => m.MunicipalityName).ToListAsync();
        }

        public async Task<List<Ward>> GetWardsByMunicipalityAsync(int municipalityId)
        {
            return await _context.Wards.Where(w => w.MunicipalityId == municipalityId).ToListAsync();
        }

        public async Task<List<Area>> GetAreasByWardAsync(int wardId)
        {
            return await _context.Areas.Where(a => a.WardId == wardId).ToListAsync();
        }

        public async Task<List<Street>> GetStreetsByAreaAsync(int areaId)
        {
            return await _context.Streets.Where(s => s.AreaId == areaId).ToListAsync();
        }

        public async Task<List<DoorNumberLookup>> GetDoorNumbersByStreetAsync(int streetId)
        {
            return await _context.DoorNumbers.Where(d => d.StreetId == streetId).ToListAsync();
        }

        public async Task<List<DocumentChecklistItem>> GetDocumentChecklistAsync()
        {
            return await _context.DocumentChecklistItems.OrderBy(d => d.DisplayOrder).ToListAsync();
        }

        public async Task AddPartnerAsync(TradeLicencePartner partner)
        {
            await _context.TradeLicencePartners.AddAsync(partner);
        }

        public async Task<List<TradeLicencePartner>> GetPartnersAsync(int applicationId)
        {
            return await _context.TradeLicencePartners
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task<TradeLicencePartner?> GetPartnerAsync(int partnerId)
        {
            return await _context.TradeLicencePartners.FindAsync(partnerId);
        }

        public void RemovePartner(TradeLicencePartner partner)
        {
            _context.TradeLicencePartners.Remove(partner);
        }

        public async Task AddMachineryAsync(TradeLicenceMachinery machinery)
        {
            await _context.TradeLicenceMachineries.AddAsync(machinery);
        }

        public async Task<List<TradeLicenceMachinery>> GetMachineryByApplicationIdAsync(int applicationId)
        {
            return await _context.TradeLicenceMachineries
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }

        // ---------------- Photographs (Step 4) ----------------

        public async Task<TradeLicencePhotograph?> GetPhotographByApplicationIdAsync(int applicationId)
        {
            return await _context.TradeLicencePhotographs
                .FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        }

        public async Task AddPhotographAsync(TradeLicencePhotograph photograph)
        {
            await _context.TradeLicencePhotographs.AddAsync(photograph);
        }

        // ---------------- Documents (Step 5) ----------------

        public async Task<TradeLicenceDocument?> GetDocumentByApplicationAndNameAsync(int applicationId, string documentName)
        {
            return await _context.TradeLicenceDocuments
                .FirstOrDefaultAsync(x => x.ApplicationId == applicationId && x.DocumentName == documentName);
        }

        public async Task<TradeLicenceDocument?> GetDocumentByIdAsync(int documentId)
        {
            return await _context.TradeLicenceDocuments.FindAsync(documentId);
        }

        public async Task<List<TradeLicenceDocument>> GetDocumentsByApplicationIdAsync(int applicationId)
        {
            return await _context.TradeLicenceDocuments
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task AddDocumentAsync(TradeLicenceDocument document)
        {
            await _context.TradeLicenceDocuments.AddAsync(document);
        }

        public void RemoveDocument(TradeLicenceDocument document)
        {
            _context.TradeLicenceDocuments.Remove(document);
        }

        // ---------------- Shop Establishment Registration (Step 6) ----------------

        public async Task<ShopEstablishmentRegistration?> GetShopRegistrationByApplicationIdAsync(int applicationId)
        {
            return await _context.ShopEstablishmentRegistrations
                .FirstOrDefaultAsync(x => x.ApplicationId == applicationId);
        }

        public async Task AddShopRegistrationAsync(ShopEstablishmentRegistration registration)
        {
            await _context.ShopEstablishmentRegistrations.AddAsync(registration);
        }

        public async Task<bool> UpdateCurrentStepAsync(int applicationId, int step)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(applicationId);
            if (application == null) return false;

            // Never move the step backward — if the user somehow re-triggers an
            // earlier step's Next after already being further along, don't lose
            // their progress marker.
            if (step > application.CurrentStep)
            {
                application.CurrentStep = step;
            }

            return true;
        }
        public async Task<int> GetNextApplicationSequenceNumberAsync()
        {
            // "NEXT VALUE FOR" is evaluated atomically by SQL Server itself — no
            // row locking, no read-then-write race condition, safe even if two
            // requests call this in the exact same millisecond.
            var connection = _context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR dbo.ApplicationNumberSequence";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
    }
}