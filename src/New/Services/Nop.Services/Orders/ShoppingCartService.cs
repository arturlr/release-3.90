using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Stores;

namespace Nop.Services.Orders;

public partial class ShoppingCartService(
    IRepository<ShoppingCartItem> sciRepository,
    IWorkContext workContext,
    IStoreContext storeContext,
    ICurrencyService currencyService,
    IProductService productService,
    ILocalizationService localizationService,
    IProductAttributeParser productAttributeParser,
    ICheckoutAttributeService checkoutAttributeService,
    ICheckoutAttributeParser checkoutAttributeParser,
    IPriceFormatter priceFormatter,
    ICustomerService customerService,
    ShoppingCartSettings shoppingCartSettings,
    IEventPublisher eventPublisher,
    IPermissionService permissionService,
    IAclService aclService,
    IDateRangeService dateRangeService,
    IStoreMappingService storeMappingService,
    IGenericAttributeService genericAttributeService,
    IProductAttributeService productAttributeService,
    IDateTimeHelper dateTimeHelper) : IShoppingCartService
{
    public virtual async Task<IList<ShoppingCartItem>> GetShoppingCartAsync(Customer customer,
        ShoppingCartType? shoppingCartType = null, int storeId = 0)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var items = sciRepository.Table.Where(sci => sci.CustomerId == customer.Id);
        if (shoppingCartType.HasValue)
            items = items.Where(sci => sci.ShoppingCartTypeId == (int)shoppingCartType.Value);
        if (storeId > 0)
            items = items.Where(sci => sci.StoreId == storeId);
        return await Task.FromResult(items.OrderByDescending(sci => sci.CreatedOnUtc).ToList());
    }

    public virtual async Task DeleteShoppingCartItemAsync(ShoppingCartItem shoppingCartItem,
        bool resetCheckoutData = true, bool ensureOnlyActiveCheckoutAttributes = false)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);
        var customerId = shoppingCartItem.CustomerId;
        var storeId = shoppingCartItem.StoreId;

        if (resetCheckoutData)
        {
            var customer = await customerService.GetCustomerByIdAsync(customerId);
            if (customer != null)
                await customerService.ResetCheckoutDataAsync(customer, storeId);
        }

        sciRepository.Delete(shoppingCartItem);

        var cust = await customerService.GetCustomerByIdAsync(customerId);
        if (cust != null)
        {
            cust.HasShoppingCartItems = sciRepository.Table.Any(sci => sci.CustomerId == customerId);
            await customerService.UpdateCustomerAsync(cust);
        }

        if (ensureOnlyActiveCheckoutAttributes &&
            shoppingCartItem.ShoppingCartType == ShoppingCartType.ShoppingCart && cust != null)
        {
            var cart = await GetShoppingCartAsync(cust, ShoppingCartType.ShoppingCart, storeId);
            var checkoutAttributesXml = await cust.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.CheckoutAttributes, genericAttributeService, storeId);
            checkoutAttributesXml = await checkoutAttributeParser.EnsureOnlyActiveAttributesAsync(
                checkoutAttributesXml ?? string.Empty, cart);
            await genericAttributeService.SaveAttributeAsync(cust,
                SystemCustomerAttributeNames.CheckoutAttributes, checkoutAttributesXml, storeId);
        }

        await eventPublisher.EntityDeletedAsync(shoppingCartItem);
    }

    public virtual async Task<int> DeleteExpiredShoppingCartItemsAsync(DateTime olderThanUtc)
    {
        var items = sciRepository.Table.Where(sci => sci.UpdatedOnUtc < olderThanUtc).ToList();
        foreach (var item in items)
            sciRepository.Delete(item);
        return await Task.FromResult(items.Count);
    }

    public virtual async Task<IList<string>> GetRequiredProductWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        int storeId, bool automaticallyAddRequiredProductsIfEnabled)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);

        var cart = await GetShoppingCartAsync(customer, shoppingCartType, storeId);
        var warnings = new List<string>();

        if (!product.RequireOtherProducts)
            return warnings;

        foreach (var rpId in ParseRequiredProductIds(product))
        {
            var rp = await productService.GetProductByIdAsync(rpId);
            if (rp == null) continue;
            if (cart.Any(sci => sci.ProductId == rp.Id)) continue;

            if (product.AutomaticallyAddRequiredProducts && automaticallyAddRequiredProductsIfEnabled)
            {
                var addWarnings = await AddToCartAsync(customer, rp, shoppingCartType, storeId,
                    automaticallyAddRequiredProductsIfEnabled: false);
                if (addWarnings.Any())
                    warnings.Add(string.Format(
                        await localizationService.GetResourceAsync("ShoppingCart.RequiredProductWarning"),
                        rp.Name ?? string.Empty));
            }
            else
            {
                warnings.Add(string.Format(
                    await localizationService.GetResourceAsync("ShoppingCart.RequiredProductWarning"),
                    rp.Name ?? string.Empty));
            }
        }
        return warnings;
    }

    public virtual async Task<IList<string>> GetStandardWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        string? attributesXml, decimal customerEnteredPrice, int quantity)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();

        if (product.Deleted)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.ProductDeleted"));
            return warnings;
        }
        if (!product.Published)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.ProductUnpublished"));
        if (product.ProductType != ProductType.SimpleProduct)
            warnings.Add("This is not simple product");
        if (!aclService.Authorize(product, customer))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.ProductUnpublished"));
        if (!await storeMappingService.AuthorizeAsync(product, storeContext.CurrentStore.Id))
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.ProductUnpublished"));
        if (shoppingCartType == ShoppingCartType.ShoppingCart && product.DisableBuyButton)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.BuyingDisabled"));
        if (shoppingCartType == ShoppingCartType.Wishlist && product.DisableWishlistButton)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.WishlistDisabled"));
        if (shoppingCartType == ShoppingCartType.ShoppingCart && product.CallForPrice)
            warnings.Add(await localizationService.GetResourceAsync("Products.CallForPrice"));

        if (product.CustomerEntersPrice &&
            (customerEnteredPrice < product.MinimumCustomerEnteredPrice ||
             customerEnteredPrice > product.MaximumCustomerEnteredPrice))
        {
            var min = currencyService.ConvertFromPrimaryStoreCurrency(
                product.MinimumCustomerEnteredPrice, workContext.WorkingCurrency);
            var max = currencyService.ConvertFromPrimaryStoreCurrency(
                product.MaximumCustomerEnteredPrice, workContext.WorkingCurrency);
            warnings.Add(string.Format(
                await localizationService.GetResourceAsync("ShoppingCart.CustomerEnteredPrice.RangeError"),
                await priceFormatter.FormatPriceAsync(min, false, false),
                await priceFormatter.FormatPriceAsync(max, false, false)));
        }

        var hasQtyWarnings = false;
        if (quantity < product.OrderMinimumQuantity)
        {
            warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.MinimumQuantity"), product.OrderMinimumQuantity));
            hasQtyWarnings = true;
        }
        if (quantity > product.OrderMaximumQuantity)
        {
            warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.MaximumQuantity"), product.OrderMaximumQuantity));
            hasQtyWarnings = true;
        }
        var allowedQuantities = ParseAllowedQuantities(product);
        if (allowedQuantities.Length > 0 && !allowedQuantities.Contains(quantity))
            warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.AllowedQuantities"),
                string.Join(", ", allowedQuantities)));

        var validateOutOfStock = shoppingCartType == ShoppingCartType.ShoppingCart ||
            !shoppingCartSettings.AllowOutOfStockItemsToBeAddedToWishlist;
        if (validateOutOfStock && !hasQtyWarnings)
            await ValidateStockAsync(product, attributesXml, quantity, warnings);

        if (product.AvailableStartDateTimeUtc.HasValue && product.AvailableStartDateTimeUtc.Value > DateTime.UtcNow)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.NotAvailable"));
        else if (product.AvailableEndDateTimeUtc.HasValue && product.AvailableEndDateTimeUtc.Value < DateTime.UtcNow)
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.NotAvailable"));

        return warnings;
    }

    private async Task ValidateStockAsync(Product product, string? attributesXml, int quantity, List<string> warnings)
    {
        switch (product.ManageInventoryMethod)
        {
            case ManageInventoryMethod.ManageStock:
                if (product.BackorderMode == BackorderMode.NoBackorders)
                {
                    var stock = product.StockQuantity;
                    if (stock < quantity)
                    {
                        if (stock <= 0)
                        {
                            var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(product.ProductAvailabilityRangeId);
                            warnings.Add(par == null
                                ? await localizationService.GetResourceAsync("ShoppingCart.OutOfStock")
                                : string.Format(await localizationService.GetResourceAsync("ShoppingCart.AvailabilityRange"), par.Name ?? string.Empty));
                        }
                        else
                            warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.QuantityExceedsStock"), stock));
                    }
                }
                break;
            case ManageInventoryMethod.ManageStockByAttributes:
                var combination = await productAttributeParser.FindProductAttributeCombinationAsync(product, attributesXml ?? string.Empty);
                if (combination != null)
                {
                    if (!combination.AllowOutOfStockOrders && combination.StockQuantity < quantity)
                    {
                        if (combination.StockQuantity <= 0)
                        {
                            var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(product.ProductAvailabilityRangeId);
                            warnings.Add(par == null
                                ? await localizationService.GetResourceAsync("ShoppingCart.OutOfStock")
                                : string.Format(await localizationService.GetResourceAsync("ShoppingCart.AvailabilityRange"), par.Name ?? string.Empty));
                        }
                        else
                            warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.QuantityExceedsStock"), combination.StockQuantity));
                    }
                }
                else if (product.AllowAddingOnlyExistingAttributeCombinations)
                {
                    var par = await dateRangeService.GetProductAvailabilityRangeByIdAsync(product.ProductAvailabilityRangeId);
                    warnings.Add(par == null
                        ? await localizationService.GetResourceAsync("ShoppingCart.OutOfStock")
                        : string.Format(await localizationService.GetResourceAsync("ShoppingCart.AvailabilityRange"), par.Name ?? string.Empty));
                }
                break;
        }
    }

    private static int[] ParseRequiredProductIds(Product product)
    {
        if (string.IsNullOrEmpty(product.RequiredProductIds)) return [];
        return product.RequiredProductIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var val) ? val : 0)
            .Where(id => id > 0).ToArray();
    }

    private static int[] ParseAllowedQuantities(Product product)
    {
        if (string.IsNullOrEmpty(product.AllowedQuantities)) return [];
        return product.AllowedQuantities
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(q => int.TryParse(q, out var val) ? val : 0)
            .Where(q => q > 0).ToArray();
    }

    private static bool ShouldHaveValues(int attributeControlTypeId)
    {
        var ct = (AttributeControlType)attributeControlTypeId;
        return ct != AttributeControlType.TextBox &&
               ct != AttributeControlType.MultilineTextbox &&
               ct != AttributeControlType.Datepicker &&
               ct != AttributeControlType.FileUpload;
    }
}
