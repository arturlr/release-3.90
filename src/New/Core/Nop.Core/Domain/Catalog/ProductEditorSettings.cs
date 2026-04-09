using Nop.Core.Configuration;

namespace Nop.Core.Domain.Catalog;

public class ProductEditorSettings : ISettings
{
    public bool Id { get; set; }
    public bool ProductType { get; set; }
    public bool VisibleIndividually { get; set; }
    public bool ProductTemplate { get; set; }
    public bool AdminComment { get; set; }
    public bool Vendor { get; set; }
    public bool Stores { get; set; }
    public bool ACL { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool DisplayOrder { get; set; }
    public bool AllowCustomerReviews { get; set; }
    public bool ProductTags { get; set; }
    public bool ManufacturerPartNumber { get; set; }
    public bool GTIN { get; set; }
    public bool ProductCost { get; set; }
    public bool TierPrices { get; set; }
    public bool Discounts { get; set; }
    public bool DisableBuyButton { get; set; }
    public bool DisableWishlistButton { get; set; }
    public bool AvailableForPreOrder { get; set; }
    public bool CallForPrice { get; set; }
    public bool OldPrice { get; set; }
    public bool CustomerEntersPrice { get; set; }
    public bool PAngV { get; set; }
    public bool RequireOtherProductsAddedToTheCart { get; set; }
    public bool IsGiftCard { get; set; }
    public bool DownloadableProduct { get; set; }
    public bool RecurringProduct { get; set; }
    public bool IsRental { get; set; }
    public bool FreeShipping { get; set; }
    public bool ShipSeparately { get; set; }
    public bool AdditionalShippingCharge { get; set; }
    public bool DeliveryDate { get; set; }
    public bool TelecommunicationsBroadcastingElectronicServices { get; set; }
    public bool ProductAvailabilityRange { get; set; }
    public bool UseMultipleWarehouses { get; set; }
    public bool Warehouse { get; set; }
    public bool DisplayStockAvailability { get; set; }
    public bool DisplayStockQuantity { get; set; }
    public bool MinimumStockQuantity { get; set; }
    public bool LowStockActivity { get; set; }
    public bool NotifyAdminForQuantityBelow { get; set; }
    public bool Backorders { get; set; }
    public bool AllowBackInStockSubscriptions { get; set; }
    public bool MinimumCartQuantity { get; set; }
    public bool MaximumCartQuantity { get; set; }
    public bool AllowedQuantities { get; set; }
    public bool AllowAddingOnlyExistingAttributeCombinations { get; set; }
    public bool NotReturnable { get; set; }
    public bool Weight { get; set; }
    public bool Dimensions { get; set; }
    public bool AvailableStartDate { get; set; }
    public bool AvailableEndDate { get; set; }
    public bool MarkAsNew { get; set; }
    public bool MarkAsNewStartDate { get; set; }
    public bool MarkAsNewEndDate { get; set; }
    public bool Published { get; set; }
    public bool CreatedOn { get; set; }
    public bool UpdatedOn { get; set; }
    public bool RelatedProducts { get; set; }
    public bool CrossSellsProducts { get; set; }
    public bool Seo { get; set; }
    public bool PurchasedWithOrders { get; set; }
    public bool OneColumnProductPage { get; set; }
    public bool ProductAttributes { get; set; }
    public bool SpecificationAttributes { get; set; }
    public bool Manufacturers { get; set; }
    public bool StockQuantityHistory { get; set; }
}
