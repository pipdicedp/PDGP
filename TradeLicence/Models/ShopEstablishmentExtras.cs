using System;
using System.ComponentModel.DataAnnotations;

namespace TradeLicence.Models
{
    /// <summary>
    /// One row of the "Full name and residential address of the employer
    /// other than the Manager" repeatable table on the Shop Establishment
    /// tab. Multiple rows per application — [+] adds a new employer.
    /// </summary>
    public class ShopEmployer
    {
        [Key]
        public int EmployerRowId { get; set; }
        public int ApplicationId { get; set; }

        public string? EmployerType { get; set; }
        public string? EmployerName { get; set; }
        public bool IsMinor { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
        public string? ResidentialAddress { get; set; }
        public string? District { get; set; }
        public string? PinCode { get; set; }
        public bool RcToBeIssued { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Form IX, Part-A [see sub-rule 2(a) of rule 22] — one row per worker.
    /// </summary>
    public class ShopFormIXPartA
    {
        [Key]
        public int PartARowId { get; set; }
        public int ApplicationId { get; set; }

        public string? EmployeeName { get; set; }
        public string? Sex { get; set; }
        public string? FatherHusbandName { get; set; }
        public string? Designation { get; set; }
        public string? EmployeeNumber { get; set; }
        public DateTime? DateOfEntryIntoService { get; set; }
        public string? PersonCategory { get; set; }
        public string? Shift { get; set; }
        public string? TimeOfCommencementOfWork { get; set; }
        public string? RestIntervalHours { get; set; }
        public string? TimeWorkEnds { get; set; }
        public string? WeeklyHoliday { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Form IX, Part-B — wage rate by class of worker.
    /// </summary>
    public class ShopFormIXPartB
    {
        [Key]
        public int PartBRowId { get; set; }
        public int ApplicationId { get; set; }

        public string? ClassOfWorkers { get; set; }
        public decimal? MaxRateOfWage { get; set; }
        public decimal? MinRateOfWage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// The 4 fixed "Upload Annexure Files" slots under Shop Establishment —
    /// same encrypted-bytes-in-DB pattern as TradeLicenceDocument (Step 5),
    /// just a separate table so the two document checklists don't mix.
    /// </summary>
    public class ShopAnnexureDocument
    {
        [Key]
        public int AnnexureDocumentId { get; set; }
        public int ApplicationId { get; set; }

        [Required]
        public string DocumentName { get; set; } = string.Empty; // one of the 4 fixed names below
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public byte[]? FileData { get; set; }       // encrypted, same as TradeLicenceDocument.DocumentData
        public byte[]? FileDataIV { get; set; }      // same as TradeLicenceDocument.DocumentIV

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }

    public static class ShopAnnexureDocumentNames
    {
        public const string FeesSubmitted = "Details of Fees Submitted";
        public const string LegalOccupancy = "Proof for Legal Occupancy";
        public const string EmployerIdProof = "ID Proof of the Employer";
        public const string PremisesPhoto = "Photo of the Premises With Name Board";

        public static readonly string[] All = { FeesSubmitted, LegalOccupancy, EmployerIdProof, PremisesPhoto };
    }
}
