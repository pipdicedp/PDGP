using System.ComponentModel.DataAnnotations;

namespace WaterConnection.Models.Masters
{
    // Maps to dbo.Department_Master
    public class DepartmentMaster
    {
        [Key]
        public int DeptCode { get; set; }

        [Required]
        [StringLength(200)]
        public string DepartmentName { get; set; } = string.Empty;

        public ICollection<SectionMaster>? Sections { get; set; }
    }
}
