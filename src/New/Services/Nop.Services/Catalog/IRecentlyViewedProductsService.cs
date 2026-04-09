using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IRecentlyViewedProductsService
{
    Task<IList<Product>> GetRecentlyViewedProductsAsync(int number);
    void AddProductToRecentlyViewedList(int productId);
}
