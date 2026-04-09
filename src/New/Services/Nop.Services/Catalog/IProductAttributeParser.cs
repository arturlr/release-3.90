using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductAttributeParser
{
    // Product attributes
    Task<IList<ProductAttributeMapping>> ParseProductAttributeMappingsAsync(string attributesXml);
    Task<IList<ProductAttributeValue>> ParseProductAttributeValuesAsync(string attributesXml, int productAttributeMappingId = 0);
    IList<string> ParseValues(string attributesXml, int productAttributeMappingId);
    string AddProductAttribute(string attributesXml, ProductAttributeMapping productAttributeMapping, string value, int? quantity = null);
    string RemoveProductAttribute(string attributesXml, ProductAttributeMapping productAttributeMapping);
    Task<bool> AreProductAttributesEqualAsync(string attributesXml1, string attributesXml2, bool ignoreNonCombinableAttributes, bool ignoreQuantity = true);
    Task<bool?> IsConditionMetAsync(ProductAttributeMapping pam, string selectedAttributesXml);
    Task<ProductAttributeCombination?> FindProductAttributeCombinationAsync(Product product, string attributesXml, bool ignoreNonCombinableAttributes = true);
    Task<IList<string>> GenerateAllCombinationsAsync(Product product, bool ignoreNonCombinableAttributes = false);

    // Gift card attributes
    string AddGiftCardAttribute(string attributesXml, string recipientName, string recipientEmail,
        string senderName, string senderEmail, string giftCardMessage);
    void GetGiftCardAttribute(string attributesXml, out string recipientName, out string recipientEmail,
        out string senderName, out string senderEmail, out string giftCardMessage);
}
