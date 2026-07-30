using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TradeLicence.Models
{
    public class TradeLicencePartner
    {
        [Key]
        public int PartnerId { get; set; }

        public int ApplicationId { get; set; }

        [Required]
        [StringLength(200)]
        public string PartnerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Designation { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}