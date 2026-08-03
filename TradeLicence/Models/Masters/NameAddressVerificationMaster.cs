using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.NameAddressVerification_Master
    public class NameAddressVerificationMaster
    {
        [Key]
        public int NaVerifyCode { get; set; }

        [Required]
        [StringLength(200)]
        public string DocumentName { get; set; } = string.Empty;
    }
}
