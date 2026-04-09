namespace Nop.Core.Domain.Discounts
{

    public enum DiscountType
    {

        AssignedToOrderTotal = 1,

        AssignedToSkus = 2,

        AssignedToCategories = 5,

        AssignedToManufacturers = 6,

        AssignedToShipping = 10,

        AssignedToOrderSubTotal = 20,
    }
}
