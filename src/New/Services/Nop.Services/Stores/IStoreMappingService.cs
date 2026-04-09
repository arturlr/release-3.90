using Nop.Core;
using Nop.Core.Domain.Stores;

namespace Nop.Services.Stores;

public interface IStoreMappingService
{
    Task<StoreMapping?> GetStoreMappingByIdAsync(int storeMappingId);
    Task<IList<StoreMapping>> GetStoreMappingsAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported;
    Task InsertStoreMappingAsync(StoreMapping storeMapping);
    Task InsertStoreMappingAsync<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported;
    Task UpdateStoreMappingAsync(StoreMapping storeMapping);
    Task DeleteStoreMappingAsync(StoreMapping storeMapping);
    Task<int[]> GetStoreIdsWithAccessAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported;
    Task<bool> AuthorizeAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported;
    Task<bool> AuthorizeAsync<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported;
}
