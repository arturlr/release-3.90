using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CategoryController
{
    private async Task PrepareCategoryModelDropdownsAsync(CategoryModel model)
    {
        // Parent categories
        var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);
        model.AvailableCategories.Add(new SelectListItem { Text = "[None]", Value = "0" });
        foreach (var c in allCategories)
        {
            if (c.Id != model.Id) // exclude self to prevent circular parent
                model.AvailableCategories.Add(new SelectListItem
                {
                    Text = GetFormattedBreadCrumb(c, allCategories),
                    Value = c.Id.ToString()
                });
        }

        // Category templates
        foreach (var t in await categoryTemplateService.GetAllCategoryTemplatesAsync())
            model.AvailableCategoryTemplates.Add(new SelectListItem { Text = t.Name, Value = t.Id.ToString() });
    }

    private static Category MapModelToEntity(CategoryModel model, Category category)
    {
        category.Name = model.Name;
        category.Description = model.Description;
        category.CategoryTemplateId = model.CategoryTemplateId;
        category.MetaKeywords = model.MetaKeywords;
        category.MetaDescription = model.MetaDescription;
        category.MetaTitle = model.MetaTitle;
        category.ParentCategoryId = model.ParentCategoryId;
        category.PictureId = model.PictureId;
        category.PageSize = model.PageSize;
        category.AllowCustomersToSelectPageSize = model.AllowCustomersToSelectPageSize;
        category.PageSizeOptions = model.PageSizeOptions;
        category.ShowOnHomePage = model.ShowOnHomePage;
        category.IncludeInTopMenu = model.IncludeInTopMenu;
        category.Published = model.Published;
        category.DisplayOrder = model.DisplayOrder;
        return category;
    }

    private static CategoryModel MapEntityToModel(Category category)
    {
        return new CategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CategoryTemplateId = category.CategoryTemplateId,
            MetaKeywords = category.MetaKeywords,
            MetaDescription = category.MetaDescription,
            MetaTitle = category.MetaTitle,
            ParentCategoryId = category.ParentCategoryId,
            PictureId = category.PictureId,
            PageSize = category.PageSize,
            AllowCustomersToSelectPageSize = category.AllowCustomersToSelectPageSize,
            PageSizeOptions = category.PageSizeOptions,
            ShowOnHomePage = category.ShowOnHomePage,
            IncludeInTopMenu = category.IncludeInTopMenu,
            Published = category.Published,
            DisplayOrder = category.DisplayOrder
        };
    }

    private static string GetFormattedBreadCrumb(Category category, IEnumerable<Category> allCategories)
    {
        var lookup = allCategories.ToDictionary(c => c.Id);
        var parts = new List<string>();
        var current = category;
        while (current is not null)
        {
            parts.Add(current.Name ?? string.Empty);
            current = current.ParentCategoryId > 0 && lookup.TryGetValue(current.ParentCategoryId, out var parent)
                ? parent
                : null;
        }
        parts.Reverse();
        return string.Join(" >> ", parts);
    }
}
