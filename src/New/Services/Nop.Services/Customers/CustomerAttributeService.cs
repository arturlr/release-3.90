using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Services.Events;

namespace Nop.Services.Customers;

public class CustomerAttributeService : ICustomerAttributeService
{
    private const string AllKey = "Nop.customerattribute.all";
    private const string ByIdKey = "Nop.customerattribute.id-{0}";
    private const string ValuesAllKey = "Nop.customerattributevalue.all-{0}";
    private const string ValuesByIdKey = "Nop.customerattributevalue.id-{0}";
    private const string AttributesPrefix = "Nop.customerattribute.";
    private const string ValuesPrefix = "Nop.customerattributevalue.";

    private readonly IRepository<CustomerAttribute> _customerAttributeRepository;
    private readonly IRepository<CustomerAttributeValue> _customerAttributeValueRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public CustomerAttributeService(
        IRepository<CustomerAttribute> customerAttributeRepository,
        IRepository<CustomerAttributeValue> customerAttributeValueRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _customerAttributeRepository = customerAttributeRepository;
        _customerAttributeValueRepository = customerAttributeValueRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual async Task<IList<CustomerAttribute>> GetAllCustomerAttributesAsync()
    {
        var key = new CacheKey(AllKey, AttributesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _customerAttributeRepository.TableNoTracking
                .OrderBy(ca => ca.DisplayOrder).ThenBy(ca => ca.Id);
            return Task.FromResult<IList<CustomerAttribute>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<CustomerAttribute?> GetCustomerAttributeByIdAsync(int customerAttributeId)
    {
        if (customerAttributeId == 0)
            return null;
        var key = new CacheKey(string.Format(ByIdKey, customerAttributeId), AttributesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_customerAttributeRepository.GetById(customerAttributeId)));
    }

    public virtual async Task InsertCustomerAttributeAsync(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);
        _customerAttributeRepository.Insert(customerAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityInsertedAsync(customerAttribute);
    }

    public virtual async Task UpdateCustomerAttributeAsync(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);
        _customerAttributeRepository.Update(customerAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityUpdatedAsync(customerAttribute);
    }

    public virtual async Task DeleteCustomerAttributeAsync(CustomerAttribute customerAttribute)
    {
        ArgumentNullException.ThrowIfNull(customerAttribute);
        _customerAttributeRepository.Delete(customerAttribute);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityDeletedAsync(customerAttribute);
    }

    public virtual async Task<IList<CustomerAttributeValue>> GetCustomerAttributeValuesAsync(int customerAttributeId)
    {
        var key = new CacheKey(string.Format(ValuesAllKey, customerAttributeId), ValuesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _customerAttributeValueRepository.TableNoTracking
                .Where(cav => cav.CustomerAttributeId == customerAttributeId)
                .OrderBy(cav => cav.DisplayOrder).ThenBy(cav => cav.Id);
            return Task.FromResult<IList<CustomerAttributeValue>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<CustomerAttributeValue?> GetCustomerAttributeValueByIdAsync(int customerAttributeValueId)
    {
        if (customerAttributeValueId == 0)
            return null;
        var key = new CacheKey(string.Format(ValuesByIdKey, customerAttributeValueId), ValuesPrefix);
        return await _cacheManager.GetAsync(key, () =>
            Task.FromResult(_customerAttributeValueRepository.GetById(customerAttributeValueId)));
    }

    public virtual async Task InsertCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);
        _customerAttributeValueRepository.Insert(customerAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityInsertedAsync(customerAttributeValue);
    }

    public virtual async Task UpdateCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);
        _customerAttributeValueRepository.Update(customerAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityUpdatedAsync(customerAttributeValue);
    }

    public virtual async Task DeleteCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(customerAttributeValue);
        _customerAttributeValueRepository.Delete(customerAttributeValue);
        await InvalidateCachesAsync();
        await _eventPublisher.EntityDeletedAsync(customerAttributeValue);
    }

    private async Task InvalidateCachesAsync()
    {
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ValuesPrefix);
    }
}
