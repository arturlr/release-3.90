using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Settings;

// --- Store scope ---

public class StoreScopeConfigurationModel
{
    public int StoreId { get; set; }
    public List<StoreItem> Stores { get; set; } = [];

    public class StoreItem
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}

// --- Blog ---

public class BlogSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool Enabled { get; set; }
    public bool Enabled_OverrideForStore { get; set; }

    public int PostsPageSize { get; set; }
    public bool PostsPageSize_OverrideForStore { get; set; }

    public bool AllowNotRegisteredUsersToLeaveComments { get; set; }
    public bool AllowNotRegisteredUsersToLeaveComments_OverrideForStore { get; set; }

    public bool NotifyAboutNewBlogComments { get; set; }
    public bool NotifyAboutNewBlogComments_OverrideForStore { get; set; }

    public int NumberOfTags { get; set; }
    public bool NumberOfTags_OverrideForStore { get; set; }

    public bool ShowHeaderRssUrl { get; set; }
    public bool ShowHeaderRssUrl_OverrideForStore { get; set; }

    public bool BlogCommentsMustBeApproved { get; set; }
    public bool BlogCommentsMustBeApproved_OverrideForStore { get; set; }

    public bool ShowBlogCommentsPerStore { get; set; }
}

// --- Vendor ---

public class VendorSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public string? DefaultVendorPageSizeOptions { get; set; }
    public bool DefaultVendorPageSizeOptions_OverrideForStore { get; set; }

    public int VendorsBlockItemsToDisplay { get; set; }
    public bool VendorsBlockItemsToDisplay_OverrideForStore { get; set; }

    public bool ShowVendorOnProductDetailsPage { get; set; }
    public bool ShowVendorOnProductDetailsPage_OverrideForStore { get; set; }

    public bool AllowCustomersToContactVendors { get; set; }
    public bool AllowCustomersToContactVendors_OverrideForStore { get; set; }

    public bool AllowCustomersToApplyForVendorAccount { get; set; }
    public bool AllowCustomersToApplyForVendorAccount_OverrideForStore { get; set; }

    public bool AllowSearchByVendor { get; set; }
    public bool AllowSearchByVendor_OverrideForStore { get; set; }

    public bool AllowVendorsToEditInfo { get; set; }
    public bool AllowVendorsToEditInfo_OverrideForStore { get; set; }

    public bool NotifyStoreOwnerAboutVendorInformationChange { get; set; }
    public bool NotifyStoreOwnerAboutVendorInformationChange_OverrideForStore { get; set; }

    public int MaximumProductNumber { get; set; }
    public bool MaximumProductNumber_OverrideForStore { get; set; }

    public bool AllowVendorsToImportProducts { get; set; }
    public bool AllowVendorsToImportProducts_OverrideForStore { get; set; }
}

// --- Forum ---

public class ForumSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool ForumsEnabled { get; set; }
    public bool ForumsEnabled_OverrideForStore { get; set; }

    public bool RelativeDateTimeFormattingEnabled { get; set; }
    public bool RelativeDateTimeFormattingEnabled_OverrideForStore { get; set; }

    public bool AllowCustomersToEditPosts { get; set; }
    public bool AllowCustomersToEditPosts_OverrideForStore { get; set; }

    public bool AllowCustomersToManageSubscriptions { get; set; }
    public bool AllowCustomersToManageSubscriptions_OverrideForStore { get; set; }

    public bool AllowGuestsToCreatePosts { get; set; }
    public bool AllowGuestsToCreatePosts_OverrideForStore { get; set; }

    public bool AllowGuestsToCreateTopics { get; set; }
    public bool AllowGuestsToCreateTopics_OverrideForStore { get; set; }

    public bool AllowCustomersToDeletePosts { get; set; }
    public bool AllowCustomersToDeletePosts_OverrideForStore { get; set; }

    public bool AllowPostVoting { get; set; }
    public bool AllowPostVoting_OverrideForStore { get; set; }

    public int MaxVotesPerDay { get; set; }
    public bool MaxVotesPerDay_OverrideForStore { get; set; }

    public int TopicSubjectMaxLength { get; set; }
    public bool TopicSubjectMaxLength_OverrideForStore { get; set; }

    public int PostMaxLength { get; set; }
    public bool PostMaxLength_OverrideForStore { get; set; }

    public int TopicsPageSize { get; set; }
    public bool TopicsPageSize_OverrideForStore { get; set; }

    public int PostsPageSize { get; set; }
    public bool PostsPageSize_OverrideForStore { get; set; }

    public int SearchResultsPageSize { get; set; }
    public bool SearchResultsPageSize_OverrideForStore { get; set; }

    public bool AllowPrivateMessages { get; set; }
    public bool AllowPrivateMessages_OverrideForStore { get; set; }

    public bool ShowAlertForPM { get; set; }
    public bool ShowAlertForPM_OverrideForStore { get; set; }

    public int PrivateMessagesPageSize { get; set; }
    public bool PrivateMessagesPageSize_OverrideForStore { get; set; }

    public bool NotifyAboutPrivateMessages { get; set; }
    public bool NotifyAboutPrivateMessages_OverrideForStore { get; set; }

    public bool SignaturesEnabled { get; set; }
    public bool SignaturesEnabled_OverrideForStore { get; set; }

    public int ForumSearchTermMinimumLength { get; set; }
    public bool ForumSearchTermMinimumLength_OverrideForStore { get; set; }
}

