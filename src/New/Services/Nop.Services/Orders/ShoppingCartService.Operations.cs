using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Security;

namespace Nop.Services.Orders;

public partial class ShoppingCartService
{
    public virtual async Task<ShoppingCartItem?> FindShoppingCartItemInTheCartAsync(
        IList<ShoppingCartItem> shoppingCart, ShoppingCartType shoppingCartType,
        Product product, string? attributesXml = "",
        decimal customerEnteredPrice = decimal.Zero,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null)
    {
        ArgumentNullException.ThrowIfNull(shoppingCart);
        ArgumentNullException.ThrowIfNull(product);

        foreach (var sci in shoppingCart.Where(a => a.ShoppingCartType == shoppingCartType))
        {
            if (sci.ProductId != product.Id) continue;

            var attributesEqual = await productAttributeParser.AreProductAttributesEqualAsync(
                sci.AttributesXml ?? string.Empty, attributesXml ?? string.Empty, false, false);

            bool giftCardInfoSame = true;
            if (product.IsGiftCard)
            {
                productAttributeParser.GetGiftCardAttribute(attributesXml ?? string.Empty,
                    out var rn1, out _, out var sn1, out _, out _);
                productAttributeParser.GetGiftCardAttribute(sci.AttributesXml ?? string.Empty,
                    out var rn2, out _, out var sn2, out _, out _);
                if (!string.Equals(rn1, rn2, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(sn1, sn2, StringComparison.OrdinalIgnoreCase))
                    giftCardInfoSame = false;
            }

            bool pricesEqual = !product.CustomerEntersPrice ||
                Math.Round(sci.CustomerEnteredPrice, 2) == Math.Round(customerEnteredPrice, 2);

            bool rentalEqual = !product.IsRental ||
                (sci.RentalStartDateUtc == rentalStartDate && sci.RentalEndDateUtc == rentalEndDate);

            if (attributesEqual && giftCardInfoSame && pricesEqual && rentalEqual)
                return sci;
        }
        return null;
    }

    public virtual async Task<IList<string>> AddToCartAsync(Customer customer, Product product,
        ShoppingCartType shoppingCartType, int storeId,
        string? attributesXml = null, decimal customerEnteredPrice = decimal.Zero,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(product);
        var warnings = new List<string>();

        if (shoppingCartType == ShoppingCartType.ShoppingCart &&
            !permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart, customer))
        {
            warnings.Add("Shopping cart is disabled");
            return warnings;
        }
        if (shoppingCartType == ShoppingCartType.Wishlist &&
            !permissionService.Authorize(StandardPermissionProvider.EnableWishlist, customer))
        {
            warnings.Add("Wishlist is disabled");
            return warnings;
        }

        var searchEngine = await customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.SearchEngine);
        if (searchEngine != null && customer.Id == searchEngine.Id)
        {
            warnings.Add("Search engine can't add to cart");
            return warnings;
        }

        if (quantity <= 0)
        {
            warnings.Add(await localizationService.GetResourceAsync("ShoppingCart.QuantityShouldPositive"));
            return warnings;
        }

        await customerService.ResetCheckoutDataAsync(customer, storeId);

        var cart = await GetShoppingCartAsync(customer, shoppingCartType, storeId);
        var existingItem = await FindShoppingCartItemInTheCartAsync(cart, shoppingCartType, product,
            attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate);

        if (existingItem != null)
        {
            int newQuantity = existingItem.Quantity + quantity;
            warnings.AddRange(await GetShoppingCartItemWarningsAsync(customer, shoppingCartType, product,
                storeId, attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate,
                newQuantity, automaticallyAddRequiredProductsIfEnabled));

            if (!warnings.Any())
            {
                existingItem.AttributesXml = attributesXml;
                existingItem.Quantity = newQuantity;
                existingItem.UpdatedOnUtc = DateTime.UtcNow;
                sciRepository.Update(existingItem);
                await eventPublisher.EntityUpdatedAsync(existingItem);
            }
        }
        else
        {
            warnings.AddRange(await GetShoppingCartItemWarningsAsync(customer, shoppingCartType, product,
                storeId, attributesXml, customerEnteredPrice, rentalStartDate, rentalEndDate,
                quantity, automaticallyAddRequiredProductsIfEnabled));

            if (!warnings.Any())
            {
                if (shoppingCartType == ShoppingCartType.ShoppingCart &&
                    cart.Count >= shoppingCartSettings.MaximumShoppingCartItems)
                {
                    warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.MaximumShoppingCartItems"),
                        shoppingCartSettings.MaximumShoppingCartItems));
                    return warnings;
                }
                if (shoppingCartType == ShoppingCartType.Wishlist &&
                    cart.Count >= shoppingCartSettings.MaximumWishlistItems)
                {
                    warnings.Add(string.Format(await localizationService.GetResourceAsync("ShoppingCart.MaximumWishlistItems"),
                        shoppingCartSettings.MaximumWishlistItems));
                    return warnings;
                }

