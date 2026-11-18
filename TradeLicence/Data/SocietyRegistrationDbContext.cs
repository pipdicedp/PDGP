using Microsoft.EntityFrameworkCore;
using TradeLicence.Models;

namespace TradeLicence.Data
{
    public class SocietyRegistrationDbContext : DbContext
    {
        public SocietyRegistrationDbContext(DbContextOptions<SocietyRegistrationDbContext> options) : base(options) { }

        public DbSet<SocietyApplication> SocietyApplications { get; set; } = null!;
        public DbSet<SocietyMember> SocietyMembers { get; set; } = null!;
        public DbSet<SocietyApplicationDocument> SocietyApplicationDocuments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SocietyApplication>(e =>
            {
                e.ToTable("SocietyApplications");
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.ApplicationNumber);
                e.HasMany(x => x.Members).WithOne(m => m.Application!)
                    .HasForeignKey(m => m.SocietyApplicationId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SocietyMember>().ToTable("SocietyMembers");

            modelBuilder.Entity<SocietyApplicationDocument>(e =>
            {
                e.ToTable("SocietyApplicationDocuments");
                e.HasIndex(x => x.SocietyApplicationId);
            });
        }
    }
}