// --- News ---

public class NewsSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool Enabled { get; set; }
    public bool Enabled_OverrideForStore { get; set; }

    public bool AllowNotRegisteredUsersToLeaveComments { get; set; }
    public bool AllowNotRegisteredUsersToLeaveComments_OverrideForStore { get; set; }

    public bool NotifyAboutNewNewsComments { get; set; }
    public bool NotifyAboutNewNewsComments_OverrideForStore { get; set; }

    public bool ShowNewsOnMainPage { get; set; }
    public bool ShowNewsOnMainPage_OverrideForStore { get; set; }

    public int MainPageNewsCount { get; set; }
    public bool MainPageNewsCount_OverrideForStore { get; set; }

    public int NewsArchivePageSize { get; set; }
    public bool NewsArchivePageSize_OverrideForStore { get; set; }

    public bool ShowHeaderRssUrl { get; set; }
    public bool ShowHeaderRssUrl_OverrideForStore { get; set; }

    public bool NewsCommentsMustBeApproved { get; set; }
    public bool NewsCommentsMustBeApproved_OverrideForStore { get; set; }

    public bool ShowNewsCommentsPerStore { get; set; }
}

// --- Catalog ---

public class CatalogSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool AllowViewUnpublishedProductPage { get; set; }
    public bool AllowViewUnpublishedProductPage_OverrideForStore { get; set; }

    public bool ShowSkuOnProductDetailsPage { get; set; }
    public bool ShowSkuOnProductDetailsPage_OverrideForStore { get; set; }

    public bool ShowSkuOnCatalogPages { get; set; }
    public bool ShowSkuOnCatalogPages_OverrideForStore { get; set; }

    public bool ShowManufacturerPartNumber { get; set; }
    public bool ShowManufacturerPartNumber_OverrideForStore { get; set; }

    public bool ShowGtin { get; set; }
    public bool ShowGtin_OverrideForStore { get; set; }

    public bool ShowFreeShippingNotification { get; set; }
    public bool ShowFreeShippingNotification_OverrideForStore { get; set; }

    public bool AllowProductSorting { get; set; }
    public bool AllowProductSorting_OverrideForStore { get; set; }

    public bool AllowProductViewModeChanging { get; set; }
    public bool AllowProductViewModeChanging_OverrideForStore { get; set; }

    public bool ShowProductsFromSubcategories { get; set; }
    public bool ShowProductsFromSubcategories_OverrideForStore { get; set; }

    public bool ShowCategoryProductNumber { get; set; }
    public bool ShowCategoryProductNumber_OverrideForStore { get; set; }

    public bool ShowCategoryProductNumberIncludingSubcategories { get; set; }
    public bool ShowCategoryProductNumberIncludingSubcategories_OverrideForStore { get; set; }

    public bool CategoryBreadcrumbEnabled { get; set; }
    public bool CategoryBreadcrumbEnabled_OverrideForStore { get; set; }

    public bool ShowShareButton { get; set; }
    public bool ShowShareButton_OverrideForStore { get; set; }

    public bool ProductReviewsMustBeApproved { get; set; }
    public bool ProductReviewsMustBeApproved_OverrideForStore { get; set; }

    public bool AllowAnonymousUsersToReviewProduct { get; set; }
    public bool AllowAnonymousUsersToReviewProduct_OverrideForStore { get; set; }

    public bool ProductReviewPossibleOnlyAfterPurchasing { get; set; }
    public bool ProductReviewPossibleOnlyAfterPurchasing_OverrideForStore { get; set; }

    public bool NotifyStoreOwnerAboutNewProductReviews { get; set; }
    public bool NotifyStoreOwnerAboutNewProductReviews_OverrideForStore { get; set; }

    public bool EmailAFriendEnabled { get; set; }
    public bool EmailAFriendEnabled_OverrideForStore { get; set; }

    public bool AllowAnonymousUsersToEmailAFriend { get; set; }
    public bool AllowAnonymousUsersToEmailAFriend_OverrideForStore { get; set; }

    public int RecentlyViewedProductsNumber { get; set; }
    public bool RecentlyViewedProductsNumber_OverrideForStore { get; set; }

    public bool RecentlyViewedProductsEnabled { get; set; }
    public bool RecentlyViewedProductsEnabled_OverrideForStore { get; set; }

    public bool NewProductsEnabled { get; set; }
    public bool NewProductsEnabled_OverrideForStore { get; set; }

    public int NewProductsNumber { get; set; }
    public bool NewProductsNumber_OverrideForStore { get; set; }

    public bool CompareProductsEnabled { get; set; }
    public bool CompareProductsEnabled_OverrideForStore { get; set; }

    public bool ShowBestsellersOnHomepage { get; set; }
    public bool ShowBestsellersOnHomepage_OverrideForStore { get; set; }

    public int NumberOfBestsellersOnHomepage { get; set; }
    public bool NumberOfBestsellersOnHomepage_OverrideForStore { get; set; }

    public int SearchPageProductsPerPage { get; set; }
    public bool SearchPageProductsPerPage_OverrideForStore { get; set; }

    public bool ProductSearchAutoCompleteEnabled { get; set; }
    public bool ProductSearchAutoCompleteEnabled_OverrideForStore { get; set; }

    public int ProductSearchAutoCompleteNumberOfProducts { get; set; }
    public bool ProductSearchAutoCompleteNumberOfProducts_OverrideForStore { get; set; }

    public bool ShowProductImagesInSearchAutoComplete { get; set; }
    public bool ShowProductImagesInSearchAutoComplete_OverrideForStore { get; set; }

    public int ProductSearchTermMinimumLength { get; set; }
    public bool ProductSearchTermMinimumLength_OverrideForStore { get; set; }

    public bool ProductsAlsoPurchasedEnabled { get; set; }
    public bool ProductsAlsoPurchasedEnabled_OverrideForStore { get; set; }

    public int ProductsAlsoPurchasedNumber { get; set; }
    public bool ProductsAlsoPurchasedNumber_OverrideForStore { get; set; }

    public int NumberOfProductTags { get; set; }
    public bool NumberOfProductTags_OverrideForStore { get; set; }

    public int ProductsByTagPageSize { get; set; }
    public bool ProductsByTagPageSize_OverrideForStore { get; set; }

    public bool IncludeShortDescriptionInCompareProducts { get; set; }
    public bool IncludeShortDescriptionInCompareProducts_OverrideForStore { get; set; }

    public bool IncludeFullDescriptionInCompareProducts { get; set; }
    public bool IncludeFullDescriptionInCompareProducts_OverrideForStore { get; set; }

    public bool IgnoreDiscounts { get; set; }
    public bool IgnoreDiscounts_OverrideForStore { get; set; }

    public bool IgnoreAcl { get; set; }
    public bool IgnoreAcl_OverrideForStore { get; set; }

    public bool IgnoreStoreLimitations { get; set; }
    public bool IgnoreStoreLimitations_OverrideForStore { get; set; }

    public bool CacheProductPrices { get; set; }
    public bool CacheProductPrices_OverrideForStore { get; set; }

    public int DefaultCategoryPageSize { get; set; }
    public bool DefaultCategoryPageSize_OverrideForStore { get; set; }

    public string? DefaultCategoryPageSizeOptions { get; set; }
    public bool DefaultCategoryPageSizeOptions_OverrideForStore { get; set; }

    public int DefaultManufacturerPageSize { get; set; }
    public bool DefaultManufacturerPageSize_OverrideForStore { get; set; }

    public string? DefaultManufacturerPageSizeOptions { get; set; }
    public bool DefaultManufacturerPageSizeOptions_OverrideForStore { get; set; }
}

