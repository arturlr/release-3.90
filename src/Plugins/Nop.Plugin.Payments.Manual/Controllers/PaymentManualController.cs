using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Payments.Manual.Models;
using Nop.Plugin.Payments.Manual.Validators;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Manual.Controllers
{
    /// <remarks>
    /// <para>
    /// Task 12.2 substitutions, reusing decisions tasks 7.3 (§30), 8.3 (§55) and 10.1 (§83.9)
    /// already recorded: <c>System.Web.Mvc</c> -&gt; <c>Microsoft.AspNetCore.Mvc</c> +
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> (<see cref="SelectListItem"/> lives in a
    /// different namespace from the rest of MVC) + <c>Microsoft.AspNetCore.Http</c>
    /// (<see cref="IFormCollection"/>); <c>FormCollection</c> -&gt;
    /// <see cref="IFormCollection"/> on the two <see cref="BasePaymentController"/> overrides,
    /// a public signature change task 6.2 made on the base class; <c>[ChildActionOnly]</c> x3
    /// deleted (deferral 7.3-4 - no counterpart, and these actions MUST stay invocable because
    /// <c>IPaymentMethod</c>'s action/controller triple is how the host reaches them).
    /// <c>View("~/Plugins/Payments.Manual/Views/...")</c> is unchanged.
    /// </para>
    /// <para>
    /// <b>The <see cref="IFormCollection"/> indexer yields <c>StringValues</c>, not
    /// <c>string</c></b> - the change §55.5 records at 20 admin sites. It converts implicitly to
    /// <c>string</c>, so plain assignments compile untouched, but every place the value is
    /// COMPARED or PARSED is spelled with an explicit <c>.ToString()</c> below. That is not
    /// cosmetic: <c>string.Equals(string, StringComparison)</c> would bind through the implicit
    /// conversion and silently compare a comma-joined multi-value form field, and
    /// <c>StringValues.ToString()</c> returns <c>string.Empty</c> rather than <c>null</c> for a
    /// missing key, which is what <c>x.Value.Equals(...)</c> needs in order not to be given a
    /// null. 3.90's <c>NameValueCollection</c> indexer returned <c>null</c> for a missing key
    /// and <c>string.Equals(null, cmp)</c> is <c>false</c>, which is also what
    /// <c>Equals(string.Empty, cmp)</c> gives for these non-empty option values - so the
    /// observable selection behaviour is identical.
    /// </para>
    /// <para>
    /// <b>DEFECT FOUND AND FIXED</b> - see the <c>HasFormContentType</c> guard in
    /// <see cref="PaymentInfo"/>.
    /// </para>
    /// </remarks>
    public class PaymentManualController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public PaymentManualController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
        }
        
        [AdminAuthorize]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var manualPaymentSettings = _settingService.LoadSetting<ManualPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.TransactModeId = Convert.ToInt32(manualPaymentSettings.TransactMode);
            model.AdditionalFee = manualPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = manualPaymentSettings.AdditionalFeePercentage;
            model.TransactModeValues = manualPaymentSettings.TransactMode.ToSelectList();

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(manualPaymentSettings, x => x.TransactMode, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(manualPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(manualPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.Manual/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var manualPaymentSettings = _settingService.LoadSetting<ManualPaymentSettings>(storeScope);

            //save settings
            manualPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            manualPaymentSettings.AdditionalFee = model.AdditionalFee;
            manualPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            _settingService.SaveSettingOverridablePerStore(manualPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(manualPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(manualPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        /// <summary>
        /// Renders the payment-info step for this method.
        /// </summary>
        /// <remarks>
        /// <b>DEFECT FOUND AND FIXED - task 12.2. Without the <c>HasFormContentType</c> guard
        /// this action throws on the ordinary GET of the checkout page.</b> 3.90 read
        /// <c>this.Request.Form</c> unconditionally to repopulate the card fields after a failed
        /// validation round-trip; <c>System.Web</c> returned an EMPTY
        /// <c>NameValueCollection</c> for a request with no form body. ASP.NET Core's
        /// <c>HttpRequest.Form</c> getter THROWS <c>InvalidOperationException</c> ("Incorrect
        /// Content-Type") when <c>HasFormContentType</c> is false.
        ///
        /// This action is invoked as a CHILD ACTION from <c>Views/Checkout/PaymentInfo.cshtml</c>
        /// through <c>IPaymentMethod.GetPaymentInfoRoute</c> and the <c>Html.Action</c> bridge,
        /// on the SAME <c>HttpContext</c> as the parent request - and
        /// <c>GET /checkout/paymentinfo</c> has no form body. The unguarded read would therefore
        /// have thrown inside view rendering on the first arrival at the payment step: an HTTP
        /// 500 on the checkout page of a store using this payment method, every time. It is not
        /// an edge case, and it is the reason the payment plugins had to be checked rather than
        /// mechanically ported.
        ///
        /// The guard is the pattern already established across this migration for exactly this
        /// API change (<c>FormValueRequiredAttribute</c>, <c>ParameterBasedOnFormName*</c>,
        /// <c>CaptchaValidatorAttribute</c>, <c>HoneypotValidatorAttribute</c>,
        /// <c>BaseAdminController</c>, <c>ReturnRequestController</c>,
        /// <c>ShoppingCartController</c>), and it reproduces 3.90's observable behaviour
        /// exactly: no form =&gt; nothing to repopulate =&gt; empty controls and no preselected
        /// option.
        /// </remarks>
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            
            //CC types
            model.CreditCardTypes.Add(new SelectListItem
                {
                    Text = "Visa",
                    Value = "Visa",
                });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Master card",
                Value = "MasterCard",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Discover",
                Value = "Discover",
            });
            model.CreditCardTypes.Add(new SelectListItem
            {
                Text = "Amex",
                Value = "Amex",
            });
            
            //years
            for (int i = 0; i < 15; i++)
            {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++)
            {
                string text = (i < 10) ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            if (Request.HasFormContentType)
            {
                var form = this.Request.Form;
                model.CardholderName = form["CardholderName"];
                model.CardNumber = form["CardNumber"];
                model.CardCode = form["CardCode"];
                var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(form["CreditCardType"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedCcType != null)
                    selectedCcType.Selected = true;
                var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedMonth != null)
                    selectedMonth.Selected = true;
                var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedYear != null)
                    selectedYear.Selected = true;
            }

            return View("~/Plugins/Payments.Manual/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"].ToString());
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"].ToString());
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            return paymentInfo;
        }
    }
}