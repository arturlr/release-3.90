using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Security;
using Nop.Services.Seo;

namespace Nop.Web.Controllers;

public partial class ShoppingCartController
{
    // --- Wishlist ---

    public async Task<IActionResult> Wishlist(Guid? customerGuid)
    {
        if (!permissionService.Authorize(StandardPermissionProvider.EnableWishlist))
            return RedirectToAction("Index", "Home");

        var customer = customerGuid.HasValue
            ? await customerService.GetCustomerByGuidAsync(customerGuid.Value)
            : workContext.CurrentCustomer;
        if (customer == null)
            return RedirectToAction("Index", "Home");

        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, storeContext.CurrentStore.Id);
        var model = await PrepareWishlistModelAsync(cart, !customerGuid.HasValue);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateWishlist(IFormCollection form)
    {
        if (!permissionService.Authorize(StandardPermissionProvider.EnableWishlist))
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        var cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, storeContext.CurrentStore.Id);

        var allIdsToRemove = (form["removefromcart"].ToString() ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToList();

        var innerWarnings = new Dictionary<int, IList<string>>();
        foreach (var sci in cart)
        {
            if (allIdsToRemove.Contains(sci.Id))
            {
                await shoppingCartService.DeleteShoppingCartItemAsync(sci);
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

        cart = await shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.Wishlist, storeContext.CurrentStore.Id);
        var model = await PrepareWishlistModelAsync(cart, true);

        foreach (var (sciId, warnings) in innerWarnings)
        {
            var sciModel = model.Items.FirstOrDefault(x => x.Id == sciId);
            if (sciModel != null)
                foreach (var w in warnings.Where(w => !sciModel.Warnings.Contains(w)))
                    sciModel.Warnings.Add(w);
        }

        return View("Wishlist", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid, IFormCollection form)
    {
        if (!permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart) ||
            !permissionService.Authorize(StandardPermissionProvider.EnableWishlist))
            return RedirectToAction("Index", "Home");

        var pageCustomer = customerGuid.HasValue
            ? await customerService.GetCustomerByGuidAsync(customerGuid.Value)
            : workContext.CurrentCustomer;
        if (pageCustomer == null)
            return RedirectToAction("Index", "Home");

        var pageCart = await shoppingCartService.GetShoppingCartAsync(pageCustomer, ShoppingCartType.Wishlist, storeContext.CurrentStore.Id);

        var allIdsToAdd = (form["addtocart"].ToString() ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse).ToList();

        var allWarnings = new List<string>();
        var numberOfAddedItems = 0;
        var customer = workContext.CurrentCustomer;
        var storeId = storeContext.CurrentStore.Id;

        foreach (var sci in pageCart.Where(sci => allIdsToAdd.Contains(sci.Id)))
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;

            var warnings = await shoppingCartService.AddToCartAsync(customer, product,
                ShoppingCartType.ShoppingCart, storeId,
                sci.AttributesXml, sci.CustomerEnteredPrice,
                sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity);

            if (!warnings.Any())
            {
                numberOfAddedItems++;
                if (shoppingCartSettings.MoveItemsFromWishlistToCart && !customerGuid.HasValue)
                    await shoppingCartService.DeleteShoppingCartItemAsync(sci);
            }
            allWarnings.AddRange(warnings);
        }

        if (numberOfAddedItems > 0)
            return RedirectToAction("Cart");

        // No items added — redisplay wishlist
        var cart = await shoppingCartService.GetShoppingCartAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);
        var model = await PrepareWishlistModelAsync(cart, !customerGuid.HasValue);
        return View("Wishlist", model);
    }
}