// --- Reward Points ---

public class RewardPointsSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool Enabled { get; set; }
    public bool Enabled_OverrideForStore { get; set; }

    public decimal ExchangeRate { get; set; }
    public bool ExchangeRate_OverrideForStore { get; set; }

    public int MinimumRewardPointsToUse { get; set; }
    public bool MinimumRewardPointsToUse_OverrideForStore { get; set; }

    public int PointsForRegistration { get; set; }
    public bool PointsForRegistration_OverrideForStore { get; set; }

    public decimal PointsForPurchases_Amount { get; set; }
    public bool PointsForPurchases_OverrideForStore { get; set; }

    public int PointsForPurchases_Points { get; set; }

    public int ActivationDelay { get; set; }
    public bool ActivationDelay_OverrideForStore { get; set; }

    public int ActivationDelayPeriodId { get; set; }

    public bool DisplayHowMuchWillBeEarned { get; set; }
    public bool DisplayHowMuchWillBeEarned_OverrideForStore { get; set; }

    public bool PointsAccumulatedForAllStores { get; set; }

    public int PageSize { get; set; }
    public bool PageSize_OverrideForStore { get; set; }
}

// --- Order ---

public class OrderSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool IsReOrderAllowed { get; set; }
    public bool IsReOrderAllowed_OverrideForStore { get; set; }

    public decimal MinOrderSubtotalAmount { get; set; }
    public bool MinOrderSubtotalAmount_OverrideForStore { get; set; }

    public bool MinOrderSubtotalAmountIncludingTax { get; set; }
    public bool MinOrderSubtotalAmountIncludingTax_OverrideForStore { get; set; }

    public decimal MinOrderTotalAmount { get; set; }
    public bool MinOrderTotalAmount_OverrideForStore { get; set; }

    public bool AnonymousCheckoutAllowed { get; set; }
    public bool AnonymousCheckoutAllowed_OverrideForStore { get; set; }

    public bool TermsOfServiceOnShoppingCartPage { get; set; }
    public bool TermsOfServiceOnShoppingCartPage_OverrideForStore { get; set; }

    public bool TermsOfServiceOnOrderConfirmPage { get; set; }
    public bool TermsOfServiceOnOrderConfirmPage_OverrideForStore { get; set; }

    public bool OnePageCheckoutEnabled { get; set; }
    public bool OnePageCheckoutEnabled_OverrideForStore { get; set; }

    public bool DisableBillingAddressCheckoutStep { get; set; }
    public bool DisableBillingAddressCheckoutStep_OverrideForStore { get; set; }

    public bool DisableOrderCompletedPage { get; set; }
    public bool DisableOrderCompletedPage_OverrideForStore { get; set; }

    public bool ReturnRequestsEnabled { get; set; }
    public bool ReturnRequestsEnabled_OverrideForStore { get; set; }

    public bool ReturnRequestsAllowFiles { get; set; }
    public bool ReturnRequestsAllowFiles_OverrideForStore { get; set; }

    public int NumberOfDaysReturnRequestAvailable { get; set; }
    public bool NumberOfDaysReturnRequestAvailable_OverrideForStore { get; set; }

    public string? CustomOrderNumberMask { get; set; }
    public bool CustomOrderNumberMask_OverrideForStore { get; set; }

    public int MinimumOrderPlacementInterval { get; set; }
    public bool MinimumOrderPlacementInterval_OverrideForStore { get; set; }

    public bool CompleteOrderWhenDelivered { get; set; }
    public bool CompleteOrderWhenDelivered_OverrideForStore { get; set; }
}

