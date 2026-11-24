using Microsoft.EntityFrameworkCore;
using TradeLicence.Models;
using TradeLicence.Models.Drugs;

namespace TradeLicence.Data
{
    /// <summary>
    /// DbContext for the Restricted Drug Licence services (Form 20A + Form 21A).
    /// Same pattern as WaterApplicationDbContext / ElectricityApplicationDbContext: one context per
    /// service for the service's own tables; Officers + the generic Workflow* tables stay in
    /// ApplicationDbContext (the engine takes both).
    /// </summary>
    public class DrugsApplicationDbContext : DbContext
    {
        public DrugsApplicationDbContext(DbContextOptions<DrugsApplicationDbContext> options) : base(options) { }

        public DbSet<Form20AApplication> Form20AApplications { get; set; } = null!;
        public DbSet<Form21AApplication> Form21AApplications { get; set; } = null!;
        public DbSet<DrugLicencePharmacist> Pharmacists { get; set; } = null!;
        public DbSet<DrugLicenceDocument> Documents { get; set; } = null!;
        public DbSet<DropdownMaster> DropdownMasters { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureApplication<Form20AApplication>(modelBuilder, "Form20A", "Application_Id");
            ConfigureApplication<Form21AApplication>(modelBuilder, "Form21A", "Application_id");

            modelBuilder.Entity<DrugLicencePharmacist>(e =>
            {
                e.ToTable("DrugLicencePharmacists");
                e.HasKey(x => x.PharmacistId);
                e.Property(x => x.ValidityDate).HasColumnType("date");
                e.Property(x => x.DateOfBirth).HasColumnType("date");
                e.HasIndex(x => new { x.FormType, x.ApplicationId });
            });

            modelBuilder.Entity<DrugLicenceDocument>(e =>
            {
                e.ToTable("DrugLicenceDocuments");
                e.HasKey(x => x.DocumentId);
                e.HasIndex(x => new { x.FormType, x.ApplicationId, x.DocumentType }).IsUnique();
            });

            modelBuilder.Entity<DropdownMaster>().ToTable("DropdownMasters");
        }

        private static void ConfigureApplication<T>(ModelBuilder modelBuilder, string table, string keyColumn)
            where T : DrugLicenceApplicationBase
        {
            modelBuilder.Entity<T>(e =>
            {
                e.ToTable(table);
                e.HasKey(x => x.ApplicationId);
                e.Property(x => x.ApplicationId).HasColumnName(keyColumn).ValueGeneratedOnAdd();

                // varchar(12) column holding 'Registration' or 'Renewal' (guarded by CK_Form20A/21A_RegistrationOrRenewal);
                // the model exposes it as a bool: false = Registration, true = Renewal
                e.Property(x => x.IsRenewal)
                    .HasColumnName("RegistrationOrRenewal")
                    .HasColumnType("varchar(12)")
                    .HasConversion(v => v ? "Renewal" : "Registration", v => v == "Renewal");

                // The table also has Name/Sex/Qualification/Experience/CouncilNo/ValidityDate/DateOfBirth/YearOfPassing
                // (the old single-pharmacist columns). They are nullable and intentionally not mapped:
                // pharmacists now live in DrugLicencePharmacists so an application can have many.

                e.Property(x => x.CreatedDate).HasColumnType("datetime2(7)");
                e.Property(x => x.SubmittedDate).HasColumnType("datetime2(7)");
                e.Property(x => x.ModifiedDate).HasColumnType("datetime2(7)");

                e.HasIndex(x => x.UserId);
                e.HasIndex(x => new { x.Status, x.CurrentStage });
            });
        }
    }
}
