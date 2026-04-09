using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface ICheckoutAttributeParser
{
    Task<IList<CheckoutAttribute>> ParseCheckoutAttributesAsync(string attributesXml);
    Task<IList<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(string attributesXml);
    IList<string> ParseValues(string attributesXml, int checkoutAttributeId);
    string AddCheckoutAttribute(string attributesXml, CheckoutAttribute ca, string value);
    string RemoveCheckoutAttribute(string attributesXml, CheckoutAttribute attribute);
    Task<string> EnsureOnlyActiveAttributesAsync(string attributesXml, IList<ShoppingCartItem> cart);
    Task<bool?> IsConditionMetAsync(CheckoutAttribute attribute, string selectedAttributesXml);
}
