using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Settings;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SettingController
{
    public async Task<IActionResult> Catalog()
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<CatalogSettings>(storeScope);
        var model = new CatalogSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            AllowViewUnpublishedProductPage = s.AllowViewUnpublishedProductPage,
            ShowSkuOnProductDetailsPage = s.ShowSkuOnProductDetailsPage,
            ShowSkuOnCatalogPages = s.ShowSkuOnCatalogPages,
            ShowManufacturerPartNumber = s.ShowManufacturerPartNumber,
            ShowGtin = s.ShowGtin,
            ShowFreeShippingNotification = s.ShowFreeShippingNotification,
            AllowProductSorting = s.AllowProductSorting,
            AllowProductViewModeChanging = s.AllowProductViewModeChanging,
            ShowProductsFromSubcategories = s.ShowProductsFromSubcategories,
            ShowCategoryProductNumber = s.ShowCategoryProductNumber,
            ShowCategoryProductNumberIncludingSubcategories = s.ShowCategoryProductNumberIncludingSubcategories,
            CategoryBreadcrumbEnabled = s.CategoryBreadcrumbEnabled,
            ShowShareButton = s.ShowShareButton,
            ProductReviewsMustBeApproved = s.ProductReviewsMustBeApproved,
            AllowAnonymousUsersToReviewProduct = s.AllowAnonymousUsersToReviewProduct,
            ProductReviewPossibleOnlyAfterPurchasing = s.ProductReviewPossibleOnlyAfterPurchasing,
            NotifyStoreOwnerAboutNewProductReviews = s.NotifyStoreOwnerAboutNewProductReviews,
            EmailAFriendEnabled = s.EmailAFriendEnabled,
            AllowAnonymousUsersToEmailAFriend = s.AllowAnonymousUsersToEmailAFriend,
            RecentlyViewedProductsNumber = s.RecentlyViewedProductsNumber,
            RecentlyViewedProductsEnabled = s.RecentlyViewedProductsEnabled,
            NewProductsEnabled = s.NewProductsEnabled,
            NewProductsNumber = s.NewProductsNumber,
            CompareProductsEnabled = s.CompareProductsEnabled,
            ShowBestsellersOnHomepage = s.ShowBestsellersOnHomepage,
            NumberOfBestsellersOnHomepage = s.NumberOfBestsellersOnHomepage,
            SearchPageProductsPerPage = s.SearchPageProductsPerPage,
            ProductSearchAutoCompleteEnabled = s.ProductSearchAutoCompleteEnabled,
            ProductSearchAutoCompleteNumberOfProducts = s.ProductSearchAutoCompleteNumberOfProducts,
            ShowProductImagesInSearchAutoComplete = s.ShowProductImagesInSearchAutoComplete,
            ProductSearchTermMinimumLength = s.ProductSearchTermMinimumLength,
            ProductsAlsoPurchasedEnabled = s.ProductsAlsoPurchasedEnabled,
            ProductsAlsoPurchasedNumber = s.ProductsAlsoPurchasedNumber,
            NumberOfProductTags = s.NumberOfProductTags,
            ProductsByTagPageSize = s.ProductsByTagPageSize,
            IncludeShortDescriptionInCompareProducts = s.IncludeShortDescriptionInCompareProducts,
            IncludeFullDescriptionInCompareProducts = s.IncludeFullDescriptionInCompareProducts,
            IgnoreDiscounts = s.IgnoreDiscounts,
            IgnoreAcl = s.IgnoreAcl,
            IgnoreStoreLimitations = s.IgnoreStoreLimitations,
            CacheProductPrices = s.CacheProductPrices,
            DefaultCategoryPageSize = s.DefaultCategoryPageSize,
            DefaultCategoryPageSizeOptions = s.DefaultCategoryPageSizeOptions,
            DefaultManufacturerPageSize = s.DefaultManufacturerPageSize,
            DefaultManufacturerPageSizeOptions = s.DefaultManufacturerPageSizeOptions
        };
        if (storeScope > 0)
        {
            model.AllowViewUnpublishedProductPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowViewUnpublishedProductPage, storeScope);
            model.ShowSkuOnProductDetailsPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowSkuOnProductDetailsPage, storeScope);
            model.ShowSkuOnCatalogPages_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowSkuOnCatalogPages, storeScope);
            model.ShowManufacturerPartNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowManufacturerPartNumber, storeScope);
            model.ShowGtin_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowGtin, storeScope);
            model.ShowFreeShippingNotification_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowFreeShippingNotification, storeScope);
            model.AllowProductSorting_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowProductSorting, storeScope);
            model.AllowProductViewModeChanging_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowProductViewModeChanging, storeScope);
            model.ShowProductsFromSubcategories_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowProductsFromSubcategories, storeScope);
            model.ShowCategoryProductNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowCategoryProductNumber, storeScope);
            model.ShowCategoryProductNumberIncludingSubcategories_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowCategoryProductNumberIncludingSubcategories, storeScope);
            model.CategoryBreadcrumbEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CategoryBreadcrumbEnabled, storeScope);
            model.ShowShareButton_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowShareButton, storeScope);
            model.ProductReviewsMustBeApproved_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductReviewsMustBeApproved, storeScope);
            model.AllowAnonymousUsersToReviewProduct_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowAnonymousUsersToReviewProduct, storeScope);
            model.ProductReviewPossibleOnlyAfterPurchasing_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductReviewPossibleOnlyAfterPurchasing, storeScope);
            model.NotifyStoreOwnerAboutNewProductReviews_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NotifyStoreOwnerAboutNewProductReviews, storeScope);
            model.EmailAFriendEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.EmailAFriendEnabled, storeScope);
            model.AllowAnonymousUsersToEmailAFriend_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowAnonymousUsersToEmailAFriend, storeScope);
            model.RecentlyViewedProductsNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.RecentlyViewedProductsNumber, storeScope);
            model.RecentlyViewedProductsEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.RecentlyViewedProductsEnabled, storeScope);
            model.NewProductsEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NewProductsEnabled, storeScope);
            model.NewProductsNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NewProductsNumber, storeScope);
            model.CompareProductsEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CompareProductsEnabled, storeScope);
            model.ShowBestsellersOnHomepage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowBestsellersOnHomepage, storeScope);
            model.NumberOfBestsellersOnHomepage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NumberOfBestsellersOnHomepage, storeScope);
            model.SearchPageProductsPerPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.SearchPageProductsPerPage, storeScope);
            model.ProductSearchAutoCompleteEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductSearchAutoCompleteEnabled, storeScope);
            model.ProductSearchAutoCompleteNumberOfProducts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductSearchAutoCompleteNumberOfProducts, storeScope);
            model.ShowProductImagesInSearchAutoComplete_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowProductImagesInSearchAutoComplete, storeScope);
            model.ProductSearchTermMinimumLength_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductSearchTermMinimumLength, storeScope);
            model.ProductsAlsoPurchasedEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductsAlsoPurchasedEnabled, storeScope);
            model.ProductsAlsoPurchasedNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductsAlsoPurchasedNumber, storeScope);
            model.NumberOfProductTags_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NumberOfProductTags, storeScope);
            model.ProductsByTagPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductsByTagPageSize, storeScope);
            model.IncludeShortDescriptionInCompareProducts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IncludeShortDescriptionInCompareProducts, storeScope);
            model.IncludeFullDescriptionInCompareProducts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IncludeFullDescriptionInCompareProducts, storeScope);
            model.IgnoreDiscounts_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IgnoreDiscounts, storeScope);
            model.IgnoreAcl_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IgnoreAcl, storeScope);
            model.IgnoreStoreLimitations_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IgnoreStoreLimitations, storeScope);
            model.CacheProductPrices_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CacheProductPrices, storeScope);
            model.DefaultCategoryPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultCategoryPageSize, storeScope);
            model.DefaultCategoryPageSizeOptions_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultCategoryPageSizeOptions, storeScope);
            model.DefaultManufacturerPageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultManufacturerPageSize, storeScope);
            model.DefaultManufacturerPageSizeOptions_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultManufacturerPageSizeOptions, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Catalog(CatalogSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Forbid();

        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<CatalogSettings>(storeScope);
        s.AllowViewUnpublishedProductPage = model.AllowViewUnpublishedProductPage;
        s.ShowSkuOnProductDetailsPage = model.ShowSkuOnProductDetailsPage;
        s.ShowSkuOnCatalogPages = model.ShowSkuOnCatalogPages;
        s.ShowManufacturerPartNumber = model.ShowManufacturerPartNumber;
        s.ShowGtin = model.ShowGtin;
        s.ShowFreeShippingNotification = model.ShowFreeShippingNotification;
        s.AllowProductSorting = model.AllowProductSorting;
        s.AllowProductViewModeChanging = model.AllowProductViewModeChanging;
        s.ShowProductsFromSubcategories = model.ShowProductsFromSubcategories;
        s.ShowCategoryProductNumber = model.ShowCategoryProductNumber;
        s.ShowCategoryProductNumberIncludingSubcategories = model.ShowCategoryProductNumberIncludingSubcategories;
        s.CategoryBreadcrumbEnabled = model.CategoryBreadcrumbEnabled;
        s.ShowShareButton = model.ShowShareButton;
        s.ProductReviewsMustBeApproved = model.ProductReviewsMustBeApproved;
        s.AllowAnonymousUsersToReviewProduct = model.AllowAnonymousUsersToReviewProduct;
        s.ProductReviewPossibleOnlyAfterPurchasing = model.ProductReviewPossibleOnlyAfterPurchasing;
        s.NotifyStoreOwnerAboutNewProductReviews = model.NotifyStoreOwnerAboutNewProductReviews;
        s.EmailAFriendEnabled = model.EmailAFriendEnabled;
        s.AllowAnonymousUsersToEmailAFriend = model.AllowAnonymousUsersToEmailAFriend;
        s.RecentlyViewedProductsNumber = model.RecentlyViewedProductsNumber;
        s.RecentlyViewedProductsEnabled = model.RecentlyViewedProductsEnabled;
        s.NewProductsEnabled = model.NewProductsEnabled;
        s.NewProductsNumber = model.NewProductsNumber;
        s.CompareProductsEnabled = model.CompareProductsEnabled;
        s.ShowBestsellersOnHomepage = model.ShowBestsellersOnHomepage;
        s.NumberOfBestsellersOnHomepage = model.NumberOfBestsellersOnHomepage;
        s.SearchPageProductsPerPage = model.SearchPageProductsPerPage;
        s.ProductSearchAutoCompleteEnabled = model.ProductSearchAutoCompleteEnabled;
        s.ProductSearchAutoCompleteNumberOfProducts = model.ProductSearchAutoCompleteNumberOfProducts;
        s.ShowProductImagesInSearchAutoComplete = model.ShowProductImagesInSearchAutoComplete;
        s.ProductSearchTermMinimumLength = model.ProductSearchTermMinimumLength;
        s.ProductsAlsoPurchasedEnabled = model.ProductsAlsoPurchasedEnabled;
        s.ProductsAlsoPurchasedNumber = model.ProductsAlsoPurchasedNumber;
        s.NumberOfProductTags = model.NumberOfProductTags;
        s.ProductsByTagPageSize = model.ProductsByTagPageSize;
        s.IncludeShortDescriptionInCompareProducts = model.IncludeShortDescriptionInCompareProducts;
        s.IncludeFullDescriptionInCompareProducts = model.IncludeFullDescriptionInCompareProducts;
        s.IgnoreDiscounts = model.IgnoreDiscounts;
        s.IgnoreAcl = model.IgnoreAcl;
        s.IgnoreStoreLimitations = model.IgnoreStoreLimitations;
        s.CacheProductPrices = model.CacheProductPrices;
        s.DefaultCategoryPageSize = model.DefaultCategoryPageSize;
        s.DefaultCategoryPageSizeOptions = model.DefaultCategoryPageSizeOptions;
        s.DefaultManufacturerPageSize = model.DefaultManufacturerPageSize;
        s.DefaultManufacturerPageSizeOptions = model.DefaultManufacturerPageSizeOptions;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowViewUnpublishedProductPage, model.AllowViewUnpublishedProductPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowSkuOnProductDetailsPage, model.ShowSkuOnProductDetailsPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowSkuOnCatalogPages, model.ShowSkuOnCatalogPages_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowManufacturerPartNumber, model.ShowManufacturerPartNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowGtin, model.ShowGtin_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowFreeShippingNotification, model.ShowFreeShippingNotification_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowProductSorting, model.AllowProductSorting_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowProductViewModeChanging, model.AllowProductViewModeChanging_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowProductsFromSubcategories, model.ShowProductsFromSubcategories_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowCategoryProductNumber, model.ShowCategoryProductNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowCategoryProductNumberIncludingSubcategories, model.ShowCategoryProductNumberIncludingSubcategories_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CategoryBreadcrumbEnabled, model.CategoryBreadcrumbEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowShareButton, model.ShowShareButton_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductReviewsMustBeApproved, model.ProductReviewsMustBeApproved_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowAnonymousUsersToReviewProduct, model.AllowAnonymousUsersToReviewProduct_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductReviewPossibleOnlyAfterPurchasing, model.ProductReviewPossibleOnlyAfterPurchasing_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NotifyStoreOwnerAboutNewProductReviews, model.NotifyStoreOwnerAboutNewProductReviews_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.EmailAFriendEnabled, model.EmailAFriendEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowAnonymousUsersToEmailAFriend, model.AllowAnonymousUsersToEmailAFriend_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.RecentlyViewedProductsNumber, model.RecentlyViewedProductsNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.RecentlyViewedProductsEnabled, model.RecentlyViewedProductsEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NewProductsEnabled, model.NewProductsEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NewProductsNumber, model.NewProductsNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CompareProductsEnabled, model.CompareProductsEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowBestsellersOnHomepage, model.ShowBestsellersOnHomepage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NumberOfBestsellersOnHomepage, model.NumberOfBestsellersOnHomepage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.SearchPageProductsPerPage, model.SearchPageProductsPerPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductSearchAutoCompleteEnabled, model.ProductSearchAutoCompleteEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductSearchAutoCompleteNumberOfProducts, model.ProductSearchAutoCompleteNumberOfProducts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowProductImagesInSearchAutoComplete, model.ShowProductImagesInSearchAutoComplete_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductSearchTermMinimumLength, model.ProductSearchTermMinimumLength_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductsAlsoPurchasedEnabled, model.ProductsAlsoPurchasedEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductsAlsoPurchasedNumber, model.ProductsAlsoPurchasedNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NumberOfProductTags, model.NumberOfProductTags_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductsByTagPageSize, model.ProductsByTagPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IncludeShortDescriptionInCompareProducts, model.IncludeShortDescriptionInCompareProducts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IncludeFullDescriptionInCompareProducts, model.IncludeFullDescriptionInCompareProducts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IgnoreDiscounts, model.IgnoreDiscounts_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IgnoreAcl, model.IgnoreAcl_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IgnoreStoreLimitations, model.IgnoreStoreLimitations_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CacheProductPrices, model.CacheProductPrices_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultCategoryPageSize, model.DefaultCategoryPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultCategoryPageSizeOptions, model.DefaultCategoryPageSizeOptions_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultManufacturerPageSize, model.DefaultManufacturerPageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultManufacturerPageSizeOptions, model.DefaultManufacturerPageSizeOptions_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Catalog");
    }
}
