using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Catalog;

public interface IProductAttributeFormatter
{
    Task<string> FormatAttributesAsync(Product product, string attributesXml);
    Task<string> FormatAttributesAsync(Product product, string attributesXml, Customer customer,
        string separator = "<br />", bool htmlEncode = true, bool renderPrices = true,
        bool renderProductAttributes = true, bool renderGiftCardAttributes = true, bool allowHyperlinks = true);
}
