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
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ProductController(
    IProductService productService,
    ICategoryService categoryService,
    IManufacturerService manufacturerService,
    IVendorService vendorService,
    IUrlRecordService urlRecordService,
    IWorkContext workContext,
    ITaxCategoryService taxCategoryService,
    IExportManager exportManager,
    IImportManager importManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    IDateRangeService dateRangeService,
    IShippingService shippingService,
    SeoSettings seoSettings) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var model = new ProductListModel
        {
            IsLoggedInAsVendor = workContext.CurrentVendor is not null
        };

        // Categories
        model.AvailableCategories.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var c in await categoryService.GetAllCategoriesAsync(showHidden: true))
            model.AvailableCategories.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

        // Manufacturers
        model.AvailableManufacturers.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var m in await manufacturerService.GetAllManufacturersAsync(showHidden: true))
            model.AvailableManufacturers.Add(new SelectListItem { Text = m.Name, Value = m.Id.ToString() });

        // Vendors
        model.AvailableVendors.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var v in await vendorService.GetAllVendorsAsync())
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

        // Product types
        model.AvailableProductTypes.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var pt in Enum.GetValues<ProductType>())
            model.AvailableProductTypes.Add(new SelectListItem { Text = pt.ToString(), Value = ((int)pt).ToString() });

        // Published options
        model.AvailablePublishedOptions.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailablePublishedOptions.Add(new SelectListItem { Text = "Published only", Value = "1" });
        model.AvailablePublishedOptions.Add(new SelectListItem { Text = "Unpublished only", Value = "2" });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> ProductList(DataSourceRequest command, ProductListModel model)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var vendorId = 0;
        if (workContext.CurrentVendor is not null)
            vendorId = workContext.CurrentVendor.Id;
        else if (model.SearchVendorId > 0)
            vendorId = model.SearchVendorId;

        var categoryIds = new List<int>();
        if (model.SearchCategoryId > 0)
            categoryIds.Add(model.SearchCategoryId);

        bool? overridePublished = model.SearchPublishedId switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        var products = await productService.SearchProductsAsync(
            categoryIds: categoryIds,
            manufacturerId: model.SearchManufacturerId,
            vendorId: vendorId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true,
            overridePublished: overridePublished);

        var gridModel = new DataSourceResult
        {
            Data = products.Select(p => new ProductGridModel
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ProductTypeName = p.ProductType.ToString(),
                Published = p.Published
            }),
            Total = products.TotalCount
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var model = new ProductModel
        {
            Published = true,
            AllowCustomerReviews = true,
            IsShipEnabled = true,
            OrderMinimumQuantity = 1,
            OrderMaximumQuantity = 10000,
            StockQuantity = 10000,
            NotifyAdminForQuantityBelow = 1,
            Price = 0m,
            VisibleIndividually = true
        };

        await PrepareProductModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var product = MapModelToEntity(model, new Product());
            product.CreatedOnUtc = DateTime.UtcNow;
            product.UpdatedOnUtc = DateTime.UtcNow;

            if (workContext.CurrentVendor is not null)
                product.VendorId = workContext.CurrentVendor.Id;

            await productService.InsertProductAsync(product);

            // URL record
            var seName = await product.ValidateSeNameAsync(
                model.SeName, product.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(product, seName, 0);

            customerActivityService.InsertActivity("AddNewProduct", $"Added a new product ('{product.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = product.Id });

            return RedirectToAction("List");
        }

        await PrepareProductModelDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var product = await productService.GetProductByIdAsync(id);
        if (product is null || product.Deleted)
            return RedirectToAction("List");

        if (workContext.CurrentVendor is not null && product.VendorId != workContext.CurrentVendor.Id)
            return RedirectToAction("List");

        var model = MapEntityToModel(product);
        model.SeName = await urlRecordService.GetActiveSlugAsync(product.Id, "Product", 0);

        await PrepareProductModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProductModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var product = await productService.GetProductByIdAsync(model.Id);
        if (product is null || product.Deleted)
            return RedirectToAction("List");

        if (workContext.CurrentVendor is not null && product.VendorId != workContext.CurrentVendor.Id)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            MapModelToEntity(model, product);
            product.UpdatedOnUtc = DateTime.UtcNow;

            await productService.UpdateProductAsync(product);

            // URL record
            var seName = await product.ValidateSeNameAsync(
                model.SeName, product.Name ?? string.Empty, true,
                urlRecordService, seoSettings);
            await urlRecordService.SaveSlugAsync(product, seName, 0);

            customerActivityService.InsertActivity("EditProduct", $"Edited a product ('{product.Name}')");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = product.Id });

            return RedirectToAction("List");
        }

        await PrepareProductModelDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var product = await productService.GetProductByIdAsync(id);
        if (product is null)
            return RedirectToAction("List");

        if (workContext.CurrentVendor is not null && product.VendorId != workContext.CurrentVendor.Id)
            return RedirectToAction("List");

        await productService.DeleteProductAsync(product);

        customerActivityService.InsertActivity("DeleteProduct", $"Deleted a product ('{product.Name}')");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSelected(IEnumerable<int> selectedIds)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        if (selectedIds is not null)
        {
            var products = await productService.GetProductsByIdsAsync(selectedIds.ToArray());
            foreach (var product in products)
            {
                if (workContext.CurrentVendor is not null && product.VendorId != workContext.CurrentVendor.Id)
                    continue;

                await productService.DeleteProductAsync(product);
            }
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> GoToSku(ProductListModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.GoDirectlyToSku))
        {
            var product = await productService.GetProductBySkuAsync(model.GoDirectlyToSku.Trim());
            if (product is not null)
                return RedirectToAction("Edit", new { id = product.Id });
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(ProductListModel model)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        var vendorId = workContext.CurrentVendor?.Id ?? model.SearchVendorId;
        var products = await SearchProductsFromModelAsync(model, vendorId);

        var bytes = await exportManager.ExportProductsToXlsxAsync(products);
        return File(bytes, MimeTypes.TextXlsx, "products.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> ImportExcel(IFormFile? importexcelfile)
    {
        if (!permissionService.Authorize("ManageProducts"))
            return Forbid();

        if (importexcelfile is not null && importexcelfile.Length > 0)
        {
            await using var stream = importexcelfile.OpenReadStream();
            await importManager.ImportProductsFromXlsxAsync(stream);
            return RedirectToAction("List");
        }

        return RedirectToAction("List");
    }
}