// --- Shopping Cart ---

public class ShoppingCartSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public bool DisplayCartAfterAddingProduct { get; set; }
    public bool DisplayCartAfterAddingProduct_OverrideForStore { get; set; }

    public bool DisplayWishlistAfterAddingProduct { get; set; }
    public bool DisplayWishlistAfterAddingProduct_OverrideForStore { get; set; }

    public int MaximumShoppingCartItems { get; set; }
    public bool MaximumShoppingCartItems_OverrideForStore { get; set; }

    public int MaximumWishlistItems { get; set; }
    public bool MaximumWishlistItems_OverrideForStore { get; set; }

    public bool AllowOutOfStockItemsToBeAddedToWishlist { get; set; }
    public bool AllowOutOfStockItemsToBeAddedToWishlist_OverrideForStore { get; set; }

    public bool MoveItemsFromWishlistToCart { get; set; }
    public bool MoveItemsFromWishlistToCart_OverrideForStore { get; set; }

    public bool ShowProductImagesOnShoppingCart { get; set; }
    public bool ShowProductImagesOnShoppingCart_OverrideForStore { get; set; }

    public bool ShowProductImagesOnWishList { get; set; }
    public bool ShowProductImagesOnWishList_OverrideForStore { get; set; }

    public bool ShowDiscountBox { get; set; }
    public bool ShowDiscountBox_OverrideForStore { get; set; }

    public bool ShowGiftCardBox { get; set; }
    public bool ShowGiftCardBox_OverrideForStore { get; set; }

    public int CrossSellsNumber { get; set; }
    public bool CrossSellsNumber_OverrideForStore { get; set; }

    public bool EmailWishlistEnabled { get; set; }
    public bool EmailWishlistEnabled_OverrideForStore { get; set; }

    public bool AllowAnonymousUsersToEmailWishlist { get; set; }
    public bool AllowAnonymousUsersToEmailWishlist_OverrideForStore { get; set; }

    public bool MiniShoppingCartEnabled { get; set; }
    public bool MiniShoppingCartEnabled_OverrideForStore { get; set; }

    public int MiniShoppingCartProductNumber { get; set; }
    public bool MiniShoppingCartProductNumber_OverrideForStore { get; set; }

    public bool AllowCartItemEditing { get; set; }
    public bool AllowCartItemEditing_OverrideForStore { get; set; }
}

