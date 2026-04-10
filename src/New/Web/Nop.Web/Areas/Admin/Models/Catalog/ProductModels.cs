using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Catalog;

public class ProductListModel : BaseNopModel
{
    public string? SearchProductName { get; set; }
    public int SearchCategoryId { get; set; }
    public int SearchManufacturerId { get; set; }
    public int SearchVendorId { get; set; }
    public int SearchProductTypeId { get; set; }
    public int SearchPublishedId { get; set; }
    public string? GoDirectlyToSku { get; set; }

    public List<SelectListItem> AvailableCategories { get; set; } = [];
    public List<SelectListItem> AvailableManufacturers { get; set; } = [];
    public List<SelectListItem> AvailableVendors { get; set; } = [];
    public List<SelectListItem> AvailableProductTypes { get; set; } = [];
    public List<SelectListItem> AvailablePublishedOptions { get; set; } = [];

    public bool IsLoggedInAsVendor { get; set; }
}

public class ProductModel : BaseNopEntityModel
{
    // Basic info
    public int ProductTypeId { get; set; }
    public string? ProductTypeName { get; set; }
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? FullDescription { get; set; }
    public string? AdminComment { get; set; }
    public bool ShowOnHomePage { get; set; }
    public string? MetaKeywords { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaTitle { get; set; }
    public string? SeName { get; set; }
    public bool AllowCustomerReviews { get; set; }

    // Price
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public decimal ProductCost { get; set; }
    public bool CustomerEntersPrice { get; set; }
    public decimal MinimumCustomerEnteredPrice { get; set; }
    public decimal MaximumCustomerEnteredPrice { get; set; }
    public bool BasepriceEnabled { get; set; }
    public decimal BasepriceAmount { get; set; }
    public int BasepriceUnitId { get; set; }
    public decimal BasepriceBaseAmount { get; set; }
    public int BasepriceBaseUnitId { get; set; }
    public bool DisableBuyButton { get; set; }
    public bool DisableWishlistButton { get; set; }
    public bool AvailableForPreOrder { get; set; }
    public DateTime? PreOrderAvailabilityStartDateTimeUtc { get; set; }
    public bool CallForPrice { get; set; }

    // Shipping
    public bool IsShipEnabled { get; set; }
    public bool IsFreeShipping { get; set; }
    public bool ShipSeparately { get; set; }
    public decimal AdditionalShippingCharge { get; set; }
    public int DeliveryDateId { get; set; }

    // Tax
    public bool IsTaxExempt { get; set; }
    public int TaxCategoryId { get; set; }

    // Inventory
    public int ManageInventoryMethodId { get; set; }
    public int StockQuantity { get; set; }
    public bool DisplayStockAvailability { get; set; }
    public bool DisplayStockQuantity { get; set; }
    public int MinStockQuantity { get; set; }
    public int LowStockActivityId { get; set; }
    public int NotifyAdminForQuantityBelow { get; set; }
    public int BackorderModeId { get; set; }
    public bool AllowBackInStockSubscriptions { get; set; }
    public int OrderMinimumQuantity { get; set; }
    public int OrderMaximumQuantity { get; set; }
    public int WarehouseId { get; set; }

    // SKU/GTIN
    public string? Sku { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Gtin { get; set; }

    // Weight/dimensions
    public decimal Weight { get; set; }
    public decimal Length { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }

    // Misc
    public bool Published { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime UpdatedOnUtc { get; set; }

    // Vendor
    public int VendorId { get; set; }
    public string? VendorName { get; set; }

    // Gift card
    public bool IsGiftCard { get; set; }
    public int GiftCardTypeId { get; set; }

    // Download
    public bool IsDownload { get; set; }
    public int DownloadId { get; set; }

    // Recurring
    public bool IsRecurring { get; set; }
    public int RecurringCycleLength { get; set; }
    public int RecurringCyclePeriodId { get; set; }
    public int RecurringTotalCycles { get; set; }

    // Rental
    public bool IsRental { get; set; }
    public int RentalPriceLength { get; set; }
    public int RentalPricePeriodId { get; set; }

    // Mark as new
    public bool MarkAsNew { get; set; }
    public DateTime? MarkAsNewStartDateTimeUtc { get; set; }
    public DateTime? MarkAsNewEndDateTimeUtc { get; set; }

    // Availability
    public DateTime? AvailableStartDateTimeUtc { get; set; }
    public DateTime? AvailableEndDateTimeUtc { get; set; }
    public int DisplayOrder { get; set; }
    public bool VisibleIndividually { get; set; }

    // Dropdowns
    public List<SelectListItem> AvailableProductTypes { get; set; } = [];
    public List<SelectListItem> AvailableTaxCategories { get; set; } = [];
    public List<SelectListItem> AvailableDeliveryDates { get; set; } = [];
    public List<SelectListItem> AvailableWarehouses { get; set; } = [];
    public List<SelectListItem> AvailableVendors { get; set; } = [];

    // Copy product
    public string? CopyProductName { get; set; }
    public bool CopyProductPublished { get; set; }
}

public class ProductGridModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Sku { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ProductTypeName { get; set; }
    public bool Published { get; set; }
}
