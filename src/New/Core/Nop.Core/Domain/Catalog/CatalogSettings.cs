using Nop.Core.Configuration;

namespace Nop.Core.Domain.Catalog;

public class CatalogSettings : ISettings
{
    public bool AllowViewUnpublishedProductPage { get; set; }
    public bool DisplayDiscontinuedMessageForUnpublishedProducts { get; set; }
    public bool PublishBackProductWhenCancellingOrders { get; set; }
    public bool ShowSkuOnProductDetailsPage { get; set; }
    public bool ShowSkuOnCatalogPages { get; set; }
    public bool ShowManufacturerPartNumber { get; set; }
    public bool ShowGtin { get; set; }
    public bool ShowFreeShippingNotification { get; set; }
    public bool AllowProductSorting { get; set; }
    public bool AllowProductViewModeChanging { get; set; }
    public string? DefaultViewMode { get; set; }
    public bool ShowProductsFromSubcategories { get; set; }
    public bool ShowCategoryProductNumber { get; set; }
    public bool ShowCategoryProductNumberIncludingSubcategories { get; set; }
    public bool CategoryBreadcrumbEnabled { get; set; }
    public bool ShowShareButton { get; set; }
    public string? PageShareCode { get; set; }
    public bool ProductReviewsMustBeApproved { get; set; }
    public int DefaultProductRatingValue { get; set; }
    public bool AllowAnonymousUsersToReviewProduct { get; set; }
    public bool ProductReviewPossibleOnlyAfterPurchasing { get; set; }
    public bool NotifyStoreOwnerAboutNewProductReviews { get; set; }
    public bool ShowProductReviewsPerStore { get; set; }
    public bool ShowProductReviewsTabOnAccountPage { get; set; }
    public int ProductReviewsPageSizeOnAccountPage { get; set; }
    public bool EmailAFriendEnabled { get; set; }
    public bool AllowAnonymousUsersToEmailAFriend { get; set; }
    public int RecentlyViewedProductsNumber { get; set; }
    public bool RecentlyViewedProductsEnabled { get; set; }
    public int NewProductsNumber { get; set; }
    public bool NewProductsEnabled { get; set; }
    public bool CompareProductsEnabled { get; set; }
    public int CompareProductsNumber { get; set; }
    public bool ProductSearchAutoCompleteEnabled { get; set; }
    public int ProductSearchAutoCompleteNumberOfProducts { get; set; }
    public bool ShowProductImagesInSearchAutoComplete { get; set; }
    public int ProductSearchTermMinimumLength { get; set; }
    public bool ShowBestsellersOnHomepage { get; set; }
    public int NumberOfBestsellersOnHomepage { get; set; }
    public int SearchPageProductsPerPage { get; set; }
    public bool SearchPageAllowCustomersToSelectPageSize { get; set; }
    public string? SearchPagePageSizeOptions { get; set; }
    public bool ProductsAlsoPurchasedEnabled { get; set; }
    public int ProductsAlsoPurchasedNumber { get; set; }
    public bool AjaxProcessAttributeChange { get; set; }
    public int NumberOfProductTags { get; set; }
    public int ProductsByTagPageSize { get; set; }
    public bool ProductsByTagAllowCustomersToSelectPageSize { get; set; }
    public string? ProductsByTagPageSizeOptions { get; set; }
    public bool IncludeShortDescriptionInCompareProducts { get; set; }
    public bool IncludeFullDescriptionInCompareProducts { get; set; }
    public bool IncludeFeaturedProductsInNormalLists { get; set; }
    public bool DisplayTierPricesWithDiscounts { get; set; }
    public bool IgnoreDiscounts { get; set; }
    public bool IgnoreFeaturedProducts { get; set; }
    public bool IgnoreAcl { get; set; }
    public bool IgnoreStoreLimitations { get; set; }
    public bool CacheProductPrices { get; set; }
    public int MaximumBackInStockSubscriptions { get; set; }
    public int ManufacturersBlockItemsToDisplay { get; set; }
    public bool DisplayTaxShippingInfoFooter { get; set; }
    public bool DisplayTaxShippingInfoProductDetailsPage { get; set; }
    public bool DisplayTaxShippingInfoProductBoxes { get; set; }
    public bool DisplayTaxShippingInfoShoppingCart { get; set; }
    public bool DisplayTaxShippingInfoWishlist { get; set; }
    public bool DisplayTaxShippingInfoOrderDetailsPage { get; set; }
    public string? DefaultCategoryPageSizeOptions { get; set; }
    public int DefaultCategoryPageSize { get; set; }
    public string? DefaultManufacturerPageSizeOptions { get; set; }
    public int DefaultManufacturerPageSize { get; set; }
    public List<int> ProductSortingEnumDisabled { get; set; } = [];
    public Dictionary<int, int> ProductSortingEnumDisplayOrder { get; set; } = [];
    public bool ExportImportProductAttributes { get; set; }
    public bool ExportImportUseDropdownlistsForAssociatedEntities { get; set; }
}
