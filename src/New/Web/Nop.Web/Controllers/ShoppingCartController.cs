using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Tax;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.ShoppingCart;

namespace Nop.Web.Controllers;

public partial class ShoppingCartController(
    IProductService productService,
    IWorkContext workContext,
    IStoreContext storeContext,
    IShoppingCartService shoppingCartService,
    ILocalizationService localizationService,
    IProductAttributeService productAttributeService,
    IProductAttributeParser productAttributeParser,
    IProductAttributeFormatter productAttributeFormatter,
    ICheckoutAttributeService checkoutAttributeService,
    ICheckoutAttributeParser checkoutAttributeParser,
    ICheckoutAttributeFormatter checkoutAttributeFormatter,
    ITaxService taxService,
    ICurrencyService currencyService,
    IPriceCalculationService priceCalculationService,
    IPriceFormatter priceFormatter,
    IDiscountService discountService,
    ICustomerService customerService,
    IGiftCardService giftCardService,
    IGenericAttributeService genericAttributeService,
    IPermissionService permissionService,
    IDownloadService downloadService,
    ICustomerActivityService customerActivityService,
    IOrderTotalCalculationService orderTotalCalculationService,
    ShoppingCartSettings shoppingCartSettings,
    OrderSettings orderSettings,
    CatalogSettings catalogSettings) : BasePublicController
{
    // --- Cart page ---

    public async Task<IActionResult> Cart()
    {
        if (!permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        var model = await PrepareShoppingCartModelAsync(cart, true);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCart(IFormCollection form)
    {
        if (!permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart))
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);

        var allIdsToRemove = (form["removefromcart"].ToString() ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToList();

        var innerWarnings = new Dictionary<int, IList<string>>();
        foreach (var sci in cart)
        {
            if (allIdsToRemove.Contains(sci.Id))
            {
                await shoppingCartService.DeleteShoppingCartItemAsync(sci, ensureOnlyActiveCheckoutAttributes: true);
            }
            else
            {
                var key = $"itemquantity{sci.Id}";
                if (form.ContainsKey(key) && int.TryParse(form[key], out var newQuantity))
                {
                    var warnings = await shoppingCartService.UpdateShoppingCartItemAsync(customer,
                        sci.Id, sci.AttributesXml, sci.CustomerEnteredPrice,
                        sci.RentalStartDateUtc, sci.RentalEndDateUtc, newQuantity);
                    innerWarnings[sci.Id] = warnings;
                }
            }
        }

        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        var model = await PrepareShoppingCartModelAsync(cart, true);

        foreach (var (sciId, warnings) in innerWarnings)
        {
            var sciModel = model.Items.FirstOrDefault(x => x.Id == sciId);
            if (sciModel != null)
                foreach (var w in warnings.Where(w => !sciModel.Warnings.Contains(w)))
                    sciModel.Warnings.Add(w);
        }

        return View("Cart", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContinueShopping()
    {
        var returnUrl = await workContext.CurrentCustomer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.LastContinueShoppingPage, genericAttributeService, storeContext.CurrentStore.Id);
        return !string.IsNullOrEmpty(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartCheckout(IFormCollection form)
    {
        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);

        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        var checkoutAttributesXml = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeContext.CurrentStore.Id);
        var checkoutAttributeWarnings = await shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml, true);
        if (checkoutAttributeWarnings.Any())
        {
            var model = await PrepareShoppingCartModelAsync(cart, true);
            return View("Cart", model);
        }

        var isGuest = !await IsRegisteredAsync(customer);
        if (isGuest)
        {
            if (!orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            return RedirectToAction("Login", "Customer", new { returnUrl = Url.Action("Cart") });
        }

        return RedirectToAction("Index", "Checkout");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyDiscountCoupon(string? discountcouponcode, IFormCollection form)
    {
        discountcouponcode = discountcouponcode?.Trim();

        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        var model = new ShoppingCartModel();
        if (!string.IsNullOrWhiteSpace(discountcouponcode))
        {
            var discounts = (await discountService.GetAllDiscountsAsync(couponCode: discountcouponcode, showHidden: true))
                .Where(d => d.RequiresCouponCode).ToList();

            if (discounts.Count != 0)
            {
                var userErrors = new List<string>();
                var anyValid = false;
                foreach (var discount in discounts)
                {
                    var result = await discountService.ValidateDiscountAsync(discount, customer, [discountcouponcode]);
                    if (result.IsValid)
                    {
                        anyValid = true;
                        break;
                    }
                    userErrors.AddRange(result.Errors);
                }

                if (anyValid)
                {
                    await ApplyDiscountCouponCodeAsync(customer, discountcouponcode);
                    model.DiscountBox.Messages.Add(await localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.Applied"));
                    model.DiscountBox.IsApplied = true;
                }
                else
                {
                    model.DiscountBox.Messages = userErrors.Count != 0
                        ? userErrors
                        : [await localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.WrongDiscount")];
                }
            }
            else
            {
                model.DiscountBox.Messages.Add(await localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.WrongDiscount"));
            }
        }
        else
        {
            model.DiscountBox.Messages.Add(await localizationService.GetResourceAsync("ShoppingCart.DiscountCouponCode.WrongDiscount"));
        }

        cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        model = await PrepareShoppingCartModelAsync(cart, true, model.DiscountBox);
        return View("Cart", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveDiscountCoupon(int discountId)
    {
        var customer = workContext.CurrentCustomer;
        var discount = await discountService.GetDiscountByIdAsync(discountId);
        if (discount != null)
            await RemoveDiscountCouponCodeAsync(customer, discount.CouponCode);

        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        var model = await PrepareShoppingCartModelAsync(cart, true);
        return View("Cart", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyGiftCard(string? giftcardcouponcode, IFormCollection form)
    {
        giftcardcouponcode = giftcardcouponcode?.Trim();

        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        await ParseAndSaveCheckoutAttributesAsync(cart, form);

        var model = new ShoppingCartModel();
        if (!string.IsNullOrWhiteSpace(giftcardcouponcode))
        {
            var giftCards = await giftCardService.GetAllGiftCardsAsync(giftCardCouponCode: giftcardcouponcode);
            var giftCard = giftCards.FirstOrDefault();
            if (giftCard != null && await IsGiftCardValidAsync(giftCard))
            {
                await ApplyGiftCardCouponCodeAsync(customer, giftcardcouponcode);
                model.GiftCardBox.Message = await localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.Applied");
                model.GiftCardBox.IsApplied = true;
            }
            else
            {
                model.GiftCardBox.Message = await localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
            }
        }
        else
        {
            model.GiftCardBox.Message = await localizationService.GetResourceAsync("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
        }

        cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        model = await PrepareShoppingCartModelAsync(cart, true, giftCardBox: model.GiftCardBox);
        return View("Cart", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGiftCardCode(int giftCardId)
    {
        var customer = workContext.CurrentCustomer;
        var gc = await giftCardService.GetGiftCardByIdAsync(giftCardId);
        if (gc != null)
            await RemoveGiftCardCouponCodeAsync(customer, gc.GiftCardCouponCode);

        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);
        var model = await PrepareShoppingCartModelAsync(cart, true);
        return View("Cart", model);
    }
}
