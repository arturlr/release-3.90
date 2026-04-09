namespace Nop.Core.Domain.Discounts;

/// <summary>
/// Join entity for Discount ↔ Product many-to-many (table: Discount_AppliedToProducts).
/// </summary>
public class DiscountProductMapping : BaseEntity
{
    public int DiscountId { get; set; }
    public int ProductId { get; set; }
}
