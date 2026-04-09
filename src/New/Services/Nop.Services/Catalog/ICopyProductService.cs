using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface ICopyProductService
{
    Task<Product> CopyProductAsync(Product product, string newName,
        bool isPublished = true, bool copyImages = true, bool copyAssociatedProducts = true);
}
