using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Stores;

namespace Nop.Services.Stores
{
    public interface IStoreService
    {
        Task<Store> GetStoreByIdAsync(int storeId);
        Task<IList<Store>> GetAllStoresAsync();
        Task InsertStoreAsync(Store store);
        Task UpdateStoreAsync(Store store);
        Task DeleteStoreAsync(Store store);
    }
}
