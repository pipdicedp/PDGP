using System.ComponentModel.DataAnnotations;
using WaterConnection.Models.Masters;

namespace WaterConnection.Models
{
    // Maps to dbo.WaterConnectionApplication
    public class WaterConnectionApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        // Owning user (Users.UserId). Nullable because older/legacy rows may not
        // have one, but every new application (draft or submitted) always sets it
        // from the logged-in user's claim -- see WaterConnectionController.
        public int? UserId { get; set; }

        // Applicant Details
        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? PowerOfAttorney { get; set; }

        [StringLength(150)]
        public string? FatherName { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(150)]
        public string? SpouseName { get; set; }

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

        // Master-backed lookups (foreign keys in the new schema).
        // All nullable at the entity/DB level -- required only for a real
        // "Submitted" application; a Draft row can legitimately have any of
        // these unset because the user hasn't reached that part of the form yet.
        public int? PurposeCode { get; set; }
        public PurposeMaster? Purpose { get; set; }

        public int? DeptCode { get; set; }
        public DepartmentMaster? Department { get; set; }

        public int? SectionCode { get; set; }
        public SectionMaster? Section { get; set; }

        public int? ContractorCode { get; set; }
        public ContractorMaster? Contractor { get; set; }

        public int? AreaCode { get; set; }
        public AreaMaster? Area { get; set; }

        public int? NaVerifyCode { get; set; }
        public NameAddressVerificationMaster? NameAddressVerification { get; set; }

        public int? OwnFileCode { get; set; }
        public OwnershipVerificationMaster? OwnershipVerification { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        // "Draft" while the user is still filling the form and clicks "Save Application",
        // "Submitted" once they click "Submit Application" with everything complete.
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        public ICollection<ApplicationDocument>? Documents { get; set; }
    }
}
