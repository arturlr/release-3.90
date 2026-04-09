using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;

namespace Nop.Core.Domain.Orders;

public class Order : BaseEntity
{
    public Guid OrderGuid { get; set; }
    public int StoreId { get; set; }
    public int CustomerId { get; set; }
    public int BillingAddressId { get; set; }
    public int? ShippingAddressId { get; set; }
    public int? PickupAddressId { get; set; }
    public bool PickUpInStore { get; set; }
    public int OrderStatusId { get; set; }
    public int ShippingStatusId { get; set; }
    public int PaymentStatusId { get; set; }
    public string? PaymentMethodSystemName { get; set; }
    public string? CustomerCurrencyCode { get; set; }
    public decimal CurrencyRate { get; set; }
    public int CustomerTaxDisplayTypeId { get; set; }
    public string? VatNumber { get; set; }
    public decimal OrderSubtotalInclTax { get; set; }
    public decimal OrderSubtotalExclTax { get; set; }
    public decimal OrderSubTotalDiscountInclTax { get; set; }
    public decimal OrderSubTotalDiscountExclTax { get; set; }
    public decimal OrderShippingInclTax { get; set; }
    public decimal OrderShippingExclTax { get; set; }
    public decimal PaymentMethodAdditionalFeeInclTax { get; set; }
    public decimal PaymentMethodAdditionalFeeExclTax { get; set; }
    public string? TaxRates { get; set; }
    public decimal OrderTax { get; set; }
    public decimal OrderDiscount { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal RefundedAmount { get; set; }
    public int? RewardPointsHistoryEntryId { get; set; }
    public string? CheckoutAttributeDescription { get; set; }
    public string? CheckoutAttributesXml { get; set; }
    public int CustomerLanguageId { get; set; }
    public int AffiliateId { get; set; }
    public string? CustomerIp { get; set; }
    public bool AllowStoringCreditCardNumber { get; set; }
    public string? CardType { get; set; }
    public string? CardName { get; set; }
    public string? CardNumber { get; set; }
    public string? MaskedCreditCardNumber { get; set; }
    public string? CardCvv2 { get; set; }
    public string? CardExpirationMonth { get; set; }
    public string? CardExpirationYear { get; set; }
    public string? AuthorizationTransactionId { get; set; }
    public string? AuthorizationTransactionCode { get; set; }
    public string? AuthorizationTransactionResult { get; set; }
    public string? CaptureTransactionId { get; set; }
    public string? CaptureTransactionResult { get; set; }
    public string? SubscriptionTransactionId { get; set; }
    public DateTime? PaidDateUtc { get; set; }
    public string? ShippingMethod { get; set; }
    public string? ShippingRateComputationMethodSystemName { get; set; }
    public string? CustomValuesXml { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public string? CustomOrderNumber { get; set; }

    public OrderStatus OrderStatus
    {
        get => (OrderStatus)OrderStatusId;
        set => OrderStatusId = (int)value;
    }

    public PaymentStatus PaymentStatus
    {
        get => (PaymentStatus)PaymentStatusId;
        set => PaymentStatusId = (int)value;
    }

    public ShippingStatus ShippingStatus
    {
        get => (ShippingStatus)ShippingStatusId;
        set => ShippingStatusId = (int)value;
    }

    public TaxDisplayType CustomerTaxDisplayType
    {
        get => (TaxDisplayType)CustomerTaxDisplayTypeId;
        set => CustomerTaxDisplayTypeId = (int)value;
    }
}
