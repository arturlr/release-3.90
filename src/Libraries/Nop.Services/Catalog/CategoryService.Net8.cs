using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Catalog;
using Nop.Data;

namespace Nop.Services.Catalog
{
    public class CategoryService : ICategoryService
    {
        private readonly IRepository<Category> _categoryRepository;

        public CategoryService(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public virtual async Task<Category> GetCategoryByIdAsync(int categoryId)
        {
            if (categoryId == 0)
                return null;

            return await _categoryRepository.GetByIdAsync(categoryId);
        }

        public virtual async Task<IList<Category>> GetAllCategoriesAsync()
        {
            var query = _categoryRepository.Table
                .Where(c => !c.Deleted && c.Published)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            return await query.ToListAsync();
        }

        public virtual async Task<IList<Category>> GetCategoriesByParentCategoryIdAsync(int parentCategoryId)
        {
            var query = _categoryRepository.Table
                .Where(c => c.ParentCategoryId == parentCategoryId && !c.Deleted && c.Published)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            return await query.ToListAsync();
        }

        public virtual async Task InsertCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            await _categoryRepository.InsertAsync(category);
        }

        public virtual async Task UpdateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            await _categoryRepository.UpdateAsync(category);
        }

        public virtual async Task DeleteCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            category.Deleted = true;
            await _categoryRepository.UpdateAsync(category);
        }
    }
}
