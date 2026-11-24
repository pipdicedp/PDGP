using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using TradeLicence.Models;
using static TradeLicence.Models.TradeLicenceApplication;

namespace TradeLicence.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TradeLicenceApplication> TradeLicenceApplications { get; set; } = null!;
        public DbSet<Municipality> Municipalities { get; set; } = null!;
        public DbSet<Ward> Wards { get; set; } = null!;
        public DbSet<Area> Areas { get; set; } = null!;
        public DbSet<Street> Streets { get; set; } = null!;
        public DbSet<DoorNumberLookup> DoorNumbers { get; set; } = null!;
        public DbSet<DocumentChecklistItem> DocumentChecklistItems { get; set; } = null!;
        public DbSet<ApplicationDocument> ApplicationDocuments { get; set; } = null!;
        public DbSet<TradeLicencePartner> TradeLicencePartners { get; set; } = null!;
        public DbSet<TradeLicenceMachinery> TradeLicenceMachineries { get; set; } = null!;
        public DbSet<TradeLicencePhotograph> TradeLicencePhotographs { get; set; }
        public DbSet<TradeLicenceDocument> TradeLicenceDocuments { get; set; } = null!;
        public DbSet<ShopEstablishmentRegistration> ShopEstablishmentRegistrations { get; set; }
        public DbSet<ApplicationUser> Users { get; set; } = null!;
        public DbSet<Officer> Officers { get; set; } = null!;
        public DbSet<ApplicationWorkflowHistory> ApplicationWorkflowHistories { get; set; } = null!;
        public DbSet<ShopEmployer> ShopEmployers { get; set; } = null!;
        public DbSet<ShopFormIXPartA> ShopFormIXPartAs { get; set; } = null!;
        public DbSet<ShopFormIXPartB> ShopFormIXPartBs { get; set; } = null!;
        public DbSet<ShopAnnexureDocument> ShopAnnexureDocuments { get; set; } = null!;
        public DbSet<OfficerSupportingDocument> OfficerSupportingDocuments { get; set; } = null!;
        public DbSet<TradeLicencePayment> TradeLicencePayments { get; set; } = null!;

        // ---------------- Shared Workflow Engine (every service except TradeLicence) ----------------
        public DbSet<WorkflowHistory> WorkflowHistories { get; set; } = null!;
        public DbSet<WorkflowSupportingDocument> WorkflowSupportingDocuments { get; set; } = null!;
        public DbSet<WorkflowPayment> WorkflowPayments { get; set; } = null!;

        // ---------------- CAF forwarded to a department (Industry officer -> receiving department) ----------------
        public DbSet<CafDepartmentApplication> CafDepartmentApplications { get; set; } = null!;
        public DbSet<CafIndustryReview> CafIndustryReviews { get; set; } = null!;

        // ---------------- Name Change of Society (Request for Name/Business Place/Members) ----------------
        // Own table: dbo.SocietyNameChangeRequest.
        public DbSet<SocietyNameChangeRequest> SocietyNameChangeRequests { get; set; } = null!;

        // ---------------- Name Change of Firms (Request for Name/Business Place/Partners) ----------------
        // Own table: dbo.FirmNameChangeRequest.
        public DbSet<FirmNameChangeRequest> FirmNameChangeRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TradeLicenceApplication>(entity =>
            {
                entity.ToTable("TradeLicenceApplications");
                entity.Property(e => e.TotalAreaCoveredSqFt).HasColumnType("decimal(10,2)");
                entity.Property(e => e.RentEstimatedRentPerMonth).HasColumnType("decimal(10,2)");
                entity.Property(e => e.Status).HasDefaultValue("Draft");
            });

            modelBuilder.Entity<ApplicationDocument>(entity =>
            {
                entity.ToTable("ApplicationDocuments");
                entity.HasOne(d => d.Application)
                      .WithMany(a => a.ApplicationDocuments)
                      .HasForeignKey(d => d.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Municipality>().ToTable("Municipalities");
            modelBuilder.Entity<Ward>().ToTable("Wards");
            modelBuilder.Entity<Area>().ToTable("Areas");
            modelBuilder.Entity<Street>().ToTable("Streets");
            modelBuilder.Entity<DoorNumberLookup>(entity =>
            {
                entity.ToTable("DoorNumbers");
                entity.HasKey(e => e.DoorNumberId);
            });
            modelBuilder.Entity<DocumentChecklistItem>(entity =>
            {
                entity.ToTable("DocumentChecklistItems");
                entity.HasKey(e => e.DocumentItemId);
            });

            modelBuilder.Entity<TradeLicencePartner>(entity =>
            {
                entity.ToTable("TradeLicencePartners");

                entity.HasOne(x => x.Application)
                      .WithMany(x => x.Partners)
                      .HasForeignKey(x => x.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TradeLicenceMachinery>(entity =>
            {
                entity.ToTable("TradeLicenceMachineries");

                entity.HasOne(x => x.Application)
                      .WithMany(x => x.Machineries)
                      .HasForeignKey(x => x.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TradeLicencePhotograph>(entity =>
            {
                entity.ToTable("TradeLicencePhotographs");

                entity.HasOne(x => x.Application)
                      .WithMany(x => x.Photographs)
                      .HasForeignKey(x => x.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TradeLicenceDocument>(entity =>
            {
                entity.ToTable("TradeLicenceDocuments");

                entity.HasOne(x => x.Application)
                      .WithMany(x => x.Documents)
                      .HasForeignKey(x => x.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShopEstablishmentRegistration>(entity =>
            {
                entity.HasKey(x => x.ShopRegistrationId);

                entity.ToTable("ShopEstablishmentRegistrations");

                entity.HasOne<TradeLicenceApplication>()
                      .WithOne(x => x.ShopRegistration)
                      .HasForeignKey<ShopEstablishmentRegistration>(x => x.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Username).IsUnique();
            });
            modelBuilder.Entity<Officer>(entity =>
            {
                entity.ToTable("Officers");
                entity.HasKey(e => e.OfficerId);
                entity.HasIndex(e => e.Username).IsUnique();
            });

            modelBuilder.Entity<ApplicationWorkflowHistory>(entity =>
            {
                entity.ToTable("ApplicationWorkflowHistories");
                entity.HasKey(e => e.HistoryId);

                entity.HasOne(e => e.Application)
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict (not SetNull/Cascade) ù an Officer with history
                // rows can't be deleted outright, which is the right call
                // for an audit trail. Officer.IsLocked is how you retire
                // an officer account instead.
                entity.HasOne(e => e.FromOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.FromOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ToOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.ToOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ShopEmployer>(entity =>
            {
                entity.ToTable("ShopEmployers");
                entity.HasKey(e => e.EmployerRowId);
                entity.HasOne<TradeLicenceApplication>()
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShopFormIXPartA>(entity =>
            {
                entity.ToTable("ShopFormIXPartAs");
                entity.HasKey(e => e.PartARowId);
                entity.HasOne<TradeLicenceApplication>()
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShopFormIXPartB>(entity =>
            {
                entity.ToTable("ShopFormIXPartBs");
                entity.HasKey(e => e.PartBRowId);
                entity.HasOne<TradeLicenceApplication>()
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ShopAnnexureDocument>(entity =>
            {
                entity.ToTable("ShopAnnexureDocuments");
                entity.HasKey(e => e.AnnexureDocumentId);
                entity.HasOne<TradeLicenceApplication>()
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OfficerSupportingDocument>(entity =>
            {
                entity.ToTable("OfficerSupportingDocuments");
                entity.HasKey(e => e.OfficerSupportingDocumentId);

                // Enforces "one file per stage" at the DB level too, not
                // just via the upsert logic in OfficerController.
                entity.HasIndex(e => new { e.ApplicationId, e.Stage }).IsUnique();

                entity.HasOne(e => e.Application)
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Restrict, same reasoning as ApplicationWorkflowHistory ΓÇö
                // an officer who's uploaded a file shouldn't be deletable
                // outright; ToggleOfficerStatus (Inactive) is the fallback.
                entity.HasOne(e => e.UploadedByOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.UploadedByOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<TradeLicenceApplication>(entity =>
            {
                // ... your existing config lines stay as they are ...

                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Officer workflow routing ù nullable FK, so deleting an
                // Officer account never blocks (it just un-assigns the
                // application, leaving it visible to the rest of that stage).
                entity.HasOne<Officer>()
                      .WithMany()
                      .HasForeignKey(e => e.AssignedOfficerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TradeLicencePayment>(entity =>
            {
                entity.ToTable("TradeLicencePayments");
                entity.HasKey(e => e.PaymentId);

                entity.Property(e => e.PaymentAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.ExtraCharge).HasColumnType("decimal(12,2)");
                entity.Property(e => e.TotalPaymentAmount).HasColumnType("decimal(12,2)");

                // One payment row per application - SendPaymentRequest upserts
                // onto this, same "one row per key" approach as
                // OfficerSupportingDocument's (ApplicationId, Stage) index.
                entity.HasIndex(e => e.ApplicationId).IsUnique();

                entity.HasOne<TradeLicenceApplication>()
                      .WithMany()
                      .HasForeignKey(e => e.ApplicationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------- Shared Workflow Engine tables ----------------
            // ApplicationId is a SOFT reference here (no FK) ΓÇö it points at a
            // different physical table depending on ServiceType (Water,
            // Electricity, ...), so a single FK constraint isn't possible.
            // ServiceType + ApplicationId together identify the application.
            modelBuilder.Entity<WorkflowHistory>(entity =>
            {
                entity.ToTable("WorkflowHistories");
                entity.HasKey(e => e.HistoryId);

                entity.HasIndex(e => new { e.ServiceType, e.ApplicationId });

                // Officers IS genuinely shared across every service, so this
                // FK is real (same reasoning as ApplicationWorkflowHistory ΓÇö
                // Restrict, not Cascade/SetNull: an officer with history rows
                // can't be deleted outright; Officer.IsLocked is the fallback).
                entity.HasOne(e => e.FromOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.FromOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ToOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.ToOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowSupportingDocument>(entity =>
            {
                entity.ToTable("WorkflowSupportingDocuments");
                entity.HasKey(e => e.WorkflowSupportingDocumentId);

                // One file per (ServiceType, ApplicationId, Stage) ΓÇö same
                // "one row per key" idea as OfficerSupportingDocument's
                // (ApplicationId, Stage) index, with ServiceType added since
                // this table is shared across services.
                entity.HasIndex(e => new { e.ServiceType, e.ApplicationId, e.Stage }).IsUnique();

                entity.HasOne(e => e.UploadedByOfficer)
                      .WithMany()
                      .HasForeignKey(e => e.UploadedByOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkflowPayment>(entity =>
            {
                entity.ToTable("WorkflowPayments");
                entity.HasKey(e => e.WorkflowPaymentId);

                entity.Property(e => e.PaymentAmount).HasColumnType("decimal(12,2)");
                entity.Property(e => e.ExtraCharge).HasColumnType("decimal(12,2)");
                entity.Property(e => e.TotalPaymentAmount).HasColumnType("decimal(12,2)");

                // One payment row per (ServiceType, ApplicationId).
                entity.HasIndex(e => new { e.ServiceType, e.ApplicationId }).IsUnique();
            });

            // ---------------- CAF forwarded to a department ----------------
            // One row per (CAF, department) ΓÇö a CAF is forwarded to a department at most once.
            modelBuilder.Entity<CafDepartmentApplication>(entity =>
            {
                entity.ToTable("CafDepartmentApplications");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.LoginId, e.Department }).IsUnique();

                entity.HasOne<Officer>()
                      .WithMany()
                      .HasForeignKey(e => e.ForwardedByOfficerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Officer>()
                      .WithMany()
                      .HasForeignKey(e => e.AssignedOfficerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ---------------- Name Change of Society ----------------
            modelBuilder.Entity<SocietyNameChangeRequest>(entity =>
            {
                entity.ToTable("SocietyNameChangeRequest");
                entity.HasKey(e => e.RequestId);
                entity.Property(e => e.RequestId).HasColumnName("RequestId").ValueGeneratedOnAdd();
                entity.Property(e => e.ApplicationNo).HasColumnName("ApplicationNo").HasMaxLength(50);
                entity.Property(e => e.SerialNo).HasColumnName("SerialNo").HasMaxLength(50);
                entity.Property(e => e.IsNameChange).HasColumnName("IsNameChange");
                entity.Property(e => e.IsAddressChange).HasColumnName("IsAddressChange");
                entity.Property(e => e.IsMemberChange).HasColumnName("IsMemberChange");
                entity.Property(e => e.IsOthers).HasColumnName("IsOthers");
                entity.Property(e => e.FormFileName).HasColumnName("FormFileName").HasMaxLength(520);
                entity.Property(e => e.FormFilePath).HasColumnName("FormFilePath").HasMaxLength(1000);
                entity.Property(e => e.FormContentType).HasColumnName("FormContentType").HasMaxLength(100);
                entity.Property(e => e.FormFileSizeKB).HasColumnName("FormFileSizeKB");
                entity.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(2000);
                entity.Property(e => e.RequestStatus).HasColumnName("RequestStatus").HasMaxLength(20);
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(100);
                entity.Property(e => e.CreatedOn).HasColumnName("CreatedOn");
                entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(100);
                entity.Property(e => e.ModifiedOn).HasColumnName("ModifiedOn");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });

            // ---------------- Name Change of Firms ----------------
            modelBuilder.Entity<FirmNameChangeRequest>(entity =>
            {
                entity.ToTable("FirmNameChangeRequest");
                entity.HasKey(e => e.RequestId);
                entity.Property(e => e.RequestId).HasColumnName("RequestId").ValueGeneratedOnAdd();
                entity.Property(e => e.IsNameChange).HasColumnName("IsNameChange");
                entity.Property(e => e.IsAddressChange).HasColumnName("IsAddressChange");
                entity.Property(e => e.IsPartnership).HasColumnName("IsPartnership");
                entity.Property(e => e.IsOthers).HasColumnName("IsOthers");
                entity.Property(e => e.FormFileName).HasColumnName("FormFileName").HasMaxLength(520);
                entity.Property(e => e.FormFilePath).HasColumnName("FormFilePath").HasMaxLength(1000);
                entity.Property(e => e.FormContentType).HasColumnName("FormContentType").HasMaxLength(100);
                entity.Property(e => e.FormFileSizeKB).HasColumnName("FormFileSizeKB");
                entity.Property(e => e.ApplicationNo).HasColumnName("ApplicationNo").HasMaxLength(50);
                entity.Property(e => e.ChallanNo).HasColumnName("ChallanNo").HasMaxLength(50);
                entity.Property(e => e.ChallanDate).HasColumnName("ChallanDate");
                entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)");
                entity.Property(e => e.Remarks).HasColumnName("Remarks").HasMaxLength(2000);
                entity.Property(e => e.RequestStatus).HasColumnName("RequestStatus").HasMaxLength(20);
                entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy").HasMaxLength(100);
                entity.Property(e => e.CreatedOn).HasColumnName("CreatedOn");
                entity.Property(e => e.ModifiedBy).HasColumnName("ModifiedBy").HasMaxLength(100);
                entity.Property(e => e.ModifiedOn).HasColumnName("ModifiedOn");
                entity.Property(e => e.IsActive).HasColumnName("IsActive");
            });
        }
    }
}
