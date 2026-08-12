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
            modelBuilder.Entity<TradeLicenceApplication>(entity =>
            {
                // ... your existing config lines stay as they are ...

                entity.HasOne<ApplicationUser>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

        }
    }
}
