using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductTagService
{
    Task DeleteProductTagAsync(ProductTag productTag);
    Task<IList<ProductTag>> GetAllProductTagsAsync();
    Task<ProductTag?> GetProductTagByIdAsync(int productTagId);
    Task<ProductTag?> GetProductTagByNameAsync(string name);
    Task InsertProductTagAsync(ProductTag productTag);
    Task UpdateProductTagAsync(ProductTag productTag);
    Task<int> GetProductCountAsync(int productTagId, int storeId);
    Task UpdateProductTagsAsync(Product product, string[] productTags);
    Task<IList<ProductTag>> GetProductTagsByProductIdAsync(int productId);
}
