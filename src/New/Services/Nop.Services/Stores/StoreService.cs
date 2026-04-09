using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Stores;

public class StoreService : IStoreService
{
    private const string StoresAllKey = "Nop.stores.all";
    private const string StoresByIdKey = "Nop.stores.id-{0}";
    private const string StoresPrefix = "Nop.stores.";

    private readonly IRepository<Store> _storeRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public StoreService(
        IRepository<Store> storeRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _storeRepository = storeRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task<IList<Store>> GetAllStoresAsync()
    {
        var key = new CacheKey(StoresAllKey, StoresPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var stores = _storeRepository.TableNoTracking
                .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
                .ToList();
            return Task.FromResult<IList<Store>>(stores);
        }) ?? [];
    }

    public virtual async Task<Store?> GetStoreByIdAsync(int storeId)
    {
        if (storeId == 0)
            return null;

        var key = new CacheKey(string.Format(StoresByIdKey, storeId), StoresPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_storeRepository.GetById(storeId)));
    }

    public virtual async Task InsertStoreAsync(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _storeRepository.Insert(store);
        await _cacheManager.RemoveByPrefixAsync(StoresPrefix);
        await _eventPublisher.EntityInsertedAsync(store);
    }

    public virtual async Task UpdateStoreAsync(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _storeRepository.Update(store);
        await _cacheManager.RemoveByPrefixAsync(StoresPrefix);
        await _eventPublisher.EntityUpdatedAsync(store);
    }

    public virtual async Task DeleteStoreAsync(Store store)
    {
        ArgumentNullException.ThrowIfNull(store);

        var allStores = await GetAllStoresAsync();
        if (allStores.Count == 1)
            throw new InvalidOperationException("You cannot delete the only configured store");

        _storeRepository.Delete(store);
        await _cacheManager.RemoveByPrefixAsync(StoresPrefix);
        await _eventPublisher.EntityDeletedAsync(store);
    }
}
