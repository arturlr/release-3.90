using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.ShoppingCart;

public class OrderTotalsModel : BaseNopModel
{
    public bool IsEditable { get; set; }
    public string? SubTotal { get; set; }
    public string? SubTotalDiscount { get; set; }
    public string? Shipping { get; set; }
    public bool RequiresShipping { get; set; }
    public string? SelectedShippingMethod { get; set; }
    public bool HideShippingTotal { get; set; }
    public string? PaymentMethodAdditionalFee { get; set; }
    public string? Tax { get; set; }
    public List<TaxRate> TaxRates { get; set; } = [];
    public bool DisplayTax { get; set; }
    public bool DisplayTaxRates { get; set; }
    public List<GiftCard> GiftCards { get; set; } = [];
    public string? OrderTotalDiscount { get; set; }
    public int RedeemedRewardPoints { get; set; }
    public string? RedeemedRewardPointsAmount { get; set; }
    public int WillEarnRewardPoints { get; set; }
    public string? OrderTotal { get; set; }

    public class TaxRate : BaseNopModel
    {
        public string? Rate { get; set; }
        public string? Value { get; set; }
    }

    public class GiftCard : BaseNopModel
    {
        public int Id { get; set; }
        public string? CouponCode { get; set; }
        public string? Amount { get; set; }
        public string? Remaining { get; set; }
    }
}
