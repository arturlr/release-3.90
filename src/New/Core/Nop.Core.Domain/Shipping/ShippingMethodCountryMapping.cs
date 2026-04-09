namespace Nop.Core.Domain.Shipping;

/// <summary>
/// Join entity for ShippingMethod ↔ Country many-to-many (table: ShippingMethodRestrictions).
/// </summary>
public class ShippingMethodCountryMapping : BaseEntity
{
    public int ShippingMethodId { get; set; }
    public int CountryId { get; set; }
}
