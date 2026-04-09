using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductAttributeService
{
    // Product attributes
    Task DeleteProductAttributeAsync(ProductAttribute productAttribute);
    Task<IPagedList<ProductAttribute>> GetAllProductAttributesAsync(int pageIndex = 0, int pageSize = int.MaxValue);
    Task<ProductAttribute?> GetProductAttributeByIdAsync(int productAttributeId);
    Task InsertProductAttributeAsync(ProductAttribute productAttribute);
    Task UpdateProductAttributeAsync(ProductAttribute productAttribute);
    Task<int[]> GetNotExistingAttributesAsync(int[] attributeIds);

    // Product attribute mappings
    Task DeleteProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping);
    Task<IList<ProductAttributeMapping>> GetProductAttributeMappingsByProductIdAsync(int productId);
    Task<ProductAttributeMapping?> GetProductAttributeMappingByIdAsync(int productAttributeMappingId);
    Task InsertProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping);
    Task UpdateProductAttributeMappingAsync(ProductAttributeMapping productAttributeMapping);

    // Product attribute values
    Task DeleteProductAttributeValueAsync(ProductAttributeValue productAttributeValue);
    Task<IList<ProductAttributeValue>> GetProductAttributeValuesAsync(int productAttributeMappingId);
    Task<ProductAttributeValue?> GetProductAttributeValueByIdAsync(int productAttributeValueId);
    Task InsertProductAttributeValueAsync(ProductAttributeValue productAttributeValue);
    Task UpdateProductAttributeValueAsync(ProductAttributeValue productAttributeValue);

    // Predefined product attribute values
    Task DeletePredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav);
    Task<IList<PredefinedProductAttributeValue>> GetPredefinedProductAttributeValuesAsync(int productAttributeId);
    Task<PredefinedProductAttributeValue?> GetPredefinedProductAttributeValueByIdAsync(int id);
    Task InsertPredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav);
    Task UpdatePredefinedProductAttributeValueAsync(PredefinedProductAttributeValue ppav);

    // Product attribute combinations
    Task DeleteProductAttributeCombinationAsync(ProductAttributeCombination combination);
    Task<IList<ProductAttributeCombination>> GetAllProductAttributeCombinationsAsync(int productId);
    Task<ProductAttributeCombination?> GetProductAttributeCombinationByIdAsync(int productAttributeCombinationId);
    Task<ProductAttributeCombination?> GetProductAttributeCombinationBySkuAsync(string sku);
    Task InsertProductAttributeCombinationAsync(ProductAttributeCombination combination);
    Task UpdateProductAttributeCombinationAsync(ProductAttributeCombination combination);
}