                var now = DateTime.UtcNow;
                var sci = new ShoppingCartItem
                {
                    ShoppingCartType = shoppingCartType,
                    StoreId = storeId,
                    CustomerId = customer.Id,
                    ProductId = product.Id,
                    AttributesXml = attributesXml,
                    CustomerEnteredPrice = customerEnteredPrice,
                    Quantity = quantity,
                    RentalStartDateUtc = rentalStartDate,
                    RentalEndDateUtc = rentalEndDate,
                    CreatedOnUtc = now,
                    UpdatedOnUtc = now
                };
                sciRepository.Insert(sci);

                customer.HasShoppingCartItems = true;
                await customerService.UpdateCustomerAsync(customer);
                await eventPublisher.EntityInsertedAsync(sci);
            }
        }
        return warnings;
    }

    public virtual async Task<IList<string>> UpdateShoppingCartItemAsync(Customer customer,
        int shoppingCartItemId, string? attributesXml,
        decimal customerEnteredPrice,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool resetCheckoutData = true)
    {
        ArgumentNullException.ThrowIfNull(customer);
        var warnings = new List<string>();

        var sci = sciRepository.Table.FirstOrDefault(x => x.Id == shoppingCartItemId && x.CustomerId == customer.Id);
        if (sci == null) return warnings;

        if (resetCheckoutData)
            await customerService.ResetCheckoutDataAsync(customer, sci.StoreId);

        if (quantity > 0)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null)
            {
                warnings.Add("Product not found");
                return warnings;
            }

            warnings.AddRange(await GetShoppingCartItemWarningsAsync(customer, sci.ShoppingCartType,
                product, sci.StoreId, attributesXml, customerEnteredPrice,
                rentalStartDate, rentalEndDate, quantity, false));

            if (!warnings.Any())
            {
                sci.Quantity = quantity;
                sci.AttributesXml = attributesXml;
                sci.CustomerEnteredPrice = customerEnteredPrice;
                sci.RentalStartDateUtc = rentalStartDate;
                sci.RentalEndDateUtc = rentalEndDate;
                sci.UpdatedOnUtc = DateTime.UtcNow;
                sciRepository.Update(sci);
                await eventPublisher.EntityUpdatedAsync(sci);
            }
        }
        else
        {
            await DeleteShoppingCartItemAsync(sci, resetCheckoutData, true);
        }
        return warnings;
    }

    public virtual async Task MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer,
        bool includeCouponCodes)
    {
        ArgumentNullException.ThrowIfNull(fromCustomer);
        ArgumentNullException.ThrowIfNull(toCustomer);
        if (fromCustomer.Id == toCustomer.Id) return;

        var fromCart = (await GetShoppingCartAsync(fromCustomer)).ToList();
        foreach (var sci in fromCart)
        {
            var product = await productService.GetProductByIdAsync(sci.ProductId);
            if (product == null) continue;
            await AddToCartAsync(toCustomer, product, sci.ShoppingCartType, sci.StoreId,
                sci.AttributesXml, sci.CustomerEnteredPrice,
                sci.RentalStartDateUtc, sci.RentalEndDateUtc, sci.Quantity, false);
        }
        foreach (var sci in fromCart)
            await DeleteShoppingCartItemAsync(sci);

        if (includeCouponCodes)
        {
            // migrate discount coupon codes
            var discountCodes = await fromCustomer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.DiscountCouponCode, genericAttributeService);
            if (!string.IsNullOrEmpty(discountCodes))
            {
                var existing = await toCustomer.GetAttributeAsync<string>(
                    SystemCustomerAttributeNames.DiscountCouponCode, genericAttributeService) ?? string.Empty;
                var merged = string.IsNullOrEmpty(existing) ? discountCodes : $"{existing},{discountCodes}";
                await genericAttributeService.SaveAttributeAsync(toCustomer,
                    SystemCustomerAttributeNames.DiscountCouponCode, merged);
            }

            // migrate gift card coupon codes
            var giftCardCodes = await fromCustomer.GetAttributeAsync<string>(
                SystemCustomerAttributeNames.GiftCardCouponCodes, genericAttributeService);
            if (!string.IsNullOrEmpty(giftCardCodes))
            {
                var existingGc = await toCustomer.GetAttributeAsync<string>(
                    SystemCustomerAttributeNames.GiftCardCouponCodes, genericAttributeService);
                if (string.IsNullOrEmpty(existingGc))
                    await genericAttributeService.SaveAttributeAsync(toCustomer,
                        SystemCustomerAttributeNames.GiftCardCouponCodes, giftCardCodes);
            }

            await customerService.UpdateCustomerAsync(toCustomer);
        }
    }
}
