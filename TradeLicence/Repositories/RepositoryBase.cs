using System.Threading.Tasks;
using TradeLicence.Data;

namespace TradeLicence.Repositories
{
    public abstract class RepositoryBase
    {
        protected readonly ApplicationDbContext _context;

        protected RepositoryBase(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
