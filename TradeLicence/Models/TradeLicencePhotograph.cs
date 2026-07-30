using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class TradeLicencePhotograph
    {
        [Key]
        public int PhotographId { get; set; }

        public int ApplicationId { get; set; }

        public string? ApplicantPhotoPath { get; set; }

        public string? PartnerPhotoPath { get; set; }

        public DateTime? UploadedDate { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}