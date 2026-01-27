using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    public interface ICategoryService
    {
        Task<Category> GetCategoryByIdAsync(int categoryId);
        Task<IList<Category>> GetAllCategoriesAsync();
        Task<IList<Category>> GetCategoriesByParentCategoryIdAsync(int parentCategoryId);
        Task InsertCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(Category category);
    }
}
