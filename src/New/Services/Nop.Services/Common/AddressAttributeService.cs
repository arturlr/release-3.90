using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Services.Events;

namespace Nop.Services.Common;

public class AddressAttributeService : IAddressAttributeService
{
    private const string AllKey = "Nop.addressattribute.all";
    private const string ByIdKey = "Nop.addressattribute.id-{0}";
    private const string ValuesAllKey = "Nop.addressattributevalue.all-{0}";
    private const string ValuesByIdKey = "Nop.addressattributevalue.id-{0}";
    private const string AttributesPrefix = "Nop.addressattribute.";
    private const string ValuesPrefix = "Nop.addressattributevalue.";

    private readonly IRepository<AddressAttribute> _addressAttributeRepository;
    private readonly IRepository<AddressAttributeValue> _addressAttributeValueRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public AddressAttributeService(
        IRepository<AddressAttribute> addressAttributeRepository,
        IRepository<AddressAttributeValue> addressAttributeValueRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _addressAttributeRepository = addressAttributeRepository;
        _addressAttributeValueRepository = addressAttributeValueRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task<IList<AddressAttribute>> GetAllAddressAttributesAsync()
    {
        var key = new CacheKey(AllKey, AttributesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _addressAttributeRepository.TableNoTracking
                .OrderBy(aa => aa.DisplayOrder).ThenBy(aa => aa.Id);
            return Task.FromResult<IList<AddressAttribute>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<AddressAttribute?> GetAddressAttributeByIdAsync(int addressAttributeId)
    {
        if (addressAttributeId == 0)
            return null;
        var key = new CacheKey(string.Format(ByIdKey, addressAttributeId), AttributesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_addressAttributeRepository.GetById(addressAttributeId)));
    }

    public virtual async Task InsertAddressAttributeAsync(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);
        _addressAttributeRepository.Insert(addressAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityInsertedAsync(addressAttribute);
    }

    public virtual async Task UpdateAddressAttributeAsync(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);
        _addressAttributeRepository.Update(addressAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityUpdatedAsync(addressAttribute);
    }

    public virtual async Task DeleteAddressAttributeAsync(AddressAttribute addressAttribute)
    {
        ArgumentNullException.ThrowIfNull(addressAttribute);
        _addressAttributeRepository.Delete(addressAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityDeletedAsync(addressAttribute);
    }

    public virtual async Task<IList<AddressAttributeValue>> GetAddressAttributeValuesAsync(int addressAttributeId)
    {
        var key = new CacheKey(string.Format(ValuesAllKey, addressAttributeId), ValuesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _addressAttributeValueRepository.TableNoTracking
                .Where(aav => aav.AddressAttributeId == addressAttributeId)
                .OrderBy(aav => aav.DisplayOrder).ThenBy(aav => aav.Id);
            return Task.FromResult<IList<AddressAttributeValue>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<AddressAttributeValue?> GetAddressAttributeValueByIdAsync(int addressAttributeValueId)
    {
        if (addressAttributeValueId == 0)
            return null;
        var key = new CacheKey(string.Format(ValuesByIdKey, addressAttributeValueId), ValuesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_addressAttributeValueRepository.GetById(addressAttributeValueId)));
    }

    public virtual async Task InsertAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);
        _addressAttributeValueRepository.Insert(addressAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityInsertedAsync(addressAttributeValue);
    }

    public virtual async Task UpdateAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);
        _addressAttributeValueRepository.Update(addressAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityUpdatedAsync(addressAttributeValue);
    }

    public virtual async Task DeleteAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(addressAttributeValue);
        _addressAttributeValueRepository.Delete(addressAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityDeletedAsync(addressAttributeValue);
    }

    private async Task InvalidateCachesAsync()
    {
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ValuesPrefix);
    }
}
