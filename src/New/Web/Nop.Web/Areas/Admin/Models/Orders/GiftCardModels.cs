using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Orders;

public class GiftCardListModel : BaseNopModel
{
    public int ActivatedId { get; set; }
    public string? CouponCode { get; set; }
    public string? RecipientName { get; set; }
    public List<SelectListItem> ActivatedList { get; set; } = [];
}

public class GiftCardModel : BaseNopEntityModel
{
    public int GiftCardTypeId { get; set; }
    public int? PurchasedWithOrderItemId { get; set; }
    public int? PurchasedWithOrderId { get; set; }
    public string? PurchasedWithOrderNumber { get; set; }
    public decimal Amount { get; set; }
    public string? AmountStr { get; set; }
    public string? RemainingAmountStr { get; set; }
    public bool IsGiftCardActivated { get; set; }
    public string? GiftCardCouponCode { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? SenderName { get; set; }
    public string? SenderEmail { get; set; }
    public string? Message { get; set; }
    public bool IsRecipientNotified { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? PrimaryStoreCurrencyCode { get; set; }
}

public class GiftCardGridModel : BaseNopEntityModel
{
    public string? GiftCardCouponCode { get; set; }
    public string? RecipientName { get; set; }
    public string? AmountStr { get; set; }
    public string? RemainingAmountStr { get; set; }
    public bool IsGiftCardActivated { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class GiftCardUsageHistoryModel : BaseNopEntityModel
{
    public int OrderId { get; set; }
    public string? CustomOrderNumber { get; set; }
    public string? UsedValue { get; set; }
    public DateTime CreatedOn { get; set; }
}
