using Nop.Core.Domain.Stores;

namespace Nop.Services.Stores;

public interface IStoreService
{
    Task<IList<Store>> GetAllStoresAsync();
    Task<Store?> GetStoreByIdAsync(int storeId);
    Task InsertStoreAsync(Store store);
    Task UpdateStoreAsync(Store store);
    Task DeleteStoreAsync(Store store);
}
