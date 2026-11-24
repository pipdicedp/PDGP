using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Models.Drugs;

namespace TradeLicence.Services
{
    /// <summary>
    /// Everything the Form 20A / Form 21A citizen controllers need that isn't HTTP:
    /// loading, mapping form &lt;-&gt; entity, validation, saving pharmacists and the encrypted PDF uploads.
    /// One class for both forms - TApp is Form20AApplication or Form21AApplication.
    ///
    /// Validation rules live in the "Validate*" methods below - edit them there.
    /// </summary>
    public class DrugLicenceFormService<TApp> where TApp : DrugLicenceApplicationBase, new()
    {
        private static readonly Regex PanRegex = new(@"^[A-Z]{5}[0-9]{4}[A-Z]$", RegexOptions.Compiled);
        private static readonly Regex MobileRegex = new(@"^[6-9][0-9]{9}$", RegexOptions.Compiled);
        private static readonly Regex PinRegex = new(@"^[1-9][0-9]{5}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        // letters, spaces, dot, apostrophe, hyphen (no digits)
        private static readonly Regex PersonNameRegex = new(@"^[\p{L}][\p{L}\p{M}\s.'\-]*$", RegexOptions.Compiled);

        private readonly DrugsApplicationDbContext _ctx;
        private readonly IFileEncryptionService _encryption;
        private readonly DrugFormDefinition _form;

        public DrugLicenceFormService(DrugsApplicationDbContext ctx, IFileEncryptionService encryption, DrugFormDefinition form)
        {
            _ctx = ctx;
            _encryption = encryption;
            _form = form;
        }

        private DbSet<TApp> Apps => _ctx.Set<TApp>();

        // ---------------------------------------------------------------- reads

        public static bool IsEditable(DrugLicenceApplicationBase app) =>
            app.Status == "Draft" || app.Status == "ReturnedToApplicant";

        public Task<TApp?> GetOwnedAsync(int id, int userId) =>
            Apps.FirstOrDefaultAsync(a => a.ApplicationId == id && a.UserId == userId);

        public async Task<List<DrugLicencePharmacist>> GetPharmacistsAsync(int applicationId) =>
            await _ctx.Pharmacists
                .Where(p => p.FormType == _form.Code && p.ApplicationId == applicationId)
                .OrderBy(p => p.PharmacistId)
                .ToListAsync();

        // Metadata only - the encrypted bytes are not loaded.
        public async Task<List<DrugDocumentInfo>> GetDocumentInfosAsync(int applicationId) =>
            await _ctx.Documents
                .Where(d => d.FormType == _form.Code && d.ApplicationId == applicationId)
                .Select(d => new DrugDocumentInfo
                {
                    DocumentId = d.DocumentId,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName ?? "document.pdf",
                    UploadedDate = d.UploadedDate
                })
                .ToListAsync();

        public async Task<(byte[] Bytes, string FileName)?> GetDocumentBytesAsync(int documentId)
        {
            var doc = await _ctx.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId && d.FormType == _form.Code);
            if (doc == null || doc.FileData.Length == 0) return null;
            return (_encryption.Decrypt(doc.FileData, doc.FileIV), doc.FileName ?? $"{doc.DocumentType}.pdf");
        }

        /// <summary>The application a stored document belongs to (null if no such document for this form) - used for ownership checks.</summary>
        public async Task<int?> GetDocumentApplicationIdAsync(int documentId) =>
            await _ctx.Documents.AsNoTracking()
                .Where(d => d.DocumentId == documentId && d.FormType == _form.Code)
                .Select(d => (int?)d.ApplicationId)
                .FirstOrDefaultAsync();

        private async Task<List<string>> LoadDropdownAsync(string category, string[] fallback)
        {
            var values = await _ctx.DropdownMasters
                .Where(d => d.Category == category && d.IsActive)
                .OrderBy(d => d.SortOrder)
                .Select(d => d.Value)
                .ToListAsync();
            return values.Count > 0 ? values : fallback.ToList();
        }

