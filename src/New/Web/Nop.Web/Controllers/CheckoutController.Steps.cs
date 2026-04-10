using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Common;
using Nop.Services.Payments;
using Nop.Web.Models.Checkout;

namespace Nop.Web.Controllers;

public partial class CheckoutController
{
    // --- Entry point ---

    public async Task<IActionResult> Index()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");

        if (await IsGuestNotAllowedAsync())
            return Challenge();

        // Reset checkout data
        await customerService.ResetCheckoutDataAsync(workContext.CurrentCustomer, storeContext.CurrentStore.Id);

        // Validate cart
        var checkoutAttributesXml = await workContext.CurrentCustomer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeContext.CurrentStore.Id);
        var warnings = await shoppingCartService.GetShoppingCartWarningsAsync(cart, checkoutAttributesXml ?? "", true);
        if (warnings.Count > 0)
            return RedirectToAction("Cart", "ShoppingCart");

        // OPC deferred — always use multi-step
        return RedirectToAction("BillingAddress");
    }

    // --- Billing Address ---

    public async Task<IActionResult> BillingAddress()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        var model = new CheckoutBillingAddressModel
        {
            ExistingAddresses = await GetCustomerAddressesAsync(),
            ShipToSameAddressAllowed = shippingSettings.ShipToSameAddress && await CartRequiresShippingAsync(cart),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectBillingAddress(int addressId, bool shipToSameAddress = false)
    {
        var mapping = customerAddressRepository.Table
            .FirstOrDefault(m => m.CustomerId == workContext.CurrentCustomer.Id && m.AddressId == addressId);
        if (mapping == null)
            return RedirectToAction("BillingAddress");

        await SetBillingAddressAsync(addressId);

        var cart = await GetCartAsync();
        if (shipToSameAddress && shippingSettings.ShipToSameAddress && await CartRequiresShippingAsync(cart))
        {
            await SetShippingAddressAsync(addressId);
            await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedShippingOption, null, storeContext.CurrentStore.Id);
            return RedirectToAction("ShippingMethod");
        }

        return RedirectToAction("ShippingAddress");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewBillingAddress(CheckoutBillingAddressModel model)
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (ModelState.IsValid)
        {
            var address = await FindOrCreateAddressAsync(model.NewAddress);
            await SetBillingAddressAsync(address.Id);

            if (model.ShipToSameAddress && shippingSettings.ShipToSameAddress && await CartRequiresShippingAsync(cart))
            {
                await SetShippingAddressAsync(address.Id);
                await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                    SystemCustomerAttributeNames.SelectedShippingOption, null, storeContext.CurrentStore.Id);
                return RedirectToAction("ShippingMethod");
            }

            return RedirectToAction("ShippingAddress");
        }

        // Redisplay form
        model.ExistingAddresses = await GetCustomerAddressesAsync();
        model.ShipToSameAddressAllowed = shippingSettings.ShipToSameAddress && await CartRequiresShippingAsync(cart);
        return View("BillingAddress", model);
    }

    // --- Shipping Address ---

    public async Task<IActionResult> ShippingAddress()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (!await CartRequiresShippingAsync(cart))
        {
            await SetShippingAddressAsync(null);
            return RedirectToAction("ShippingMethod");
        }

        var model = new CheckoutShippingAddressModel
        {
            ExistingAddresses = await GetCustomerAddressesAsync(),
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectShippingAddress(int addressId)
    {
        var mapping = customerAddressRepository.Table
            .FirstOrDefault(m => m.CustomerId == workContext.CurrentCustomer.Id && m.AddressId == addressId);
        if (mapping == null)
            return RedirectToAction("ShippingAddress");

        await SetShippingAddressAsync(addressId);
        return RedirectToAction("ShippingMethod");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NewShippingAddress(CheckoutShippingAddressModel model)
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (!await CartRequiresShippingAsync(cart))
        {
            await SetShippingAddressAsync(null);
            return RedirectToAction("ShippingMethod");
        }

        if (ModelState.IsValid)
        {
            var address = await FindOrCreateAddressAsync(model.NewAddress);
            await SetShippingAddressAsync(address.Id);
            return RedirectToAction("ShippingMethod");
        }

        model.ExistingAddresses = await GetCustomerAddressesAsync();
        return View("ShippingAddress", model);
    }

    // --- Shipping Method ---

    public async Task<IActionResult> ShippingMethod()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (!await CartRequiresShippingAsync(cart))
        {
            await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedShippingOption, null, storeContext.CurrentStore.Id);
            return RedirectToAction("PaymentMethod");
        }

        // Shipping options depend on plugin system [2.10] — show placeholder
        var model = new CheckoutShippingMethodModel();
        // When shipping plugins are available, populate model.ShippingMethods from IShippingService.GetShippingOptions
        // For now, auto-skip if no methods available
        if (model.ShippingMethods.Count == 0)
        {
            // Save a default "free shipping" option so order processing can proceed
            var defaultOption = System.Text.Json.JsonSerializer.Serialize(new { Name = "Ground", ShippingRateComputationMethodSystemName = "" });
            await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedShippingOption, defaultOption, storeContext.CurrentStore.Id);
            return RedirectToAction("PaymentMethod");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectShippingMethod(string? shippingoption)
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (string.IsNullOrEmpty(shippingoption))
            return RedirectToAction("ShippingMethod");

        // Format: "Name___SystemName"
        var parts = shippingoption.Split("___", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return RedirectToAction("ShippingMethod");

        var optionJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Name = parts[0],
            ShippingRateComputationMethodSystemName = parts[1]
        });
        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.SelectedShippingOption, optionJson, storeContext.CurrentStore.Id);

        return RedirectToAction("PaymentMethod");
    }

    // --- Payment Method ---

    public async Task<IActionResult> PaymentMethod()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        // Check whether payment workflow is required
        if (!await orderProcessingService.IsPaymentWorkflowRequiredAsync(cart, false))
        {
            await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedPaymentMethod, null, storeContext.CurrentStore.Id);
            return RedirectToAction("Confirm");
        }

        // Payment methods depend on plugin system [2.10] — show available methods from DI
        var model = new CheckoutPaymentMethodModel();
        // When payment plugins are available, populate model.PaymentMethods
        // For now, skip payment method selection if no methods registered
        if (model.PaymentMethods.Count == 0)
        {
            await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedPaymentMethod, null, storeContext.CurrentStore.Id);
            return RedirectToAction("Confirm");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectPaymentMethod(string? paymentmethod, CheckoutPaymentMethodModel model)
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        // Save reward points preference
        if (rewardPointsSettings.Enabled)
        {
            await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.UseRewardPointsDuringCheckout, model.UseRewardPoints,
                storeContext.CurrentStore.Id);
        }

        if (!await orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
        {
            await genericAttributeService.SaveAttributeAsync<string?>(workContext.CurrentCustomer,
                SystemCustomerAttributeNames.SelectedPaymentMethod, null, storeContext.CurrentStore.Id);
            return RedirectToAction("Confirm");
        }

        if (string.IsNullOrEmpty(paymentmethod))
            return RedirectToAction("PaymentMethod");

        await genericAttributeService.SaveAttributeAsync(workContext.CurrentCustomer,
            SystemCustomerAttributeNames.SelectedPaymentMethod, paymentmethod, storeContext.CurrentStore.Id);

        return RedirectToAction("PaymentInfo");
    }

    // --- Payment Info ---

    public async Task<IActionResult> PaymentInfo()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        if (!await orderProcessingService.IsPaymentWorkflowRequiredAsync(cart))
            return RedirectToAction("Confirm");

        var paymentMethodSystemName = await workContext.CurrentCustomer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.SelectedPaymentMethod, genericAttributeService, storeContext.CurrentStore.Id);
        if (string.IsNullOrEmpty(paymentMethodSystemName))
            return RedirectToAction("PaymentMethod");

        // Payment info collection depends on plugin system [2.10]
        // For now, skip to confirm
        return RedirectToAction("Confirm");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterPaymentInfo()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        // Payment info validation depends on plugin system [2.10]
        return RedirectToAction("Confirm");
    }

    // --- Confirm ---

    public async Task<IActionResult> Confirm()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        var model = new CheckoutConfirmModel();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmOrder()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return RedirectToAction("Cart", "ShoppingCart");
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        var model = new CheckoutConfirmModel();
        try
        {
            if (!await IsMinimumOrderPlacementIntervalValidAsync())
                throw new Exception(await localizationService.GetResourceAsync("Checkout.MinOrderPlacementInterval"));

            var processPaymentRequest = new ProcessPaymentRequest
            {
                StoreId = storeContext.CurrentStore.Id,
                CustomerId = workContext.CurrentCustomer.Id,
                PaymentMethodSystemName = await workContext.CurrentCustomer.GetAttributeAsync<string>(
                    SystemCustomerAttributeNames.SelectedPaymentMethod, genericAttributeService,
                    storeContext.CurrentStore.Id) ?? "",
            };

            var result = await orderProcessingService.PlaceOrderAsync(processPaymentRequest);
            if (result.Success)
            {
                var postProcessRequest = new PostProcessPaymentRequest { Order = result.PlacedOrder! };
                await paymentService.PostProcessPaymentAsync(postProcessRequest);
                return RedirectToAction("Completed", new { orderId = result.PlacedOrder!.Id });
            }

            foreach (var error in result.Errors)
                model.Warnings.Add(error);
        }
        catch (Exception exc)
        {
            model.Warnings.Add(exc.Message);
        }

        return View("Confirm", model);
    }

    // --- Completed ---

    public async Task<IActionResult> Completed(int? orderId)
    {
        if (await IsGuestNotAllowedAsync())
            return Challenge();

        Order? order = null;
        if (orderId.HasValue)
            order = await orderService.GetOrderByIdAsync(orderId.Value);

        order ??= (await orderService.SearchOrdersAsync(
            storeId: storeContext.CurrentStore.Id,
            customerId: workContext.CurrentCustomer.Id,
            pageSize: 1)).FirstOrDefault();

        if (order == null || order.Deleted || workContext.CurrentCustomer.Id != order.CustomerId)
            return RedirectToAction("Index", "Home");

        if (orderSettings.DisableOrderCompletedPage)
            return RedirectToAction("OrderDetails", "Order", new { orderId = order.Id });

        return View(new CheckoutCompletedModel { OrderId = order.Id });
    }
}
