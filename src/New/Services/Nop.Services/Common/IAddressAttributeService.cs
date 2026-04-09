using Nop.Core.Domain.Common;

namespace Nop.Services.Common;

public interface IAddressAttributeService
{
    Task<IList<AddressAttribute>> GetAllAddressAttributesAsync();
    Task<AddressAttribute?> GetAddressAttributeByIdAsync(int addressAttributeId);
    Task InsertAddressAttributeAsync(AddressAttribute addressAttribute);
    Task UpdateAddressAttributeAsync(AddressAttribute addressAttribute);
    Task DeleteAddressAttributeAsync(AddressAttribute addressAttribute);
    Task<IList<AddressAttributeValue>> GetAddressAttributeValuesAsync(int addressAttributeId);
    Task<AddressAttributeValue?> GetAddressAttributeValueByIdAsync(int addressAttributeValueId);
    Task InsertAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue);
    Task UpdateAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue);
    Task DeleteAddressAttributeValueAsync(AddressAttributeValue addressAttributeValue);
}
