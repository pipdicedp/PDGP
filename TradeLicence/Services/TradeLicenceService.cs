using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeLicence.Interfaces;
using TradeLicence.Models;
using static TradeLicence.Models.TradeLicenceApplication;

namespace TradeLicence.Services
{
    public class TradeLicenceService : ITradeLicenceService
    {
        private readonly ITradeLicenceRepository _repo;
        private readonly IFileEncryptionService _encryption;

        public TradeLicenceService(ITradeLicenceRepository repo, IFileEncryptionService encryption)
        {
            _repo = repo;
            _encryption = encryption;
        }

        public async Task SaveDraftAsync(TradeLicenceApplication model, List<int>? selectedDocuments)
        {
            model.Status = "Draft";

            bool isNewApplication = model.ApplicationId == 0;

            if (isNewApplication)
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
                    var doc = new ApplicationDocument { DocumentItemId = docId };

                    // New application: ID isn't known yet until SaveChanges runs the
                    // INSERT, so set the navigation and let EF fix up the FK afterward.
                    // Existing application: ID is already known — assign it directly
                    // instead of referencing `model`, which avoids attaching a second
                    // tracked instance for the same key (AddApplicationAsync already
                    // tracks the existing row separately when updating a draft).
                    if (isNewApplication)
                    {
                        doc.Application = model;
                    }
                    else
                    {
                        doc.ApplicationId = model.ApplicationId;
                    }

                    await _repo.AddApplicationDocumentAsync(doc);
                }
            }

