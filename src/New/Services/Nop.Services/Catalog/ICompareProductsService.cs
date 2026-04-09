using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface ICompareProductsService
{
    void ClearCompareProducts();
    Task<IList<Product>> GetComparedProductsAsync();
    void RemoveProductFromCompareList(int productId);
    void AddProductToCompareList(int productId);
}
