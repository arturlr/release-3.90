using System.Xml;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public class CheckoutAttributeParser(ICheckoutAttributeService checkoutAttributeService) : ICheckoutAttributeParser
{
    public async Task<IList<CheckoutAttribute>> ParseCheckoutAttributesAsync(string attributesXml)
    {
        var result = new List<CheckoutAttribute>();
        if (string.IsNullOrEmpty(attributesXml))
            return result;

        foreach (var id in ParseAttributeIds(attributesXml))
        {
            var attribute = await checkoutAttributeService.GetCheckoutAttributeByIdAsync(id);
            if (attribute != null)
                result.Add(attribute);
        }
        return result;
    }

    public async Task<IList<CheckoutAttributeValue>> ParseCheckoutAttributeValuesAsync(string attributesXml)
    {
        var values = new List<CheckoutAttributeValue>();
        if (string.IsNullOrEmpty(attributesXml))
            return values;

        var attributes = await ParseCheckoutAttributesAsync(attributesXml);
        foreach (var attribute in attributes)
        {
            if (!ShouldHaveValues(attribute.AttributeControlTypeId))
                continue;

            foreach (var valueStr in ParseValues(attributesXml, attribute.Id))
            {
                if (int.TryParse(valueStr, out var id))
                {
                    var value = await checkoutAttributeService.GetCheckoutAttributeValueByIdAsync(id);
                    if (value != null)
                        values.Add(value);
                }
            }
        }
        return values;
    }

    public IList<string> ParseValues(string attributesXml, int checkoutAttributeId)
    {
        var values = new List<string>();
        if (string.IsNullOrEmpty(attributesXml))
            return values;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute")!)
            {
                if (node.Attributes?["ID"] is not null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == checkoutAttributeId)
                {
                    foreach (XmlNode valueNode in node.SelectNodes(@"CheckoutAttributeValue/Value")!)
                        values.Add(valueNode.InnerText.Trim());
                }
            }
        }
        catch { /* malformed XML — return empty */ }

        return values;
    }

    public string AddCheckoutAttribute(string attributesXml, CheckoutAttribute ca, string value)
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

            var root = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes")!;

            // find or create attribute node
            XmlElement? attrElement = null;
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute")!)
            {
                if (node.Attributes?["ID"] is not null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == ca.Id)
                {
                    attrElement = (XmlElement)node;
                    break;
                }
            }

            if (attrElement == null)
            {
                attrElement = xmlDoc.CreateElement("CheckoutAttribute");
                attrElement.SetAttribute("ID", ca.Id.ToString());
                root.AppendChild(attrElement);
            }

            var valueElement = xmlDoc.CreateElement("CheckoutAttributeValue");
            attrElement.AppendChild(valueElement);
            var innerValue = xmlDoc.CreateElement("Value");
            innerValue.InnerText = value;
            valueElement.AppendChild(innerValue);

            return xmlDoc.OuterXml;
        }
        catch
        {
            return attributesXml;
        }
    }

    public string RemoveCheckoutAttribute(string attributesXml, CheckoutAttribute attribute)
    {
        if (string.IsNullOrEmpty(attributesXml))
            return string.Empty;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);

            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute")!)
            {
                if (node.Attributes?["ID"] is not null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id) &&
                    id == attribute.Id)
                {
                    node.ParentNode!.RemoveChild(node);
                    break;
                }
            }
            return xmlDoc.OuterXml;
        }
        catch
        {
            return attributesXml;
        }
    }

    public async Task<string> EnsureOnlyActiveAttributesAsync(string attributesXml, IList<ShoppingCartItem> cart)
    {
        if (string.IsNullOrEmpty(attributesXml))
            return attributesXml;

        // if cart has any shippable products, keep all attributes
        // without Product nav property, we can't check IsShipEnabled per item — keep all for now
        // TODO: when IProductService is available in this context, check product.IsShipEnabled
        var hasShippable = true; // conservative: assume shippable products exist

        if (!hasShippable)
        {
            var attributes = await ParseCheckoutAttributesAsync(attributesXml);
            var result = attributesXml;
            foreach (var ca in attributes.Where(a => a.ShippableProductRequired))
                result = RemoveCheckoutAttribute(result, ca);
            return result;
        }

        return attributesXml;
    }

    public async Task<bool?> IsConditionMetAsync(CheckoutAttribute attribute, string selectedAttributesXml)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (string.IsNullOrEmpty(attribute.ConditionAttributeXml))
            return null; // no condition

        var dependOnAttribute = (await ParseCheckoutAttributesAsync(attribute.ConditionAttributeXml)).FirstOrDefault();
        if (dependOnAttribute == null)
            return true;

        var requiredValues = ParseValues(attribute.ConditionAttributeXml, dependOnAttribute.Id)
            .Where(x => !string.IsNullOrEmpty(x)).ToList();
        var selectedValues = ParseValues(selectedAttributesXml, dependOnAttribute.Id);

        if (requiredValues.Count != selectedValues.Count)
            return false;

        return requiredValues.All(rv => selectedValues.Contains(rv));
    }

    private static IList<int> ParseAttributeIds(string attributesXml)
    {
        var ids = new List<int>();
        if (string.IsNullOrEmpty(attributesXml))
            return ids;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/CheckoutAttribute")!)
            {
                if (node.Attributes?["ID"] is not null &&
                    int.TryParse(node.Attributes["ID"]!.InnerText.Trim(), out var id))
                    ids.Add(id);
            }
        }
        catch { /* malformed XML */ }

        return ids;
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
