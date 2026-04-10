using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Order;

// --- Customer Order List (My Account / Orders) ---

public class CustomerOrderListModel : BaseNopModel
{
    public IList<OrderBriefModel> Orders { get; set; } = [];
    public IList<RecurringOrderModel> RecurringOrders { get; set; } = [];
    public IList<string> RecurringPaymentErrors { get; set; } = [];

    public class OrderBriefModel : BaseNopEntityModel
    {
        public string? CustomOrderNumber { get; set; }
        public string? OrderTotal { get; set; }
        public bool IsReturnRequestAllowed { get; set; }
        public OrderStatus OrderStatusEnum { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public string? ShippingStatus { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class RecurringOrderModel : BaseNopEntityModel
    {
        public string? StartDate { get; set; }
        public string? CycleInfo { get; set; }
        public string? NextPayment { get; set; }
        public int TotalCycles { get; set; }
        public int CyclesRemaining { get; set; }
        public int InitialOrderId { get; set; }
        public string? InitialOrderNumber { get; set; }
        public bool CanCancel { get; set; }
        public bool CanRetryLastPayment { get; set; }
    }
}

// --- Order Details ---

public class OrderDetailsModel : BaseNopEntityModel
{
    public bool PrintMode { get; set; }
    public bool PdfInvoiceDisabled { get; set; }
    public string? CustomOrderNumber { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? OrderStatus { get; set; }
    public bool IsReOrderAllowed { get; set; }
    public bool IsReturnRequestAllowed { get; set; }

    // Shipping
    public bool IsShippable { get; set; }
    public string? ShippingStatus { get; set; }
    public string? ShippingMethod { get; set; }
    public IList<ShipmentBriefModel> Shipments { get; set; } = [];

    // Payment
    public string? PaymentMethod { get; set; }
    public bool CanRePostProcessPayment { get; set; }

    // Totals
    public string? OrderSubtotal { get; set; }
    public string? OrderSubTotalDiscount { get; set; }
    public string? OrderShipping { get; set; }
    public string? PaymentMethodAdditionalFee { get; set; }
    public string? CheckoutAttributeInfo { get; set; }
    public string? Tax { get; set; }
    public IList<TaxRateModel> TaxRates { get; set; } = [];
    public bool DisplayTax { get; set; }
    public bool DisplayTaxRates { get; set; }
    public string? OrderTotalDiscount { get; set; }
    public int RedeemedRewardPoints { get; set; }
    public string? RedeemedRewardPointsAmount { get; set; }
    public string? OrderTotal { get; set; }
    public IList<GiftCardModel> GiftCards { get; set; } = [];

    // Items
    public bool ShowSku { get; set; }
    public IList<OrderItemModel> Items { get; set; } = [];

    // Notes
    public IList<OrderNoteModel> OrderNotes { get; set; } = [];

    public class OrderItemModel : BaseNopEntityModel
    {
        public Guid OrderItemGuid { get; set; }
        public string? Sku { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public string? UnitPrice { get; set; }
        public string? SubTotal { get; set; }
        public int Quantity { get; set; }
        public string? AttributeInfo { get; set; }
    }

    public class TaxRateModel : BaseNopModel
    {
        public string? Rate { get; set; }
        public string? Value { get; set; }
    }

    public class GiftCardModel : BaseNopModel
    {
        public string? CouponCode { get; set; }
        public string? Amount { get; set; }
    }

    public class OrderNoteModel : BaseNopEntityModel
    {
        public bool HasDownload { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class ShipmentBriefModel : BaseNopEntityModel
    {
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
    }
}

// --- Shipment Details ---

public class ShipmentDetailsModel : BaseNopEntityModel
{
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool ShowSku { get; set; }
    public IList<ShipmentItemModel> Items { get; set; } = [];
    public int OrderId { get; set; }

    public class ShipmentItemModel : BaseNopEntityModel
    {
        public string? Sku { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSeName { get; set; }
        public int QuantityOrdered { get; set; }
        public int QuantityShipped { get; set; }
    }
}

// --- Customer Reward Points ---

public class CustomerRewardPointsModel : BaseNopModel
{
    public IList<RewardPointsHistoryModel> RewardPoints { get; set; } = [];
    public int RewardPointsBalance { get; set; }
    public string? RewardPointsAmount { get; set; }
    public int MinimumRewardPointsBalance { get; set; }
    public string? MinimumRewardPointsAmount { get; set; }

    public class RewardPointsHistoryModel : BaseNopEntityModel
    {
        public int Points { get; set; }
        public string? PointsBalance { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
