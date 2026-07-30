using System.Threading.Tasks;

namespace TradeLicence.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task AddAsync(T entity);
    }
}
