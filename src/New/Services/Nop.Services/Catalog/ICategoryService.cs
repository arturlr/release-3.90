using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface ICategoryService
{
    Task DeleteCategoryAsync(Category category);
    Task<IPagedList<Category>> GetAllCategoriesAsync(string categoryName = "", int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Task<IList<Category>> GetAllCategoriesByParentCategoryIdAsync(int parentCategoryId,
        bool showHidden = false, bool includeAllLevels = false);
    Task<IList<Category>> GetAllCategoriesDisplayedOnHomePageAsync(bool showHidden = false);
    Task<Category?> GetCategoryByIdAsync(int categoryId);
    Task InsertCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);

    Task DeleteProductCategoryAsync(ProductCategory productCategory);
    Task<IPagedList<ProductCategory>> GetProductCategoriesByCategoryIdAsync(int categoryId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Task<IList<ProductCategory>> GetProductCategoriesByProductIdAsync(int productId, bool showHidden = false);
    Task<IList<ProductCategory>> GetProductCategoriesByProductIdAsync(int productId, int storeId, bool showHidden = false);
    Task<ProductCategory?> GetProductCategoryByIdAsync(int productCategoryId);
    Task InsertProductCategoryAsync(ProductCategory productCategory);
    Task UpdateProductCategoryAsync(ProductCategory productCategory);

    Task<string[]> GetNotExistingCategoriesAsync(string[] categoryNames);
    Task<IDictionary<int, int[]>> GetProductCategoryIdsAsync(int[] productIds);
}
