using System.Xml;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Services.Localization;

namespace Nop.Services.Common;

public class AddressAttributeParser : IAddressAttributeParser
{
    private readonly IAddressAttributeService _addressAttributeService;
    private readonly ILocalizationService _localizationService;

    public AddressAttributeParser(
        IAddressAttributeService addressAttributeService,
        ILocalizationService localizationService)
    {
        _addressAttributeService = addressAttributeService;
        _localizationService = localizationService;
    }

    public virtual async Task<IList<AddressAttribute>> ParseAddressAttributesAsync(string attributesXml)
    {
        var result = new List<AddressAttribute>();
        if (string.IsNullOrEmpty(attributesXml))
            return result;

        foreach (var id in ParseAddressAttributeIds(attributesXml))
        {
            var attribute = await _addressAttributeService.GetAddressAttributeByIdAsync(id);
            if (attribute != null)
                result.Add(attribute);
        }
        return result;
    }

    public virtual async Task<IList<AddressAttributeValue>> ParseAddressAttributeValuesAsync(string attributesXml)
    {
        var values = new List<AddressAttributeValue>();
        if (string.IsNullOrEmpty(attributesXml))
            return values;

        var attributes = await ParseAddressAttributesAsync(attributesXml);
        foreach (var attribute in attributes)
        {
            if (!ShouldHaveValues(attribute))
                continue;

            foreach (var valueStr in ParseValues(attributesXml, attribute.Id))
            {
                if (int.TryParse(valueStr, out var id))
                {
                    var value = await _addressAttributeService.GetAddressAttributeValueByIdAsync(id);
                    if (value != null)
                        values.Add(value);
                }
            }
        }
        return values;
    }

    public virtual IList<string> ParseValues(string attributesXml, int addressAttributeId)
    {
        var selectedValues = new List<string>();
        if (string.IsNullOrEmpty(attributesXml))
            return selectedValues;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/AddressAttribute")!)
            {
                if (node.Attributes?["ID"] != null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == addressAttributeId)
                {
                    foreach (XmlNode valueNode in node.SelectNodes(@"AddressAttributeValue/Value")!)
                    {
                        var value = valueNode.InnerText.Trim();
                        selectedValues.Add(value);
                    }
                }
            }
        }
        catch { /* malformed XML — return empty */ }

        return selectedValues;
    }

    public virtual string AddAddressAttribute(string attributesXml, AddressAttribute attribute, string value)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            if (string.IsNullOrEmpty(attributesXml))
            {
                xmlDoc.AppendChild(xmlDoc.CreateElement("Attributes"));
            }
            else
            {
                xmlDoc.LoadXml(attributesXml);
            }

            var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes")!;

            // find existing attribute element or create new
            XmlElement? attributeElement = null;
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/AddressAttribute")!)
            {
                if (node.Attributes?["ID"] != null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == attribute.Id)
                {
                    attributeElement = (XmlElement)node;
                    break;
                }
            }

            if (attributeElement == null)
            {
                attributeElement = xmlDoc.CreateElement("AddressAttribute");
                attributeElement.SetAttribute("ID", attribute.Id.ToString());
                rootElement.AppendChild(attributeElement);
            }

            var valueElement = xmlDoc.CreateElement("AddressAttributeValue");
            attributeElement.AppendChild(valueElement);

            var innerValueElement = xmlDoc.CreateElement("Value");
            innerValueElement.InnerText = value;
            valueElement.AppendChild(innerValueElement);

            return xmlDoc.OuterXml;
        }
        catch
        {
            return attributesXml;
        }
    }

    public virtual async Task<IList<string>> GetAttributeWarningsAsync(string attributesXml)
    {
        var warnings = new List<string>();

        var selectedAttributes = await ParseAddressAttributesAsync(attributesXml);
        var allAttributes = await _addressAttributeService.GetAllAddressAttributesAsync();

        foreach (var required in allAttributes.Where(a => a.IsRequired))
        {
            var found = selectedAttributes.Any(a => a.Id == required.Id) &&
                        ParseValues(attributesXml, required.Id).Any(v => !string.IsNullOrWhiteSpace(v));

            if (!found)
            {
                var name = required.Name ?? string.Empty;
                var warning = string.Format(
                    await _localizationService.GetResourceAsync("ShoppingCart.SelectAttribute"),
                    name);
                warnings.Add(warning);
            }
        }

        return warnings;
    }

    private static IList<int> ParseAddressAttributeIds(string attributesXml)
    {
        var ids = new List<int>();
        if (string.IsNullOrEmpty(attributesXml))
            return ids;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/AddressAttribute")!)
            {
                if (node.Attributes?["ID"] != null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id))
                {
                    ids.Add(id);
                }
            }
        }
        catch { /* malformed XML — return empty */ }

        return ids;
    }

    private static bool ShouldHaveValues(AddressAttribute attribute) =>
        attribute.AttributeControlType is not (
            AttributeControlType.TextBox or
            AttributeControlType.MultilineTextbox or
            AttributeControlType.Datepicker or
            AttributeControlType.FileUpload);
}
