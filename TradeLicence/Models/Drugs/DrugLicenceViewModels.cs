using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TradeLicence.Models.Drugs
{
    /// <summary>
    /// One row of the "Pharmacist / Competent Details" box. Everything is nullable on purpose:
    /// validation is done by DrugLicenceFormService (so Save-as-Draft can accept half-filled rows
    /// without MVC's implicit [Required] on non-nullable strings getting in the way).
    /// </summary>
    public class PharmacistInput
    {
        public string? Name { get; set; }
        public string? Sex { get; set; }
        public string? Qualification { get; set; }
        public string? Experience { get; set; }
        public string? CouncilNo { get; set; }
        public DateTime? ValidityDate { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? YearOfPassing { get; set; }
    }

    public class DrugDocumentInfo
    {
        public int DocumentId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
    }

    /// <summary>Posted by Views/DrugLicence/Apply.cshtml. Files are read from Request.Form.Files ("file_" + document key).</summary>
    public class DrugLicenceFormViewModel
    {
        public int ApplicationId { get; set; }

        // 1. Firm details
        public bool IsRenewal { get; set; }
        public string? ApplicantName { get; set; }
        public string? FirmName { get; set; }
        public string? ManagerInchargeName { get; set; }
        public string? ConstitutionType { get; set; }
        public string? AddressOfTheFirm { get; set; }
        public string? Landmark { get; set; }
        public string? Districts { get; set; }
        public string? PostalZipCode { get; set; }
        public string? EMail { get; set; }
        public string? MobileNumber { get; set; }
        public string? TypeOfCeilings { get; set; }

        // 2. Ownership details
        public string? NameOfProprietorPartnerDirector { get; set; }
        public string? DinNoSinNoRegistrationDetails { get; set; }
        public string? PanCardNo { get; set; }

        // 3. Pharmacist / Competent details (any number)
        public List<PharmacistInput> Pharmacists { get; set; } = new();

        // 4. Licence details
        public string? ScheduleOfDrugs { get; set; } = "NotApplicable";
        public bool AluminiumSections { get; set; }
        public bool Drawers { get; set; }
        public bool LockAndKey { get; set; }
        public bool Racks { get; set; }
        public bool GlassSections { get; set; }
        public bool Pellets { get; set; }
        public bool WoodenFurniture { get; set; }
        public bool AnyOther { get; set; }
        public bool ColdStorage { get; set; }
        public bool TwentyFourHours { get; set; }

        // ---- display-only (never bound from the request) ----
        [BindNever, ValidateNever] public DrugFormDefinition Form { get; set; } = DrugLicenceConfig.Form20A;
        [BindNever, ValidateNever] public string Status { get; set; } = "Draft";
        [BindNever, ValidateNever] public string? ApplicationNumber { get; set; }
        [BindNever, ValidateNever] public string? OfficerRemarks { get; set; }
        [BindNever, ValidateNever] public Dictionary<string, DrugDocumentInfo> ExistingDocuments { get; set; } = new();
        [BindNever, ValidateNever] public List<string> ConstitutionTypes { get; set; } = new();
        [BindNever, ValidateNever] public List<string> DistrictList { get; set; } = new();
        [BindNever, ValidateNever] public List<string> CeilingTypes { get; set; } = new();
    }

    public class DrugLicenceListViewModel
    {
        public DrugFormDefinition Form { get; set; } = DrugLicenceConfig.Form20A;
        public List<DrugLicenceApplicationBase> Applications { get; set; } = new();
        public Dictionary<int, WorkflowPayment> Payments { get; set; } = new();
    }

    public class DrugLicenceDetailsViewModel
    {
        public DrugFormDefinition Form { get; set; } = DrugLicenceConfig.Form20A;
        public DrugLicenceApplicationBase Application { get; set; } = null!;
        public List<DrugLicencePharmacist> Pharmacists { get; set; } = new();
        public List<DrugDocumentInfo> Documents { get; set; } = new();
        public WorkflowPayment? Payment { get; set; }
    }
}
