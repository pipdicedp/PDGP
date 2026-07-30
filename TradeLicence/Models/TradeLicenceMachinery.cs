using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class TradeLicenceMachinery
    {
        [Key]
        public int MachineryId { get; set; }

        public int ApplicationId { get; set; }

        [Required]
        public string MachineryName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal HorsePower { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}
