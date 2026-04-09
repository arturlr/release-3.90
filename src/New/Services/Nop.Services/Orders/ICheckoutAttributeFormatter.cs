using Nop.Core.Domain.Customers;

namespace Nop.Services.Orders;

public interface ICheckoutAttributeFormatter
{
    Task<string> FormatAttributesAsync(string attributesXml, Customer customer,
        string separator = "<br />", bool htmlEncode = true, bool renderPrices = true, bool allowHyperlinks = true);
}
