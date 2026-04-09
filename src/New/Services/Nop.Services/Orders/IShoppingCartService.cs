using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IShoppingCartService
{
    Task<IList<ShoppingCartItem>> GetShoppingCartAsync(Customer customer,
        ShoppingCartType? shoppingCartType = null, int storeId = 0);

    Task DeleteShoppingCartItemAsync(ShoppingCartItem shoppingCartItem,
        bool resetCheckoutData = true, bool ensureOnlyActiveCheckoutAttributes = false);

    Task<int> DeleteExpiredShoppingCartItemsAsync(DateTime olderThanUtc);

    Task<IList<string>> GetRequiredProductWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        int storeId, bool automaticallyAddRequiredProductsIfEnabled);

    Task<IList<string>> GetStandardWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        string? attributesXml, decimal customerEnteredPrice, int quantity);

    Task<IList<string>> GetShoppingCartItemAttributeWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product,
        int quantity = 1, string? attributesXml = "",
        bool ignoreNonCombinableAttributes = false);

    Task<IList<string>> GetShoppingCartItemGiftCardWarningsAsync(
        ShoppingCartType shoppingCartType, Product product, string? attributesXml);

    Task<IList<string>> GetRentalProductWarningsAsync(Product product,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null);

    Task<IList<string>> GetShoppingCartItemWarningsAsync(Customer customer,
        ShoppingCartType shoppingCartType, Product product, int storeId,
        string? attributesXml, decimal customerEnteredPrice,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true,
        bool getStandardWarnings = true, bool getAttributesWarnings = true,
        bool getGiftCardWarnings = true, bool getRequiredProductWarnings = true,
        bool getRentalWarnings = true);

    Task<IList<string>> GetShoppingCartWarningsAsync(IList<ShoppingCartItem> shoppingCart,
        string? checkoutAttributesXml, bool validateCheckoutAttributes);

    Task<ShoppingCartItem?> FindShoppingCartItemInTheCartAsync(IList<ShoppingCartItem> shoppingCart,
        ShoppingCartType shoppingCartType, Product product,
        string? attributesXml = "", decimal customerEnteredPrice = decimal.Zero,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null);

    Task<IList<string>> AddToCartAsync(Customer customer, Product product,
        ShoppingCartType shoppingCartType, int storeId,
        string? attributesXml = null, decimal customerEnteredPrice = decimal.Zero,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool automaticallyAddRequiredProductsIfEnabled = true);

    Task<IList<string>> UpdateShoppingCartItemAsync(Customer customer,
        int shoppingCartItemId, string? attributesXml,
        decimal customerEnteredPrice,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null,
        int quantity = 1, bool resetCheckoutData = true);

    Task MigrateShoppingCartAsync(Customer fromCustomer, Customer toCustomer,
        bool includeCouponCodes);

    Task<(string? Error, int CycleLength, RecurringProductCyclePeriod CyclePeriod, int TotalCycles)>
        GetRecurringCycleInfoAsync(IList<ShoppingCartItem> cart);
}
