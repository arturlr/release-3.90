using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Catalog;

namespace Nop.Web.Controllers;

public partial class CatalogController(
    ICategoryService categoryService,
    IManufacturerService manufacturerService,
    IProductService productService,
    IVendorService vendorService,
    IProductTagService productTagService,
    ICategoryTemplateService categoryTemplateService,
    IManufacturerTemplateService manufacturerTemplateService,
    IPriceCalculationService priceCalculationService,
    IPriceFormatter priceFormatter,
    IPictureService pictureService,
    IUrlRecordService urlRecordService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ILocalizationService localizationService,
    IGenericAttributeService genericAttributeService,
    IAclService aclService,
    IStoreMappingService storeMappingService,
    IPermissionService permissionService,
    ICustomerActivityService customerActivityService,
    ISearchTermService searchTermService,
    MediaSettings mediaSettings,
    CatalogSettings catalogSettings,
    VendorSettings vendorSettings) : BasePublicController
{
    // ── Categories ──

    public async Task<IActionResult> Category(int categoryId, CatalogPagingFilteringModel command)
    {
        var category = await categoryService.GetCategoryByIdAsync(categoryId);
        if (category == null || category.Deleted)
            return NotFound();

        if ((!category.Published || !aclService.Authorize(category) || !await storeMappingService.AuthorizeAsync(category))
            && !permissionService.Authorize(StandardPermissionProvider.ManageCategories))
            return NotFound();

        // 'Continue shopping' URL
        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.LastContinueShoppingPage,
            HttpContext.Request.Path.ToString(), storeContext.CurrentStore.Id);

        // Activity log
        customerActivityService.InsertActivity(workContext.CurrentCustomer,
            "PublicStore.ViewCategory",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewCategory"),
            category.Name ?? "");

        var model = await PrepareCategoryModelAsync(category, command);

        // Template
        var template = await categoryTemplateService.GetCategoryTemplateByIdAsync(category.CategoryTemplateId);
        var viewPath = template?.ViewPath ?? "Category";
        return View(viewPath, model);
    }

    public async Task<IActionResult> CategoryNavigation(int currentCategoryId, int currentProductId)
    {
        var model = await PrepareCategoryNavigationModelAsync(currentCategoryId);
        return PartialView(model);
    }

    public async Task<IActionResult> TopMenu()
    {
        var model = await PrepareTopMenuModelAsync();
        return PartialView(model);
    }

    public async Task<IActionResult> HomepageCategories()
    {
        var categories = await categoryService.GetAllCategoriesDisplayedOnHomePageAsync();
        if (categories.Count == 0)
            return Content("");

        var models = new List<CategoryModel>();
        foreach (var c in categories)
        {
            var seName = await c.GetSeNameAsync(0, urlRecordService);
            models.Add(new CategoryModel
            {
                Id = c.Id,
                Name = c.Name,
                SeName = seName,
                PictureUrl = c.PictureId > 0
                    ? await pictureService.GetPictureUrlAsync(c.PictureId, mediaSettings.CategoryThumbPictureSize)
                    : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.CategoryThumbPictureSize),
            });
        }
        return PartialView(models);
    }

    // ── Manufacturers ──

    public async Task<IActionResult> Manufacturer(int manufacturerId, CatalogPagingFilteringModel command)
    {
        var manufacturer = await manufacturerService.GetManufacturerByIdAsync(manufacturerId);
        if (manufacturer == null || manufacturer.Deleted)
            return NotFound();

        if ((!manufacturer.Published || !aclService.Authorize(manufacturer) || !await storeMappingService.AuthorizeAsync(manufacturer))
            && !permissionService.Authorize(StandardPermissionProvider.ManageManufacturers))
            return NotFound();

        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.LastContinueShoppingPage,
            HttpContext.Request.Path.ToString(), storeContext.CurrentStore.Id);

        customerActivityService.InsertActivity(workContext.CurrentCustomer,
            "PublicStore.ViewManufacturer",
            await localizationService.GetResourceAsync("ActivityLog.PublicStore.ViewManufacturer"),
            manufacturer.Name ?? "");

        var model = await PrepareManufacturerModelAsync(manufacturer, command);

        var template = await manufacturerTemplateService.GetManufacturerTemplateByIdAsync(manufacturer.ManufacturerTemplateId);
        var viewPath = template?.ViewPath ?? "Manufacturer";
        return View(viewPath, model);
    }

    public async Task<IActionResult> ManufacturerAll()
    {
        var manufacturers = await manufacturerService.GetAllManufacturersAsync(storeId: storeContext.CurrentStore.Id);
        var models = new List<ManufacturerModel>();
        foreach (var m in manufacturers)
        {
            var seName = await m.GetSeNameAsync(0, urlRecordService);
            models.Add(new ManufacturerModel
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                SeName = seName,
                PictureUrl = m.PictureId > 0
                    ? await pictureService.GetPictureUrlAsync(m.PictureId, mediaSettings.ManufacturerThumbPictureSize)
                    : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.ManufacturerThumbPictureSize),
            });
        }
        return View(models);
    }

    public async Task<IActionResult> ManufacturerNavigation(int currentManufacturerId)
    {
        if (catalogSettings.ManufacturersBlockItemsToDisplay == 0)
            return Content("");

        var manufacturers = await manufacturerService.GetAllManufacturersAsync(
            storeId: storeContext.CurrentStore.Id, pageSize: catalogSettings.ManufacturersBlockItemsToDisplay);

        var model = new ManufacturerNavigationModel { TotalManufacturers = manufacturers.TotalCount };
        foreach (var m in manufacturers)
        {
            var seName = await m.GetSeNameAsync(0, urlRecordService);
            model.Manufacturers.Add(new ManufacturerBriefInfoModel
            {
                Id = m.Id,
                Name = m.Name,
                SeName = seName,
                IsActive = currentManufacturerId == m.Id,
            });
        }

        if (model.Manufacturers.Count == 0)
            return Content("");

        return PartialView(model);
    }

    // ── Vendors ──

    public async Task<IActionResult> Vendor(int vendorId, CatalogPagingFilteringModel command)
    {
        var vendor = await vendorService.GetVendorByIdAsync(vendorId);
        if (vendor == null || vendor.Deleted || !vendor.Active)
            return NotFound();

        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.LastContinueShoppingPage,
            HttpContext.Request.Path.ToString(), storeContext.CurrentStore.Id);

        var model = await PrepareVendorModelAsync(vendor, command);
        return View(model);
    }

    public async Task<IActionResult> VendorAll()
    {
        if (vendorSettings.VendorsBlockItemsToDisplay == 0)
            return RedirectToAction("Index", "Home");

        var vendors = await vendorService.GetAllVendorsAsync();
        var models = new List<VendorModel>();
        foreach (var v in vendors)
        {
            var seName = await v.GetSeNameAsync(0, urlRecordService);
            models.Add(new VendorModel
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                SeName = seName,
                PictureUrl = v.PictureId > 0
                    ? await pictureService.GetPictureUrlAsync(v.PictureId, mediaSettings.VendorThumbPictureSize)
                    : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.VendorThumbPictureSize),
            });
        }
        return View(models);
    }

    public async Task<IActionResult> VendorNavigation()
    {
        if (vendorSettings.VendorsBlockItemsToDisplay == 0)
            return Content("");

        var vendors = await vendorService.GetAllVendorsAsync(pageSize: vendorSettings.VendorsBlockItemsToDisplay);
        var model = new VendorNavigationModel { TotalVendors = vendors.TotalCount };
        foreach (var v in vendors)
        {
            var seName = await v.GetSeNameAsync(0, urlRecordService);
            model.Vendors.Add(new VendorBriefInfoModel
            {
                Id = v.Id,
                Name = v.Name,
                SeName = seName,
            });
        }

        if (model.Vendors.Count == 0)
            return Content("");

        return PartialView(model);
    }

    // ── Product Tags ──

    public async Task<IActionResult> PopularProductTags()
    {
        var model = await PreparePopularProductTagsModelAsync(catalogSettings.NumberOfProductTags);
        if (model.Tags.Count == 0)
            return Content("");

        return PartialView(model);
    }

    public async Task<IActionResult> ProductsByTag(int productTagId, CatalogPagingFilteringModel command)
    {
        var productTag = await productTagService.GetProductTagByIdAsync(productTagId);
        if (productTag == null)
            return NotFound();

        var model = await PrepareProductsByTagModelAsync(productTag, command);
        return View(model);
    }

    public async Task<IActionResult> ProductTagsAll()
    {
        var model = await PreparePopularProductTagsModelAsync(int.MaxValue);
        return View(model);
    }

    // ── Search ──

    public async Task<IActionResult> Search(SearchModel model, CatalogPagingFilteringModel command)
    {
        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.LastContinueShoppingPage,
            HttpContext.Request.Path.ToString(), storeContext.CurrentStore.Id);

        model ??= new SearchModel();
        model = await PrepareSearchModelAsync(model, command);
        return View(model);
    }

    public IActionResult SearchBox()
    {
        var model = new SearchBoxModel
        {
            AutoCompleteEnabled = catalogSettings.ProductSearchAutoCompleteEnabled,
            ShowProductImagesInSearchAutoComplete = catalogSettings.ShowProductImagesInSearchAutoComplete,
            SearchTermMinimumLength = catalogSettings.ProductSearchTermMinimumLength,
        };
        return PartialView(model);
    }

    public async Task<IActionResult> SearchTermAutoComplete(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < catalogSettings.ProductSearchTermMinimumLength)
            return Content("");

        var productNumber = catalogSettings.ProductSearchAutoCompleteNumberOfProducts > 0
            ? catalogSettings.ProductSearchAutoCompleteNumberOfProducts : 10;

        var products = await productService.SearchProductsAsync(
            storeId: storeContext.CurrentStore.Id,
            keywords: term,
            languageId: workContext.WorkingLanguage.Id,
            visibleIndividuallyOnly: true,
            pageSize: productNumber);

        var result = new List<object>();
        foreach (var p in products)
        {
            var seName = await p.GetSeNameAsync(0, urlRecordService);
            string? pictureUrl = null;
            if (catalogSettings.ShowProductImagesInSearchAutoComplete)
            {
                var pictures = await pictureService.GetPicturesByProductIdAsync(p.Id, 1);
                pictureUrl = pictures.Count > 0
                    ? await pictureService.GetPictureUrlAsync(pictures[0], mediaSettings.AutoCompleteSearchThumbPictureSize)
                    : await pictureService.GetDefaultPictureUrlAsync(mediaSettings.AutoCompleteSearchThumbPictureSize);
            }
            result.Add(new
            {
                label = p.Name,
                producturl = Url.Action("ProductDetails", "Product", new { productId = p.Id, SeName = seName }),
                productpictureurl = pictureUrl ?? "",
            });
        }
        return Json(result);
    }
}
