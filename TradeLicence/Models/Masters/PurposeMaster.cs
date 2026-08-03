using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.Purpose_Master
    public class PurposeMaster
    {
        [Key]
        public int PurposeCode { get; set; }

        [Required]
        [StringLength(100)]
        public string PurposeName { get; set; } = string.Empty;
    }
}
