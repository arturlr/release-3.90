using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface ICheckoutAttributeService
{
    Task DeleteCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute);
    Task<IList<CheckoutAttribute>> GetAllCheckoutAttributesAsync(int storeId = 0, bool excludeShippableAttributes = false);
    Task<CheckoutAttribute?> GetCheckoutAttributeByIdAsync(int checkoutAttributeId);
    Task InsertCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute);
    Task UpdateCheckoutAttributeAsync(CheckoutAttribute checkoutAttribute);

    Task DeleteCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue);
    Task<IList<CheckoutAttributeValue>> GetCheckoutAttributeValuesAsync(int checkoutAttributeId);
    Task<CheckoutAttributeValue?> GetCheckoutAttributeValueByIdAsync(int checkoutAttributeValueId);
    Task InsertCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue);
    Task UpdateCheckoutAttributeValueAsync(CheckoutAttributeValue checkoutAttributeValue);
}
