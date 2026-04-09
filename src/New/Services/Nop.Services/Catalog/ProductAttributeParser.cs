using System.Xml;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public class ProductAttributeParser : IProductAttributeParser
{
    private readonly IProductAttributeService _productAttributeService;

    public ProductAttributeParser(IProductAttributeService productAttributeService)
    {
        _productAttributeService = productAttributeService;
    }

    // Parse product attribute mapping IDs from XML
    private static IList<int> ParseProductAttributeMappingIds(string attributesXml)
    {
        var ids = new List<int>();
        if (string.IsNullOrEmpty(attributesXml)) return ids;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//Attributes/ProductAttribute")!)
            {
                if (node.Attributes?["ID"] != null && int.TryParse(node.Attributes["ID"]!.Value, out var id))
                    ids.Add(id);
            }
        }
        catch { }

        return ids;
    }

    public virtual async Task<IList<ProductAttributeMapping>> ParseProductAttributeMappingsAsync(string attributesXml)
    {
        var result = new List<ProductAttributeMapping>();
        foreach (var id in ParseProductAttributeMappingIds(attributesXml))
        {
            var mapping = await _productAttributeService.GetProductAttributeMappingByIdAsync(id);
            if (mapping != null)
                result.Add(mapping);
        }
        return result;
    }

    public virtual async Task<IList<ProductAttributeValue>> ParseProductAttributeValuesAsync(string attributesXml, int productAttributeMappingId = 0)
    {
        var values = new List<ProductAttributeValue>();
        var mappings = await ParseProductAttributeMappingsAsync(attributesXml);

        foreach (var mapping in mappings)
        {
            if (productAttributeMappingId > 0 && mapping.Id != productAttributeMappingId)
                continue;

            if (!ShouldHaveValues(mapping.AttributeControlTypeId))
                continue;

            foreach (var strValue in ParseValues(attributesXml, mapping.Id))
            {
                if (int.TryParse(strValue, out var valueId))
                {
                    var value = await _productAttributeService.GetProductAttributeValueByIdAsync(valueId);
                    if (value != null)
                        values.Add(value);
                }
            }
        }

        return values;
    }

    public virtual IList<string> ParseValues(string attributesXml, int productAttributeMappingId)
    {
        var values = new List<string>();
        if (string.IsNullOrEmpty(attributesXml)) return values;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);
            foreach (XmlNode node in xmlDoc.SelectNodes($@"//Attributes/ProductAttribute[@ID='{productAttributeMappingId}']/ProductAttributeValue/Value")!)
            {
                if (!string.IsNullOrEmpty(node.InnerText))
                    values.Add(node.InnerText);
            }
        }
        catch { }

        return values;
    }

    public virtual string AddProductAttribute(string attributesXml, ProductAttributeMapping productAttributeMapping, string value, int? quantity = null)
    {
        var result = string.Empty;
        try
        {
            var xmlDoc = new XmlDocument();
            if (string.IsNullOrEmpty(attributesXml))
            {
                var element = xmlDoc.CreateElement("Attributes");
                xmlDoc.AppendChild(element);
            }
            else
            {
                xmlDoc.LoadXml(attributesXml);
            }

            var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes")!;
            XmlElement? attributeElement = null;

            // find existing attribute node or create new
            foreach (XmlNode node in rootElement.SelectNodes(@"ProductAttribute")!)
            {
                if (node.Attributes?["ID"] != null && int.TryParse(node.Attributes["ID"]!.Value, out var id) && id == productAttributeMapping.Id)
                {
                    attributeElement = (XmlElement)node;
                    break;
                }
            }

            if (attributeElement == null)
            {
                attributeElement = xmlDoc.CreateElement("ProductAttribute");
                attributeElement.SetAttribute("ID", productAttributeMapping.Id.ToString());
                rootElement.AppendChild(attributeElement);
            }

            var attributeValueElement = xmlDoc.CreateElement("ProductAttributeValue");
            var valueElement = xmlDoc.CreateElement("Value");
            valueElement.InnerText = value;
            attributeValueElement.AppendChild(valueElement);

            if (quantity.HasValue)
            {
                var quantityElement = xmlDoc.CreateElement("Quantity");
                quantityElement.InnerText = quantity.Value.ToString();
                attributeValueElement.AppendChild(quantityElement);
            }

            attributeElement.AppendChild(attributeValueElement);
            result = xmlDoc.OuterXml;
        }
        catch { }

        return result;
    }

    public virtual string RemoveProductAttribute(string attributesXml, ProductAttributeMapping productAttributeMapping)
    {
        if (string.IsNullOrEmpty(attributesXml)) return string.Empty;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);
            var rootElement = xmlDoc.SelectSingleNode(@"//Attributes");
            if (rootElement == null) return attributesXml;

            foreach (XmlNode node in rootElement.SelectNodes(@"ProductAttribute")!)
            {
                if (node.Attributes?["ID"] != null && int.TryParse(node.Attributes["ID"]!.Value, out var id) && id == productAttributeMapping.Id)
                {
                    rootElement.RemoveChild(node);
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

    public virtual async Task<bool> AreProductAttributesEqualAsync(string attributesXml1, string attributesXml2, bool ignoreNonCombinableAttributes, bool ignoreQuantity = true)
    {
        var attributes1 = await ParseProductAttributeMappingsAsync(attributesXml1);
        var attributes2 = await ParseProductAttributeMappingsAsync(attributesXml2);

        if (ignoreNonCombinableAttributes)
        {
            attributes1 = attributes1.Where(a => ShouldHaveValues(a.AttributeControlTypeId)).ToList();
            attributes2 = attributes2.Where(a => ShouldHaveValues(a.AttributeControlTypeId)).ToList();
        }

        if (attributes1.Count != attributes2.Count)
            return false;

        foreach (var a1 in attributes1)
        {
            var hasEqual = false;
            foreach (var a2 in attributes2)
            {
                if (a1.Id == a2.Id)
                {
                    var values1 = ParseValues(attributesXml1, a1.Id);
                    var values2 = ParseValues(attributesXml2, a2.Id);

                    if (values1.Count == values2.Count && !values1.Except(values2).Any())
                    {
                        hasEqual = true;
                        break;
                    }
                }
            }
            if (!hasEqual)
                return false;
        }

        return true;
    }

    public virtual async Task<bool?> IsConditionMetAsync(ProductAttributeMapping pam, string selectedAttributesXml)
    {
        ArgumentNullException.ThrowIfNull(pam);

        var conditionAttributeXml = pam.ConditionAttributeXml;
        if (string.IsNullOrEmpty(conditionAttributeXml))
            return null; // no condition

        var conditionMappings = await ParseProductAttributeMappingsAsync(conditionAttributeXml);
        if (conditionMappings.Count == 0)
            return null;

        foreach (var conditionMapping in conditionMappings)
        {
            var conditionValues = ParseValues(conditionAttributeXml, conditionMapping.Id)
                .Where(v => !string.IsNullOrEmpty(v)).ToList();

            var selectedValues = ParseValues(selectedAttributesXml, conditionMapping.Id)
                .Where(v => !string.IsNullOrEmpty(v)).ToList();

            // all condition values must be present in selected values
            if (conditionValues.Any(cv => !selectedValues.Contains(cv)))
                return false;
        }

        return true;
    }

    public virtual async Task<ProductAttributeCombination?> FindProductAttributeCombinationAsync(Product product, string attributesXml, bool ignoreNonCombinableAttributes = true)
    {
        ArgumentNullException.ThrowIfNull(product);

        var combinations = await _productAttributeService.GetAllProductAttributeCombinationsAsync(product.Id);
        foreach (var combination in combinations)
        {
            if (await AreProductAttributesEqualAsync(combination.AttributesXml ?? string.Empty, attributesXml, ignoreNonCombinableAttributes))
                return combination;
        }

        return null;
    }

    public virtual async Task<IList<string>> GenerateAllCombinationsAsync(Product product, bool ignoreNonCombinableAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(product);

        var allMappings = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);
        if (ignoreNonCombinableAttributes)
            allMappings = allMappings.Where(m => ShouldHaveValues(m.AttributeControlTypeId)).ToList();

        var allPossibleValues = new List<IList<string>>();
        foreach (var mapping in allMappings)
        {
            var values = new List<string>();
            if (ShouldHaveValues(mapping.AttributeControlTypeId))
            {
                var attrValues = await _productAttributeService.GetProductAttributeValuesAsync(mapping.Id);
                foreach (var v in attrValues)
                    values.Add(v.Id.ToString());
            }
            else
            {
                values.Add(string.Empty); // placeholder for text/file attributes
            }
            allPossibleValues.Add(values);
        }

        // generate cartesian product
        var combinations = new List<string>();
        GenerateCombinationsRecursive(allMappings, allPossibleValues, 0, string.Empty, combinations);
        return combinations;
    }

    private void GenerateCombinationsRecursive(IList<ProductAttributeMapping> mappings, List<IList<string>> allValues,
        int index, string currentXml, List<string> results)
    {
        if (index >= mappings.Count)
        {
            results.Add(currentXml);
            return;
        }

        foreach (var value in allValues[index])
        {
            var xml = AddProductAttribute(currentXml, mappings[index], value);
            GenerateCombinationsRecursive(mappings, allValues, index + 1, xml, results);
        }
    }

    // Gift card attributes

    public virtual string AddGiftCardAttribute(string attributesXml, string recipientName, string recipientEmail,
        string senderName, string senderEmail, string giftCardMessage)
    {
        var result = string.Empty;
        try
        {
            var xmlDoc = new XmlDocument();
            if (string.IsNullOrEmpty(attributesXml))
            {
                var element = xmlDoc.CreateElement("Attributes");
                xmlDoc.AppendChild(element);
            }
            else
            {
                xmlDoc.LoadXml(attributesXml);
            }

            var rootElement = (XmlElement)xmlDoc.SelectSingleNode(@"//Attributes")!;
            var giftCardElement = xmlDoc.CreateElement("GiftCardInfo");

            void AddElement(string name, string value)
            {
                var el = xmlDoc.CreateElement(name);
                el.InnerText = value;
                giftCardElement.AppendChild(el);
            }

            AddElement("RecipientName", recipientName);
            AddElement("RecipientEmail", recipientEmail);
            AddElement("SenderName", senderName);
            AddElement("SenderEmail", senderEmail);
            AddElement("Message", giftCardMessage);

            rootElement.AppendChild(giftCardElement);
            result = xmlDoc.OuterXml;
        }
        catch { }

        return result;
    }

    public virtual void GetGiftCardAttribute(string attributesXml, out string recipientName, out string recipientEmail,
        out string senderName, out string senderEmail, out string giftCardMessage)
    {
        recipientName = recipientEmail = senderName = senderEmail = giftCardMessage = string.Empty;
        if (string.IsNullOrEmpty(attributesXml)) return;

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(attributesXml);
            var giftCardNode = xmlDoc.SelectSingleNode(@"//Attributes/GiftCardInfo");
            if (giftCardNode == null) return;

            recipientName = giftCardNode.SelectSingleNode("RecipientName")?.InnerText ?? string.Empty;
            recipientEmail = giftCardNode.SelectSingleNode("RecipientEmail")?.InnerText ?? string.Empty;
            senderName = giftCardNode.SelectSingleNode("SenderName")?.InnerText ?? string.Empty;
            senderEmail = giftCardNode.SelectSingleNode("SenderEmail")?.InnerText ?? string.Empty;
            giftCardMessage = giftCardNode.SelectSingleNode("Message")?.InnerText ?? string.Empty;
        }
        catch { }
    }

    private static bool ShouldHaveValues(int attributeControlTypeId)
    {
        return attributeControlTypeId != (int)AttributeControlType.TextBox &&
               attributeControlTypeId != (int)AttributeControlType.MultilineTextbox &&
               attributeControlTypeId != (int)AttributeControlType.Datepicker &&
               attributeControlTypeId != (int)AttributeControlType.FileUpload;
    }
}
