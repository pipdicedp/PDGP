using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.OwnershipVerification_Master
    public class OwnershipVerificationMaster
    {
        [Key]
        public int OwnFileCode { get; set; }

        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; } = string.Empty;
    }
}
