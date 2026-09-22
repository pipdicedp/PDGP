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

            // The citizen wizard form never includes these — without
            // preserving them, every save (and especially a "Correct &
            // Resubmit" after an officer's Return) would silently reset
            // the application back to Initial Scrutiny / unassigned,
            // instead of going back to whichever officer/stage sent it
            // back for correction.
            var originalCurrentStage = existing.CurrentStage;
            var originalAssignedOfficerId = existing.AssignedOfficerId;

            _context.Entry(existing).CurrentValues.SetValues(application);

            // The wizard form doesn't round-trip every field on every step —
            // never let a re-save clobber these with a blank/default value.
            existing.CreatedDate = originalCreatedDate;
            existing.CurrentStage = originalCurrentStage;
            existing.AssignedOfficerId = originalAssignedOfficerId;
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

        // ---------------- Employer other than Manager ----------------

        public async Task AddEmployerAsync(ShopEmployer employer)
        {
            await _context.ShopEmployers.AddAsync(employer);
        }

        public async Task<List<ShopEmployer>> GetEmployersByApplicationIdAsync(int applicationId)
        {
            return await _context.ShopEmployers
                .Where(x => x.ApplicationId == applicationId)
                .OrderBy(x => x.EmployerRowId)
                .ToListAsync();
        }

        public async Task<ShopEmployer?> GetEmployerAsync(int employerRowId)
        {
            return await _context.ShopEmployers.FindAsync(employerRowId);
        }

        public void RemoveEmployer(ShopEmployer employer)
        {
            _context.ShopEmployers.Remove(employer);
        }

        // ---------------- Form IX, Part-A ----------------

        public async Task AddFormIXPartARowAsync(ShopFormIXPartA row)
        {
            await _context.ShopFormIXPartAs.AddAsync(row);
        }

        public async Task<List<ShopFormIXPartA>> GetFormIXPartAByApplicationIdAsync(int applicationId)
        {
            return await _context.ShopFormIXPartAs
                .Where(x => x.ApplicationId == applicationId)
                .OrderBy(x => x.PartARowId)
                .ToListAsync();
        }

        public async Task<ShopFormIXPartA?> GetFormIXPartARowAsync(int partARowId)
        {
            return await _context.ShopFormIXPartAs.FindAsync(partARowId);
        }

        public void RemoveFormIXPartARow(ShopFormIXPartA row)
        {
            _context.ShopFormIXPartAs.Remove(row);
        }

        // ---------------- Form IX, Part-B ----------------

        public async Task AddFormIXPartBRowAsync(ShopFormIXPartB row)
        {
            await _context.ShopFormIXPartBs.AddAsync(row);
        }

        public async Task<List<ShopFormIXPartB>> GetFormIXPartBByApplicationIdAsync(int applicationId)
        {
            return await _context.ShopFormIXPartBs
                .Where(x => x.ApplicationId == applicationId)
                .OrderBy(x => x.PartBRowId)
                .ToListAsync();
        }

        public async Task<ShopFormIXPartB?> GetFormIXPartBRowAsync(int partBRowId)
        {
            return await _context.ShopFormIXPartBs.FindAsync(partBRowId);
        }

        public void RemoveFormIXPartBRow(ShopFormIXPartB row)
        {
            _context.ShopFormIXPartBs.Remove(row);
        }

        // ---------------- Shop Establishment Annexure Documents ----------------

        public async Task<ShopAnnexureDocument?> GetAnnexureDocumentByApplicationAndNameAsync(int applicationId, string documentName)
        {
            return await _context.ShopAnnexureDocuments
                .FirstOrDefaultAsync(x => x.ApplicationId == applicationId && x.DocumentName == documentName);
        }

        public async Task<ShopAnnexureDocument?> GetAnnexureDocumentByIdAsync(int documentId)
        {
            return await _context.ShopAnnexureDocuments.FindAsync(documentId);
        }

        public async Task<List<ShopAnnexureDocument>> GetAnnexureDocumentsByApplicationIdAsync(int applicationId)
        {
            return await _context.ShopAnnexureDocuments
                .Where(x => x.ApplicationId == applicationId)
                .ToListAsync();
        }

        public async Task AddAnnexureDocumentAsync(ShopAnnexureDocument document)
        {
            await _context.ShopAnnexureDocuments.AddAsync(document);
        }

        public void RemoveAnnexureDocument(ShopAnnexureDocument document)
        {
            _context.ShopAnnexureDocuments.Remove(document);
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

        // ---------------- Certificate download tracking ----------------
        public async Task<bool> MarkCertificateDownloadedAsync(int applicationId)
        {
            var application = await _context.TradeLicenceApplications.FindAsync(applicationId);
            if (application == null) return false;

            // Keep the FIRST download timestamp — re-downloads shouldn't
            // overwrite when the citizen originally got their certificate.
            if (!application.IsCertificateDownloaded)
            {
                application.IsCertificateDownloaded = true;
                // Stored as local (IST) server time, not UTC — matches what's
                // shown on the certificate/dashboard, so no conversion needed at display time.
                application.CertificateDownloadedDate = DateTime.Now;
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