// --- Media ---

public class MediaSettingsModel : BaseNopModel
{
    public int ActiveStoreScopeConfiguration { get; set; }

    public int AvatarPictureSize { get; set; }
    public bool AvatarPictureSize_OverrideForStore { get; set; }

    public int ProductThumbPictureSize { get; set; }
    public bool ProductThumbPictureSize_OverrideForStore { get; set; }

    public int ProductDetailsPictureSize { get; set; }
    public bool ProductDetailsPictureSize_OverrideForStore { get; set; }

    public int ProductThumbPictureSizeOnProductDetailsPage { get; set; }
    public bool ProductThumbPictureSizeOnProductDetailsPage_OverrideForStore { get; set; }

    public int AssociatedProductPictureSize { get; set; }
    public bool AssociatedProductPictureSize_OverrideForStore { get; set; }

    public int CategoryThumbPictureSize { get; set; }
    public bool CategoryThumbPictureSize_OverrideForStore { get; set; }

    public int ManufacturerThumbPictureSize { get; set; }
    public bool ManufacturerThumbPictureSize_OverrideForStore { get; set; }

    public int CartThumbPictureSize { get; set; }
    public bool CartThumbPictureSize_OverrideForStore { get; set; }

    public int MiniCartThumbPictureSize { get; set; }
    public bool MiniCartThumbPictureSize_OverrideForStore { get; set; }

    public int MaximumImageSize { get; set; }
    public bool MaximumImageSize_OverrideForStore { get; set; }

    public int DefaultImageQuality { get; set; }
    public bool DefaultImageQuality_OverrideForStore { get; set; }

    public bool PicturesStoredIntoDatabase { get; set; }
}

// --- Customer/User ---

public class CustomerUserSettingsModel : BaseNopModel
{
    public CustomerSettingsModel CustomerSettings { get; set; } = new();
    public AddressSettingsModel AddressSettings { get; set; } = new();
    public DateTimeSettingsModel DateTimeSettings { get; set; } = new();
    public ExternalAuthenticationSettingsModel ExternalAuthenticationSettings { get; set; } = new();
}