            await _repo.SaveChangesAsync();
        }


        public async Task SubmitApplicationAsync(TradeLicenceApplication model, List<int>? selectedDocuments)
        {
            model.Status = "Submitted";

            bool isNewApplication = model.ApplicationId == 0;

            if (isNewApplication)
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
                    var doc = new ApplicationDocument { DocumentItemId = docId };

                    if (isNewApplication)
                    {
                        doc.Application = model;
                    }
                    else
                    {
                        doc.ApplicationId = model.ApplicationId;
                    }

                    await _repo.AddApplicationDocumentAsync(doc);
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

        // ---------------- Photographs (Step 4) ----------------

        public async Task SavePhotographsAsync(int applicationId, byte[]? applicantPhotoBytes, string? applicantPhotoContentType, byte[]? partnerPhotoBytes, string? partnerPhotoContentType)
        {
            var photo = await _repo.GetPhotographByApplicationIdAsync(applicationId);
            if (photo == null)
            {
                photo = new TradeLicencePhotograph { ApplicationId = applicationId };
                await _repo.AddPhotographAsync(photo);
            }

            if (applicantPhotoBytes != null)
            {
                var (cipher, iv) = _encryption.Encrypt(applicantPhotoBytes);
                photo.ApplicantPhotoData = cipher;
                photo.ApplicantPhotoIV = iv;
                photo.ApplicantPhotoContentType = applicantPhotoContentType;
            }

            if (partnerPhotoBytes != null)
            {
                var (cipher, iv) = _encryption.Encrypt(partnerPhotoBytes);
                photo.PartnerPhotoData = cipher;
                photo.PartnerPhotoIV = iv;
                photo.PartnerPhotoContentType = partnerPhotoContentType;
            }

            photo.UploadedDate = DateTime.UtcNow;

            await _repo.SaveChangesAsync();
        }

        public async Task<(byte[] Bytes, string ContentType)?> GetDecryptedApplicantPhotoAsync(int applicationId)
        {
            var photo = await _repo.GetPhotographByApplicationIdAsync(applicationId);
            if (photo?.ApplicantPhotoData == null || photo.ApplicantPhotoIV == null) return null;

            var bytes = _encryption.Decrypt(photo.ApplicantPhotoData, photo.ApplicantPhotoIV);
            return (bytes, photo.ApplicantPhotoContentType ?? "application/octet-stream");
        }

        public async Task<(byte[] Bytes, string ContentType)?> GetDecryptedPartnerPhotoAsync(int applicationId)
        {
            var photo = await _repo.GetPhotographByApplicationIdAsync(applicationId);
            if (photo?.PartnerPhotoData == null || photo.PartnerPhotoIV == null) return null;

            var bytes = _encryption.Decrypt(photo.PartnerPhotoData, photo.PartnerPhotoIV);
            return (bytes, photo.PartnerPhotoContentType ?? "application/octet-stream");
        }

        // ---------------- Documents (Step 5) ----------------

        public async Task<TradeLicenceDocument> SaveDocumentAsync(int applicationId, string documentName, string fileName, byte[] fileBytes, string contentType)
        {
            var (cipher, iv) = _encryption.Encrypt(fileBytes);

            var doc = await _repo.GetDocumentByApplicationAndNameAsync(applicationId, documentName);
            if (doc == null)
            {
                doc = new TradeLicenceDocument
                {
                    ApplicationId = applicationId,
                    DocumentName = documentName
                };
                await _repo.AddDocumentAsync(doc);
            }

            doc.FileName = fileName;
            doc.DocumentData = cipher;
            doc.DocumentIV = iv;
            doc.DocumentContentType = contentType;
            doc.UploadedDate = DateTime.UtcNow;

            await _repo.SaveChangesAsync();
            return doc;
        }

        public async Task<List<TradeLicenceDocument>> GetDocumentsAsync(int applicationId)
        {
            return await _repo.GetDocumentsByApplicationIdAsync(applicationId);
        }

        public async Task<(byte[] Bytes, string ContentType, string FileName)?> GetDecryptedDocumentAsync(int documentId)
        {
            var doc = await _repo.GetDocumentByIdAsync(documentId);
            if (doc?.DocumentData == null || doc.DocumentIV == null) return null;

            var bytes = _encryption.Decrypt(doc.DocumentData, doc.DocumentIV);
            return (bytes, doc.DocumentContentType ?? "application/octet-stream", doc.FileName ?? "document");
        }

        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            var doc = await _repo.GetDocumentByIdAsync(documentId);
            if (doc == null) return false;

            _repo.RemoveDocument(doc);
            await _repo.SaveChangesAsync();
            return true;
        }

        // ---------------- Shop Establishment Registration (Step 6) ----------------

        public async Task<ShopEstablishmentRegistration> SaveShopEstablishmentAsync(int applicationId, ShopEstablishmentRegistration input)
        {
            var registration = await _repo.GetShopRegistrationByApplicationIdAsync(applicationId);
            if (registration == null)
            {
                registration = new ShopEstablishmentRegistration { ApplicationId = applicationId };
                await _repo.AddShopRegistrationAsync(registration);
            }

            registration.ApplicantName = input.ApplicantName;
            registration.ShopOrEstablishmentName = input.ShopOrEstablishmentName;
            registration.RegistrationPeriod = input.RegistrationPeriod;
            registration.TypeOfEstablishment = input.TypeOfEstablishment;
            registration.MobileNumber = input.MobileNumber;
            registration.EmailId = input.EmailId;

            registration.ShopAddressLine1 = input.ShopAddressLine1;
            registration.ShopAddressLine2 = input.ShopAddressLine2;
            registration.ShopDistrictRegion = input.ShopDistrictRegion;
            registration.ShopCommune = input.ShopCommune;
            registration.ShopPinCode = input.ShopPinCode;

            registration.CommAddressLine1 = input.CommAddressLine1;
            registration.CommAddressLine2 = input.CommAddressLine2;
            registration.CommDistrictRegion = input.CommDistrictRegion;
            registration.CommCommune = input.CommCommune;
            registration.CommPinCode = input.CommPinCode;

            registration.MaxEmployeesProposed = input.MaxEmployeesProposed;
            registration.MaleEmployees = input.MaleEmployees;
            registration.FemaleEmployees = input.FemaleEmployees;
            registration.TransgenderEmployees = input.TransgenderEmployees;
            registration.TotalEmployees = input.TotalEmployees;

            registration.ManagerFullName = input.ManagerFullName;
            registration.ManagerAddressLine1 = input.ManagerAddressLine1;
            registration.ManagerAddressLine2 = input.ManagerAddressLine2;
            registration.ManagerCountry = input.ManagerCountry;
            registration.ManagerState = input.ManagerState;
            registration.ManagerDistrict = input.ManagerDistrict;
            registration.ManagerPostalZipCode = input.ManagerPostalZipCode;
            registration.ManagerMobileNumber = input.ManagerMobileNumber;

            registration.MigrantWorkersDirect = input.MigrantWorkersDirect;
            registration.MigrantWorkersThroughContractor = input.MigrantWorkersThroughContractor;

            registration.DateOfPaymentOfWages = input.DateOfPaymentOfWages;
            registration.AmountPaid = input.AmountPaid;
            registration.GrasReferenceNumber = input.GrasReferenceNumber;
            registration.DateOfPayment = input.DateOfPayment;

            registration.ModifiedDate = DateTime.UtcNow;

            await _repo.SaveChangesAsync();
            return registration;
        }

        public async Task<ShopEstablishmentRegistration?> GetShopEstablishmentAsync(int applicationId)
        {
            return await _repo.GetShopRegistrationByApplicationIdAsync(applicationId);
        }

        public async Task<List<TradeLicenceMachinery>> GetMachineryAsync(int applicationId)
        {
            return await _repo.GetMachineryByApplicationIdAsync(applicationId);
        }

        // ---------------- Acknowledgement PDF ----------------

        // ---------------- Application Preview (shared by citizen wizard and officer view) ----------------

        public async Task<ApplicationPreviewViewModel?> GetApplicationPreviewAsync(int applicationId)
        {
            var application = await GetApplicationAsync(applicationId);
            if (application == null) return null;

            var model = new ApplicationPreviewViewModel
            {
                Application = application,
                Partners = await GetPartnersAsync(applicationId),
                Machinery = await GetMachineryAsync(applicationId),
                Documents = await GetDocumentsAsync(applicationId),
                ShopRegistration = application.IsRegistrationForShopsEstablishment
                    ? await GetShopEstablishmentAsync(applicationId)
                    : null
            };

            if (application.MunicipalityId.HasValue)
            {
                var municipalities = await GetMunicipalitiesAsync();
                model.MunicipalityName = municipalities.FirstOrDefault(m => m.MunicipalityId == application.MunicipalityId)?.MunicipalityName;
            }
            if (application.WardId.HasValue && application.MunicipalityId.HasValue)
            {
                var wards = await GetWardsAsync(application.MunicipalityId.Value);
                model.WardName = wards.FirstOrDefault(w => w.WardId == application.WardId)?.WardName;
            }
            if (application.AreaId.HasValue && application.WardId.HasValue)
            {
                var areas = await GetAreasAsync(application.WardId.Value);
                model.AreaName = areas.FirstOrDefault(a => a.AreaId == application.AreaId)?.AreaName;
            }
            if (application.StreetId.HasValue && application.AreaId.HasValue)
            {
                var streets = await GetStreetsAsync(application.AreaId.Value);
                model.StreetName = streets.FirstOrDefault(s => s.StreetId == application.StreetId)?.StreetName;
            }

            return model;
        }

        public async Task<byte[]> GenerateAcknowledgementPdfAsync(int applicationId)
        {
            var application = await _repo.GetApplicationWithDocumentsAsync(applicationId)
                ?? throw new InvalidOperationException("Application not found.");

            var partners = await _repo.GetPartnersAsync(applicationId);
            var machinery = await _repo.GetMachineryByApplicationIdAsync(applicationId);
            var documents = await _repo.GetDocumentsByApplicationIdAsync(applicationId);
            var shop = application.IsRegistrationForShopsEstablishment
                ? await _repo.GetShopRegistrationByApplicationIdAsync(applicationId)
                : null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Trade Licence Application — Acknowledgement Slip")
                            .Bold().FontSize(16).FontColor("#1a3a52");
                        col.Item().AlignCenter().Text("Government of Puducherry — Local Administration Department")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor("#1a3a52");
                    });

                    page.Content().PaddingTop(15).Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Background("#f5f9fa").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Application Number: ").SemiBold();
                                t.Span(application.ApplicationNumber ?? "-");
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Status: ").SemiBold();
                                t.Span(application.Status);
                            });
                        });

                        col.Item().Text("Applicant & Trade Details").Bold().FontSize(12).FontColor("#1a3a52");
                        col.Item().Element(c => AddDetailTable(c, new (string, string)[]
                        {
                            ("Applicant Name", application.ApplicantName),
                            ("Father/Husband Name", application.ApplicantFatherHusbandName),
                            ("Age", application.AgeOfApplicant.ToString()),
                            ("Mobile Number", application.MobileNumber),
                            ("Residential Address", application.ApplicantResidentialAddress),
                            ("Ownership Type", application.OwnershipType),
                            ("Name & Style of Factory/Trade", application.NameAndStyleOfFactory),
                            ("Purpose of Licence", application.PurposeOfLicence),
                            ("Trade Place Address", application.TradePlaceCommunicationAddress),
                            ("Total Area (Sq.Ft.)", application.TotalAreaCoveredSqFt.ToString()),
                            ("Date of Commencement", application.DateOfCommencement.ToString("dd-MM-yyyy")),
                            ("Licence Period", application.LicencePeriod)
                        }));

                        if (partners.Any())
                        {
                            col.Item().PaddingTop(8).Text("Partners").Bold().FontSize(12).FontColor("#1a3a52");
                            col.Item().Element(c => AddListTable(c,
                                new[] { "Partner Name", "Designation", "Address" },
                                partners.Select(p => new[] { p.PartnerName, p.Designation, p.Address })));
                        }

                        if (machinery.Any())
                        {
                            col.Item().PaddingTop(8).Text("Machinery Details").Bold().FontSize(12).FontColor("#1a3a52");
                            col.Item().Element(c => AddListTable(c,
                                new[] { "Machinery Name", "Quantity", "Horse Power" },
                                machinery.Select(m => new[] { m.MachineryName, m.Quantity.ToString(), m.HorsePower.ToString() })));
                        }

                        if (documents.Any())
                        {
                            col.Item().PaddingTop(8).Text("Documents Submitted").Bold().FontSize(12).FontColor("#1a3a52");
                            col.Item().Element(c => AddListTable(c,
                                new[] { "Document Name", "File Name", "Uploaded Date" },
                                documents.Select(d => new[]
                                {
                                    d.DocumentName,
                                    d.FileName ?? "-",
                                    d.UploadedDate?.ToLocalTime().ToString("dd-MM-yyyy") ?? "-"
                                })));
                        }

                        if (shop != null)
                        {
                            col.Item().PaddingTop(8).Text("Shop / Establishment Registration").Bold().FontSize(12).FontColor("#1a3a52");
                            col.Item().Element(c => AddDetailTable(c, new (string, string)[]
                            {
                                ("Shop/Establishment Name", shop.ShopOrEstablishmentName ?? "-"),
                                ("Registration Period", shop.RegistrationPeriod ?? "-"),
                                ("Type of Establishment", shop.TypeOfEstablishment ?? "-"),
                                ("Total Employees", shop.TotalEmployees.ToString()),
                                ("Manager Name", shop.ManagerFullName ?? "-")
                            }));
                        }

                        col.Item().PaddingTop(15).Background("#FFF8E6").Padding(10).Text(
                            "Please keep this acknowledgement for your records. You can track the status of your application anytime using the Application Number above.")
                            .FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Generated on ").FontSize(8);
                        t.Span(DateTime.Now.ToString("dd-MM-yyyy hh:mm tt")).FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static void AddDetailTable(IContainer container, (string Label, string? Value)[] rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                foreach (var (label, value) in rows)
                {
                    table.Cell().Background("#f5f9fa").Padding(5).Text(label).SemiBold();
                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(value ?? "-");
                }
            });
        }

        private static void AddListTable(IContainer container, string[] headers, IEnumerable<string?[]> rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    foreach (var _ in headers) columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    foreach (var h in headers)
                        header.Cell().Background("#1a3a52").Padding(5).Text(h).FontColor(Colors.White).SemiBold();
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cell ?? "-");
                }
            });
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
