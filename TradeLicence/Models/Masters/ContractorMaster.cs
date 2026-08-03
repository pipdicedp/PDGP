using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.Contractor_Master
    public class ContractorMaster
    {
        [Key]
        public int ContractorCode { get; set; }

        [Required]
        [StringLength(150)]
        public string ContractorName { get; set; } = string.Empty;

        [StringLength(300)]
        public string? ContractorAddress { get; set; }

        public int? SectionCode { get; set; }

        public SectionMaster? Section { get; set; }

        public ICollection<AreaMaster>? Areas { get; set; }
    }
}
