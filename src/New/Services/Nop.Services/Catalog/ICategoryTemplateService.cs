using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface ICategoryTemplateService
{
    Task<CategoryTemplate?> GetCategoryTemplateByIdAsync(int categoryTemplateId);
    Task<IList<CategoryTemplate>> GetAllCategoryTemplatesAsync();
    Task InsertCategoryTemplateAsync(CategoryTemplate categoryTemplate);
    Task UpdateCategoryTemplateAsync(CategoryTemplate categoryTemplate);
    Task DeleteCategoryTemplateAsync(CategoryTemplate categoryTemplate);
}
