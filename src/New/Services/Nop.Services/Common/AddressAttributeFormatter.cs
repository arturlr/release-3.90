using System.Net;
using System.Text;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Html;

namespace Nop.Services.Common;

public class AddressAttributeFormatter : IAddressAttributeFormatter
{
    private readonly IWorkContext _workContext;
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly IAddressAttributeParser _addressAttributeParser;

    public AddressAttributeFormatter(
        IWorkContext workContext,
        IAddressAttributeService addressAttributeService,
        IAddressAttributeParser addressAttributeParser)
    {
        _workContext = workContext;
        _addressAttributeService = addressAttributeService;
        _addressAttributeParser = addressAttributeParser;
    }

    public virtual async Task<string> FormatAttributesAsync(string attributesXml,
        string separator = "<br />",
        bool htmlEncode = true)
    {
        var result = new StringBuilder();
        var attributes = await _addressAttributeParser.ParseAddressAttributesAsync(attributesXml);

        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            var valuesStr = _addressAttributeParser.ParseValues(attributesXml, attribute.Id);

            for (var j = 0; j < valuesStr.Count; j++)
            {
                var valueStr = valuesStr[j];
                var formattedAttribute = string.Empty;

                if (!ShouldHaveValues(attribute))
                {
                    // free-text attributes
                    var attributeName = attribute.Name ?? string.Empty;
                    if (htmlEncode)
                        attributeName = WebUtility.HtmlEncode(attributeName);

                    if (attribute.AttributeControlType == AttributeControlType.MultilineTextbox)
                    {
                        formattedAttribute = $"{attributeName}: {HtmlHelper.FormatText(valueStr, false, true, false, false, false, false)}";
                    }
                    else if (attribute.AttributeControlType != AttributeControlType.FileUpload)
                    {
                        formattedAttribute = $"{attributeName}: {valueStr}";
                        if (htmlEncode)
                            formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                    }
                }
                else
                {
                    // predefined values
                    if (int.TryParse(valueStr, out var attributeValueId))
                    {
                        var attributeValue = await _addressAttributeService.GetAddressAttributeValueByIdAsync(attributeValueId);
                        if (attributeValue != null)
                        {
                            formattedAttribute = $"{attribute.Name}: {attributeValue.Name}";
                            if (htmlEncode)
                                formattedAttribute = WebUtility.HtmlEncode(formattedAttribute);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(formattedAttribute))
                {
                    if (i != 0 || j != 0)
                        result.Append(separator);
                    result.Append(formattedAttribute);
                }
            }
        }

        return result.ToString();
    }

    private static bool ShouldHaveValues(AddressAttribute attribute) =>
        attribute.AttributeControlType is not (
            AttributeControlType.TextBox or
            AttributeControlType.MultilineTextbox or
            AttributeControlType.Datepicker or
            AttributeControlType.FileUpload);
}
