using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Catalog;

public interface IPriceCalculationService
{
    Task<decimal> GetFinalPriceAsync(Product product, Customer customer,
        decimal additionalCharge = 0m, bool includeDiscounts = true, int quantity = 1);
    Task<(decimal FinalPrice, decimal DiscountAmount)> GetFinalPriceWithDiscountAsync(Product product, Customer customer,
        decimal additionalCharge = 0m, bool includeDiscounts = true, int quantity = 1,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null);
    Task<decimal> GetUnitPriceAsync(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true);
    Task<decimal> GetSubTotalAsync(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true);
    Task<decimal> GetProductCostAsync(Product product, string attributesXml);
    Task<decimal> GetProductAttributeValuePriceAdjustmentAsync(ProductAttributeValue value);
}
