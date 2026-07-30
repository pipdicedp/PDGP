using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class WaterConnectionModel
    {
        [Key]
        public int ApplicationId { get; set; }

        // Applicant Details
        public string? Name { get; set; }
        public string? PowerOfAttorney { get; set; }
        public string? FatherName { get; set; }
        public string? SpouseName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        // Communication Address
        public string? CommDoorNo { get; set; }
        public string? CommAddress1 { get; set; }
        public string? CommAddress2 { get; set; }
        public string? CommCity { get; set; }

        // Connection Address
        public string? ConnDoorNo { get; set; }
        public string? ConnAddress1 { get; set; }
        public string? ConnAddress2 { get; set; }
        public string? ConnCity { get; set; }

        // Dropdowns
        public string? Department { get; set; }
        public string? Section { get; set; }
        public string? Contractor { get; set; }
        public string? Area { get; set; }
        public string? Purpose { get; set; }

        // Additional Details
        public string? ApplicantName { get; set; }
        public string? ConnectionAddress { get; set; }
        public string? PermanentAddress { get; set; }
        public string? PwdContractorName { get; set; }
        public string? PwdContractorAddress { get; set; }

        // Document Dropdown Selections
        public string? NameAddressDocument { get; set; }
        public string? OwnershipDocument1 { get; set; }
        public string? OwnershipDocument2 { get; set; }
        public string? OthersDocument { get; set; }
        public string? ContractorConsentDocument { get; set; }

        // Stored File Names
        public string? NameAddressFileName { get; set; }
        public string? OwnershipFile1Name { get; set; }
        public string? OwnershipFile2Name { get; set; }
        public string? OthersFileName { get; set; }
        public string? ContractorConsentFileName { get; set; }
        public string? IdentityCardFileName { get; set; }
        public string? RoadCuttingPermissionFileName { get; set; }

        // File Upload Controls
        public IFormFile? NameAddressFile { get; set; }
        public IFormFile? OwnershipFile1 { get; set; }
        public IFormFile? OwnershipFile2 { get; set; }
        public IFormFile? OthersFile { get; set; }
        public IFormFile? ContractorConsentFile { get; set; }
        public IFormFile? IdentityCardFile { get; set; }
        public IFormFile? RoadCuttingPermissionFile { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
