using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.PurchaseOrder.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.PurchaseOrder.Controllers
{
    /// <remarks>
    /// <para>
    /// Task 12.5 substitutions, reusing decisions tasks 7.3 (§30), 8.3 (§55) and 10.1 (§83.9)
    /// already recorded: <c>System.Web.Mvc</c> → <c>Microsoft.AspNetCore.Mvc</c> +
    /// <c>Microsoft.AspNetCore.Http</c>; <c>FormCollection</c> → <see cref="IFormCollection"/>
    /// on the two <see cref="BasePaymentController"/> overrides (a public signature change made
    /// by task 6.2 on the base class); <c>[ChildActionOnly]</c> ×3 deleted (deferral 7.3-4 — no
    /// counterpart, and the actions MUST stay invocable because <c>IPaymentMethod</c>'s
    /// action/controller triple is how the host reaches them). The
    /// <c>View("~/Plugins/Payments.PurchaseOrder/Views/…")</c> paths are unchanged.
    /// </para>
    /// <para>
    /// <b>TWO REAL BEHAVIOURAL DEFECTS WERE FOUND AND FIXED HERE, both on the money path.</b>
    /// See <see cref="PaymentInfo"/> and <see cref="GetPaymentInfo"/>.
    /// </para>
    /// </remarks>
    public class PaymentPurchaseOrderController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;

        public PaymentPurchaseOrderController(IWorkContext workContext,
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
            var purchaseOrderPaymentSettings = _settingService.LoadSetting<PurchaseOrderPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.AdditionalFee = purchaseOrderPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = purchaseOrderPaymentSettings.AdditionalFeePercentage;
            model.ShippableProductRequired = purchaseOrderPaymentSettings.ShippableProductRequired;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.ShippableProductRequired_OverrideForStore = _settingService.SettingExists(purchaseOrderPaymentSettings, x => x.ShippableProductRequired, storeScope);
            }

            return View("~/Plugins/Payments.PurchaseOrder/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var purchaseOrderPaymentSettings = _settingService.LoadSetting<PurchaseOrderPaymentSettings>(storeScope);

            //save settings
            purchaseOrderPaymentSettings.AdditionalFee = model.AdditionalFee;
            purchaseOrderPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            purchaseOrderPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(purchaseOrderPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore , storeScope, false);
            _settingService.SaveSettingOverridablePerStore(purchaseOrderPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore , storeScope, false);
            _settingService.SaveSettingOverridablePerStore(purchaseOrderPaymentSettings, x => x.ShippableProductRequired, model.ShippableProductRequired_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        /// <summary>
        /// Renders the payment-info step for this method.
        /// </summary>
        /// <remarks>
        /// <b>DEFECT FOUND AND FIXED — task 12.5. Without the <c>HasFormContentType</c> guard
        /// this action throws on the ordinary GET of the checkout page.</b> 3.90 read
        /// <c>this.Request.Form</c> unconditionally to repopulate the field after a failed
        /// validation round-trip; <c>System.Web</c> returned an EMPTY <c>NameValueCollection</c>
        /// for a request with no form body, so the read was harmless. ASP.NET Core's
        /// <c>HttpRequest.Form</c> getter THROWS <c>InvalidOperationException</c> ("Incorrect
        /// Content-Type") when <c>HasFormContentType</c> is false.
        ///
        /// This action is invoked as a child action from
        /// <c>Views/Checkout/PaymentInfo.cshtml</c> via <c>IPaymentMethod.GetPaymentInfoRoute</c>
        /// and the <c>Html.Action</c> bridge, on the same <c>HttpContext</c> as the parent
        /// request — and <c>GET /checkout/paymentinfo</c> has no form body. So the unguarded read
        /// would have thrown inside view rendering on the FIRST arrival at the payment step,
        /// i.e. an HTTP 500 on the checkout page, every time. It is not an edge case.
        ///
        /// The guard is the pattern already established across this migration for exactly this
        /// API change — <c>FormValueRequiredAttribute</c>, <c>ParameterBasedOnFormName*</c>,
        /// <c>CaptchaValidatorAttribute</c>, <c>HoneypotValidatorAttribute</c>,
        /// <c>BaseAdminController</c>, <c>ReturnRequestController</c>,
        /// <c>ShoppingCartController</c> — and it reproduces 3.90's observable behaviour
        /// exactly: no form ⇒ empty value ⇒ an empty text box.
        /// </remarks>
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            
            //set postback values
            if (Request.HasFormContentType)
            {
                var form = this.Request.Form;
                model.PurchaseOrderNumber = form["PurchaseOrderNumber"];
            }

            return View("~/Plugins/Payments.PurchaseOrder/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        /// <summary>
        /// Collects the entered purchase-order number into the payment request.
        /// </summary>
        /// <remarks>
        /// <b>DEFERRAL 7.3-2 — THIS IS THE ONE CALL SITE IN THE WHOLE SOLUTION THAT THE DEFERRAL
        /// IS ABOUT, and the <c>.ToString()</c> below is the fix.</b>
        ///
        /// <c>ProcessPaymentRequest.CustomValues</c> is <c>Dictionary&lt;string, object&gt;</c>,
        /// and <c>CheckoutController</c> stashes the whole request in the session between the
        /// payment-info step and order confirmation. Task 7.3 replaced <c>System.Web</c>'s
        /// object-indexed session with a <c>System.Text.Json</c> bridge
        /// (<c>Nop.Web/Extensions/SessionExtensions.cs</c>) because <c>ISession</c> is a
        /// <c>byte[]</c> store, and JSON cannot recover the CLR type of an <c>object</c> value.
        ///
        /// Without the <c>.ToString()</c> the value stored would be a <c>StringValues</c>
        /// STRUCT, not a string: ASP.NET Core's form indexer returns <c>StringValues</c>, and
        /// boxing it into <c>object</c> preserves it. <c>StringValues</c> implements
        /// <c>IEnumerable&lt;string&gt;</c>, so <c>JsonSerializer</c> — which uses the RUNTIME
        /// type for an <c>object</c>-declared value — writes it as a JSON ARRAY. On the way back
        /// it becomes a <c>JsonElement</c> of kind <c>Array</c>, and
        /// <c>PaymentExtensions.SerializeCustomValues</c> persists custom values with
        /// <c>value.ToString()</c> — so the order's <c>CustomValuesXml</c> would have recorded
        /// the literal text <c>["PO-1234"]</c> instead of <c>PO-1234</c>, and that string is
        /// what the customer's order-details page, the order-confirmation e-mail and the admin
        /// order screen all display. A silent, permanent corruption of an order field, on the
        /// money path.
        ///
        /// With <c>.ToString()</c> the value is a <c>string</c>: JSON writes a string, the
        /// round-trip yields a <c>JsonElement</c> of kind <c>String</c>, and
        /// <c>JsonElement.ToString()</c> returns the string CONTENT (not a quoted form) — so
        /// <c>SerializeCustomValues</c> writes exactly the entered text, byte-identical to
        /// 3.90. That is asserted by execution, not by reading: see
        /// <c>Nop.Web.SmokeTests.PaymentCustomValuesRoundTripTests</c>, which drives the real
        /// <c>SessionExtensions</c> helper and the real <c>SerializeCustomValues</c> and
        /// includes the counterfactual (the un-fixed <c>StringValues</c> shape) as a measured
        /// contrast.
        ///
        /// <c>TypeNameHandling</c>-style type-preserving serialization was NOT used to "solve"
        /// this: task 4.2 refused it as a deserialization-gadget hazard, and it would be a
        /// remote-code-execution surface reachable from a session cookie.
        ///
        /// Note also that <c>StringValues.ToString()</c> is not a no-op for the empty case: it
        /// returns <c>string.Empty</c> where an implicit conversion to <c>string</c> would give
        /// <c>null</c>. <c>System.Web</c>'s <c>FormCollection["missing"]</c> returned
        /// <c>null</c>, so <c>string.Empty</c> is a deliberate, visible difference — and it is
        /// the SAFER of the two, because <c>DictionarySerializer.WriteXml</c> writes a null
        /// value as an empty element that <c>ReadXml</c> then reads back as <c>""</c> anyway.
        /// The stored order value is therefore identical either way.
        /// </remarks>
        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CustomValues.Add(_localizationService.GetResource("Plugins.Payment.PurchaseOrder.PurchaseOrderNumber"),
                form["PurchaseOrderNumber"].ToString());
            return paymentInfo;
        }
    }
}