        /// <summary>Blank form (app == null) or the saved application as a form.</summary>
        public async Task<DrugLicenceFormViewModel> BuildViewModelAsync(TApp? app)
        {
            var vm = new DrugLicenceFormViewModel();
            if (app != null)
            {
                vm.ApplicationId = app.ApplicationId;
                vm.IsRenewal = app.IsRenewal;
                vm.ApplicantName = app.ApplicantName;
                vm.FirmName = app.FirmName;
                vm.ManagerInchargeName = app.ManagerInchargeName;
                vm.ConstitutionType = app.ConstitutionType;
                vm.AddressOfTheFirm = app.AddressOfTheFirm;
                vm.Landmark = app.Landmark;
                vm.Districts = app.Districts;
                vm.PostalZipCode = app.PostalZipCode;
                vm.EMail = app.EMail;
                vm.MobileNumber = app.MobileNumber;
                vm.TypeOfCeilings = app.TypeOfCeilings;
                vm.NameOfProprietorPartnerDirector = app.NameOfProprietorPartnerDirector;
                vm.DinNoSinNoRegistrationDetails = app.DinNoSinNoRegistrationDetails;
                vm.PanCardNo = app.PanCardNo;
                vm.ScheduleOfDrugs = app.ScheduleOfDrugs;
                vm.AluminiumSections = app.AluminiumSections;
                vm.Drawers = app.Drawers;
                vm.LockAndKey = app.LockAndKey;
                vm.Racks = app.Racks;
                vm.GlassSections = app.GlassSections;
                vm.Pellets = app.Pellets;
                vm.WoodenFurniture = app.WoodenFurniture;
                vm.AnyOther = app.AnyOther;
                vm.ColdStorage = app.ColdStorage;
                vm.TwentyFourHours = app.TwentyFourHours;

                vm.Pharmacists = (await GetPharmacistsAsync(app.ApplicationId))
                    .Select(p => new PharmacistInput
                    {
                        Name = p.Name, Sex = p.Sex, Qualification = p.Qualification, Experience = p.Experience,
                        CouncilNo = p.CouncilNo, ValidityDate = p.ValidityDate, DateOfBirth = p.DateOfBirth,
                        YearOfPassing = p.YearOfPassing
                    }).ToList();
            }

            await FillDisplayDataAsync(vm, app);
            return vm;
        }

        /// <summary>Fills the non-posted parts (dropdowns, uploaded-file list, status). Also used when re-showing a posted form with errors.</summary>
        public async Task FillDisplayDataAsync(DrugLicenceFormViewModel vm, TApp? app)
        {
            vm.Form = _form;
            vm.ConstitutionTypes = await LoadDropdownAsync(DrugLicenceConfig.CatConstitution, DrugLicenceConfig.DefaultConstitutionTypes);
            vm.DistrictList = await LoadDropdownAsync(DrugLicenceConfig.CatDistrict, DrugLicenceConfig.DefaultDistricts);
            vm.CeilingTypes = await LoadDropdownAsync(DrugLicenceConfig.CatCeiling, DrugLicenceConfig.DefaultCeilingTypes);

            if (app != null)
            {
                vm.ApplicationId = app.ApplicationId;
                vm.Status = app.Status;
                vm.ApplicationNumber = app.ApplicationNumber;
                vm.OfficerRemarks = app.OfficerRemarks;
                vm.ExistingDocuments = (await GetDocumentInfosAsync(app.ApplicationId))
                    .ToDictionary(d => d.DocumentType, d => d);
            }
        }

        // ---------------------------------------------------------------- mapping

