using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IOrderTotalCalculationService
{
    /// <summary>
    /// Gets shopping cart subtotal.
    /// </summary>
    /// <returns>(discountAmount, appliedDiscounts, subTotalWithoutDiscount, subTotalWithDiscount, taxRates)</returns>
    Task<(decimal DiscountAmount, List<Discount> AppliedDiscounts,
        decimal SubTotalWithoutDiscount, decimal SubTotalWithDiscount,
        SortedDictionary<decimal, decimal> TaxRates)>
        GetShoppingCartSubTotalAsync(IList<ShoppingCartItem> cart, bool includingTax);

    /// <summary>
    /// Adjust shipping rate (free shipping, additional charges, discounts).
    /// </summary>
    /// <returns>(adjustedRate, appliedDiscounts)</returns>
    Task<(decimal AdjustedRate, List<Discount> AppliedDiscounts)>
        AdjustShippingRateAsync(decimal shippingRate, IList<ShoppingCartItem> cart);

    /// <summary>
    /// Gets shopping cart additional shipping charge.
    /// </summary>
    Task<decimal> GetShoppingCartAdditionalShippingChargeAsync(IList<ShoppingCartItem> cart);

    /// <summary>
    /// Gets a value indicating whether shipping is free.
    /// </summary>
    Task<bool> IsFreeShippingAsync(IList<ShoppingCartItem> cart, decimal? subTotal = null);

    /// <summary>
    /// Gets shopping cart shipping total.
    /// </summary>
    /// <returns>(shippingTotal, taxRate, appliedDiscounts) — null shippingTotal if cannot be calculated</returns>
    Task<(decimal? ShippingTotal, decimal TaxRate, List<Discount> AppliedDiscounts)>
        GetShoppingCartShippingTotalAsync(IList<ShoppingCartItem> cart, bool includingTax);

    /// <summary>
    /// Gets tax total.
    /// </summary>
    /// <returns>(taxTotal, taxRates)</returns>
    Task<(decimal TaxTotal, SortedDictionary<decimal, decimal> TaxRates)>
        GetTaxTotalAsync(IList<ShoppingCartItem> cart, bool usePaymentMethodAdditionalFee = true);

    /// <summary>
    /// Gets shopping cart total.
    /// </summary>
    /// <returns>Null if total couldn't be calculated (e.g. shipping not selected)</returns>
    Task<ShoppingCartTotal?> GetShoppingCartTotalAsync(IList<ShoppingCartItem> cart,
        bool? useRewardPoints = null, bool usePaymentMethodAdditionalFee = true);

    /// <summary>
    /// Converts existing reward points to amount.
    /// </summary>
    decimal ConvertRewardPointsToAmount(int rewardPoints);

    /// <summary>
    /// Converts an amount to reward points.
    /// </summary>
    int ConvertAmountToRewardPoints(decimal amount);

    /// <summary>
    /// Gets a value indicating whether a customer has minimum amount of reward points to use.
    /// </summary>
    bool CheckMinimumRewardPointsToUseRequirement(int rewardPoints);

    /// <summary>
    /// Calculate how much of the order total reward points apply to (excludes shipping).
    /// </summary>
    decimal CalculateApplicableOrderTotalForRewardPoints(decimal orderShippingInclTax, decimal orderTotal);

    /// <summary>
    /// Calculate how many reward points will be earned based on amount spent.
    /// </summary>
    Task<int> CalculateRewardPointsAsync(Customer customer, decimal amount);
}

/// <summary>
/// Result of GetShoppingCartTotalAsync.
/// </summary>
public sealed class ShoppingCartTotal
{
    public decimal OrderTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public List<Discount> AppliedDiscounts { get; init; } = [];
    public List<AppliedGiftCard> AppliedGiftCards { get; init; } = [];
    public int RedeemedRewardPoints { get; init; }
    public decimal RedeemedRewardPointsAmount { get; init; }
}

/// <summary>
/// Represents a gift card applied to an order total.
/// </summary>
public sealed class AppliedGiftCard
{
    public required GiftCard GiftCard { get; init; }
    public decimal AmountCanBeUsed { get; init; }
}
