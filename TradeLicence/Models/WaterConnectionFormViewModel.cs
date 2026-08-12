using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WaterConnection.Models
{
    // Backs the New Water Connection form. Kept separate from the EF entity so that
    // dropdown sources and file uploads don't leak into the persisted model.
    //
    // Every field is mandatory, including all four document uploads. File uploads
    // are also restricted to PDF (checked here for the missing-file case, and again
    // server-side in the controller against the file's actual bytes, plus client-side
    // in wc-attachments.js at the moment a file is chosen).
    public class WaterConnectionFormViewModel
    {
        // Set when this form is displaying/updating an existing row (draft or
        // submitted) that belongs to the logged-in user -- null means "brand new".
        public int? ApplicationId { get; set; }

        // Applicant Details
        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Power of Attorney is required")]
        [StringLength(150)]
        public string? PowerOfAttorney { get; set; }

        [Required(ErrorMessage = "Father Name is required")]
        [StringLength(150)]
        public string? FatherName { get; set; }

        [Required(ErrorMessage = "Spouse Name is required")]
        [StringLength(150)]
        public string? SpouseName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(15)]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number must contain digits only")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email address")]
        [StringLength(150)]
        public string? Email { get; set; }

        // Communication Address
        [Required(ErrorMessage = "Door No. is required")]
        [StringLength(50)]
        public string? CommDoorNo { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required")]
        [StringLength(200)]
        public string? CommAddress1 { get; set; }

        [Required(ErrorMessage = "Address Line 2 is required")]
        [StringLength(200)]
        public string? CommAddress2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string? CommCity { get; set; }

        // Connection Address
        [Required(ErrorMessage = "Door No. is required")]
        [StringLength(50)]
        public string? ConnDoorNo { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required")]
        [StringLength(200)]
        public string? ConnAddress1 { get; set; }

        [Required(ErrorMessage = "Address Line 2 is required")]
        [StringLength(200)]
        public string? ConnAddress2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string? ConnCity { get; set; }

        // Master-backed dropdowns -- posted back as codes, not free text
        [Required(ErrorMessage = "Please select a department")]
        [Display(Name = "Department")]
        public int? DeptCode { get; set; }

        [Required(ErrorMessage = "Please select a section")]
        [Display(Name = "Section")]
        public int? SectionCode { get; set; }

        [Required(ErrorMessage = "Please select a contractor")]
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
        [Required(ErrorMessage = "Please select an option")]
        public string? OthersDocument { get; set; }

        [Required(ErrorMessage = "Please select an option")]
        public string? ContractorConsentDocument { get; set; }

        // File uploads. Not [Required] here -- a browser can never pre-fill a file
        // input, so a resumed draft that already has a document on file would fail
        // this attribute every time even though nothing is actually missing. Instead
        // the controller requires either a freshly posted file OR a matching
        // Existing*DocumentId below (see ValidateRequiredDocument/IsFormComplete).
        public IFormFile? NameAddressFile { get; set; }
        public IFormFile? OwnershipFile { get; set; }
        public IFormFile? OthersFile { get; set; }
        public IFormFile? ContractorConsentFile { get; set; }

        // Set by the controller when resuming a draft/application that already has a
        // document saved for this slot (see WaterConnectionController.PopulateExistingDocuments).
        // Posted back as hidden fields so the server still knows a document is on file
        // even when the visible <input type="file"> is empty. Cleared client-side
        // (wc-attachments.js) if the user removes the existing document or picks a new one.
        public int? ExistingNameAddressDocumentId { get; set; }
        public int? ExistingOwnershipDocumentId { get; set; }
        public int? ExistingOthersDocumentId { get; set; }
        public int? ExistingContractorConsentDocumentId { get; set; }

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
