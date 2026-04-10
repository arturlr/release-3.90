using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ProductController
{
    private async Task PrepareProductModelDropdownsAsync(ProductModel model)
    {
        // Product types
        if (model.AvailableProductTypes.Count == 0)
            foreach (var pt in Enum.GetValues<ProductType>())
                model.AvailableProductTypes.Add(new SelectListItem { Text = pt.ToString(), Value = ((int)pt).ToString() });

        // Tax categories
        model.AvailableTaxCategories.Add(new SelectListItem { Text = "---", Value = "0" });
        foreach (var tc in await taxCategoryService.GetAllTaxCategoriesAsync())
            model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString() });

        // Delivery dates
        model.AvailableDeliveryDates.Add(new SelectListItem { Text = "---", Value = "0" });
        foreach (var dd in await dateRangeService.GetAllDeliveryDatesAsync())
            model.AvailableDeliveryDates.Add(new SelectListItem { Text = dd.Name, Value = dd.Id.ToString() });

        // Warehouses
        model.AvailableWarehouses.Add(new SelectListItem { Text = "---", Value = "0" });
        foreach (var wh in await shippingService.GetAllWarehousesAsync())
            model.AvailableWarehouses.Add(new SelectListItem { Text = wh.Name, Value = wh.Id.ToString() });

        // Vendors (admin only)
        if (workContext.CurrentVendor is null)
        {
            model.AvailableVendors.Add(new SelectListItem { Text = "---", Value = "0" });
            foreach (var v in await vendorService.GetAllVendorsAsync())
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });
        }
    }

    private static Product MapModelToEntity(ProductModel model, Product product)
    {
        product.ProductTypeId = model.ProductTypeId;
        product.Name = model.Name;
        product.ShortDescription = model.ShortDescription;
        product.FullDescription = model.FullDescription;
        product.AdminComment = model.AdminComment;
        product.ShowOnHomePage = model.ShowOnHomePage;
        product.MetaKeywords = model.MetaKeywords;
        product.MetaDescription = model.MetaDescription;
        product.MetaTitle = model.MetaTitle;
        product.AllowCustomerReviews = model.AllowCustomerReviews;
        product.Sku = model.Sku;
        product.ManufacturerPartNumber = model.ManufacturerPartNumber;
        product.Gtin = model.Gtin;
        product.IsGiftCard = model.IsGiftCard;
        product.GiftCardTypeId = model.GiftCardTypeId;
        product.IsDownload = model.IsDownload;
        product.DownloadId = model.DownloadId;
        product.IsRecurring = model.IsRecurring;
        product.RecurringCycleLength = model.RecurringCycleLength;
        product.RecurringCyclePeriodId = model.RecurringCyclePeriodId;
        product.RecurringTotalCycles = model.RecurringTotalCycles;
        product.IsRental = model.IsRental;
        product.RentalPriceLength = model.RentalPriceLength;
        product.RentalPricePeriodId = model.RentalPricePeriodId;
        product.IsShipEnabled = model.IsShipEnabled;
        product.IsFreeShipping = model.IsFreeShipping;
        product.ShipSeparately = model.ShipSeparately;
        product.AdditionalShippingCharge = model.AdditionalShippingCharge;
        product.DeliveryDateId = model.DeliveryDateId;
        product.IsTaxExempt = model.IsTaxExempt;
        product.TaxCategoryId = model.TaxCategoryId;
        product.ManageInventoryMethodId = model.ManageInventoryMethodId;
        product.StockQuantity = model.StockQuantity;
        product.DisplayStockAvailability = model.DisplayStockAvailability;
        product.DisplayStockQuantity = model.DisplayStockQuantity;
        product.MinStockQuantity = model.MinStockQuantity;
        product.LowStockActivityId = model.LowStockActivityId;
        product.NotifyAdminForQuantityBelow = model.NotifyAdminForQuantityBelow;
        product.BackorderModeId = model.BackorderModeId;
        product.AllowBackInStockSubscriptions = model.AllowBackInStockSubscriptions;
        product.OrderMinimumQuantity = model.OrderMinimumQuantity;
        product.OrderMaximumQuantity = model.OrderMaximumQuantity;
        product.WarehouseId = model.WarehouseId;
        product.DisableBuyButton = model.DisableBuyButton;
        product.DisableWishlistButton = model.DisableWishlistButton;
        product.AvailableForPreOrder = model.AvailableForPreOrder;
        product.PreOrderAvailabilityStartDateTimeUtc = model.PreOrderAvailabilityStartDateTimeUtc;
        product.CallForPrice = model.CallForPrice;
        product.Price = model.Price;
        product.OldPrice = model.OldPrice;
        product.ProductCost = model.ProductCost;
        product.CustomerEntersPrice = model.CustomerEntersPrice;
        product.MinimumCustomerEnteredPrice = model.MinimumCustomerEnteredPrice;
        product.MaximumCustomerEnteredPrice = model.MaximumCustomerEnteredPrice;
        product.BasepriceEnabled = model.BasepriceEnabled;
        product.BasepriceAmount = model.BasepriceAmount;
        product.BasepriceUnitId = model.BasepriceUnitId;
        product.BasepriceBaseAmount = model.BasepriceBaseAmount;
        product.BasepriceBaseUnitId = model.BasepriceBaseUnitId;
        product.MarkAsNew = model.MarkAsNew;
        product.MarkAsNewStartDateTimeUtc = model.MarkAsNewStartDateTimeUtc;
        product.MarkAsNewEndDateTimeUtc = model.MarkAsNewEndDateTimeUtc;
        product.Weight = model.Weight;
        product.Length = model.Length;
        product.Width = model.Width;
        product.Height = model.Height;
        product.AvailableStartDateTimeUtc = model.AvailableStartDateTimeUtc;
        product.AvailableEndDateTimeUtc = model.AvailableEndDateTimeUtc;
        product.DisplayOrder = model.DisplayOrder;
        product.Published = model.Published;
        product.VendorId = model.VendorId;
        product.VisibleIndividually = model.VisibleIndividually;
        return product;
    }

    private static ProductModel MapEntityToModel(Product product)
    {
        return new ProductModel
        {
            Id = product.Id,
            ProductTypeId = product.ProductTypeId,
            ProductTypeName = product.ProductType.ToString(),
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            AdminComment = product.AdminComment,
            ShowOnHomePage = product.ShowOnHomePage,
            MetaKeywords = product.MetaKeywords,
            MetaDescription = product.MetaDescription,
            MetaTitle = product.MetaTitle,
            AllowCustomerReviews = product.AllowCustomerReviews,
            Sku = product.Sku,
            ManufacturerPartNumber = product.ManufacturerPartNumber,
            Gtin = product.Gtin,
            IsGiftCard = product.IsGiftCard,
            GiftCardTypeId = product.GiftCardTypeId,
            IsDownload = product.IsDownload,
            DownloadId = product.DownloadId,
            IsRecurring = product.IsRecurring,
            RecurringCycleLength = product.RecurringCycleLength,
            RecurringCyclePeriodId = product.RecurringCyclePeriodId,
            RecurringTotalCycles = product.RecurringTotalCycles,
            IsRental = product.IsRental,
            RentalPriceLength = product.RentalPriceLength,
            RentalPricePeriodId = product.RentalPricePeriodId,
            IsShipEnabled = product.IsShipEnabled,
            IsFreeShipping = product.IsFreeShipping,
            ShipSeparately = product.ShipSeparately,
            AdditionalShippingCharge = product.AdditionalShippingCharge,
            DeliveryDateId = product.DeliveryDateId,
            IsTaxExempt = product.IsTaxExempt,
            TaxCategoryId = product.TaxCategoryId,
            ManageInventoryMethodId = product.ManageInventoryMethodId,
            StockQuantity = product.StockQuantity,
            DisplayStockAvailability = product.DisplayStockAvailability,
            DisplayStockQuantity = product.DisplayStockQuantity,
            MinStockQuantity = product.MinStockQuantity,
            LowStockActivityId = product.LowStockActivityId,
            NotifyAdminForQuantityBelow = product.NotifyAdminForQuantityBelow,
            BackorderModeId = product.BackorderModeId,
            AllowBackInStockSubscriptions = product.AllowBackInStockSubscriptions,
            OrderMinimumQuantity = product.OrderMinimumQuantity,
            OrderMaximumQuantity = product.OrderMaximumQuantity,
            WarehouseId = product.WarehouseId,
            DisableBuyButton = product.DisableBuyButton,
            DisableWishlistButton = product.DisableWishlistButton,
            AvailableForPreOrder = product.AvailableForPreOrder,
            PreOrderAvailabilityStartDateTimeUtc = product.PreOrderAvailabilityStartDateTimeUtc,
            CallForPrice = product.CallForPrice,
            Price = product.Price,
            OldPrice = product.OldPrice,
            ProductCost = product.ProductCost,
            CustomerEntersPrice = product.CustomerEntersPrice,
            MinimumCustomerEnteredPrice = product.MinimumCustomerEnteredPrice,
            MaximumCustomerEnteredPrice = product.MaximumCustomerEnteredPrice,
            BasepriceEnabled = product.BasepriceEnabled,
            BasepriceAmount = product.BasepriceAmount,
            BasepriceUnitId = product.BasepriceUnitId,
            BasepriceBaseAmount = product.BasepriceBaseAmount,
            BasepriceBaseUnitId = product.BasepriceBaseUnitId,
            MarkAsNew = product.MarkAsNew,
            MarkAsNewStartDateTimeUtc = product.MarkAsNewStartDateTimeUtc,
            MarkAsNewEndDateTimeUtc = product.MarkAsNewEndDateTimeUtc,
            Weight = product.Weight,
            Length = product.Length,
            Width = product.Width,
            Height = product.Height,
            AvailableStartDateTimeUtc = product.AvailableStartDateTimeUtc,
            AvailableEndDateTimeUtc = product.AvailableEndDateTimeUtc,
            DisplayOrder = product.DisplayOrder,
            Published = product.Published,
            CreatedOnUtc = product.CreatedOnUtc,
            UpdatedOnUtc = product.UpdatedOnUtc,
            VendorId = product.VendorId,
            VisibleIndividually = product.VisibleIndividually
        };
    }

    private async Task<IList<Product>> SearchProductsFromModelAsync(ProductListModel model, int vendorId)
    {
        var categoryIds = new List<int>();
        if (model.SearchCategoryId > 0)
            categoryIds.Add(model.SearchCategoryId);

        bool? overridePublished = model.SearchPublishedId switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        return (await productService.SearchProductsAsync(
            categoryIds: categoryIds,
            manufacturerId: model.SearchManufacturerId,
            vendorId: vendorId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            showHidden: true,
            overridePublished: overridePublished)).ToList();
    }
}
