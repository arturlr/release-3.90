using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface ISpecificationAttributeService
{
    // Specification attributes
    Task<SpecificationAttribute?> GetSpecificationAttributeByIdAsync(int specificationAttributeId);
    Task<IPagedList<SpecificationAttribute>> GetSpecificationAttributesAsync(int pageIndex = 0, int pageSize = int.MaxValue);
    Task DeleteSpecificationAttributeAsync(SpecificationAttribute specificationAttribute);
    Task InsertSpecificationAttributeAsync(SpecificationAttribute specificationAttribute);
    Task UpdateSpecificationAttributeAsync(SpecificationAttribute specificationAttribute);

    // Specification attribute options
    Task<SpecificationAttributeOption?> GetSpecificationAttributeOptionByIdAsync(int specificationAttributeOptionId);
    Task<IList<SpecificationAttributeOption>> GetSpecificationAttributeOptionsByIdsAsync(int[] ids);
    Task<IList<SpecificationAttributeOption>> GetSpecificationAttributeOptionsBySpecificationAttributeAsync(int specificationAttributeId);
    Task DeleteSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption);
    Task InsertSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption);
    Task UpdateSpecificationAttributeOptionAsync(SpecificationAttributeOption specificationAttributeOption);

    // Product specification attributes
    Task<IList<ProductSpecificationAttribute>> GetProductSpecificationAttributesAsync(int productId = 0,
        int specificationAttributeOptionId = 0, bool? allowFiltering = null, bool? showOnProductPage = null);
    Task<ProductSpecificationAttribute?> GetProductSpecificationAttributeByIdAsync(int productSpecificationAttributeId);
    Task DeleteProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute);
    Task InsertProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute);
    Task UpdateProductSpecificationAttributeAsync(ProductSpecificationAttribute productSpecificationAttribute);
    Task<int> GetProductSpecificationAttributeCountAsync(int productId = 0, int specificationAttributeOptionId = 0);
}
