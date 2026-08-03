using System.ComponentModel.DataAnnotations;
using WaterConnection.Models.Masters;

namespace WaterConnection.Models
{
    // Maps to dbo.WaterConnectionApplication
    public class WaterConnectionApplication
    {
        [Key]
        public int ApplicationId { get; set; }

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

        // Master-backed lookups (foreign keys in the new schema)
        [Required]
        public int PurposeCode { get; set; }
        public PurposeMaster? Purpose { get; set; }

        [Required]
        public int DeptCode { get; set; }
        public DepartmentMaster? Department { get; set; }

        [Required]
        public int SectionCode { get; set; }
        public SectionMaster? Section { get; set; }

        // Nullable in the DB (Contractor_Code allows NULL on WaterConnectionApplication)
        public int? ContractorCode { get; set; }
        public ContractorMaster? Contractor { get; set; }

        [Required]
        public int AreaCode { get; set; }
        public AreaMaster? Area { get; set; }

        [Required]
        public int NaVerifyCode { get; set; }
        public NameAddressVerificationMaster? NameAddressVerification { get; set; }

        [Required]
        public int OwnFileCode { get; set; }
        public OwnershipVerificationMaster? OwnershipVerification { get; set; }

        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Submitted";

        public ICollection<ApplicationDocument>? Documents { get; set; }
    }
}
