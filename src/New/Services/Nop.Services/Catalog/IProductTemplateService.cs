using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IProductTemplateService
{
    Task<ProductTemplate?> GetProductTemplateByIdAsync(int productTemplateId);
    Task<IList<ProductTemplate>> GetAllProductTemplatesAsync();
    Task InsertProductTemplateAsync(ProductTemplate productTemplate);
    Task UpdateProductTemplateAsync(ProductTemplate productTemplate);
    Task DeleteProductTemplateAsync(ProductTemplate productTemplate);
}
