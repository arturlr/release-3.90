using Nop.Core.Domain.Customers;

namespace Nop.Services.Customers;

public interface ICustomerAttributeService
{
    Task<IList<CustomerAttribute>> GetAllCustomerAttributesAsync();
    Task<CustomerAttribute?> GetCustomerAttributeByIdAsync(int customerAttributeId);
    Task InsertCustomerAttributeAsync(CustomerAttribute customerAttribute);
    Task UpdateCustomerAttributeAsync(CustomerAttribute customerAttribute);
    Task DeleteCustomerAttributeAsync(CustomerAttribute customerAttribute);
    Task<IList<CustomerAttributeValue>> GetCustomerAttributeValuesAsync(int customerAttributeId);
    Task<CustomerAttributeValue?> GetCustomerAttributeValueByIdAsync(int customerAttributeValueId);
    Task InsertCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue);
    Task UpdateCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue);
    Task DeleteCustomerAttributeValueAsync(CustomerAttributeValue customerAttributeValue);
}
