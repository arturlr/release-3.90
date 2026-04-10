using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Orders;

public class RecurringPaymentModel : BaseNopEntityModel
{
    public int CycleLength { get; set; }
    public int CyclePeriodId { get; set; }
    public string CyclePeriodStr { get; set; } = string.Empty;
    public int TotalCycles { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string NextPaymentDate { get; set; } = string.Empty;
    public int CyclesRemaining { get; set; }
    public int InitialOrderId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public bool CanCancelRecurringPayment { get; set; }
    public bool LastPaymentFailed { get; set; }
}

public class RecurringPaymentHistoryModel : BaseNopEntityModel
{
    public int OrderId { get; set; }
    public string CustomOrderNumber { get; set; } = string.Empty;
    public int RecurringPaymentId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string ShippingStatus { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
}
