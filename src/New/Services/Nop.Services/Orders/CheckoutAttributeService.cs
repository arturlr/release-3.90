using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Orders;

public class CheckoutAttributeService(
    IRepository<CheckoutAttribute> checkoutAttributeRepository,
    IRepository<CheckoutAttributeValue> checkoutAttributeValueRepository,
    IRepository<StoreMapping> storeMappingRepository,
    IStaticCacheManager cacheManager,
    IEventPublisher eventPublisher) : ICheckoutAttributeService
{
    private const string AttributesAllKey = "Nop.checkoutattribute.all-{0}-{1}";
    private const string AttributeValuesAllKey = "Nop.checkoutattributevalue.all-{0}";
    private const string AttributesPrefix = "Nop.checkoutattribute.";
    private const string AttributeValuesPrefix = "Nop.checkoutattributevalue.";

    public async Task DeleteCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);
        checkoutAttributeRepository.Delete(checkoutAttribute);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await cacheManager.RemoveByPrefixAsync(AttributeValuesPrefix);
        await eventPublisher.EntityDeletedAsync(checkoutAttribute);
    }

    public async Task<IList<CheckoutAttribute>> GetAllCheckoutAttributesAsync(int storeId = 0, bool excludeShippableAttributes = false)
    {
        var key = new CacheKey(string.Format(AttributesAllKey, storeId, excludeShippableAttributes), AttributesPrefix);
        return await cacheManager.GetAsync(key, () =>
        {
            var query = checkoutAttributeRepository.Table;

            if (storeId > 0)
            {
                query = from ca in query
                        join sm in storeMappingRepository.Table
                            on new { EntityId = ca.Id, EntityName = "CheckoutAttribute" }
                            equals new { sm.EntityId, sm.EntityName } into smJoin
                        from sm in smJoin.DefaultIfEmpty()
                        where !ca.LimitedToStores || sm.StoreId == storeId
                        select ca;
                query = query.Distinct();
            }

            if (excludeShippableAttributes)
                query = query.Where(ca => !ca.ShippableProductRequired);

            query = query.OrderBy(ca => ca.DisplayOrder).ThenBy(ca => ca.Id);
            return Task.FromResult<IList<CheckoutAttribute>>(query.ToList());
        }) ?? [];
    }

    public Task<CheckoutAttribute?> GetCheckoutAttributeByIdAsync(int checkoutAttributeId) =>
        Task.FromResult(checkoutAttributeId == 0 ? null : (CheckoutAttribute?)checkoutAttributeRepository.GetById(checkoutAttributeId));

    public async Task InsertCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);
        checkoutAttributeRepository.Insert(checkoutAttribute);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await eventPublisher.EntityInsertedAsync(checkoutAttribute);
    }

    public async Task UpdateCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttribute);
        checkoutAttributeRepository.Update(checkoutAttribute);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await eventPublisher.EntityUpdatedAsync(checkoutAttribute);
    }

    // Values
    public async Task DeleteCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttributeValue);
        checkoutAttributeValueRepository.Delete(checkoutAttributeValue);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await cacheManager.RemoveByPrefixAsync(AttributeValuesPrefix);
        await eventPublisher.EntityDeletedAsync(checkoutAttributeValue);
    }

    public async Task<IList<CheckoutAttributeValue>> GetCheckoutAttributeValuesAsync(int checkoutAttributeId)
    {
        var key = new CacheKey(string.Format(AttributeValuesAllKey, checkoutAttributeId), AttributeValuesPrefix);
        return await cacheManager.GetAsync(key, () =>
            Task.FromResult<IList<CheckoutAttributeValue>>(
                checkoutAttributeValueRepository.Table
                    .Where(cav => cav.CheckoutAttributeId == checkoutAttributeId)
                    .OrderBy(cav => cav.DisplayOrder).ThenBy(cav => cav.Id)
                    .ToList())) ?? [];
    }

    public Task<CheckoutAttributeValue?> GetCheckoutAttributeValueByIdAsync(int checkoutAttributeValueId) =>
        Task.FromResult(checkoutAttributeValueId == 0 ? null : (CheckoutAttributeValue?)checkoutAttributeValueRepository.GetById(checkoutAttributeValueId));

    public async Task InsertCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttributeValue);
        checkoutAttributeValueRepository.Insert(checkoutAttributeValue);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await cacheManager.RemoveByPrefixAsync(AttributeValuesPrefix);
        await eventPublisher.EntityInsertedAsync(checkoutAttributeValue);
    }

    public async Task UpdateCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue)
    {
        ArgumentNullException.ThrowIfNull(checkoutAttributeValue);
        checkoutAttributeValueRepository.Update(checkoutAttributeValue);
        await cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await cacheManager.RemoveByPrefixAsync(AttributeValuesPrefix);
        await eventPublisher.EntityUpdatedAsync(checkoutAttributeValue);
    }
}
