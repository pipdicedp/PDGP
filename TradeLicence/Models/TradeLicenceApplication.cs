using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class TradeLicenceApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        public int? UserId { get; set; }
        public string? ApplicationNumber { get; set; }

        public bool IsApplicationForTradeLicence { get; set; }
        public bool IsRegistrationForShopsEstablishment { get; set; }

        [Required(ErrorMessage = "Applicant name is required")]
        public string ApplicantName { get; set; } = null!;

        [Required(ErrorMessage = "Residential address is required")]
        public string ApplicantResidentialAddress { get; set; } = null!;

        [Required(ErrorMessage = "Father/Husband name is required")]
        public string ApplicantFatherHusbandName { get; set; } = null!;

        [Required(ErrorMessage = "Age is required")]
        public int AgeOfApplicant { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Mobile number must be 10 digits")]
        public string MobileNumber { get; set; } = null!;

        [Required(ErrorMessage = "Ownership type is required")]
        public string OwnershipType { get; set; } = null!;

        public int? MunicipalityId { get; set; }
        public int? WardId { get; set; }
        public int? AreaId { get; set; }
        public int? StreetId { get; set; }

        [Required(ErrorMessage = "Door number is required")]
        public string DoorNumber { get; set; } = null!;

        [Required(ErrorMessage = "Floor number is required")]
        public string FloorNumber { get; set; } = null!;

        public string? ShopNumber { get; set; }

        [Required(ErrorMessage = "Property tax assessment number is required")]
        public string PropertyTaxAssessmentNumber { get; set; } = null!;

        [Required(ErrorMessage = "Communication address is required")]
        public string TradePlaceCommunicationAddress { get; set; } = null!;

        [Required(ErrorMessage = "Total area is required")]
        public decimal TotalAreaCoveredSqFt { get; set; }

        [Required(ErrorMessage = "Number of floors is required")]
        public int NumberOfFloors { get; set; }

        [Required(ErrorMessage = "Name and style of factory is required")]
        public string NameAndStyleOfFactory { get; set; } = null!;

        [Required(ErrorMessage = "Other particulars is required")]
        public string OtherParticulars { get; set; } = null!;

        [Required(ErrorMessage = "Purpose of licence is required")]
        public string PurposeOfLicence { get; set; } = null!;

        public string? AdditionalPurpose { get; set; }

        [Required(ErrorMessage = "Purpose of licence required text is required")]
        public string PurposeOfLicenceRequiredText { get; set; } = null!;

        [Required(ErrorMessage = "Number of manager/supervisor is required")]
        public int NumberOfManagerSupervisor { get; set; }

        [Required(ErrorMessage = "Number of staff/worker is required")]
        public int NumberOfStaffWorker { get; set; }

        public DateTime DateOfCommencement { get; set; }

        public string LicencePeriod { get; set; } = null!;

        public string? CurrentDemandYear { get; set; }

        [Required(ErrorMessage = "Building owner name is required")]
        public string BuildingOwnerName { get; set; } = null!;

        [Required(ErrorMessage = "Building owner address is required")]
        public string BuildingOwnerAddress { get; set; } = null!;

        public decimal? RentEstimatedRentPerMonth { get; set; }

        public string Status { get; set; } = "Draft";


        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        public string? CreatedByUserId { get; set; }

        // Navigation properties
        public virtual ICollection<ApplicationDocument>? ApplicationDocuments { get; set; }
        public virtual ICollection<TradeLicencePartner>? Partners { get; set; }
        public virtual ICollection<TradeLicenceMachinery>? Machineries { get; set; }
        public virtual ICollection<TradeLicencePhotograph>? Photographs { get; set; }
        // Change the Model name
        public virtual ICollection<TradeLicencePhotograph>? Documents { get; set; }
        public ShopEstablishmentRegistration? ShopRegistration { get; set; }
    }

    public class ApplicationDocument
    {
        public int ApplicationDocumentId { get; set; }
        public int ApplicationId { get; set; }
        public int DocumentItemId { get; set; }
        public string? FilePath { get; set; }
        public DateTime? UploadedDate { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}
