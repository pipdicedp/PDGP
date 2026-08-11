using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    public class TradeLicencePhotograph
    {
        [Key]
        public int PhotographId { get; set; }

        public int ApplicationId { get; set; }

        // Kept for backward compatibility — no longer written to. Photos are now
        // stored encrypted, directly in the database (see *Data / *IV columns below).
        public string? ApplicantPhotoPath { get; set; }
        public string? PartnerPhotoPath { get; set; }

        public byte[]? ApplicantPhotoData { get; set; }
        public byte[]? ApplicantPhotoIV { get; set; }
        public string? ApplicantPhotoContentType { get; set; }

        public byte[]? PartnerPhotoData { get; set; }
        public byte[]? PartnerPhotoIV { get; set; }
        public string? PartnerPhotoContentType { get; set; }

        public DateTime? UploadedDate { get; set; }

        public virtual TradeLicenceApplication? Application { get; set; }
    }
}