public class CustomerSettingsModel
{
    public bool UsernamesEnabled { get; set; }
    public bool AllowUsersToChangeUsernames { get; set; }
    public bool CheckUsernameAvailabilityEnabled { get; set; }
    public int UserRegistrationType { get; set; }
    public bool AllowCustomersToUploadAvatars { get; set; }
    public bool DefaultAvatarEnabled { get; set; }
    public bool ShowCustomersLocation { get; set; }
    public bool ShowCustomersJoinDate { get; set; }
    public bool AllowViewingProfiles { get; set; }
    public bool NotifyNewCustomerRegistration { get; set; }
    public bool HideDownloadableProductsTab { get; set; }
    public bool HideBackInStockSubscriptionsTab { get; set; }
    public bool DownloadableProductsValidateUser { get; set; }
    public int CustomerNameFormat { get; set; }
    public bool NewsletterEnabled { get; set; }
    public bool NewsletterTickedByDefault { get; set; }
    public bool HideNewsletterBlock { get; set; }
    public bool NewsletterBlockAllowToUnsubscribe { get; set; }
    public int OnlineCustomerMinutes { get; set; }
    public bool StoreLastVisitedPage { get; set; }
    public bool EnteringEmailTwice { get; set; }
    public bool GenderEnabled { get; set; }
    public bool DateOfBirthEnabled { get; set; }
    public bool DateOfBirthRequired { get; set; }
    public bool CompanyEnabled { get; set; }
    public bool CompanyRequired { get; set; }
    public bool StreetAddressEnabled { get; set; }
    public bool StreetAddressRequired { get; set; }
    public bool StreetAddress2Enabled { get; set; }
    public bool StreetAddress2Required { get; set; }
    public bool ZipPostalCodeEnabled { get; set; }
    public bool ZipPostalCodeRequired { get; set; }
    public bool CityEnabled { get; set; }
    public bool CityRequired { get; set; }
    public bool CountryEnabled { get; set; }
    public bool CountryRequired { get; set; }
    public bool StateProvinceEnabled { get; set; }
    public bool StateProvinceRequired { get; set; }
    public bool PhoneEnabled { get; set; }
    public bool PhoneRequired { get; set; }
    public bool FaxEnabled { get; set; }
    public bool FaxRequired { get; set; }
    public bool AcceptPrivacyPolicyEnabled { get; set; }
}

public class AddressSettingsModel
{
    public bool CompanyEnabled { get; set; }
    public bool CompanyRequired { get; set; }
    public bool StreetAddressEnabled { get; set; }
    public bool StreetAddressRequired { get; set; }
    public bool StreetAddress2Enabled { get; set; }
    public bool StreetAddress2Required { get; set; }
    public bool ZipPostalCodeEnabled { get; set; }
    public bool ZipPostalCodeRequired { get; set; }
    public bool CityEnabled { get; set; }
    public bool CityRequired { get; set; }
    public bool CountryEnabled { get; set; }
    public bool StateProvinceEnabled { get; set; }
    public bool PhoneEnabled { get; set; }
    public bool PhoneRequired { get; set; }
    public bool FaxEnabled { get; set; }
    public bool FaxRequired { get; set; }
}

public class DateTimeSettingsModel
{
    public bool AllowCustomersToSetTimeZone { get; set; }
    public string? DefaultStoreTimeZoneId { get; set; }
    public List<TimeZoneSelectItem> AvailableTimeZones { get; set; } = [];

    public class TimeZoneSelectItem
    {
        public string? Text { get; set; }
        public string? Value { get; set; }
        public bool Selected { get; set; }
    }
}

public class ExternalAuthenticationSettingsModel
{
    public bool AutoRegisterEnabled { get; set; }
}

// --- AllSettings raw CRUD ---

public class AllSettingsListModel : BaseNopModel
{
    public string? SearchSettingName { get; set; }
    public string? SearchSettingValue { get; set; }
}

public class SettingModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Value { get; set; }
    public string? Store { get; set; }
    public int StoreId { get; set; }
}
