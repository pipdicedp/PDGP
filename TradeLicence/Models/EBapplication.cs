using System;
using System.ComponentModel.DataAnnotations.Schema;
using TradeLicence.Models;

namespace TradeLicence.Models
{
    public class EBapplication : IWorkflowApplication
    {
        public int Id { get; set; }

        // IWorkflowApplication needs "ApplicationId" — Id is the real,
        // already-used-everywhere primary key, so this is just a read-only
        // passthrough. [NotMapped] so EF doesn't try to create a second
        // "ApplicationId" column for it.
        [NotMapped]
        public int ApplicationId => Id;

        // Was missing entirely — needed so the citizen's "My Applications"
        // list and payment ownership checks know which user this belongs to.
        // ElectricityController must set this when a new application is
        // first created (see the citizen-side note alongside this file).
        public int? UserId { get; set; }

        public string? ApplicationNumber { get; set; }
        public string? ServiceCategory { get; set; }
        public string? OwnershipType { get; set; }
        public string? ConnectionType { get; set; }
        public string? ServiceType { get; set; }
        public string? ApplicantName { get; set; }
        public string? Gender { get; set; }
        public string? RelativeName { get; set; }
        public string? ProposedPincode { get; set; }
        public string? MobileNumber { get; set; }

        public string? SiteDoorNo { get; set; }
        public string? SiteStreet { get; set; }
        public string? SiteArea { get; set; }
        public string? SiteDistrict { get; set; }
        public string? SitePincode { get; set; }
        public string? Email { get; set; }
        public string? Landmark1 { get; set; }
        public string? Landmark2 { get; set; }

        public bool? IsSameAddress { get; set; }
        public string? CommDoorNo { get; set; }
        public string? CommStreet { get; set; }
        public string? CommArea { get; set; }
        public string? CommRegion { get; set; }
        public string? CommPincode { get; set; }

        public string? RSNo { get; set; }
        public decimal? PlotArea { get; set; }
        public decimal? BuildArea { get; set; }
        public string? SupplyCategory { get; set; }
        public string? Purpose { get; set; }
        public decimal? TotalLoad { get; set; }
        public string? TypeOfSupply { get; set; }
        public string? OwnMeter { get; set; }
        public DateTime? SupplyFromDate { get; set; }
        public DateTime? SupplyToDate { get; set; }
        // ⚡ Existing Power Supply Consumer Number Segments
        public string? ExistingConsumerPart1 { get; set; }
        public string? ExistingConsumerPart2 { get; set; }
        public string? ExistingConsumerPart3 { get; set; }
        public string? ExistingConsumerPart4 { get; set; }

        // Full combined Consumer Number for DB storage
        public string? ExistingConsumerNumber { get; set; }
        public bool? HasExistingSupply { get; set; }
        public string? ExistingConsumerNo { get; set; }
        public decimal? ExistingDueAmount { get; set; }
        public bool? HasDuesInArea { get; set; }
        public bool? HasDuesForPremises { get; set; }
        public bool? HasDuesAssociatedFirm { get; set; }

        // Mandatory Document Paths
        public string? PhotoPath { get; set; }
        public string? AddressProofType { get; set; }
        public string? AddressProofPath { get; set; }
        public string? IdentityProofType { get; set; }
        public string? IdentityProofPath { get; set; }
        public string? TestReportPath { get; set; }

        // Optional Document Paths
        public string? SaleDeedPath { get; set; }
        public string? PowerOfAttorneyPath { get; set; }
        public string? MunicipalTaxPath { get; set; }
        public string? AllotmentLetterPath { get; set; }
        public string? HouseRegistrationPath { get; set; }
        public string? LeasePath { get; set; }
        public string? OtherOwnershipPath { get; set; }
        public string? PowerAgentPhotoPath { get; set; }
        public string? OthersPath { get; set; }

        public string? ApplicationStatus { get; set; } = "Pending";

        // IWorkflowApplication needs "Status" — ApplicationStatus is the
        // real, already-used-everywhere column, so this is just a
        // read/write passthrough. [NotMapped] so EF doesn't create a
        // second "Status" column for it.
        //[NotMapped]
        //public string Status
        //{
        //    get => ApplicationStatus ?? "Draft";
        //    set => ApplicationStatus = value;
        //}

        public string Status { get; set; } = "Draft";
        public string? AdminRemarks { get; set; }
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        // ---------------- Officer workflow (added for the shared engine) ----------------
        // Same 4-stage pattern as every other service — see OfficerWorkflow.cs.
        public string CurrentStage { get; set; } = "Initial Scrutiny";
        public int? AssignedOfficerId { get; set; }
        public string PaymentStatus { get; set; } = "NotRequested";
        public string? OfficerRemarks { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}