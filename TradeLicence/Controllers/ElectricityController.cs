using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
// iText 7 PDF namespaces 📑
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TradeLicence.Data;
using TradeLicence.Models;
using TradeLicence.Repositories;
using PdfTable = iText.Layout.Element.Table;

namespace TradeLicence.Controllers
{
    [Authorize]
    public class ElectricityController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationRepository _repository;
        private readonly ElectricityApplicationDbContext _context;

        public ElectricityController(IWebHostEnvironment environment, ApplicationRepository repository, ElectricityApplicationDbContext context)
        {
            _environment = environment;
            _repository = repository;
            _context = context;
        }
        private void LoadDropdowns()
        {
            ViewBag.ServiceCategoryList = _context.DropdownMasters
                .Where(d => d.Category == "ServiceCategory" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();

            ViewBag.OwnershipTypeList = _context.DropdownMasters
                .Where(d => d.Category == "OwnershipType" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();

            ViewBag.SupplyCategoryList = _context.DropdownMasters
                .Where(d => d.Category == "SupplyCategory" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();

            ViewBag.TypeOfSupplyList = _context.DropdownMasters
                .Where(d => d.Category == "TypeOfSupply" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();

            ViewBag.AddressProofTypeList = _context.DropdownMasters
                .Where(d => d.Category == "AddressProofType" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();

            ViewBag.IdentityProofTypeList = _context.DropdownMasters
                .Where(d => d.Category == "IdentityProofType" && d.IsActive)
                .OrderBy(d => d.SortOrder).Select(d => d.Value).ToList();
        }
        [HttpGet]
        public IActionResult Index()
        {
            var model = new ApplicationViewModel
            {
                CurrentStep = 1,
                ApplicationNumber = "T/" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss")
            };
            LoadDropdowns();
            return View(model);
        }

        [HttpPost]
        public IActionResult Index(ApplicationViewModel model, string actionButton)
        {
            if (actionButton == "next")
            {
                ModelState.Clear();
                ValidateCurrentStep(model);

                if (!ModelState.IsValid)
                {
                    LoadDropdowns();
                    return View(model);
                }

                _repository.SaveOrUpdateStep(model, model.CurrentStep);

                if (model.CurrentStep < 4)
                {
                    model.CurrentStep++;
                    ModelState.Clear();
                }
            }
            else if (actionButton == "prev")
            {
                ModelState.Clear();
                if (model.CurrentStep > 1)
                {
                    model.CurrentStep--;
                }
            }
            else if (actionButton == "submit")
            {
                ModelState.Clear();
                ValidateCurrentStep(model);

                if (!ModelState.IsValid)
                {
                    LoadDropdowns();
                    return View(model);
                }

                // Mandatory uploads
                model.PhotoPath = UploadFile(model.PhotoFile) ?? model.PhotoPath;
                model.AddressProofPath = UploadFile(model.AddressProofFile) ?? model.AddressProofPath;
                model.IdentityProofPath = UploadFile(model.IdentityProofFile) ?? model.IdentityProofPath;
                model.TestReportPath = UploadFile(model.TestReportFile) ?? model.TestReportPath;

                // Optional uploads
                if (model.SelectedSaleDeed) model.SaleDeedPath = UploadFile(model.SaleDeedFile) ?? model.SaleDeedPath;
                if (model.SelectedPowerOfAttorney) model.PowerOfAttorneyPath = UploadFile(model.PowerOfAttorneyFile) ?? model.PowerOfAttorneyPath;
                if (model.SelectedMunicipalTax) model.MunicipalTaxPath = UploadFile(model.MunicipalTaxFile) ?? model.MunicipalTaxPath;
                if (model.SelectedAllotmentLetter) model.AllotmentLetterPath = UploadFile(model.AllotmentLetterFile) ?? model.AllotmentLetterPath;
                if (model.SelectedHouseRegistration) model.HouseRegistrationPath = UploadFile(model.HouseRegistrationFile) ?? model.HouseRegistrationPath;
                if (model.SelectedLease) model.LeasePath = UploadFile(model.LeaseFile) ?? model.LeasePath;
                if (model.SelectedOtherOwnership) model.OtherOwnershipPath = UploadFile(model.OtherOwnershipFile) ?? model.OtherOwnershipPath;
                if (model.SelectedPowerAgentPhoto) model.PowerAgentPhotoPath = UploadFile(model.PowerAgentPhotoFile) ?? model.PowerAgentPhotoPath;
                if (model.SelectedOthers) model.OthersPath = UploadFile(model.OthersFile) ?? model.OthersPath;

                _repository.SaveOrUpdateStep(model, 4);

                TempData["ShowSuccessModal"] = true;
                TempData["ApplicantName"] = model.ApplicantName;
                TempData["AppNumber"] = model.ApplicationNumber;
                TempData["ServiceCat"] = model.ServiceCategory;
                TempData["Mobile"] = model.MobileNumber;

                return RedirectToAction("Preview", new { appNum = model.ApplicationNumber });
            }
            LoadDropdowns();
            return View(model);
        }

        [HttpGet]
        public IActionResult Preview(string? appNum)
        {
            if (string.IsNullOrWhiteSpace(appNum))
            {
                return RedirectToAction("Index");
            }

            var model = _repository.GetByApplicationNumber(appNum);
            if (model == null)
            {
                return NotFound("Application details could not be found.");
            }

            return View("Preview", model);
        }

        [HttpGet]
        public IActionResult DownloadAcknowledgement(string? appNum)
        {
            if (string.IsNullOrWhiteSpace(appNum))
            {
                return RedirectToAction("Index");
            }

            var model = _repository.GetByApplicationNumber(appNum);
            if (model == null)
            {
                return NotFound("Application details could not be found.");
            }

            using (MemoryStream stream = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(stream);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);
                document.SetMargins(20, 20, 20, 20);

                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                DeviceRgb primaryColor = new DeviceRgb(13, 110, 253);
                DeviceRgb darkHeader = new DeviceRgb(33, 37, 41);
                DeviceRgb bgLight = new DeviceRgb(248, 249, 250);

                // Header Banner
                PdfTable headerTable = new PdfTable(UnitValue.CreatePercentArray(new float[] { 100 })).SetWidth(UnitValue.CreatePercentValue(100));
                Cell headerCell = new Cell().SetBackgroundColor(primaryColor).SetPadding(12).SetTextAlignment(TextAlignment.CENTER);
                headerCell.Add(new Paragraph("ELECTRICITY DEPARTMENT").SetFontSize(16).SetFont(boldFont).SetFontColor(ColorConstants.WHITE));
                headerCell.Add(new Paragraph("NEW SERVICE CONNECTION ACKNOWLEDGEMENT").SetFontSize(11).SetFont(regularFont).SetFontColor(ColorConstants.WHITE));
                headerTable.AddCell(headerCell);
                document.Add(headerTable);
                document.Add(new Paragraph("\n"));

                // Summary & Photo
                PdfTable mainSummaryTable = new PdfTable(UnitValue.CreatePercentArray(new float[] { 75, 25 })).SetWidth(UnitValue.CreatePercentValue(100));
                Cell summaryCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                summaryCell.Add(new Paragraph($"Application No : {model.ApplicationNumber ?? "N/A"}").SetFont(boldFont).SetFontSize(12).SetFontColor(primaryColor));
                summaryCell.Add(new Paragraph($"Applicant Name : {model.ApplicantName ?? "N/A"}").SetFont(regularFont).SetFontSize(10));
                summaryCell.Add(new Paragraph($"Mobile Number  : {model.MobileNumber ?? "N/A"}").SetFont(regularFont).SetFontSize(10));
                summaryCell.Add(new Paragraph($"Connection Type: {model.ConnectionType ?? "N/A"}").SetFont(regularFont).SetFontSize(10));
                summaryCell.Add(new Paragraph($"Submission Date: {DateTime.Now:dd/MM/yyyy hh:mm tt}").SetFont(regularFont).SetFontSize(9).SetFontColor(ColorConstants.DARK_GRAY));
                mainSummaryTable.AddCell(summaryCell);

                Cell photoCell = new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
                string photoFullPath = string.IsNullOrEmpty(model.PhotoPath) ? "" : Path.Combine(_environment.WebRootPath, model.PhotoPath.TrimStart('/'));

                if (!string.IsNullOrEmpty(photoFullPath) && System.IO.File.Exists(photoFullPath))
                {
                    ImageData imgData = ImageDataFactory.Create(photoFullPath);
                    Image photo = new Image(imgData).SetWidth(80).SetHeight(90).SetAutoScale(false);
                    photoCell.Add(photo);
                }
                else
                {
                    photoCell.Add(new Paragraph("[ Photo ]\nUploaded").SetFontSize(9).SetFont(regularFont).SetTextAlignment(TextAlignment.CENTER).SetPadding(15).SetBackgroundColor(bgLight));
                }
                mainSummaryTable.AddCell(photoCell);
                document.Add(mainSummaryTable);
                document.Add(new Paragraph("\n"));

                void AddSectionHeader(string title)
                {
                    PdfTable secTable = new PdfTable(UnitValue.CreatePercentArray(new float[] { 100 })).SetWidth(UnitValue.CreatePercentValue(100));
                    secTable.AddCell(new Cell().SetBackgroundColor(darkHeader).SetPadding(4).Add(new Paragraph(title).SetFontSize(10).SetFont(boldFont).SetFontColor(ColorConstants.WHITE)));
                    document.Add(secTable);
                }

                void AddDataRow(PdfTable t, string l1, string? v1, string l2, string? v2)
                {
                    t.AddCell(new Cell().Add(new Paragraph(l1).SetFont(boldFont).SetFontSize(9)).SetBackgroundColor(bgLight));
                    t.AddCell(new Cell().Add(new Paragraph(v1 ?? "N/A").SetFont(regularFont).SetFontSize(9)));
                    t.AddCell(new Cell().Add(new Paragraph(l2).SetFont(boldFont).SetFontSize(9)).SetBackgroundColor(bgLight));
                    t.AddCell(new Cell().Add(new Paragraph(v2 ?? "N/A").SetFont(regularFont).SetFontSize(9)));
                }

                // 1. Applicant & Service Details
                AddSectionHeader("1. APPLICANT & SERVICE DETAILS");
                PdfTable tbl1 = new PdfTable(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).SetWidth(UnitValue.CreatePercentValue(100));
                AddDataRow(tbl1, "Service Category", model.ServiceCategory, "Ownership Type", model.OwnershipType);
                AddDataRow(tbl1, "Connection Type", model.ConnectionType, "Service Type", model.ServiceType);
                AddDataRow(tbl1, "Applicant Name", model.ApplicantName, "Gender", model.Gender);
                AddDataRow(tbl1, "Father/Husband Name", model.RelativeName, "Mobile Number", model.MobileNumber);
                AddDataRow(tbl1, "Proposed Pincode", model.ProposedPincode, "Email Address", model.Email);
                document.Add(tbl1);
                document.Add(new Paragraph("\n"));

                // 2. Address Details
                AddSectionHeader("2. ADDRESS DETAILS");
                PdfTable tbl2 = new PdfTable(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).SetWidth(UnitValue.CreatePercentValue(100));
                AddDataRow(tbl2, "Site Door/Plot No", model.SiteDoorNo, "Site Street Name", model.SiteStreet);
                AddDataRow(tbl2, "Site Area", model.SiteArea, "District/Region", model.SiteDistrict);
                AddDataRow(tbl2, "Site Pincode", model.SitePincode, "Landmark 1", model.Landmark1);
                AddDataRow(tbl2, "Landmark 2", model.Landmark2, "Comm. Same as Site?", model.IsSameAddress ? "Yes" : "No");
                if (!model.IsSameAddress)
                {
                    AddDataRow(tbl2, "Comm. Door No", model.CommDoorNo, "Comm. Street", model.CommStreet);
                    AddDataRow(tbl2, "Comm. Area", model.CommArea, "Comm. Region", model.CommRegion);
                    AddDataRow(tbl2, "Comm. Pincode", model.CommPincode, "-", "-");
                }
                document.Add(tbl2);
                document.Add(new Paragraph("\n"));

                // 3. Technical Specifications
                AddSectionHeader("3. PLOT & TECHNICAL SPECIFICATIONS");
                PdfTable tbl3 = new PdfTable(UnitValue.CreatePercentArray(new float[] { 25, 25, 25, 25 })).SetWidth(UnitValue.CreatePercentValue(100));
                AddDataRow(tbl3, "RS No.", model.RSNo, "Plot Area (Sq.ft)", model.PlotArea?.ToString("0.##"));
                AddDataRow(tbl3, "Build Area (Sq.ft)", model.BuildArea?.ToString("0.##"), "Supply Category", model.SupplyCategory);
                AddDataRow(tbl3, "Purpose", model.Purpose, "Applied Load (Watts)", model.TotalLoad?.ToString("0.##"));
                AddDataRow(tbl3, "Type of Supply", model.TypeOfSupply, "Own Meter Preference", model.OwnMeter == "true" ? "Yes" : "No");
                document.Add(tbl3);
                document.Add(new Paragraph("\n"));

                // 4. Document Checklist
                AddSectionHeader("4. SUBMITTED DOCUMENTS CHECKLIST");
                PdfTable tbl4 = new PdfTable(UnitValue.CreatePercentArray(new float[] { 30, 40, 30 })).SetWidth(UnitValue.CreatePercentValue(100));
                tbl4.AddCell(new Cell().Add(new Paragraph("Document Type").SetFont(boldFont).SetFontSize(9)).SetBackgroundColor(bgLight));
                tbl4.AddCell(new Cell().Add(new Paragraph("Selected Proof Type").SetFont(boldFont).SetFontSize(9)).SetBackgroundColor(bgLight));
                tbl4.AddCell(new Cell().Add(new Paragraph("Upload Status").SetFont(boldFont).SetFontSize(9)).SetBackgroundColor(bgLight));

                tbl4.AddCell(new Cell().Add(new Paragraph("Photograph").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Passport Size Photo (.jpg)").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Uploaded").SetFont(regularFont).SetFontSize(9).SetFontColor(new DeviceRgb(25, 135, 84))));

                tbl4.AddCell(new Cell().Add(new Paragraph("Address Proof").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph(model.AddressProofType ?? "Address Proof").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Uploaded").SetFont(regularFont).SetFontSize(9).SetFontColor(new DeviceRgb(25, 135, 84))));

                tbl4.AddCell(new Cell().Add(new Paragraph("Identity Proof").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph(model.IdentityProofType ?? "Identity Proof").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Uploaded").SetFont(regularFont).SetFontSize(9).SetFontColor(new DeviceRgb(25, 135, 84))));

                tbl4.AddCell(new Cell().Add(new Paragraph("Test Report").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Third Party Contractor Report").SetFont(regularFont).SetFontSize(9)));
                tbl4.AddCell(new Cell().Add(new Paragraph("Uploaded").SetFont(regularFont).SetFontSize(9).SetFontColor(new DeviceRgb(25, 135, 84))));

                document.Add(tbl4);

                document.Add(new Paragraph("\nNote: This is an official computer-generated acknowledgement receipt.").SetFontSize(8).SetFont(italicFont).SetTextAlignment(TextAlignment.CENTER).SetFontColor(ColorConstants.GRAY));

                document.Close();
                return File(stream.ToArray(), "application/pdf", $"Acknowledgement_{model.ApplicationNumber?.Replace("/", "_")}.pdf");
            }
        }

        private void ValidateCurrentStep(ApplicationViewModel model)
        {
            if (model.CurrentStep == 1)
            {
                if (string.IsNullOrWhiteSpace(model.ServiceCategory)) ModelState.AddModelError("ServiceCategory", "Service Category is required.");
                if (string.IsNullOrWhiteSpace(model.OwnershipType)) ModelState.AddModelError("OwnershipType", "Ownership Type is required.");
                if (string.IsNullOrWhiteSpace(model.ConnectionType)) ModelState.AddModelError("ConnectionType", "Connection Type is required.");
                if (string.IsNullOrWhiteSpace(model.ServiceType)) ModelState.AddModelError("ServiceType", "Service Type is required.");
                if (string.IsNullOrWhiteSpace(model.ApplicantName)) ModelState.AddModelError("ApplicantName", "Applicant Name is required.");
                if (string.IsNullOrWhiteSpace(model.Gender)) ModelState.AddModelError("Gender", "Gender selection is required.");
                if (string.IsNullOrWhiteSpace(model.RelativeName)) ModelState.AddModelError("RelativeName", "Father/Husband Name is required.");

                // Proposed Pincode: 6 digits
                if (string.IsNullOrWhiteSpace(model.ProposedPincode))
                    ModelState.AddModelError("ProposedPincode", "Proposed Pincode is required.");
                else if (!Regex.IsMatch(model.ProposedPincode, @"^[0-9]{6}$"))
                    ModelState.AddModelError("ProposedPincode", "Proposed Pincode must be exactly 6 digits.");

                // Mobile Number: 10 digits
                if (string.IsNullOrWhiteSpace(model.MobileNumber))
                    ModelState.AddModelError("MobileNumber", "Mobile Number is required.");
                else if (!Regex.IsMatch(model.MobileNumber, @"^[0-9]{10}$"))
                    ModelState.AddModelError("MobileNumber", "Mobile Number must be exactly 10 digits.");
            }
            else if (model.CurrentStep == 2)
            {
                if (string.IsNullOrWhiteSpace(model.SiteDoorNo)) ModelState.AddModelError("SiteDoorNo", "Door/Plot No. is required.");
                if (string.IsNullOrWhiteSpace(model.SiteStreet)) ModelState.AddModelError("SiteStreet", "Street Name is required.");
                if (string.IsNullOrWhiteSpace(model.SiteArea)) ModelState.AddModelError("SiteArea", "Area is required.");
                if (string.IsNullOrWhiteSpace(model.SiteDistrict)) ModelState.AddModelError("SiteDistrict", "District is required.");

                // Site Pincode
                if (string.IsNullOrWhiteSpace(model.SitePincode))
                    ModelState.AddModelError("SitePincode", "Site Pincode is required.");
                else if (!Regex.IsMatch(model.SitePincode, @"^[0-9]{6}$"))
                    ModelState.AddModelError("SitePincode", "Site Pincode must be exactly 6 digits.");

                // Email Validation
                if (string.IsNullOrWhiteSpace(model.Email))
                    ModelState.AddModelError("Email", "Email address is required.");
                else if (!Regex.IsMatch(model.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    ModelState.AddModelError("Email", "Please enter a valid email address.");

                if (string.IsNullOrWhiteSpace(model.Landmark1)) ModelState.AddModelError("Landmark1", "Landmark 1 is required.");
                if (string.IsNullOrWhiteSpace(model.Landmark2)) ModelState.AddModelError("Landmark2", "Landmark 2 is required.");

                if (!model.IsSameAddress)
                {
                    if (string.IsNullOrWhiteSpace(model.CommDoorNo)) ModelState.AddModelError("CommDoorNo", "Communication Door No. is required.");
                    if (string.IsNullOrWhiteSpace(model.CommStreet)) ModelState.AddModelError("CommStreet", "Communication Street is required.");
                    if (string.IsNullOrWhiteSpace(model.CommArea)) ModelState.AddModelError("CommArea", "Communication Area is required.");
                    if (string.IsNullOrWhiteSpace(model.CommRegion)) ModelState.AddModelError("CommRegion", "Communication Region is required.");

                    if (string.IsNullOrWhiteSpace(model.CommPincode))
                        ModelState.AddModelError("CommPincode", "Communication Pincode is required.");
                    else if (!Regex.IsMatch(model.CommPincode, @"^[0-9]{6}$"))
                        ModelState.AddModelError("CommPincode", "Communication Pincode must be exactly 6 digits.");
                }
            }
            else if (model.CurrentStep == 3)
            {
                if (string.IsNullOrWhiteSpace(model.RSNo))
                    ModelState.AddModelError("RSNo", "RS No. is required.");

                // Plot & Build Area checks
                if (!model.PlotArea.HasValue || model.PlotArea <= 0)
                    ModelState.AddModelError("PlotArea", "Plot Area is required and must be greater than 0.");

                if (!model.BuildArea.HasValue || model.BuildArea <= 0)
                    ModelState.AddModelError("BuildArea", "Build Area is required and must be greater than 0.");
                else if (model.PlotArea.HasValue && model.BuildArea > model.PlotArea)
                    ModelState.AddModelError("BuildArea", "Build Area cannot exceed the total Plot Area.");

                if (string.IsNullOrWhiteSpace(model.SupplyCategory))
                    ModelState.AddModelError("SupplyCategory", "Supply Category is required.");

                if (string.IsNullOrWhiteSpace(model.Purpose))
                    ModelState.AddModelError("Purpose", "Purpose is required.");

                // Applied Load Check (Watts)
                if (!model.TotalLoad.HasValue || model.TotalLoad <= 0)
                {
                    ModelState.AddModelError("TotalLoad", "Total Applied Load is required and must be greater than 0.");
                }
                else if (model.ConnectionType == "HT" && model.TotalLoad < 50000)
                {
                    ModelState.AddModelError("TotalLoad", "For HT Service, the applied load must be at least 50,000 Watts (50 kW / 63 kVA).");
                }
                else if (model.ConnectionType == "LT" && model.TotalLoad > 50000)
                {
                    ModelState.AddModelError("TotalLoad", "Loads exceeding 50,000 Watts should apply under HT Service.");
                }

                if (string.IsNullOrWhiteSpace(model.TypeOfSupply))
                    ModelState.AddModelError("TypeOfSupply", "Type of Supply is required.");

                if (string.IsNullOrWhiteSpace(model.OwnMeter))
                    ModelState.AddModelError("OwnMeter", "Meter selection is required.");
            }
            else if (model.CurrentStep == 4)
            {
                if (string.IsNullOrWhiteSpace(model.AddressProofType))
                    ModelState.AddModelError("AddressProofType", "Address Proof selection is required.");

                if (string.IsNullOrWhiteSpace(model.IdentityProofType))
                    ModelState.AddModelError("IdentityProofType", "Identity Proof selection is required.");

                // 📸 Photo: .jpg/.jpeg only, max 50 KB
                if ((model.PhotoFile == null || model.PhotoFile.Length == 0) && string.IsNullOrEmpty(model.PhotoPath))
                {
                    ModelState.AddModelError("PhotoFile", "Photograph is required.");
                }
                else if (model.PhotoFile != null && model.PhotoFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.PhotoFile.FileName).ToLowerInvariant();
                    if (ext != ".jpg" && ext != ".jpeg")
                        ModelState.AddModelError("PhotoFile", "Photo must be in .jpg or .jpeg format.");
                    if (model.PhotoFile.Length > 50 * 1024)
                        ModelState.AddModelError("PhotoFile", "Photograph size cannot exceed 50 KB.");
                }

                // 📄 Address Proof: .pdf only, max 1024 KB
                if ((model.AddressProofFile == null || model.AddressProofFile.Length == 0) && string.IsNullOrEmpty(model.AddressProofPath))
                {
                    ModelState.AddModelError("AddressProofFile", "Address proof file is required.");
                }
                else if (model.AddressProofFile != null && model.AddressProofFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.AddressProofFile.FileName).ToLowerInvariant();
                    if (ext != ".pdf")
                        ModelState.AddModelError("AddressProofFile", "Address proof must be in .pdf format.");
                    if (model.AddressProofFile.Length > 1024 * 1024)
                        ModelState.AddModelError("AddressProofFile", "Address proof size cannot exceed 1024 KB.");
                }

                // 📄 Identity Proof: .pdf only, max 1024 KB
                if ((model.IdentityProofFile == null || model.IdentityProofFile.Length == 0) && string.IsNullOrEmpty(model.IdentityProofPath))
                {
                    ModelState.AddModelError("IdentityProofFile", "Identity proof file is required.");
                }
                else if (model.IdentityProofFile != null && model.IdentityProofFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.IdentityProofFile.FileName).ToLowerInvariant();
                    if (ext != ".pdf")
                        ModelState.AddModelError("IdentityProofFile", "Identity proof must be in .pdf format.");
                    if (model.IdentityProofFile.Length > 1024 * 1024)
                        ModelState.AddModelError("IdentityProofFile", "Identity proof size cannot exceed 1024 KB.");
                }

                // 📄 Test Report: .pdf only, max 1024 KB
                // Only checks format/size IF a file was chosen - no longer mandatory
                if (model.TestReportFile != null && model.TestReportFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.TestReportFile.FileName).ToLowerInvariant();
                    if (ext != ".pdf")
                        ModelState.AddModelError("TestReportFile", "Test report must be in .pdf format.");
                    if (model.TestReportFile.Length > 1024 * 1024)
                        ModelState.AddModelError("TestReportFile", "Test report size cannot exceed 1024 KB.");
                }

                // Optional File Validations
                void ValidateOptionalPdf(IFormFile? file, bool isSelected, string? existingPath, string fieldName, string label, long maxBytes = 1024 * 1024)
                {
                    if (isSelected)
                    {
                        if ((file == null || file.Length == 0) && string.IsNullOrEmpty(existingPath))
                        {
                            ModelState.AddModelError(fieldName, $"{label} is selected and requires a file.");
                        }
                        else if (file != null && file.Length > 0)
                        {
                            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                            if (ext != ".pdf")
                                ModelState.AddModelError(fieldName, $"{label} must be a .pdf file.");
                            if (file.Length > maxBytes)
                                ModelState.AddModelError(fieldName, $"{label} size exceeds allowed limit.");
                        }
                    }
                }

                ValidateOptionalPdf(model.SaleDeedFile, model.SelectedSaleDeed, model.SaleDeedPath, "SaleDeedFile", "Sale Deed", 10 * 1024 * 1024);
                ValidateOptionalPdf(model.PowerOfAttorneyFile, model.SelectedPowerOfAttorney, model.PowerOfAttorneyPath, "PowerOfAttorneyFile", "Power Of Attorney");
                ValidateOptionalPdf(model.MunicipalTaxFile, model.SelectedMunicipalTax, model.MunicipalTaxPath, "MunicipalTaxFile", "Municipal Tax");
                ValidateOptionalPdf(model.AllotmentLetterFile, model.SelectedAllotmentLetter, model.AllotmentLetterPath, "AllotmentLetterFile", "Letter Of Allotment");
                ValidateOptionalPdf(model.HouseRegistrationFile, model.SelectedHouseRegistration, model.HouseRegistrationPath, "HouseRegistrationFile", "House Registration");
                ValidateOptionalPdf(model.LeaseFile, model.SelectedLease, model.LeasePath, "LeaseFile", "Lease Document");
                ValidateOptionalPdf(model.OtherOwnershipFile, model.SelectedOtherOwnership, model.OtherOwnershipPath, "OtherOwnershipFile", "Other Ownership Document");
                ValidateOptionalPdf(model.PowerAgentPhotoFile, model.SelectedPowerAgentPhoto, model.PowerAgentPhotoPath, "PowerAgentPhotoFile", "PowerAgent Photo");
                ValidateOptionalPdf(model.OthersFile, model.SelectedOthers, model.OthersPath, "OthersFile", "Other Document");
            }
        }

        private string? UploadFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return null;

            string[] allowedExtensions = { ".jpg", ".jpeg", ".pdf" };
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (Array.IndexOf(allowedExtensions, extension) < 0)
                return null;

            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return "/uploads/" + uniqueFileName;
        }
    }
}