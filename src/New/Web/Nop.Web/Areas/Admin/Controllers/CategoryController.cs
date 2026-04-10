using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Services.Catalog;
using Nop.Services.ExportImport;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CategoryController(
    ICategoryService categoryService,
    ICategoryTemplateService categoryTemplateService,
    IUrlRecordService urlRecordService,
    IStoreService storeService,
    IExportManager exportManager,
    IImportManager importManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    SeoSettings seoSettings,
    CatalogSettings catalogSettings) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var model = new CategoryListModel();
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> CategoryList(DataSourceRequest command, CategoryListModel model)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var categories = await categoryService.GetAllCategoriesAsync(
            model.SearchCategoryName ?? string.Empty,
            model.SearchStoreId,
            command.Page - 1,
            command.PageSize,
            showHidden: true);

        var allCategories = await categoryService.GetAllCategoriesAsync(showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = categories.Select(c => new CategoryGridModel
            {
                Id = c.Id,
                Name = c.Name,
                Breadcrumb = GetFormattedBreadCrumb(c, allCategories),
                Published = c.Published,
                DisplayOrder = c.DisplayOrder
            }),
            Total = categories.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var model = new CategoryModel
        {
            PageSize = catalogSettings.DefaultCategoryPageSize > 0 ? catalogSettings.DefaultCategoryPageSize : 6,
            PageSizeOptions = catalogSettings.DefaultCategoryPageSizeOptions ?? "6, 3, 9",
            Published = true,
            IncludeInTopMenu = true,
            AllowCustomersToSelectPageSize = true
        };

        await PrepareCategoryModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var category = MapModelToEntity(model, new Category());
            category.CreatedOnUtc = DateTime.UtcNow;
            category.UpdatedOnUtc = DateTime.UtcNow;

            await categoryService.InsertCategoryAsync(category);

            var seName = await category.ValidateSeNameAsync(
                model.SeName, category.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(category, seName, 0);

            customerActivityService.InsertActivity("AddNewCategory", $"Added a new category ('{category.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = category.Id });

            return RedirectToAction("List");
        }

        await PrepareCategoryModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var category = await categoryService.GetCategoryByIdAsync(id);
        if (category is null || category.Deleted)
            return RedirectToAction("List");

        var model = MapEntityToModel(category);
        model.SeName = await urlRecordService.GetActiveSlugAsync(category.Id, "Category", 0);

        await PrepareCategoryModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var category = await categoryService.GetCategoryByIdAsync(model.Id);
        if (category is null || category.Deleted)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            MapModelToEntity(model, category);
            category.UpdatedOnUtc = DateTime.UtcNow;

            await categoryService.UpdateCategoryAsync(category);

            var seName = await category.ValidateSeNameAsync(
                model.SeName, category.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(category, seName, 0);

            customerActivityService.InsertActivity("EditCategory", $"Edited a category ('{category.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = category.Id });

            return RedirectToAction("List");
        }

        await PrepareCategoryModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var category = await categoryService.GetCategoryByIdAsync(id);
        if (category is null)
            return RedirectToAction("List");

        await categoryService.DeleteCategoryAsync(category);

        customerActivityService.InsertActivity("DeleteCategory", $"Deleted a category ('{category.Name}')");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(IEnumerable<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        if (selectedIds is not null)
        {
            foreach (var id in selectedIds)
            {
                var category = await categoryService.GetCategoryByIdAsync(id);
                if (category is not null)
                    await categoryService.DeleteCategoryAsync(category);
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcel()
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        var categories = (await categoryService.GetAllCategoriesAsync(showHidden: true))
            .Where(c => !c.Deleted)
            .ToList();

        var bytes = await exportManager.ExportCategoriesToXlsxAsync(categories);
        return File(bytes, MimeTypes.TextXlsx, "categories.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile? importexcelfile)
    {
        if (!permissionService.Authorize("ManageCategories"))
            return Forbid();

        if (importexcelfile is not null && importexcelfile.Length > 0)
        {
            await using var stream = importexcelfile.OpenReadStream();
            await importManager.ImportCategoriesFromXlsxAsync(stream);
        }

        return RedirectToAction("List");
    }
}
