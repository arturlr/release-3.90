using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Stores;

public class StoreMappingService : IStoreMappingService
{
    private const string StoreMappingByEntityKey = "Nop.storemapping.entityid-name-{0}-{1}";
    private const string StoreMappingPrefix = "Nop.storemapping.";

    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IStoreContext _storeContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CatalogSettings _catalogSettings;

    public StoreMappingService(
        IRepository<StoreMapping> storeMappingRepository,
        IStoreContext storeContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CatalogSettings catalogSettings)
    {
        _storeMappingRepository = storeMappingRepository;
        _storeContext = storeContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _catalogSettings = catalogSettings;
    }

    public virtual Task<StoreMapping?> GetStoreMappingByIdAsync(int storeMappingId)
    {
        if (storeMappingId == 0)
            return Task.FromResult<StoreMapping?>(null);

        return Task.FromResult<StoreMapping?>(_storeMappingRepository.GetById(storeMappingId));
    }

    public virtual Task<IList<StoreMapping>> GetStoreMappingsAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityName = typeof(T).Name;
        IList<StoreMapping> result = _storeMappingRepository.TableNoTracking
            .Where(sm => sm.EntityId == entity.Id && sm.EntityName == entityName)
            .ToList();
        return Task.FromResult(result);
    }

    public virtual async Task InsertStoreMappingAsync(StoreMapping storeMapping)
    {
        ArgumentNullException.ThrowIfNull(storeMapping);
        _storeMappingRepository.Insert(storeMapping);
        await _cacheManager.RemoveByPrefixAsync(StoreMappingPrefix);
        await _eventPublisher.EntityInsertedAsync(storeMapping);
    }

    public virtual Task InsertStoreMappingAsync<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentOutOfRangeException.ThrowIfZero(storeId);

        var storeMapping = new StoreMapping
        {
            EntityId = entity.Id,
            EntityName = typeof(T).Name,
            StoreId = storeId
        };
        return InsertStoreMappingAsync(storeMapping);
    }

    public virtual async Task UpdateStoreMappingAsync(StoreMapping storeMapping)
    {
        ArgumentNullException.ThrowIfNull(storeMapping);
        _storeMappingRepository.Update(storeMapping);
        await _cacheManager.RemoveByPrefixAsync(StoreMappingPrefix);
        await _eventPublisher.EntityUpdatedAsync(storeMapping);
    }

    public virtual async Task DeleteStoreMappingAsync(StoreMapping storeMapping)
    {
        ArgumentNullException.ThrowIfNull(storeMapping);
        _storeMappingRepository.Delete(storeMapping);
        await _cacheManager.RemoveByPrefixAsync(StoreMappingPrefix);
        await _eventPublisher.EntityDeletedAsync(storeMapping);
    }

    public virtual async Task<int[]> GetStoreIdsWithAccessAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported
    {
        ArgumentNullException.ThrowIfNull(entity);

        var entityName = typeof(T).Name;
        var key = new CacheKey(string.Format(StoreMappingByEntityKey, entity.Id, entityName), StoreMappingPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var storeIds = _storeMappingRepository.TableNoTracking
                .Where(sm => sm.EntityId == entity.Id && sm.EntityName == entityName)
                .Select(sm => sm.StoreId)
                .ToArray();
            return Task.FromResult(storeIds);
        }) ?? [];
    }

    public virtual Task<bool> AuthorizeAsync<T>(T entity) where T : BaseEntity, IStoreMappingSupported
    {
        return AuthorizeAsync(entity, _storeContext.CurrentStore.Id);
    }

    public virtual async Task<bool> AuthorizeAsync<T>(T entity, int storeId) where T : BaseEntity, IStoreMappingSupported
    {
        if (entity == null)
            return false;

        if (storeId == 0)
            return true;

        if (_catalogSettings.IgnoreStoreLimitations)
            return true;

        if (!entity.LimitedToStores)
            return true;

        var storeIds = await GetStoreIdsWithAccessAsync(entity);
        return storeIds.Contains(storeId);
    }
}
