using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.Area_Master
    public class AreaMaster
    {
        [Key]
        public int AreaCode { get; set; }

        [Required]
        [StringLength(150)]
        public string AreaName { get; set; } = string.Empty;

        public int? ContractorCode { get; set; }

        public ContractorMaster? Contractor { get; set; }
    }
}
