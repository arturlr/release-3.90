using System.Xml;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Services.Localization;

namespace Nop.Services.Customers;

public class CustomerAttributeParser : ICustomerAttributeParser
{
    private readonly ICustomerAttributeService _customerAttributeService;
    private readonly ILocalizationService _localizationService;

    public CustomerAttributeParser(
        ICustomerAttributeService customerAttributeService,
        ILocalizationService localizationService)
    {
        _customerAttributeService = customerAttributeService;
        _localizationService = localizationService;
    }

    public virtual async Task<IList<CustomerAttribute>> ParseCustomerAttributesAsync(string attributesXml)
    {
        var result = new List<CustomerAttribute>();
        if (string.IsNullOrEmpty(attributesXml))
            return result;

        foreach (var id in ParseCustomerAttributeIds(attributesXml))
        {
            var attribute = await _customerAttributeService.GetCustomerAttributeByIdAsync(id);
            if (attribute != null)
                result.Add(attribute);
        }
        return result;
    }

    public virtual async Task<IList<CustomerAttributeValue>> ParseCustomerAttributeValuesAsync(string attributesXml)
    {
        var values = new List<CustomerAttributeValue>();
        if (string.IsNullOrEmpty(attributesXml))
            return values;

        var attributes = await ParseCustomerAttributesAsync(attributesXml);
        foreach (var attribute in attributes)
        {
            if (!ShouldHaveValues(attribute))
                continue;

            foreach (var valueStr in ParseValues(attributesXml, attribute.Id))
            {
                if (int.TryParse(valueStr, out var id))
                {
                    var value = await _customerAttributeService.GetCustomerAttributeValueByIdAsync(id);
                    if (value != null)
                        values.Add(value);
                }
            }
        }
        return values;
    }

    public virtual IList<string> ParseValues(string attributesXml, int customerAttributeId)
    {
        var selectedValues = new List<string>();
        if (string.IsNullOrEmpty(attributesXml))
            return selectedValues;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CustomerAttribute")!)
            {
                if (node.Attributes?["ID"] != null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == customerAttributeId)
                {
                    foreach (XmlNode valueNode in node.SelectNodes(@"CustomerAttributeValue/Value")!)
                    {
                        selectedValues.Add(valueNode.InnerText.Trim());
                    }
                }
            }
        }
        catch { /* malformed XML — return empty */ }

        return selectedValues;
    }

    public virtual string AddCustomerAttribute(string attributesXml, CustomerAttribute attribute, string value)
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

            XmlElement? attributeElement = null;
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CustomerAttribute")!)
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
                attributeElement = xmlDoc.CreateElement("CustomerAttribute");
                attributeElement.SetAttribute("ID", attribute.Id.ToString());
                rootElement.AppendChild(attributeElement);
            }

            var valueElement = xmlDoc.CreateElement("CustomerAttributeValue");
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

        var selectedAttributes = await ParseCustomerAttributesAsync(attributesXml);
        var allAttributes = await _customerAttributeService.GetAllCustomerAttributesAsync();

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

    private static IList<int> ParseCustomerAttributeIds(string attributesXml)
    {
        var ids = new List<int>();
        if (string.IsNullOrEmpty(attributesXml))
            return ids;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CustomerAttribute")!)
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

    private static bool ShouldHaveValues(CustomerAttribute attribute) =>
        attribute.AttributeControlType is not (
            AttributeControlType.TextBox or
            AttributeControlType.MultilineTextbox or
            AttributeControlType.Datepicker or
            AttributeControlType.FileUpload);
}
