using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.PayPalDirect.Models
{
    /// <remarks>
    /// Task 12.3: <c>using System.Web.Mvc;</c> -&gt;
    /// <c>using Microsoft.AspNetCore.Mvc.Rendering;</c> (<see cref="SelectListItem"/>), and the
    /// FIVE <c>[AllowHtml]</c> attributes DELETED - no ASP.NET Core counterpart, because they
    /// opted OUT of ASP.NET request validation and that feature does not exist. Deferral
    /// <b>7.3-3</b>'s recorded SECURITY-RELEVANT RELAXATION, here at 5 sites, all previously
    /// exempt and therefore unchanged in risk. As with Payments.Manual, the controls that
    /// actually apply to these cardholder fields are untouched:
    /// <c>Validators/PaymentInfoValidator</c> still enforces <c>IsCreditCard()</c> and the
    /// CVV pattern through <c>ValidatePaymentForm</c>, and no value is ever rendered with
    /// <c>Html.Raw</c>.
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