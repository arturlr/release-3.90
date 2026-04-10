using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Orders;

public class OrderListModel : BaseNopModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? OrderStatusIds { get; set; }
    public string? PaymentStatusIds { get; set; }
    public string? ShippingStatusIds { get; set; }
    public int StoreId { get; set; }
    public int VendorId { get; set; }
    public int BillingCountryId { get; set; }
    public string? PaymentMethodSystemName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingLastName { get; set; }
    public string? OrderNotes { get; set; }
    public string? GoDirectlyToNumber { get; set; }

    public List<SelectListItem> AvailableOrderStatuses { get; set; } = [];
    public List<SelectListItem> AvailablePaymentStatuses { get; set; } = [];
    public List<SelectListItem> AvailableShippingStatuses { get; set; } = [];
    public List<SelectListItem> AvailableStores { get; set; } = [];
    public List<SelectListItem> AvailableVendors { get; set; } = [];
    public List<SelectListItem> AvailableCountries { get; set; } = [];
    public List<SelectListItem> AvailablePaymentMethods { get; set; } = [];

    public bool IsLoggedInAsVendor { get; set; }
}

public class OrderGridModel : BaseNopEntityModel
{
    public string? CustomOrderNumber { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? ShippingStatus { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal OrderTotal { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class OrderModel : BaseNopEntityModel
{
    // Order info
    public Guid OrderGuid { get; set; }
    public string? CustomOrderNumber { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerIp { get; set; }
    public string? VatNumber { get; set; }
    public int AffiliateId { get; set; }

    // Status
    public string? OrderStatusName { get; set; }
    public int OrderStatusId { get; set; }
    public string? PaymentStatusName { get; set; }
    public int PaymentStatusId { get; set; }
    public string? ShippingStatusName { get; set; }
    public int ShippingStatusId { get; set; }

    // Payment
    public string? PaymentMethod { get; set; }

    // Totals
    public decimal OrderSubtotalInclTax { get; set; }
    public decimal OrderSubtotalExclTax { get; set; }
    public decimal OrderSubTotalDiscountInclTax { get; set; }
    public decimal OrderSubTotalDiscountExclTax { get; set; }
    public decimal OrderShippingInclTax { get; set; }
    public decimal OrderShippingExclTax { get; set; }
    public decimal PaymentMethodAdditionalFeeInclTax { get; set; }
    public decimal PaymentMethodAdditionalFeeExclTax { get; set; }
    public decimal OrderTax { get; set; }
    public decimal OrderDiscount { get; set; }
    public decimal OrderTotal { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? TaxRates { get; set; }

    // Shipping
    public string? ShippingMethod { get; set; }
    public bool IsShippable { get; set; }

    // Billing address
    public string? BillingFirstName { get; set; }
    public string? BillingLastName { get; set; }
    public string? BillingEmail { get; set; }
    public string? BillingPhone { get; set; }
    public string? BillingAddress1 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingZipPostalCode { get; set; }
    public string? BillingCountry { get; set; }

    // Shipping address
    public string? ShippingFirstName { get; set; }
    public string? ShippingLastName { get; set; }
    public string? ShippingPhone { get; set; }
    public string? ShippingAddress1 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingZipPostalCode { get; set; }
    public string? ShippingCountry { get; set; }

    // Payment operations
    public bool CanCancelOrder { get; set; }
    public bool CanCapture { get; set; }
    public bool CanMarkOrderAsPaid { get; set; }
    public bool CanRefund { get; set; }
    public bool CanRefundOffline { get; set; }
    public bool CanVoid { get; set; }
    public bool CanVoidOffline { get; set; }

    // Dates
    public DateTime CreatedOn { get; set; }

    // Order status dropdown
    public List<SelectListItem> AvailableOrderStatuses { get; set; } = [];

    // Order items
    public List<OrderItemModel> Items { get; set; } = [];

    // Order notes
    public List<OrderNoteModel> OrderNotes { get; set; } = [];
}

public class OrderItemModel : BaseNopEntityModel
{
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? Sku { get; set; }
    public decimal UnitPriceInclTax { get; set; }
    public decimal UnitPriceExclTax { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountInclTax { get; set; }
    public decimal DiscountExclTax { get; set; }
    public decimal SubTotalInclTax { get; set; }
    public decimal SubTotalExclTax { get; set; }
}

public class OrderNoteModel : BaseNopEntityModel
{
    public int OrderId { get; set; }
    public string? Note { get; set; }
    public bool DisplayToCustomer { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class ShipmentGridModel : BaseNopEntityModel
{
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
}
