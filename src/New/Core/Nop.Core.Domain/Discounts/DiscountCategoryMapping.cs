namespace Nop.Core.Domain.Discounts;

/// <summary>
/// Join entity for Discount ↔ Category many-to-many (table: Discount_AppliedToCategories).
/// </summary>
public class DiscountCategoryMapping : BaseEntity
{
    public int DiscountId { get; set; }
    public int CategoryId { get; set; }
}
