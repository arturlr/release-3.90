using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class ShoppingCartController(
    ICustomerService customerService,
    IShoppingCartService shoppingCartService,
    IProductService productService,
    IPriceCalculationService priceCalculationService,
    IPriceFormatter priceFormatter,
    ITaxService taxService,
    IStoreService storeService,
    IDateTimeHelper dateTimeHelper,
    IProductAttributeFormatter productAttributeFormatter,
    IPermissionService permissionService) : BaseAdminController
{
    #region Utilities

    private async Task<IActionResult> CustomerCartListAsync(DataSourceRequest command, ShoppingCartType cartType)
    {
        var customers = await customerService.GetAllCustomersAsync(
            loadOnlyWithShoppingCart: true,
            sct: cartType,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var models = new List<ShoppingCartModel>();
        foreach (var c in customers)
        {
            var cart = await shoppingCartService.GetShoppingCartAsync(c, cartType);
            var roleIds = await customerService.GetCustomerRoleIdsAsync(c);
            var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
                Nop.Core.Domain.Customers.SystemCustomerRoleNames.Registered);
            var isRegistered = registeredRole != null && roleIds.Contains(registeredRole.Id);

            models.Add(new ShoppingCartModel
            {
                CustomerId = c.Id,
                CustomerEmail = isRegistered ? c.Email ?? "Guest" : "Guest",
                TotalItems = cart.Sum(sci => sci.Quantity),
            });
        }

        return Json(new DataSourceResult { Data = models, Total = customers.TotalCount });
    }

    private async Task<IActionResult> CartDetailsAsync(int customerId, ShoppingCartType cartType)
    {
        var customer = await customerService.GetCustomerByIdAsync(customerId);
        if (customer is null)
            return Json(new DataSourceResult { Data = Array.Empty<ShoppingCartItemModel>(), Total = 0 });

        var cart = await shoppingCartService.GetShoppingCartAsync(customer, cartType);
        var models = new List<ShoppingCartItemModel>();

        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            var store = await storeService.GetStoreByIdAsync(sci.StoreId);

            var unitPrice = await priceCalculationService.GetUnitPriceAsync(sci);
            var (taxUnitPrice, _) = product != null
                ? await taxService.GetProductPriceAsync(product, unitPrice)
                : (unitPrice, 0m);

            var subTotal = await priceCalculationService.GetSubTotalAsync(sci);
            var (taxSubTotal, _) = product != null
                ? await taxService.GetProductPriceAsync(product, subTotal)
                : (subTotal, 0m);

            models.Add(new ShoppingCartItemModel
            {
                Id = sci.Id,
                Store = store?.Name ?? "Unknown",
                ProductId = sci.ProductId,
                ProductName = product?.Name ?? $"Product #{sci.ProductId}",
                AttributeInfo = product != null
                    ? await productAttributeFormatter.FormatAttributesAsync(product, sci.AttributesXml ?? string.Empty)
                    : string.Empty,
                UnitPrice = await priceFormatter.FormatPriceAsync(taxUnitPrice),
                Quantity = sci.Quantity,
                Total = await priceFormatter.FormatPriceAsync(taxSubTotal),
                UpdatedOn = dateTimeHelper.ConvertToUserTime(sci.UpdatedOnUtc, DateTimeKind.Utc),
            });
        }

        return Json(new DataSourceResult { Data = models, Total = models.Count });
    }

    #endregion

    #region Shopping Carts

    public IActionResult CurrentCarts()
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CurrentCartsList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return await CustomerCartListAsync(command, ShoppingCartType.ShoppingCart);
    }

    [HttpPost]
    public async Task<IActionResult> GetCartDetails(int customerId)
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return await CartDetailsAsync(customerId, ShoppingCartType.ShoppingCart);
    }

    #endregion

    #region Wishlists

    public IActionResult CurrentWishlists()
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CurrentWishlistsList(DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return await CustomerCartListAsync(command, ShoppingCartType.Wishlist);
    }

    [HttpPost]
    public async Task<IActionResult> GetWishlistDetails(int customerId)
    {
        if (!permissionService.Authorize("ManageCurrentCarts"))
            return Forbid();

        return await CartDetailsAsync(customerId, ShoppingCartType.Wishlist);
    }

    #endregion
}
