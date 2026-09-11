using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.PurchaseOrder.Models
{
    /// <remarks>
    /// Task 12.5: <c>using System.Web.Mvc;</c> and <c>[AllowHtml]</c> DELETED. <c>[AllowHtml]</c>
    /// has no ASP.NET Core counterpart - it opted OUT of ASP.NET request validation, which does
    /// not exist in ASP.NET Core, so there is nothing to opt out of. Deferral <b>7.3-3</b>'s
    /// recorded SECURITY-RELEVANT RELAXATION, here at 1 site.
    ///
    /// Exposure on this one is unchanged rather than widened: the property was ALREADY exempt in
    /// 3.90. What the field's value reaches is worth stating, because this is a money path -
    /// the purchase-order number is copied into <c>ProcessPaymentRequest.CustomValues</c> and
    /// ends up in the order's <c>CustomValuesXml</c>, from where it is rendered by
    /// <c>Views/Order/Details.cshtml</c>, <c>_OrderReviewData.cshtml</c> and the admin
    /// <c>_OrderDetails.Info.cshtml</c>. All three render it as <c>@item.Value</c>, i.e. Razor
    /// HTML-encodes it; the e-mail token path (<c>MessageTokenProvider</c>) calls
    /// <c>WebUtility.HtmlEncode</c> explicitly. No <c>Html.Raw</c> is involved anywhere, so the
    /// output encoding that deferral 7.3-3 names as the primary control is in force at every
    /// site - checked, not assumed.
    /// </remarks>
    public class PaymentInfoModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payment.PurchaseOrder.PurchaseOrderNumber")]
        public string PurchaseOrderNumber { get; set; }
    }
}