namespace Nop.Core.Domain.Discounts;

/// <summary>
/// Join entity for Discount ↔ Manufacturer many-to-many (table: Discount_AppliedToManufacturers).
/// </summary>
public class DiscountManufacturerMapping : BaseEntity
{
    public int DiscountId { get; set; }
    public int ManufacturerId { get; set; }
}
