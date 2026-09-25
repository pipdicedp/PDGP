using TradeLicence.Models;
using Microsoft.EntityFrameworkCore;

namespace TradeLicence.Data
{
    public class ElectricityApplicationDbContext : DbContext
    {
        public ElectricityApplicationDbContext(DbContextOptions<ElectricityApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EBapplication> EBapplications { get; set; } = null!;
        public DbSet<DropdownMaster> DropdownMasters { get; set; } = null!;
    }
}
