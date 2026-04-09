using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public interface ICustomerAttributeParser
{
    Task<IList<CustomerAttribute>> ParseCustomerAttributesAsync(string attributesXml);
    Task<IList<CustomerAttributeValue>> ParseCustomerAttributeValuesAsync(string attributesXml);
    IList<string> ParseValues(string attributesXml, int customerAttributeId);
    string AddCustomerAttribute(string attributesXml, CustomerAttribute attribute, string value);
    Task<IList<string>> GetAttributeWarningsAsync(string attributesXml);
}
