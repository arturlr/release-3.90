using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Customer;

namespace Nop.Web.Controllers;

public partial class CheckoutController(
    IWorkContext workContext,
    IStoreContext storeContext,
    IShoppingCartService shoppingCartService,
    IOrderProcessingService orderProcessingService,
    IOrderService orderService,
    ICustomerService customerService,
    IGenericAttributeService genericAttributeService,
    IPaymentService paymentService,
    IAddressService addressService,
    ILocalizationService localizationService,
    IProductService productService,
    IRepository<CustomerAddressMapping> customerAddressRepository,
    OrderSettings orderSettings,
    ShippingSettings shippingSettings,
    RewardPointsSettings rewardPointsSettings) : BasePublicController
{
    // --- Helpers ---

    private async Task<IList<ShoppingCartItem>> GetCartAsync() =>
        await shoppingCartService.GetShoppingCartAsync(
            workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, storeContext.CurrentStore.Id);

    private async Task<bool> IsGuestNotAllowedAsync()
    {
        var roleIds = await customerService.GetCustomerRoleIdsAsync(workContext.CurrentCustomer);
        var guestRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests);
        if (guestRole == null || !roleIds.Contains(guestRole.Id))
            return false; // not a guest
        return !orderSettings.AnonymousCheckoutAllowed;
    }

    private async Task<bool> CartRequiresShippingAsync(IList<ShoppingCartItem> cart)
    {
        foreach (var sci in cart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product is { IsShipEnabled: true }) return true;
        }
        return false;
    }

    private async Task<IList<AddressModel>> GetCustomerAddressesAsync()
    {
        var mappings = customerAddressRepository.Table
            .Where(m => m.CustomerId == workContext.CurrentCustomer.Id).ToList();
        var list = new List<AddressModel>();
        foreach (var m in mappings)
        {
            var addr = await addressService.GetAddressByIdAsync(m.AddressId);
            if (addr != null)
                list.Add(MapAddress(addr));
        }
        return list;
    }

    private static AddressModel MapAddress(Address a) => new()
    {
        Id = a.Id,
        FirstName = a.FirstName,
        LastName = a.LastName,
        Email = a.Email,
        Company = a.Company,
        CountryId = a.CountryId,
        StateProvinceId = a.StateProvinceId,
        City = a.City,
        Address1 = a.Address1,
        Address2 = a.Address2,
        ZipPostalCode = a.ZipPostalCode,
        PhoneNumber = a.PhoneNumber,
        FaxNumber = a.FaxNumber,
    };

    private static Address MapToAddress(AddressModel m) => new()
    {
        FirstName = m.FirstName,
        LastName = m.LastName,
        Email = m.Email,
        Company = m.Company,
        CountryId = m.CountryId == 0 ? null : m.CountryId,
        StateProvinceId = m.StateProvinceId == 0 ? null : m.StateProvinceId,
        City = m.City,
        Address1 = m.Address1,
        Address2 = m.Address2,
        ZipPostalCode = m.ZipPostalCode,
        PhoneNumber = m.PhoneNumber,
        FaxNumber = m.FaxNumber,
        CreatedOnUtc = DateTime.UtcNow,
    };

    private async Task<Address> FindOrCreateAddressAsync(AddressModel model)
    {
        // Try to find an existing address with the same values (don't duplicate)
        var mappings = customerAddressRepository.Table
            .Where(m => m.CustomerId == workContext.CurrentCustomer.Id).ToList();
        foreach (var mapping in mappings)
        {
            var existing = await addressService.GetAddressByIdAsync(mapping.AddressId);
            if (existing != null && AddressesMatch(existing, model))
                return existing;
        }

        // Create new
        var address = MapToAddress(model);
        await addressService.InsertAddressAsync(address);
        customerAddressRepository.Insert(new CustomerAddressMapping
        {
            CustomerId = workContext.CurrentCustomer.Id,
            AddressId = address.Id,
        });
        return address;
    }

    private static bool AddressesMatch(Address a, AddressModel m) =>
        string.Equals(a.FirstName, m.FirstName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.LastName, m.LastName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Email, m.Email, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.Address1, m.Address1, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.City, m.City, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.ZipPostalCode, m.ZipPostalCode, StringComparison.OrdinalIgnoreCase) &&
        a.CountryId == (m.CountryId == 0 ? null : m.CountryId) &&
        a.StateProvinceId == (m.StateProvinceId == 0 ? null : m.StateProvinceId);

    private async Task SetBillingAddressAsync(int addressId)
    {
        var customer = workContext.CurrentCustomer;
        customer.BillingAddressId = addressId;
        await customerService.UpdateCustomerAsync(customer);
    }

    private async Task SetShippingAddressAsync(int? addressId)
    {
        var customer = workContext.CurrentCustomer;
        customer.ShippingAddressId = addressId;
        await customerService.UpdateCustomerAsync(customer);
    }

    private async Task<bool> IsMinimumOrderPlacementIntervalValidAsync()
    {
        if (orderSettings.MinimumOrderPlacementInterval == 0)
            return true;
        var lastOrder = (await orderService.SearchOrdersAsync(
            storeId: storeContext.CurrentStore.Id,
            customerId: workContext.CurrentCustomer.Id,
            pageSize: 1)).FirstOrDefault();
        if (lastOrder == null)
            return true;
        return (DateTime.UtcNow - lastOrder.CreatedOnUtc).TotalSeconds > orderSettings.MinimumOrderPlacementInterval;
    }
}
