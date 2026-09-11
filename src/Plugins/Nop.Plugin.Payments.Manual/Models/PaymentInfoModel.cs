using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Manual.Models
{
    /// <remarks>
    /// Task 12.2: <c>using System.Web.Mvc;</c> -&gt;
    /// <c>using Microsoft.AspNetCore.Mvc.Rendering;</c> (<see cref="SelectListItem"/>), and the
    /// SIX <c>[AllowHtml]</c> attributes DELETED. <c>[AllowHtml]</c> has no ASP.NET Core
    /// counterpart - it opted OUT of ASP.NET request validation, which does not exist in
    /// ASP.NET Core at all, so there is nothing to opt out of. Deferral <b>7.3-3</b>'s recorded
    /// SECURITY-RELEVANT RELAXATION, here at 6 sites, all of them previously exempt and
    /// therefore unchanged in risk.
    ///
    /// These six carry cardholder data, so it is worth being explicit about what does and does
    /// not change. Request validation never protected the card fields - they were marked
    /// [AllowHtml] precisely to bypass it, presumably because a rejected form on the payment
    /// step is worse than a rejected one anywhere else. The controls that DO apply are
    /// unchanged: <c>Validators/PaymentInfoValidator</c> still enforces
    /// <c>IsCreditCard()</c> on CardNumber and <c>^[0-9]{3,4}$</c> on CardCode through
    /// <c>ValidatePaymentForm</c>, and the values are never rendered with <c>Html.Raw</c>.
    /// Nothing was loosened here to make anything compile.
    /// </remarks>
    public class PaymentInfoModel : BaseNopModel
    {
        public PaymentInfoModel()
        {
            CreditCardTypes = new List<SelectListItem>();
            ExpireMonths = new List<SelectListItem>();
            ExpireYears = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Payment.SelectCreditCard")]
        public string CreditCardType { get; set; }
        [NopResourceDisplayName("Payment.SelectCreditCard")]
        public IList<SelectListItem> CreditCardTypes { get; set; }

        [NopResourceDisplayName("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [NopResourceDisplayName("Payment.CardNumber")]
        public string CardNumber { get; set; }

        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireMonth { get; set; }
        [NopResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireYear { get; set; }
        public IList<SelectListItem> ExpireMonths { get; set; }
        public IList<SelectListItem> ExpireYears { get; set; }

        [NopResourceDisplayName("Payment.CardCode")]
        public string CardCode { get; set; }
    }
}