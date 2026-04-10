using Nop.Web.Framework.Mvc;
using Nop.Web.Models.Customer;

namespace Nop.Web.Models.Checkout;

public class CheckoutBillingAddressModel : BaseNopModel
{
    public IList<AddressModel> ExistingAddresses { get; set; } = [];
    public AddressModel NewAddress { get; set; } = new();
    public bool ShipToSameAddress { get; set; } = true;
    public bool ShipToSameAddressAllowed { get; set; }
    public bool NewAddressPreselected { get; set; }
}

public class CheckoutShippingAddressModel : BaseNopModel
{
    public IList<AddressModel> ExistingAddresses { get; set; } = [];
    public AddressModel NewAddress { get; set; } = new();
    public bool NewAddressPreselected { get; set; }
}

public class CheckoutShippingMethodModel : BaseNopModel
{
    public IList<ShippingMethodModel> ShippingMethods { get; set; } = [];
    public IList<string> Warnings { get; set; } = [];

    public class ShippingMethodModel : BaseNopModel
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Fee { get; set; }
        public bool Selected { get; set; }
        public string? ShippingRateComputationMethodSystemName { get; set; }
    }
}

public class CheckoutPaymentMethodModel : BaseNopModel
{
    public IList<PaymentMethodModel> PaymentMethods { get; set; } = [];
    public bool DisplayRewardPoints { get; set; }
    public int RewardPointsBalance { get; set; }
    public string? RewardPointsAmount { get; set; }
    public bool UseRewardPoints { get; set; }

    public class PaymentMethodModel : BaseNopModel
    {
        public string? PaymentMethodSystemName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Fee { get; set; }
        public bool Selected { get; set; }
        public string? LogoUrl { get; set; }
    }
}

public class CheckoutPaymentInfoModel : BaseNopModel
{
    public string? PaymentMethodSystemName { get; set; }
    /// <summary>
    /// URL or partial view name for the payment method's info widget.
    /// </summary>
    public string? PaymentInfoWidget { get; set; }
}

public class CheckoutConfirmModel : BaseNopModel
{
    public IList<string> Warnings { get; set; } = [];
    public decimal OrderTotal { get; set; }
    public string? OrderTotalFormatted { get; set; }
    public int? MinOrderTotalWarning { get; set; }
}

public class CheckoutCompletedModel : BaseNopModel
{
    public int OrderId { get; set; }
}

public class CheckoutProgressModel : BaseNopModel
{
    public CheckoutProgressStep CheckoutProgressStep { get; set; }
}

public enum CheckoutProgressStep
{
    Cart,
    Address,
    Shipping,
    Payment,
    Confirm,
    Complete
}