        private static string Clean(string? s) => (s ?? string.Empty).Trim();
        private static string? CleanOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static void ApplyToEntity(DrugLicenceFormViewModel vm, TApp app)
        {
            // NOT NULL columns get "" rather than null so Save-as-Draft works on a half-filled form.
            app.IsRenewal = vm.IsRenewal;
            app.ApplicantName = Clean(vm.ApplicantName);
            app.FirmName = Clean(vm.FirmName);
            app.ManagerInchargeName = Clean(vm.ManagerInchargeName);
            app.ConstitutionType = Clean(vm.ConstitutionType);
            app.AddressOfTheFirm = Clean(vm.AddressOfTheFirm);
            app.Landmark = CleanOrNull(vm.Landmark);
            app.Districts = Clean(vm.Districts);
            app.PostalZipCode = Clean(vm.PostalZipCode);
            app.EMail = Clean(vm.EMail);
            app.MobileNumber = Clean(vm.MobileNumber);
            app.TypeOfCeilings = Clean(vm.TypeOfCeilings);

            app.NameOfProprietorPartnerDirector = CleanOrNull(vm.NameOfProprietorPartnerDirector);
            app.DinNoSinNoRegistrationDetails = CleanOrNull(vm.DinNoSinNoRegistrationDetails);
            app.PanCardNo = Clean(vm.PanCardNo).ToUpperInvariant();

            app.ScheduleOfDrugs = string.IsNullOrWhiteSpace(vm.ScheduleOfDrugs) ? "NotApplicable" : vm.ScheduleOfDrugs;

            app.AluminiumSections = vm.AluminiumSections;
            app.Drawers = vm.Drawers;
            app.LockAndKey = vm.LockAndKey;
            app.Racks = vm.Racks;
            app.GlassSections = vm.GlassSections;
            app.Pellets = vm.Pellets;
            app.WoodenFurniture = vm.WoodenFurniture;
            app.AnyOther = vm.AnyOther;

            app.ColdStorage = vm.ColdStorage;
            app.TwentyFourHours = vm.TwentyFourHours;
        }

        // ---------------------------------------------------------------- save

