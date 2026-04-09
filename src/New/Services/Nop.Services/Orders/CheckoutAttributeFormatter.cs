using System.Net;
using System.Text;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Catalog;
using Nop.Services.Tax;

namespace Nop.Services.Orders;

public class CheckoutAttributeFormatter(
    ICheckoutAttributeParser checkoutAttributeParser,
    ICheckoutAttributeService checkoutAttributeService,
    IPriceFormatter priceFormatter,
    ITaxService taxService) : ICheckoutAttributeFormatter
{
    public async Task<string> FormatAttributesAsync(string attributesXml, Customer customer,
        string separator = "<br />", bool htmlEncode = true, bool renderPrices = true, bool allowHyperlinks = true)
    {
        var result = new StringBuilder();
        if (string.IsNullOrEmpty(attributesXml))
            return string.Empty;

        var attributes = await checkoutAttributeParser.ParseCheckoutAttributesAsync(attributesXml);
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            var valuesStr = checkoutAttributeParser.ParseValues(attributesXml, attribute.Id);

            for (var j = 0; j < valuesStr.Count; j++)
            {
                var valueStr = valuesStr[j];
                var formattedAttribute = string.Empty;

                if (!ShouldHaveValues(attribute.AttributeControlTypeId))
                {
                    // text, multiline, datepicker, file upload
                    formattedAttribute = $"{attribute.Name}: {valueStr}";
                }
                else
                {
                    if (int.TryParse(valueStr, out var id))
                    {
                        var attributeValue = await checkoutAttributeService.GetCheckoutAttributeValueByIdAsync(id);
                        if (attributeValue != null)
                        {
                            formattedAttribute = $"{attribute.Name}: {attributeValue.Name}";

                            if (renderPrices)
                            {
                                var priceAdjustment = attributeValue.PriceAdjustment;
                                if (priceAdjustment > 0)
                                {
                                    var (adjustedPrice, _) = await taxService.GetCheckoutAttributePriceAsync(
                                        attributeValue, attribute, true, customer);
                                    if (adjustedPrice > 0)
                                    {
                                        var priceStr = await priceFormatter.FormatPriceAsync(adjustedPrice);
                                        formattedAttribute += $" [+{priceStr}]";
                                    }
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(formattedAttribute))
                {
                    if (i != 0 || j != 0)
                        result.Append(separator);
                    result.Append(htmlEncode ? WebUtility.HtmlEncode(formattedAttribute) : formattedAttribute);
                }
            }
        }

        return result.ToString();
    }

    private static bool ShouldHaveValues(int attributeControlTypeId)
    {
        var controlType = (AttributeControlType)attributeControlTypeId;
        return controlType != AttributeControlType.TextBox &&
               controlType != AttributeControlType.MultilineTextbox &&
               controlType != AttributeControlType.Datepicker &&
               controlType != AttributeControlType.FileUpload;
    }
}
