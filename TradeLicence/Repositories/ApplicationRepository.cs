using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TradeLicence.Data;
using TradeLicence.Models;

namespace TradeLicence.Repositories
{
    public class ApplicationRepository
    {
        private readonly ElectricityApplicationDbContext _context;

        public ApplicationRepository(ElectricityApplicationDbContext context)
        {
            _context = context;
        }

        // 🔍 Fetch single record and convert to ApplicationViewModel
        public ApplicationViewModel? GetByApplicationNumber(string? appNum)
        {
            if (string.IsNullOrWhiteSpace(appNum)) return null;

            var entity = _context.EBapplications
                                 .AsNoTracking()
                                 .FirstOrDefault(a => a.ApplicationNumber == appNum);

            if (entity == null) return null;

            return MapEntityToViewModel(entity);
        }

        // 📅 Get all records with safe null handling
        public List<ApplicationViewModel> GetAllApplications(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.EBapplications.AsNoTracking().AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.CreatedDate != null && a.CreatedDate >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.CreatedDate != null && a.CreatedDate <= endOfDay);
            }

            var entities = query.OrderByDescending(a => a.CreatedDate ?? DateTime.MinValue).ToList();

            return entities.Select(MapEntityToViewModel).ToList();
        }

        // ✍️ Update Status & Remarks for Admin Actions
        public bool UpdateStatus(string appNumber, string status, string? remarks)
        {
            if (string.IsNullOrWhiteSpace(appNumber)) return false;

            var application = _context.EBapplications.FirstOrDefault(a => a.ApplicationNumber == appNumber);
            if (application == null) return false;

            application.ApplicationStatus = status;
            application.AdminRemarks = remarks;
            _context.SaveChanges();
            return true;
        }

        // 💾 Step-by-Step Save
        public void SaveOrUpdateStep(ApplicationViewModel model, int currentStep)
        {
            if (string.IsNullOrWhiteSpace(model.ApplicationNumber)) return;

            var entity = _context.EBapplications
                                 .FirstOrDefault(a => a.ApplicationNumber == model.ApplicationNumber);

            if (entity == null)
            {
                entity = new EBapplication
                {
                    ApplicationNumber = model.ApplicationNumber,
                    CreatedDate = DateTime.Now,
                    ApplicationStatus = "Pending"
                };
                _context.EBapplications.Add(entity);
            }

            // Step 1
            entity.ServiceCategory = model.ServiceCategory;
            entity.OwnershipType = model.OwnershipType;
            entity.ConnectionType = model.ConnectionType;
            entity.ServiceType = model.ServiceType;
            entity.ApplicantName = model.ApplicantName;
            entity.Gender = model.Gender;
            entity.RelativeName = model.RelativeName;
            entity.ProposedPincode = model.ProposedPincode;
            entity.MobileNumber = model.MobileNumber;

            // Step 2
            entity.SiteDoorNo = model.SiteDoorNo;
            entity.SiteStreet = model.SiteStreet;
            entity.SiteArea = model.SiteArea;
            entity.SiteDistrict = model.SiteDistrict;
            entity.SitePincode = model.SitePincode;
            entity.Email = model.Email;
            entity.Landmark1 = model.Landmark1;
            entity.Landmark2 = model.Landmark2;
            entity.IsSameAddress = model.IsSameAddress;
            entity.CommDoorNo = model.CommDoorNo;
            entity.CommStreet = model.CommStreet;
            entity.CommArea = model.CommArea;
            entity.CommRegion = model.CommRegion;
            entity.CommPincode = model.CommPincode;

            // Step 3
            entity.RSNo = model.RSNo;
            entity.PlotArea = model.PlotArea;
            entity.BuildArea = model.BuildArea;
            entity.SupplyCategory = model.SupplyCategory;
            entity.Purpose = model.Purpose;
            entity.TotalLoad = model.TotalLoad;
            entity.TypeOfSupply = model.TypeOfSupply;
            entity.OwnMeter = model.OwnMeter;
            entity.HasExistingSupply = model.HasExistingSupply;
            entity.ExistingConsumerNo = model.ExistingConsumerNo;
            entity.ExistingDueAmount = model.ExistingDueAmount;
            entity.HasDuesInArea = model.HasDuesInArea;
            entity.HasDuesForPremises = model.HasDuesForPremises;
            entity.HasDuesAssociatedFirm = model.HasDuesAssociatedFirm;

            // Step 4 (Document Paths)
            if (!string.IsNullOrEmpty(model.PhotoPath)) entity.PhotoPath = model.PhotoPath;
            if (!string.IsNullOrEmpty(model.AddressProofType)) entity.AddressProofType = model.AddressProofType;
            if (!string.IsNullOrEmpty(model.AddressProofPath)) entity.AddressProofPath = model.AddressProofPath;
            if (!string.IsNullOrEmpty(model.IdentityProofType)) entity.IdentityProofType = model.IdentityProofType;
            if (!string.IsNullOrEmpty(model.IdentityProofPath)) entity.IdentityProofPath = model.IdentityProofPath;
            if (!string.IsNullOrEmpty(model.TestReportPath)) entity.TestReportPath = model.TestReportPath;

            if (!string.IsNullOrEmpty(model.SaleDeedPath)) entity.SaleDeedPath = model.SaleDeedPath;
            if (!string.IsNullOrEmpty(model.PowerOfAttorneyPath)) entity.PowerOfAttorneyPath = model.PowerOfAttorneyPath;
            if (!string.IsNullOrEmpty(model.MunicipalTaxPath)) entity.MunicipalTaxPath = model.MunicipalTaxPath;
            if (!string.IsNullOrEmpty(model.AllotmentLetterPath)) entity.AllotmentLetterPath = model.AllotmentLetterPath;
            if (!string.IsNullOrEmpty(model.HouseRegistrationPath)) entity.HouseRegistrationPath = model.HouseRegistrationPath;
            if (!string.IsNullOrEmpty(model.LeasePath)) entity.LeasePath = model.LeasePath;
            if (!string.IsNullOrEmpty(model.OtherOwnershipPath)) entity.OtherOwnershipPath = model.OtherOwnershipPath;
            if (!string.IsNullOrEmpty(model.PowerAgentPhotoPath)) entity.PowerAgentPhotoPath = model.PowerAgentPhotoPath;
            if (!string.IsNullOrEmpty(model.OthersPath)) entity.OthersPath = model.OthersPath;

            _context.SaveChanges();
        }

        // Explicit, safe mapping between Entity and ViewModel
        private static ApplicationViewModel MapEntityToViewModel(EBapplication entity)
        {
            return new ApplicationViewModel
            {
                ApplicationNumber = entity.ApplicationNumber,
                ServiceCategory = entity.ServiceCategory,
                OwnershipType = entity.OwnershipType,
                ConnectionType = entity.ConnectionType,
                ServiceType = entity.ServiceType,
                ApplicantName = entity.ApplicantName,
                Gender = entity.Gender,
                RelativeName = entity.RelativeName,
                ProposedPincode = entity.ProposedPincode,
                MobileNumber = entity.MobileNumber,

                SiteDoorNo = entity.SiteDoorNo,
                SiteStreet = entity.SiteStreet,
                SiteArea = entity.SiteArea,
                SiteDistrict = entity.SiteDistrict,
                SitePincode = entity.SitePincode,
                Email = entity.Email,
                Landmark1 = entity.Landmark1,
                Landmark2 = entity.Landmark2,

                // 🛡️ Fix 1: Explicitly fall back to false for nullable bools
                IsSameAddress = entity.IsSameAddress ?? false,
                CommDoorNo = entity.CommDoorNo,
                CommStreet = entity.CommStreet,
                CommArea = entity.CommArea,
                CommRegion = entity.CommRegion,
                CommPincode = entity.CommPincode,

                RSNo = entity.RSNo,
                PlotArea = entity.PlotArea,
                BuildArea = entity.BuildArea,
                SupplyCategory = entity.SupplyCategory,
                Purpose = entity.Purpose,
                TotalLoad = entity.TotalLoad,
                TypeOfSupply = entity.TypeOfSupply,
                OwnMeter = entity.OwnMeter,

                // 🛡️ Fix 1: Explicitly fall back to false for nullable bools
                HasExistingSupply = entity.HasExistingSupply ?? false,
                ExistingConsumerNo = entity.ExistingConsumerNo,
                ExistingDueAmount = entity.ExistingDueAmount,
                HasDuesInArea = entity.HasDuesInArea ?? false,
                HasDuesForPremises = entity.HasDuesForPremises ?? false,
                HasDuesAssociatedFirm = entity.HasDuesAssociatedFirm ?? false,

                PhotoPath = entity.PhotoPath,
                AddressProofType = entity.AddressProofType,
                AddressProofPath = entity.AddressProofPath,
                IdentityProofType = entity.IdentityProofType,
                IdentityProofPath = entity.IdentityProofPath,
                TestReportPath = entity.TestReportPath,

                SaleDeedPath = entity.SaleDeedPath,
                PowerOfAttorneyPath = entity.PowerOfAttorneyPath,
                MunicipalTaxPath = entity.MunicipalTaxPath,
                AllotmentLetterPath = entity.AllotmentLetterPath,
                HouseRegistrationPath = entity.HouseRegistrationPath,
                LeasePath = entity.LeasePath,
                OtherOwnershipPath = entity.OtherOwnershipPath,
                PowerAgentPhotoPath = entity.PowerAgentPhotoPath,
                OthersPath = entity.OthersPath,

                ApplicationStatus = entity.ApplicationStatus ?? "Pending",
                AdminRemarks = entity.AdminRemarks,
                CreatedDate = entity.CreatedDate ?? DateTime.Now
            };
        }
    }
}