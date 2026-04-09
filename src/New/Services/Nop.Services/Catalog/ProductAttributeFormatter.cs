using System.Net;
using System.Text;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Catalog;

public class ProductAttributeFormatter : IProductAttributeFormatter
{
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly IProductAttributeService _productAttributeService;

    public ProductAttributeFormatter(
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService)
    {
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
    }

    public virtual Task<string> FormatAttributesAsync(Product product, string attributesXml) =>
        FormatAttributesAsync(product, attributesXml, null!);

    public virtual async Task<string> FormatAttributesAsync(Product product, string attributesXml, Customer customer,
        string separator = "<br />", bool htmlEncode = true, bool renderPrices = true,
        bool renderProductAttributes = true, bool renderGiftCardAttributes = true, bool allowHyperlinks = true)
    {
        var result = new StringBuilder();

        if (renderProductAttributes)
        {
            var mappings = await _productAttributeParser.ParseProductAttributeMappingsAsync(attributesXml);
            foreach (var mapping in mappings)
            {
                var productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(mapping.ProductAttributeId);
                if (productAttribute == null) continue;

                var attributeName = productAttribute.Name ?? string.Empty;
                var valuesStr = _productAttributeParser.ParseValues(attributesXml, mapping.Id);

                foreach (var valueStr in valuesStr)
                {
                    var formattedAttribute = string.Empty;

                    if (!ShouldHaveValues(mapping.AttributeControlTypeId))
                    {
                        // text, multiline, datepicker, file upload
                        formattedAttribute = $"{attributeName}: {valueStr}";
                    }
                    else
                    {
                        if (int.TryParse(valueStr, out var valueId))
                        {
                            var attributeValue = await _productAttributeService.GetProductAttributeValueByIdAsync(valueId);
                            if (attributeValue != null)
                            {
                                formattedAttribute = $"{attributeName}: {attributeValue.Name}";
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(formattedAttribute)) continue;

                    if (htmlEncode)
                        formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);

                    if (result.Length > 0)
                        result.Append(separator);
                    result.Append(formattedAttribute);
                }
            }
        }

        if (renderGiftCardAttributes)
        {
            if (product.IsGiftCard)
            {
                _productAttributeParser.GetGiftCardAttribute(attributesXml,
                    out var recipientName, out var recipientEmail,
                    out var senderName, out var senderEmail, out _);

                if (!string.IsNullOrEmpty(recipientName))
                {
                    if (result.Length > 0) result.Append(separator);
                    var val = htmlEncode ? WebUtility.HtmlEncode(recipientName) : recipientName;
                    result.Append($"For: {val}");
                }

                if (!string.IsNullOrEmpty(senderName))
                {
                    if (result.Length > 0) result.Append(separator);
                    var val = htmlEncode ? WebUtility.HtmlEncode(senderName) : senderName;
                    result.Append($"From: {val}");
                }
            }
        }

        return result.ToString();
    }

    private static bool ShouldHaveValues(int attributeControlTypeId)
    {
        return attributeControlTypeId != (int)AttributeControlType.TextBox &&
               attributeControlTypeId != (int)AttributeControlType.MultilineTextbox &&
               attributeControlTypeId != (int)AttributeControlType.Datepicker &&
               attributeControlTypeId != (int)AttributeControlType.FileUpload;
    }
}
