using Nop.Core.Domain.Discounts;

namespace Nop.Services.Discounts;

public static class DiscountExtensions
{
    /// <summary>
    /// Gets the discount amount for the specified value.
    /// </summary>
    public static decimal GetDiscountAmount(this Discount discount, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(discount);

        decimal result;
        if (discount.UsePercentage)
            result = amount * discount.DiscountPercentage / 100m;
        else
            result = discount.DiscountAmount;

        // cap at maximum discount amount for percentage discounts
        if (discount.UsePercentage && discount.MaximumDiscountAmount.HasValue && result > discount.MaximumDiscountAmount.Value)
            result = discount.MaximumDiscountAmount.Value;

        return Math.Max(result, 0m);
    }

    /// <summary>
    /// Get preferred discount(s) — the combination yielding the maximum discount value.
    /// Compares best single discount vs sum of all cumulative discounts.
    /// </summary>
    public static List<Discount> GetPreferredDiscount(this IList<Discount> discounts, decimal amount, out decimal discountAmount)
    {
        ArgumentNullException.ThrowIfNull(discounts);

        var result = new List<Discount>();
        discountAmount = 0m;
        if (discounts.Count == 0) return result;

        // best single discount
        foreach (var discount in discounts)
        {
            var currentValue = discount.GetDiscountAmount(amount);
            if (currentValue > discountAmount)
            {
                discountAmount = currentValue;
                result.Clear();
                result.Add(discount);
            }
        }

        // cumulative discounts — sum of all IsCumulative discounts
        var cumulativeDiscounts = discounts.Where(x => x.IsCumulative).OrderBy(x => x.Name).ToList();
        if (cumulativeDiscounts.Count > 1)
        {
            var cumulativeAmount = cumulativeDiscounts.Sum(d => d.GetDiscountAmount(amount));
            if (cumulativeAmount > discountAmount)
            {
                discountAmount = cumulativeAmount;
                result.Clear();
                result.AddRange(cumulativeDiscounts);
            }
        }

        return result;
    }

    /// <summary>
    /// Check whether a list of discounts already contains a certain discount instance (by Id).
    /// </summary>
    public static bool ContainsDiscount(this IList<Discount> discounts, Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discounts);
        ArgumentNullException.ThrowIfNull(discount);
        return discounts.Any(d => d.Id == discount.Id);
    }
}
