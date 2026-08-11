using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models
{
    public class TradeLicenceMachinery
    {
        [Key]
        public int MachineryId { get; set; }

        public int ApplicationId { get; set; }

        [Required]
        public string MachineryName { get; set; } = string.Empty;

        // DB column is actually named "NumberOfItems" — the C# property stays
        // "Quantity" (matches how it's used everywhere else in the code:
        // service, controller, view fields) and this attribute just tells EF
        // Core which physical column to read/write.
        [Column("NumberOfItems")]
        public int Quantity { get; set; }

        public decimal HorsePower { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }

    public class MachineryInput
    {
        public string MachineryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal HorsePower { get; set; }
    }

    public class SaveMachineryRequest
    {
        public int ApplicationId { get; set; }
        public List<MachineryInput> Machinery { get; set; } = new();
    }
}