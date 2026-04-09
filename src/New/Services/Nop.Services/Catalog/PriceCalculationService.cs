using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Customers;

namespace Nop.Services.Catalog;

public class PriceCalculationService : IPriceCalculationService
{
    private readonly IRepository<TierPrice> _tierPriceRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IProductAttributeParser _productAttributeParser;
    private readonly IProductAttributeService _productAttributeService;
    private readonly IProductService _productService;
    private readonly CatalogSettings _catalogSettings;

    public PriceCalculationService(
        IRepository<TierPrice> tierPriceRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IProductAttributeParser productAttributeParser,
        IProductAttributeService productAttributeService,
        IProductService productService,
        CatalogSettings catalogSettings)
    {
        _tierPriceRepository = tierPriceRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _productAttributeParser = productAttributeParser;
        _productAttributeService = productAttributeService;
        _productService = productService;
        _catalogSettings = catalogSettings;
    }

    public virtual Task<decimal> GetFinalPriceAsync(Product product, Customer customer,
        decimal additionalCharge = 0m, bool includeDiscounts = true, int quantity = 1)
    {
        var (finalPrice, _) = GetFinalPriceInternal(product, customer, additionalCharge, includeDiscounts, quantity);
        return Task.FromResult(finalPrice);
    }

    public virtual Task<(decimal FinalPrice, decimal DiscountAmount)> GetFinalPriceWithDiscountAsync(Product product, Customer customer,
        decimal additionalCharge = 0m, bool includeDiscounts = true, int quantity = 1,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null)
    {
        var result = GetFinalPriceInternal(product, customer, additionalCharge, includeDiscounts, quantity, rentalStartDate, rentalEndDate);
        return Task.FromResult(result);
    }

    private (decimal FinalPrice, decimal DiscountAmount) GetFinalPriceInternal(Product product, Customer customer,
        decimal additionalCharge, bool includeDiscounts, int quantity,
        DateTime? rentalStartDate = null, DateTime? rentalEndDate = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        var finalPrice = product.Price;

        // tier price
        if (product.HasTierPrices)
        {
            var tierPrice = GetMinimumTierPrice(product, customer, quantity);
            if (tierPrice.HasValue && tierPrice.Value < finalPrice)
                finalPrice = tierPrice.Value;
        }

        // additional charge (attribute price adjustments)
        finalPrice += additionalCharge;

        // customer entered price
        if (product.CustomerEntersPrice)
            return (finalPrice, 0m);

        // rental period
        if (product.IsRental && rentalStartDate.HasValue && rentalEndDate.HasValue)
        {
            var rentalPeriods = GetRentalPeriods(product, rentalStartDate.Value, rentalEndDate.Value);
            finalPrice *= rentalPeriods;
        }

        // discount support deferred to [4.5] IDiscountService
        var discountAmount = 0m;

        finalPrice = Math.Max(finalPrice, 0m);
        finalPrice = Math.Round(finalPrice, 2);

        return (finalPrice, discountAmount);
    }

    public virtual async Task<decimal> GetUnitPriceAsync(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);
        var product = await _productService.GetProductByIdAsync(shoppingCartItem.ProductId);
        if (product == null) return 0m;

        if (product.CustomerEntersPrice)
            return shoppingCartItem.CustomerEnteredPrice;

        // attribute price adjustments
        var attributeCharge = await GetAttributePriceAdjustmentAsync(shoppingCartItem.AttributesXml);

        var (finalPrice, _) = GetFinalPriceInternal(product, null!, attributeCharge, includeDiscounts,
            shoppingCartItem.Quantity, shoppingCartItem.RentalStartDateUtc, shoppingCartItem.RentalEndDateUtc);
        return finalPrice;
    }

    public virtual async Task<decimal> GetSubTotalAsync(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true)
    {
        var unitPrice = await GetUnitPriceAsync(shoppingCartItem, includeDiscounts);
        return unitPrice * shoppingCartItem.Quantity;
    }

    public virtual async Task<decimal> GetProductCostAsync(Product product, string attributesXml)
    {
        ArgumentNullException.ThrowIfNull(product);
        var cost = product.ProductCost;

        var attributeValues = await _productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
        foreach (var value in attributeValues)
        {
            if (value.AttributeValueTypeId == (int)AttributeValueType.AssociatedToProduct)
            {
                var associatedProduct = await _productService.GetProductByIdAsync(value.AssociatedProductId);
                if (associatedProduct != null)
                    cost += associatedProduct.ProductCost * value.Quantity;
            }
            else
            {
                cost += value.Cost;
            }
        }

        return cost;
    }

    public virtual Task<decimal> GetProductAttributeValuePriceAdjustmentAsync(ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.AttributeValueTypeId == (int)AttributeValueType.AssociatedToProduct)
        {
            var product = _productService.GetProductByIdAsync(value.AssociatedProductId).GetAwaiter().GetResult();
            return Task.FromResult(product?.Price ?? 0m);
        }
        return Task.FromResult(value.PriceAdjustment);
    }

    private async Task<decimal> GetAttributePriceAdjustmentAsync(string? attributesXml)
    {
        if (string.IsNullOrEmpty(attributesXml)) return 0m;
        var adjustment = 0m;
        var values = await _productAttributeParser.ParseProductAttributeValuesAsync(attributesXml);
        foreach (var value in values)
            adjustment += await GetProductAttributeValuePriceAdjustmentAsync(value);
        return adjustment;
    }

    private decimal? GetMinimumTierPrice(Product product, Customer? customer, int quantity)
    {
        var tierPrices = _tierPriceRepository.TableNoTracking
            .Where(tp => tp.ProductId == product.Id && tp.Quantity <= quantity)
            .OrderBy(tp => tp.Quantity).ToList();

        if (tierPrices.Count == 0) return null;

        // filter by customer role if applicable
        if (customer != null)
        {
            var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
                .Where(m => m.CustomerId == customer.Id)
                .Select(m => m.CustomerRoleId).ToHashSet();

            tierPrices = tierPrices
                .Where(tp => !tp.CustomerRoleId.HasValue || tp.CustomerRoleId == 0 || customerRoleIds.Contains(tp.CustomerRoleId.Value))
                .ToList();
        }

        // filter by store
        tierPrices = tierPrices.Where(tp => tp.StoreId == 0).ToList(); // store filtering simplified

        return tierPrices.Count > 0 ? tierPrices.Min(tp => tp.Price) : null;
    }

    private static int GetRentalPeriods(Product product, DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate) return 1;
        var totalDays = (endDate - startDate).TotalDays;
        return product.RentalPricePeriodId switch
        {
            (int)RentalPricePeriod.Days => Math.Max((int)Math.Ceiling(totalDays), 1),
            (int)RentalPricePeriod.Weeks => Math.Max((int)Math.Ceiling(totalDays / 7), 1),
            (int)RentalPricePeriod.Months => Math.Max((int)Math.Ceiling(totalDays / 30), 1),
            (int)RentalPricePeriod.Years => Math.Max((int)Math.Ceiling(totalDays / 365), 1),
            _ => 1
        };
    }
}
