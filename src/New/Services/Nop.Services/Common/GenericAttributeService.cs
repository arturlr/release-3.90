using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Services.Events;

namespace Nop.Services.Common;

public class GenericAttributeService : IGenericAttributeService
{
    private const string AttributesByEntityKey = "Nop.genericattribute.{0}-{1}";
    private const string AttributesPrefix = "Nop.genericattribute.";

    private readonly IRepository<GenericAttribute> _genericAttributeRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;

    public GenericAttributeService(
        IRepository<GenericAttribute> genericAttributeRepository,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher)
    {
        _genericAttributeRepository = genericAttributeRepository;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
    }

    public virtual Task<GenericAttribute?> GetAttributeByIdAsync(int attributeId)
    {
        if (attributeId == 0)
            return Task.FromResult<GenericAttribute?>(null);
        return Task.FromResult(_genericAttributeRepository.GetById(attributeId));
    }

    public virtual async Task<IList<GenericAttribute>> GetAttributesForEntityAsync(int entityId, string keyGroup)
    {
        var key = new CacheKey(string.Format(AttributesByEntityKey, entityId, keyGroup), AttributesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = from ga in _genericAttributeRepository.TableNoTracking
                        where ga.EntityId == entityId && ga.KeyGroup == keyGroup
                        select ga;
            return Task.FromResult<IList<GenericAttribute>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task SaveAttributeAsync<TPropType>(BaseEntity entity, string key, TPropType value, int storeId = 0)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(key);

        var keyGroup = entity.GetType().Name;
        var props = await GetAttributesForEntityAsync(entity.Id, keyGroup);
        var prop = props.FirstOrDefault(ga =>
            ga.StoreId == storeId && ga.Key!.Equals(key, StringComparison.InvariantCultureIgnoreCase));

        var valueStr = CommonHelper.To<string>(value!);

        if (prop != null)
        {
            if (string.IsNullOrWhiteSpace(valueStr))
                await DeleteAttributeAsync(prop);
            else
            {
                prop.Value = valueStr;
                await UpdateAttributeAsync(prop);
            }
        }
        else if (!string.IsNullOrWhiteSpace(valueStr))
        {
            prop = new GenericAttribute
            {
                EntityId = entity.Id,
                Key = key,
                KeyGroup = keyGroup,
                Value = valueStr,
                StoreId = storeId,
            };
            await InsertAttributeAsync(prop);
        }
    }

    public virtual async Task InsertAttributeAsync(GenericAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _genericAttributeRepository.Insert(attribute);
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await _eventPublisher.EntityInsertedAsync(attribute);
    }

    public virtual async Task UpdateAttributeAsync(GenericAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _genericAttributeRepository.Update(attribute);
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await _eventPublisher.EntityUpdatedAsync(attribute);
    }

    public virtual async Task DeleteAttributeAsync(GenericAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _genericAttributeRepository.Delete(attribute);
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        await _eventPublisher.EntityDeletedAsync(attribute);
    }

    public virtual async Task DeleteAttributesAsync(IList<GenericAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        _genericAttributeRepository.Delete(attributes);
        await _cacheManager.RemoveByPrefixAsync(AttributesPrefix);
        foreach (var attribute in attributes)
            await _eventPublisher.EntityDeletedAsync(attribute);
    }
}
