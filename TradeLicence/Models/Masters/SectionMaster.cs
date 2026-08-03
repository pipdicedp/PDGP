using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.Section_Master
    public class SectionMaster
    {
        [Key]
        public int SectionCode { get; set; }

        [Required]
        [StringLength(200)]
        public string SectionName { get; set; } = string.Empty;

        public int? DeptCode { get; set; }

        public DepartmentMaster? Department { get; set; }

        public ICollection<ContractorMaster>? Contractors { get; set; }
    }
}
