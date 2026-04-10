using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Web.Areas.Admin.Models.Settings;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SettingController
{
    #region Order

    public async Task<IActionResult> Order()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<OrderSettings>(storeScope);
        var model = new OrderSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            IsReOrderAllowed = s.IsReOrderAllowed,
            MinOrderSubtotalAmount = s.MinOrderSubtotalAmount,
            MinOrderSubtotalAmountIncludingTax = s.MinOrderSubtotalAmountIncludingTax,
            MinOrderTotalAmount = s.MinOrderTotalAmount,
            AnonymousCheckoutAllowed = s.AnonymousCheckoutAllowed,
            TermsOfServiceOnShoppingCartPage = s.TermsOfServiceOnShoppingCartPage,
            TermsOfServiceOnOrderConfirmPage = s.TermsOfServiceOnOrderConfirmPage,
            OnePageCheckoutEnabled = s.OnePageCheckoutEnabled,
            DisableBillingAddressCheckoutStep = s.DisableBillingAddressCheckoutStep,
            DisableOrderCompletedPage = s.DisableOrderCompletedPage,
            ReturnRequestsEnabled = s.ReturnRequestsEnabled,
            ReturnRequestsAllowFiles = s.ReturnRequestsAllowFiles,
            NumberOfDaysReturnRequestAvailable = s.NumberOfDaysReturnRequestAvailable,
            CustomOrderNumberMask = s.CustomOrderNumberMask,
            MinimumOrderPlacementInterval = s.MinimumOrderPlacementInterval,
            CompleteOrderWhenDelivered = s.CompleteOrderWhenDelivered
        };
        if (storeScope > 0)
        {
            model.IsReOrderAllowed_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.IsReOrderAllowed, storeScope);
            model.MinOrderSubtotalAmount_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MinOrderSubtotalAmount, storeScope);
            model.MinOrderSubtotalAmountIncludingTax_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MinOrderSubtotalAmountIncludingTax, storeScope);
            model.MinOrderTotalAmount_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MinOrderTotalAmount, storeScope);
            model.AnonymousCheckoutAllowed_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AnonymousCheckoutAllowed, storeScope);
            model.TermsOfServiceOnShoppingCartPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.TermsOfServiceOnShoppingCartPage, storeScope);
            model.TermsOfServiceOnOrderConfirmPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.TermsOfServiceOnOrderConfirmPage, storeScope);
            model.OnePageCheckoutEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.OnePageCheckoutEnabled, storeScope);
            model.DisableBillingAddressCheckoutStep_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DisableBillingAddressCheckoutStep, storeScope);
            model.DisableOrderCompletedPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DisableOrderCompletedPage, storeScope);
            model.ReturnRequestsEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ReturnRequestsEnabled, storeScope);
            model.ReturnRequestsAllowFiles_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ReturnRequestsAllowFiles, storeScope);
            model.NumberOfDaysReturnRequestAvailable_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.NumberOfDaysReturnRequestAvailable, storeScope);
            model.CustomOrderNumberMask_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CustomOrderNumberMask, storeScope);
            model.MinimumOrderPlacementInterval_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MinimumOrderPlacementInterval, storeScope);
            model.CompleteOrderWhenDelivered_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CompleteOrderWhenDelivered, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Order(OrderSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<OrderSettings>(storeScope);
        s.IsReOrderAllowed = model.IsReOrderAllowed; s.MinOrderSubtotalAmount = model.MinOrderSubtotalAmount;
        s.MinOrderSubtotalAmountIncludingTax = model.MinOrderSubtotalAmountIncludingTax; s.MinOrderTotalAmount = model.MinOrderTotalAmount;
        s.AnonymousCheckoutAllowed = model.AnonymousCheckoutAllowed; s.TermsOfServiceOnShoppingCartPage = model.TermsOfServiceOnShoppingCartPage;
        s.TermsOfServiceOnOrderConfirmPage = model.TermsOfServiceOnOrderConfirmPage; s.OnePageCheckoutEnabled = model.OnePageCheckoutEnabled;
        s.DisableBillingAddressCheckoutStep = model.DisableBillingAddressCheckoutStep; s.DisableOrderCompletedPage = model.DisableOrderCompletedPage;
        s.ReturnRequestsEnabled = model.ReturnRequestsEnabled; s.ReturnRequestsAllowFiles = model.ReturnRequestsAllowFiles;
        s.NumberOfDaysReturnRequestAvailable = model.NumberOfDaysReturnRequestAvailable; s.CustomOrderNumberMask = model.CustomOrderNumberMask;
        s.MinimumOrderPlacementInterval = model.MinimumOrderPlacementInterval; s.CompleteOrderWhenDelivered = model.CompleteOrderWhenDelivered;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.IsReOrderAllowed, model.IsReOrderAllowed_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MinOrderSubtotalAmount, model.MinOrderSubtotalAmount_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MinOrderSubtotalAmountIncludingTax, model.MinOrderSubtotalAmountIncludingTax_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MinOrderTotalAmount, model.MinOrderTotalAmount_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AnonymousCheckoutAllowed, model.AnonymousCheckoutAllowed_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.TermsOfServiceOnShoppingCartPage, model.TermsOfServiceOnShoppingCartPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.TermsOfServiceOnOrderConfirmPage, model.TermsOfServiceOnOrderConfirmPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.OnePageCheckoutEnabled, model.OnePageCheckoutEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DisableBillingAddressCheckoutStep, model.DisableBillingAddressCheckoutStep_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DisableOrderCompletedPage, model.DisableOrderCompletedPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ReturnRequestsEnabled, model.ReturnRequestsEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ReturnRequestsAllowFiles, model.ReturnRequestsAllowFiles_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.NumberOfDaysReturnRequestAvailable, model.NumberOfDaysReturnRequestAvailable_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CustomOrderNumberMask, model.CustomOrderNumberMask_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MinimumOrderPlacementInterval, model.MinimumOrderPlacementInterval_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CompleteOrderWhenDelivered, model.CompleteOrderWhenDelivered_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();
        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Order");
    }

    #endregion

    #region ShoppingCart

    public async Task<IActionResult> ShoppingCart()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<ShoppingCartSettings>(storeScope);
        var model = new ShoppingCartSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            DisplayCartAfterAddingProduct = s.DisplayCartAfterAddingProduct,
            DisplayWishlistAfterAddingProduct = s.DisplayWishlistAfterAddingProduct,
            MaximumShoppingCartItems = s.MaximumShoppingCartItems,
            MaximumWishlistItems = s.MaximumWishlistItems,
            AllowOutOfStockItemsToBeAddedToWishlist = s.AllowOutOfStockItemsToBeAddedToWishlist,
            MoveItemsFromWishlistToCart = s.MoveItemsFromWishlistToCart,
            ShowProductImagesOnShoppingCart = s.ShowProductImagesOnShoppingCart,
            ShowProductImagesOnWishList = s.ShowProductImagesOnWishList,
            ShowDiscountBox = s.ShowDiscountBox,
            ShowGiftCardBox = s.ShowGiftCardBox,
            CrossSellsNumber = s.CrossSellsNumber,
            EmailWishlistEnabled = s.EmailWishlistEnabled,
            AllowAnonymousUsersToEmailWishlist = s.AllowAnonymousUsersToEmailWishlist,
            MiniShoppingCartEnabled = s.MiniShoppingCartEnabled,
            MiniShoppingCartProductNumber = s.MiniShoppingCartProductNumber,
            AllowCartItemEditing = s.AllowCartItemEditing
        };
        if (storeScope > 0)
        {
            model.DisplayCartAfterAddingProduct_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DisplayCartAfterAddingProduct, storeScope);
            model.DisplayWishlistAfterAddingProduct_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DisplayWishlistAfterAddingProduct, storeScope);
            model.MaximumShoppingCartItems_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MaximumShoppingCartItems, storeScope);
            model.MaximumWishlistItems_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MaximumWishlistItems, storeScope);
            model.AllowOutOfStockItemsToBeAddedToWishlist_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowOutOfStockItemsToBeAddedToWishlist, storeScope);
            model.MoveItemsFromWishlistToCart_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MoveItemsFromWishlistToCart, storeScope);
            model.ShowProductImagesOnShoppingCart_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowProductImagesOnShoppingCart, storeScope);
            model.ShowProductImagesOnWishList_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowProductImagesOnWishList, storeScope);
            model.ShowDiscountBox_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowDiscountBox, storeScope);
            model.ShowGiftCardBox_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ShowGiftCardBox, storeScope);
            model.CrossSellsNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CrossSellsNumber, storeScope);
            model.EmailWishlistEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.EmailWishlistEnabled, storeScope);
            model.AllowAnonymousUsersToEmailWishlist_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowAnonymousUsersToEmailWishlist, storeScope);
            model.MiniShoppingCartEnabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MiniShoppingCartEnabled, storeScope);
            model.MiniShoppingCartProductNumber_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MiniShoppingCartProductNumber, storeScope);
            model.AllowCartItemEditing_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AllowCartItemEditing, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ShoppingCart(ShoppingCartSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<ShoppingCartSettings>(storeScope);
        s.DisplayCartAfterAddingProduct = model.DisplayCartAfterAddingProduct; s.DisplayWishlistAfterAddingProduct = model.DisplayWishlistAfterAddingProduct;
        s.MaximumShoppingCartItems = model.MaximumShoppingCartItems; s.MaximumWishlistItems = model.MaximumWishlistItems;
        s.AllowOutOfStockItemsToBeAddedToWishlist = model.AllowOutOfStockItemsToBeAddedToWishlist; s.MoveItemsFromWishlistToCart = model.MoveItemsFromWishlistToCart;
        s.ShowProductImagesOnShoppingCart = model.ShowProductImagesOnShoppingCart; s.ShowProductImagesOnWishList = model.ShowProductImagesOnWishList;
        s.ShowDiscountBox = model.ShowDiscountBox; s.ShowGiftCardBox = model.ShowGiftCardBox; s.CrossSellsNumber = model.CrossSellsNumber;
        s.EmailWishlistEnabled = model.EmailWishlistEnabled; s.AllowAnonymousUsersToEmailWishlist = model.AllowAnonymousUsersToEmailWishlist;
        s.MiniShoppingCartEnabled = model.MiniShoppingCartEnabled; s.MiniShoppingCartProductNumber = model.MiniShoppingCartProductNumber;
        s.AllowCartItemEditing = model.AllowCartItemEditing;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DisplayCartAfterAddingProduct, model.DisplayCartAfterAddingProduct_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DisplayWishlistAfterAddingProduct, model.DisplayWishlistAfterAddingProduct_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MaximumShoppingCartItems, model.MaximumShoppingCartItems_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MaximumWishlistItems, model.MaximumWishlistItems_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowOutOfStockItemsToBeAddedToWishlist, model.AllowOutOfStockItemsToBeAddedToWishlist_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MoveItemsFromWishlistToCart, model.MoveItemsFromWishlistToCart_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowProductImagesOnShoppingCart, model.ShowProductImagesOnShoppingCart_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowProductImagesOnWishList, model.ShowProductImagesOnWishList_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowDiscountBox, model.ShowDiscountBox_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ShowGiftCardBox, model.ShowGiftCardBox_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CrossSellsNumber, model.CrossSellsNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.EmailWishlistEnabled, model.EmailWishlistEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowAnonymousUsersToEmailWishlist, model.AllowAnonymousUsersToEmailWishlist_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MiniShoppingCartEnabled, model.MiniShoppingCartEnabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MiniShoppingCartProductNumber, model.MiniShoppingCartProductNumber_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AllowCartItemEditing, model.AllowCartItemEditing_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();
        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("ShoppingCart");
    }

    #endregion

    #region Media

    public async Task<IActionResult> Media()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<MediaSettings>(storeScope);
        var model = new MediaSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            AvatarPictureSize = s.AvatarPictureSize,
            ProductThumbPictureSize = s.ProductThumbPictureSize,
            ProductDetailsPictureSize = s.ProductDetailsPictureSize,
            ProductThumbPictureSizeOnProductDetailsPage = s.ProductThumbPictureSizeOnProductDetailsPage,
            AssociatedProductPictureSize = s.AssociatedProductPictureSize,
            CategoryThumbPictureSize = s.CategoryThumbPictureSize,
            ManufacturerThumbPictureSize = s.ManufacturerThumbPictureSize,
            CartThumbPictureSize = s.CartThumbPictureSize,
            MiniCartThumbPictureSize = s.MiniCartThumbPictureSize,
            MaximumImageSize = s.MaximumImageSize,
            DefaultImageQuality = s.DefaultImageQuality,
            PicturesStoredIntoDatabase = pictureService.StoreInDb
        };
        if (storeScope > 0)
        {
            model.AvatarPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AvatarPictureSize, storeScope);
            model.ProductThumbPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductThumbPictureSize, storeScope);
            model.ProductDetailsPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductDetailsPictureSize, storeScope);
            model.ProductThumbPictureSizeOnProductDetailsPage_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ProductThumbPictureSizeOnProductDetailsPage, storeScope);
            model.AssociatedProductPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.AssociatedProductPictureSize, storeScope);
            model.CategoryThumbPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CategoryThumbPictureSize, storeScope);
            model.ManufacturerThumbPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ManufacturerThumbPictureSize, storeScope);
            model.CartThumbPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.CartThumbPictureSize, storeScope);
            model.MiniCartThumbPictureSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MiniCartThumbPictureSize, storeScope);
            model.MaximumImageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MaximumImageSize, storeScope);
            model.DefaultImageQuality_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DefaultImageQuality, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Media(MediaSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<MediaSettings>(storeScope);
        s.AvatarPictureSize = model.AvatarPictureSize; s.ProductThumbPictureSize = model.ProductThumbPictureSize;
        s.ProductDetailsPictureSize = model.ProductDetailsPictureSize; s.ProductThumbPictureSizeOnProductDetailsPage = model.ProductThumbPictureSizeOnProductDetailsPage;
        s.AssociatedProductPictureSize = model.AssociatedProductPictureSize; s.CategoryThumbPictureSize = model.CategoryThumbPictureSize;
        s.ManufacturerThumbPictureSize = model.ManufacturerThumbPictureSize; s.CartThumbPictureSize = model.CartThumbPictureSize;
        s.MiniCartThumbPictureSize = model.MiniCartThumbPictureSize; s.MaximumImageSize = model.MaximumImageSize;
        s.DefaultImageQuality = model.DefaultImageQuality;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AvatarPictureSize, model.AvatarPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductThumbPictureSize, model.ProductThumbPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductDetailsPictureSize, model.ProductDetailsPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ProductThumbPictureSizeOnProductDetailsPage, model.ProductThumbPictureSizeOnProductDetailsPage_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.AssociatedProductPictureSize, model.AssociatedProductPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CategoryThumbPictureSize, model.CategoryThumbPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ManufacturerThumbPictureSize, model.ManufacturerThumbPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.CartThumbPictureSize, model.CartThumbPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MiniCartThumbPictureSize, model.MiniCartThumbPictureSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MaximumImageSize, model.MaximumImageSize_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DefaultImageQuality, model.DefaultImageQuality_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();
        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("Media");
    }

    #endregion
}
