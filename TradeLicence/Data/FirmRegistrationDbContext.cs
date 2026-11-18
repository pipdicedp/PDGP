using Microsoft.EntityFrameworkCore;
using TradeLicence.Models;

namespace TradeLicence.Data
{
    public class FirmRegistrationDbContext : DbContext
    {
        public FirmRegistrationDbContext(DbContextOptions<FirmRegistrationDbContext> options) : base(options) { }

        public DbSet<FirmApplication> FirmApplications { get; set; } = null!;
        public DbSet<FirmPartner> FirmPartners { get; set; } = null!;
        public DbSet<FirmApplicationDocument> FirmApplicationDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FirmApplication>(e =>
            {
                e.ToTable("FirmApplications");
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.ApplicationNumber);
                e.HasMany(x => x.Partners).WithOne(p => p.Application!)
                    .HasForeignKey(p => p.FirmApplicationId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FirmPartner>().ToTable("FirmPartners");

            modelBuilder.Entity<FirmApplicationDocument>(e =>
            {
                e.ToTable("FirmApplicationDocuments");
                e.HasIndex(x => x.FirmApplicationId);
            });
        }
    }
}
