using Nop.Core;
using Nop.Core.Domain.Common;

namespace Nop.Services.Common;

public interface IGenericAttributeService
{
    Task<GenericAttribute?> GetAttributeByIdAsync(int attributeId);
    Task<IList<GenericAttribute>> GetAttributesForEntityAsync(int entityId, string keyGroup);
    Task SaveAttributeAsync<TPropType>(BaseEntity entity, string key, TPropType value, int storeId = 0);
    Task InsertAttributeAsync(GenericAttribute attribute);
    Task UpdateAttributeAsync(GenericAttribute attribute);
    Task DeleteAttributeAsync(GenericAttribute attribute);
    Task DeleteAttributesAsync(IList<GenericAttribute> attributes);
}
