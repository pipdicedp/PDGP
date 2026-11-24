using System.ComponentModel.DataAnnotations.Schema;

namespace TradeLicence.Models.Drugs
{
    /// <summary>
    /// Columns shared by dbo.Form20A and dbo.Form21A (the two tables are identical).
    /// This base class is deliberately NOT registered as an entity - EF maps its properties into each
    /// derived entity (see DrugsApplicationDbContext). It implements IWorkflowApplication, so the shared
    /// WorkflowEngineService works on both forms without any service-specific code.
    /// </summary>
    public abstract class DrugLicenceApplicationBase : IWorkflowApplication
    {
        /// <summary>"20A" or "21A".</summary>
        [NotMapped] public abstract string FormType { get; }

        // Form20A.Application_Id / Form21A.Application_id (identity)
        public int ApplicationId { get; set; }
        [NotMapped] public int Id => ApplicationId;

        public int? UserId { get; set; }

        public string? ApplicationNumber { get; set; }

        // ---- 1. Firm details ----
        /// <summary>Column RegistrationOrRenewal: false = Registration (fresh), true = Renewal.</summary>
        public bool IsRenewal { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string FirmName { get; set; } = string.Empty;
        public string ManagerInchargeName { get; set; } = string.Empty;
        public string ConstitutionType { get; set; } = string.Empty;
        public string AddressOfTheFirm { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public string Districts { get; set; } = string.Empty;
        public string PostalZipCode { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string TypeOfCeilings { get; set; } = string.Empty;

        // ---- 2. Ownership details ----
        public string? NameOfProprietorPartnerDirector { get; set; }
        public string? DinNoSinNoRegistrationDetails { get; set; }
        public string PanCardNo { get; set; } = string.Empty;

        // ---- 4. Licence details ----
        // Schedule of drugs - radio on the form, one bit per option in the table (exactly one is true).
        public bool DrugsSpecifiedInScheduleCC1ExcludingX { get; set; }
        public bool DrugsOtherThanSpecifiedInScheduleCC1AndX { get; set; }
        public bool DrugsSpecifiedInScheduleX { get; set; }
        public bool DrugsSpecifiedInScheduleC1ExcludingX { get; set; }
        public bool ScheduleNotApplicable { get; set; } = true;

        // Storage details (checkboxes)
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

        // ---- Uploaded documents ----
        // The existing NOT NULL varchar(500) columns now hold the uploaded FILE NAME ("" = nothing uploaded).
        // The PDF bytes themselves live in dbo.DrugLicenceDocuments (varbinary).
        public string DetailsOfProprietorOrPartnersOrManagingDirector { get; set; } = string.Empty;
        public string AffidavitUnderSection34OrSpecialDeclarationOfRegisteredPharmacist { get; set; } = string.Empty;
        public string DetailsOfPharmacistCompetent { get; set; } = string.Empty;
        public string PlanOfThePremisesByTheCertifiedEngineer { get; set; } = string.Empty;
        public string PremisesOwnershipDocumentRentalLeaseReceipt { get; set; } = string.Empty;
        public string ColdStoragePurchaseWorkingConditionProof { get; set; } = string.Empty;
        public string Others { get; set; } = string.Empty;

        // ---- Officer workflow (IWorkflowApplication) ----
        // "Draft" / "Submitted" / "ReturnedToApplicant" / "Approved" / "Rejected"
        public string Status { get; set; } = "Draft";
        public string CurrentStage { get; set; } = "Initial Scrutiny";
        public int? AssignedOfficerId { get; set; }
        public string PaymentStatus { get; set; } = "NotRequested";
        public string? OfficerRemarks { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // ---- helpers (not mapped) ----
        public string GetDocumentFileName(string documentKey) => documentKey switch
        {
            "ProprietorDetails" => DetailsOfProprietorOrPartnersOrManagingDirector,
            "Affidavit" => AffidavitUnderSection34OrSpecialDeclarationOfRegisteredPharmacist,
            "PharmacistDetails" => DetailsOfPharmacistCompetent,
            "PremisesPlan" => PlanOfThePremisesByTheCertifiedEngineer,
            "PremisesOwnership" => PremisesOwnershipDocumentRentalLeaseReceipt,
            "ColdStorageProof" => ColdStoragePurchaseWorkingConditionProof,
            "Others" => Others,
            _ => string.Empty
        };

        public void SetDocumentFileName(string documentKey, string fileName)
        {
            switch (documentKey)
            {
                case "ProprietorDetails": DetailsOfProprietorOrPartnersOrManagingDirector = fileName; break;
                case "Affidavit": AffidavitUnderSection34OrSpecialDeclarationOfRegisteredPharmacist = fileName; break;
                case "PharmacistDetails": DetailsOfPharmacistCompetent = fileName; break;
                case "PremisesPlan": PlanOfThePremisesByTheCertifiedEngineer = fileName; break;
                case "PremisesOwnership": PremisesOwnershipDocumentRentalLeaseReceipt = fileName; break;
                case "ColdStorageProof": ColdStoragePurchaseWorkingConditionProof = fileName; break;
                case "Others": Others = fileName; break;
            }
        }

        /// <summary>The single schedule-of-drugs choice as the radio value used on the form.</summary>
        [NotMapped]
        public string ScheduleOfDrugs
        {
            get
            {
                if (DrugsSpecifiedInScheduleCC1ExcludingX) return "CC1ExcludingX";
                if (DrugsOtherThanSpecifiedInScheduleCC1AndX) return "OtherThanCC1AndX";
                if (DrugsSpecifiedInScheduleX) return "X";
                if (DrugsSpecifiedInScheduleC1ExcludingX) return "C1ExcludingX";
                return "NotApplicable";
            }
            set
            {
                DrugsSpecifiedInScheduleCC1ExcludingX = value == "CC1ExcludingX";
                DrugsOtherThanSpecifiedInScheduleCC1AndX = value == "OtherThanCC1AndX";
                DrugsSpecifiedInScheduleX = value == "X";
                DrugsSpecifiedInScheduleC1ExcludingX = value == "C1ExcludingX";
                ScheduleNotApplicable = value != "CC1ExcludingX" && value != "OtherThanCC1AndX"
                                        && value != "X" && value != "C1ExcludingX";
            }
        }

        public string ScheduleOfDrugsLabel =>
            DrugLicenceConfig.ScheduleOptions.FirstOrDefault(o => o.Value == ScheduleOfDrugs)?.Label ?? "Not Applicable";

        public bool GetStorage(string propertyName) => propertyName switch
        {
            "AluminiumSections" => AluminiumSections,
            "Drawers" => Drawers,
            "LockAndKey" => LockAndKey,
            "Racks" => Racks,
            "GlassSections" => GlassSections,
            "Pellets" => Pellets,
            "WoodenFurniture" => WoodenFurniture,
            "AnyOther" => AnyOther,
            _ => false
        };

        public void SetStorage(string propertyName, bool value)
        {
            switch (propertyName)
            {
                case "AluminiumSections": AluminiumSections = value; break;
                case "Drawers": Drawers = value; break;
                case "LockAndKey": LockAndKey = value; break;
                case "Racks": Racks = value; break;
                case "GlassSections": GlassSections = value; break;
                case "Pellets": Pellets = value; break;
                case "WoodenFurniture": WoodenFurniture = value; break;
                case "AnyOther": AnyOther = value; break;
            }
        }
    }

    /// <summary>Maps to dbo.Form20A.</summary>
    public class Form20AApplication : DrugLicenceApplicationBase
    {
        public override string FormType => "20A";
    }

    /// <summary>Maps to dbo.Form21A.</summary>
    public class Form21AApplication : DrugLicenceApplicationBase
    {
        public override string FormType => "21A";
    }
}