        /// <summary>
        /// Saves the form as it stands (insert or update), replaces the pharmacist rows and stores any newly
        /// uploaded PDFs. Does NOT change the workflow status - Submit is a separate step so a failed
        /// validation never loses what the applicant typed or uploaded.
        /// Returns the saved application and one message per rejected file (wrong type / too big).
        /// </summary>
        public async Task<(TApp App, List<string> FileErrors)> SaveAsync(
            DrugLicenceFormViewModel vm, TApp? existing, int userId, IFormFileCollection files)
        {
            var fileErrors = new List<string>();

            await using var tx = await _ctx.Database.BeginTransactionAsync();

            var app = existing;
            if (app == null)
            {
                app = new TApp
                {
                    UserId = userId,
                    Status = "Draft",
                    CurrentStage = "Initial Scrutiny",
                    PaymentStatus = "NotRequested",
                    CreatedDate = DateTime.UtcNow
                };
                Apps.Add(app);
            }

            ApplyToEntity(vm, app);
            app.ModifiedDate = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();   // identity value is known after this

            if (string.IsNullOrEmpty(app.ApplicationNumber))
            {
                app.ApplicationNumber = $"DL{_form.Code}/{DateTime.UtcNow.Year}/{app.ApplicationId:D6}";
                await _ctx.SaveChangesAsync();
            }

            // ---- pharmacists: replace the whole list with what the form posted ----
            var oldPharmacists = await _ctx.Pharmacists
                .Where(p => p.FormType == _form.Code && p.ApplicationId == app.ApplicationId)
                .ToListAsync();
            _ctx.Pharmacists.RemoveRange(oldPharmacists);

            foreach (var p in vm.Pharmacists.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
            {
                _ctx.Pharmacists.Add(new DrugLicencePharmacist
                {
                    FormType = _form.Code,
                    ApplicationId = app.ApplicationId,
                    Name = Clean(p.Name),
                    Sex = CleanOrNull(p.Sex),
                    Qualification = CleanOrNull(p.Qualification),
                    Experience = CleanOrNull(p.Experience),
                    CouncilNo = CleanOrNull(p.CouncilNo),
                    ValidityDate = p.ValidityDate?.Date,
                    DateOfBirth = p.DateOfBirth?.Date,
                    YearOfPassing = p.YearOfPassing
                });
            }

            // ---- documents: a newly chosen file replaces the previous one, no file = keep what's there ----
            var existingDocs = await _ctx.Documents
                .Where(d => d.FormType == _form.Code && d.ApplicationId == app.ApplicationId)
                .ToListAsync();

            foreach (var def in DrugLicenceConfig.Documents)
            {
                var file = files.GetFile("file_" + def.Key);
                if (file == null || file.Length == 0) continue;

                var problem = await ValidatePdfAsync(file);
                if (problem != null)
                {
                    fileErrors.Add($"{ShortLabel(def)}: {problem}");
                    continue;
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var (cipher, iv) = _encryption.Encrypt(ms.ToArray());

                var doc = existingDocs.FirstOrDefault(d => d.DocumentType == def.Key);
                if (doc == null)
                {
                    doc = new DrugLicenceDocument { FormType = _form.Code, ApplicationId = app.ApplicationId, DocumentType = def.Key };
                    _ctx.Documents.Add(doc);
                    existingDocs.Add(doc);
                }
                var safeName = Path.GetFileName(file.FileName);
                if (safeName.Length > 240) safeName = safeName[^240..];
                doc.FileName = safeName;
                doc.ContentType = "application/pdf";
                doc.FileData = cipher;
                doc.FileIV = iv;
                doc.UploadedDate = DateTime.UtcNow;

                app.SetDocumentFileName(def.Key, safeName);   // legacy NOT NULL varchar(500) column
            }

            await _ctx.SaveChangesAsync();
            await tx.CommitAsync();

            return (app, fileErrors);
        }

        private static string ShortLabel(DrugDocumentDef def) =>
            def.Key switch
            {
                "Affidavit" => "Affidavit / Special declaration",
                _ => def.Label
            };

        // PDF only, max 1 MB (same rule as the portal's sample form). Checks the real file header, not just the extension.
        private static async Task<string?> ValidatePdfAsync(IFormFile file)
        {
            if (file.Length > DrugLicenceConfig.MaxFileBytes)
                return "file is larger than 1 MB.";

            if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                return "only PDF files are allowed.";

            var header = new byte[5];
            using var s = file.OpenReadStream();
            var read = await s.ReadAsync(header.AsMemory(0, 5));
            if (read < 5 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46 || header[4] != 0x2D)
                return "this is not a valid PDF file.";

            return null;
        }

        // ---------------------------------------------------------------- submit

        /// <summary>Draft -> Submitted (lands at Initial Scrutiny, unassigned, so any DEO of the department can pick it up).</summary>
        public async Task SubmitAsync(TApp app)
        {
            app.Status = "Submitted";
            app.CurrentStage = "Initial Scrutiny";
            app.AssignedOfficerId = null;
            app.SubmittedDate = DateTime.UtcNow;
            app.ModifiedDate = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();
        }

        // ---------------------------------------------------------------- validation (used on Submit)

        /// <summary>
        /// Full validation. Returns (field name, message) pairs; field name is the form property so the
        /// message shows next to the field (use "" for messages that belong to no single field).
        /// Call AFTER SaveAsync so newly uploaded files count as present.
        /// </summary>
        public async Task<List<(string Field, string Message)>> ValidateForSubmitAsync(DrugLicenceFormViewModel vm, TApp app)
        {
            var errors = new List<(string, string)>();

            // ---- 1. Firm details ----
            var applicant = Clean(vm.ApplicantName);
            if (applicant.Length == 0) errors.Add((nameof(vm.ApplicantName), "Applicant name is required."));
            else if (!PersonNameRegex.IsMatch(applicant)) errors.Add((nameof(vm.ApplicantName), "Applicant name should contain letters only."));

            if (Clean(vm.FirmName).Length == 0) errors.Add((nameof(vm.FirmName), "Firm name is required."));

            var manager = Clean(vm.ManagerInchargeName);
            if (manager.Length == 0) errors.Add((nameof(vm.ManagerInchargeName), "Manager / Incharge name is required."));
            else if (!PersonNameRegex.IsMatch(manager)) errors.Add((nameof(vm.ManagerInchargeName), "Manager / Incharge name should contain letters only."));

            if (Clean(vm.ConstitutionType).Length == 0) errors.Add((nameof(vm.ConstitutionType), "Select the constitution type."));
            if (Clean(vm.AddressOfTheFirm).Length == 0) errors.Add((nameof(vm.AddressOfTheFirm), "Address of the firm is required."));
            if (Clean(vm.Districts).Length == 0) errors.Add((nameof(vm.Districts), "Select the district."));

            var pin = Clean(vm.PostalZipCode);
            if (pin.Length == 0) errors.Add((nameof(vm.PostalZipCode), "Postal / Zip code is required."));
            else if (!PinRegex.IsMatch(pin)) errors.Add((nameof(vm.PostalZipCode), "Enter a valid 6-digit postal code."));

            var email = Clean(vm.EMail);
            if (email.Length == 0) errors.Add((nameof(vm.EMail), "E-mail is required."));
            else if (!EmailRegex.IsMatch(email)) errors.Add((nameof(vm.EMail), "Enter a valid e-mail address."));

            var mobile = Clean(vm.MobileNumber);
            if (mobile.Length == 0) errors.Add((nameof(vm.MobileNumber), "Mobile number is required."));
            else if (!MobileRegex.IsMatch(mobile)) errors.Add((nameof(vm.MobileNumber), "Enter a valid 10-digit mobile number."));

            if (Clean(vm.TypeOfCeilings).Length == 0) errors.Add((nameof(vm.TypeOfCeilings), "Select the type of ceiling."));

            // ---- 2. Ownership ----
            var proprietor = Clean(vm.NameOfProprietorPartnerDirector);
            if (proprietor.Length > 0 && !PersonNameRegex.IsMatch(proprietor))
                errors.Add((nameof(vm.NameOfProprietorPartnerDirector), "Name should contain letters only."));

            var pan = Clean(vm.PanCardNo).ToUpperInvariant();
            if (pan.Length == 0) errors.Add((nameof(vm.PanCardNo), "PAN card number is required."));
            else if (!PanRegex.IsMatch(pan)) errors.Add((nameof(vm.PanCardNo), "Enter a valid PAN (e.g. ABCDE1234F)."));

            // ---- 3. Pharmacists ----
            var pharmacists = vm.Pharmacists.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
            if (pharmacists.Count == 0)
                errors.Add(("Pharmacists", "Add at least one Pharmacist / Competent person."));
            foreach (var (p, i) in pharmacists.Select((p, i) => (p, i)))
            {
                var who = $"Pharmacist #{i + 1}";
                if (!PersonNameRegex.IsMatch(Clean(p.Name))) errors.Add(("Pharmacists", $"{who}: name should contain letters only."));
                if (Clean(p.Sex).Length == 0) errors.Add(("Pharmacists", $"{who}: select the sex."));
                if (Clean(p.Qualification).Length == 0) errors.Add(("Pharmacists", $"{who}: qualification is required."));
                if (Clean(p.CouncilNo).Length == 0) errors.Add(("Pharmacists", $"{who}: council number is required."));
                if (p.YearOfPassing.HasValue && (p.YearOfPassing < 1950 || p.YearOfPassing > DateTime.Now.Year))
                    errors.Add(("Pharmacists", $"{who}: year of passing must be between 1950 and {DateTime.Now.Year}."));
            }

            // ---- 4. Licence details ----
            if (!DrugLicenceConfig.ScheduleOptions.Any(o => o.Value == vm.ScheduleOfDrugs))
                errors.Add((nameof(vm.ScheduleOfDrugs), "Select the schedule of drugs."));

            var anyStorage = vm.AluminiumSections || vm.Drawers || vm.LockAndKey || vm.Racks
                             || vm.GlassSections || vm.Pellets || vm.WoodenFurniture || vm.AnyOther;
            if (!anyStorage) errors.Add(("StorageDetails", "Select at least one storage detail."));

            // ---- documents (already saved by SaveAsync) ----
            var present = (await GetDocumentInfosAsync(app.ApplicationId)).Select(d => d.DocumentType).ToHashSet();
            foreach (var def in DrugLicenceConfig.Documents)
            {
                var required = def.AlwaysRequired || (def.Key == "ColdStorageProof" && vm.ColdStorage);
                if (required && !present.Contains(def.Key))
                    errors.Add(("file_" + def.Key, $"Upload: {def.Label}."));
            }

            return errors;
        }
    }
}
