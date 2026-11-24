using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace TradeLicence.Models
{
    /// <summary>
    /// Shared shape of a "Name Availability" request. The Society and Firm
    /// services each store their rows in their OWN table
    /// (dbo.SocietyNameAvailabilities / dbo.FirmNameAvailabilities), so the
    /// common columns live here and the two concrete classes below only add
    /// the table mapping. The DB "create" scripts are in
    /// Database/NameAvailability_Tables.sql.
    /// </summary>
    public abstract class NameAvailabilityBase
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Proposed Name 1 is required")]
        [StringLength(200)]
        [Display(Name = "Proposed Name")]
        public string ProposedName1 { get; set; } = string.Empty;

        [StringLength(200)] public string? ProposedName2 { get; set; }
        [StringLength(200)] public string? ProposedName3 { get; set; }
        [StringLength(200)] public string? ProposedName4 { get; set; }
        [StringLength(200)] public string? ProposedName5 { get; set; }

        [StringLength(1000)]
        public string? Purpose { get; set; }

        [Required(ErrorMessage = "Applicant Name is required")]
        [StringLength(150)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Applicant Address is required")]
        [StringLength(500)]
        public string ApplicantAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile No is required")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit mobile number")]
        public string MobileNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email Id is required")]
        [EmailAddress(ErrorMessage = "Enter a valid email")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select Yes or No")]
        public bool? InConsonanceWithObjects { get; set; }

        [Required(ErrorMessage = "Please select Yes or No")]
        public bool? NocAttached { get; set; }

        [Required(ErrorMessage = "Please select Yes or No")]
        public bool? EmblemsActCompliant { get; set; }

        // Saved file paths (stored in DB)
        public string? IdentityProofPath { get; set; }
        public string? SignatureProofPath { get; set; }
        public string? NocPath { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        // Upload controls (not stored in DB)
        [NotMapped]
        [Required(ErrorMessage = "Identity proof is required")]
        public IFormFile? IdentityProofFile { get; set; }

        [NotMapped]
        [Required(ErrorMessage = "Signature proof is required")]
        public IFormFile? SignatureProofFile { get; set; }

        [NotMapped]
        public IFormFile? NocFile { get; set; }
    }

    /// <summary>Name Availability of Societies ΓÇö one row per request, own table.</summary>
    [Table("SocietyNameAvailabilities")]
    public class SocietyNameAvailability : NameAvailabilityBase
    {
    }

    /// <summary>Name Availability of Firms ΓÇö one row per request, own table.</summary>
    [Table("FirmNameAvailabilities")]
    public class FirmNameAvailability : NameAvailabilityBase
    {
    }
}
