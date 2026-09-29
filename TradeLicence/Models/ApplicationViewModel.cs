using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace TradeLicence.Models
{
    public class ApplicationViewModel
    {
        public int CurrentStep { get; set; } = 1;

        // 📝 Step 1: Applicant Details
        public string? ApplicationNumber { get; set; }
        public string? ServiceCategory { get; set; }
        public string? OwnershipType { get; set; }
        public string? ConnectionType { get; set; }
        public string? ServiceType { get; set; }
        public string? ApplicantName { get; set; }
        public string? Gender { get; set; } = "Male";
        public string? RelativeName { get; set; }

        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
        public string? ProposedPincode { get; set; }

        [Required(ErrorMessage = "Mobile Number is required.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Mobile Number must be exactly 10 digits.")]
        public string? MobileNumber { get; set; }

        // 📍 Step 2: Address Details
        public string? SiteDoorNo { get; set; }
        public string? SiteStreet { get; set; }
        public string? SiteArea { get; set; }
        public string? SiteDistrict { get; set; }

        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(@"^[0-9]{6}$", ErrorMessage = "Pincode must be exactly 6 digits.")]
        public string? SitePincode { get; set; }

        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string? Email { get; set; }
        public string? Landmark1 { get; set; }
        public string? Landmark2 { get; set; }

        public bool IsSameAddress { get; set; }
        public string? CommDoorNo { get; set; }
        public string? CommStreet { get; set; }
        public string? CommArea { get; set; }
        public string? CommRegion { get; set; }
        public string? CommPincode { get; set; }

        // ⚡ Step 3: Technical Specifications
        public string? RSNo { get; set; }

        [Required(ErrorMessage = "Plot Area is required.")]
        [Range(0.01, 9999999.99, ErrorMessage = "Enter a valid plot area in sq. ft.")]
        public decimal? PlotArea { get; set; }

        [Required(ErrorMessage = "Build Area is required.")]
        [Range(0.01, 9999999.99, ErrorMessage = "Enter a valid build area in sq. ft.")]
        public decimal? BuildArea { get; set; }

        [StringLength(255)]
        public string? SupplyCategory { get; set; }
        public string? Purpose { get; set; }

        [Required(ErrorMessage = "Total Load is required.")]
        [Range(0.01, 10000000.00, ErrorMessage = "Please enter a valid load in Watts.")]
        public decimal? TotalLoad { get; set; }
        public string? TypeOfSupply { get; set; }
        public string? OwnMeter { get; set; }
        // Optional duration dates for Temporary supply
        [Display(Name = "Supply From Date")]
        [DataType(DataType.Date)]
        public DateTime? SupplyFromDate { get; set; }

        [Display(Name = "Supply To Date")]
        [DataType(DataType.Date)]
        public DateTime? SupplyToDate { get; set; }
        public bool HasExistingSupply { get; set; } = false;
        public string? ExistingConsumerNo { get; set; }
        public decimal? ExistingDueAmount { get; set; }

        public bool HasDuesInArea { get; set; } = false;
        public bool HasDuesForPremises { get; set; } = false;
        public bool HasDuesAssociatedFirm { get; set; } = false;

        // 📂 Step 4: Mandatory Document Files & Types
        public IFormFile? PhotoFile { get; set; }
        public string? PhotoPath { get; set; }

        [StringLength(255)]
        public string? AddressProofType { get; set; }
        public IFormFile? AddressProofFile { get; set; }
        public string? AddressProofPath { get; set; }

        [StringLength(255)]
        public string? IdentityProofType { get; set; }
        public IFormFile? IdentityProofFile { get; set; }
        public string? IdentityProofPath { get; set; }
        // ⚡ Existing Power Supply Consumer Number Segments
        public string? ExistingConsumerPart1 { get; set; }
        public string? ExistingConsumerPart2 { get; set; }
        public string? ExistingConsumerPart3 { get; set; }
        public string? ExistingConsumerPart4 { get; set; }

        // Full combined Consumer Number for DB storage
        public string? ExistingConsumerNumber { get; set; }

        public bool SelectedTestReport { get; set; }
        public IFormFile? TestReportFile { get; set; }
        public string? TestReportPath { get; set; }

        // 📂 Step 4: Optional Ownership Documents
        public bool SelectedSaleDeed { get; set; }
        public IFormFile? SaleDeedFile { get; set; }
        public string? SaleDeedPath { get; set; }

        public bool SelectedPowerOfAttorney { get; set; }
        public IFormFile? PowerOfAttorneyFile { get; set; }
        public string? PowerOfAttorneyPath { get; set; }

        public bool SelectedMunicipalTax { get; set; }
        public IFormFile? MunicipalTaxFile { get; set; }
        public string? MunicipalTaxPath { get; set; }

        public bool SelectedAllotmentLetter { get; set; }
        public IFormFile? AllotmentLetterFile { get; set; }
        public string? AllotmentLetterPath { get; set; }

        public bool SelectedHouseRegistration { get; set; }
        public IFormFile? HouseRegistrationFile { get; set; }
        public string? HouseRegistrationPath { get; set; }

        public bool SelectedLease { get; set; }
        public IFormFile? LeaseFile { get; set; }
        public string? LeasePath { get; set; }

        public bool SelectedOtherOwnership { get; set; }
        public IFormFile? OtherOwnershipFile { get; set; }
        public string? OtherOwnershipPath { get; set; }

        public bool SelectedPowerAgentPhoto { get; set; }
        public IFormFile? PowerAgentPhotoFile { get; set; }
        public string? PowerAgentPhotoPath { get; set; }

        public bool SelectedOthers { get; set; }
        public IFormFile? OthersFile { get; set; }
        public string? OthersPath { get; set; }

        // 🛡️ Application lifecycle status
        public string ApplicationStatus { get; set; } = "Pending";
        public string? AdminRemarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}