using Nop.Core.Domain.Common;

namespace Nop.Services.Common;

public interface IAddressAttributeParser
{
    Task<IList<AddressAttribute>> ParseAddressAttributesAsync(string attributesXml);
    Task<IList<AddressAttributeValue>> ParseAddressAttributeValuesAsync(string attributesXml);
    IList<string> ParseValues(string attributesXml, int addressAttributeId);
    string AddAddressAttribute(string attributesXml, AddressAttribute attribute, string value);
    Task<IList<string>> GetAttributeWarningsAsync(string attributesXml);
}
