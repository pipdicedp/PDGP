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
            await _context.TradeLicenceApplications.AddAsync(application);
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
