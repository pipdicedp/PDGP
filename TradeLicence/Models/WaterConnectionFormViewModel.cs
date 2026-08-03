using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WaterConnection.Models
{
    // Backs the New Water Connection form. Kept separate from the EF entity so that
    // dropdown sources and file uploads don't leak into the persisted model.
    public class WaterConnectionFormViewModel
    {
        // Applicant Details
        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? PowerOfAttorney { get; set; }

        [StringLength(150)]
        public string? FatherName { get; set; }

        [StringLength(150)]
        public string? SpouseName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(150)]
        public string? Email { get; set; }

        // Communication Address
        [StringLength(50)]
        public string? CommDoorNo { get; set; }

        [StringLength(200)]
        public string? CommAddress1 { get; set; }

        [StringLength(200)]
        public string? CommAddress2 { get; set; }

        [StringLength(100)]
        public string? CommCity { get; set; }

        // Connection Address
        [StringLength(50)]
        public string? ConnDoorNo { get; set; }

        [StringLength(200)]
        public string? ConnAddress1 { get; set; }

        [StringLength(200)]
        public string? ConnAddress2 { get; set; }

        [StringLength(100)]
        public string? ConnCity { get; set; }

        // Master-backed dropdowns -- posted back as codes, not free text
        [Required(ErrorMessage = "Please select a department")]
        [Display(Name = "Department")]
        public int? DeptCode { get; set; }

        [Required(ErrorMessage = "Please select a section")]
        [Display(Name = "Section")]
        public int? SectionCode { get; set; }

        [Display(Name = "Contractor")]
        public int? ContractorCode { get; set; }

        [Required(ErrorMessage = "Please select an area")]
        [Display(Name = "Area")]
        public int? AreaCode { get; set; }

        [Required(ErrorMessage = "Please select a purpose")]
        [Display(Name = "Purpose")]
        public int? PurposeCode { get; set; }

        [Required(ErrorMessage = "Please select a name/address verification document")]
        [Display(Name = "Name and Address Verification")]
        public int? NaVerifyCode { get; set; }

        [Required(ErrorMessage = "Please select an ownership verification document")]
        [Display(Name = "Ownership Verification")]
        public int? OwnFileCode { get; set; }

        // Document types that have no master table in the new schema; kept as fixed lists
        public string? OthersDocument { get; set; }
        public string? ContractorConsentDocument { get; set; }

        // File uploads
        public IFormFile? NameAddressFile { get; set; }
        public IFormFile? OwnershipFile { get; set; }
        public IFormFile? OthersFile { get; set; }
        public IFormFile? ContractorConsentFile { get; set; }

        // Dropdown sources, populated by the controller before the view renders
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Sections { get; set; } = new();
        public List<SelectListItem> Contractors { get; set; } = new();
        public List<SelectListItem> Areas { get; set; } = new();
        public List<SelectListItem> Purposes { get; set; } = new();
        public List<SelectListItem> NameAddressVerifications { get; set; } = new();
        public List<SelectListItem> OwnershipVerifications { get; set; } = new();
    }
}
