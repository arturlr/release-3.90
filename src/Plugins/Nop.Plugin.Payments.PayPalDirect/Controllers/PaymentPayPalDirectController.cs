using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayPalDirect.Models;
using Nop.Plugin.Payments.PayPalDirect.Validators;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using PayPal.Api;

namespace Nop.Plugin.Payments.PayPalDirect.Controllers
{
    /// <remarks>
    /// <para>
    /// Task 12.3 substitutions, reusing decisions tasks 7.3 (§30), 8.3 (§55) and 10.1 (§83.9)
    /// already recorded: <c>System.Web.Mvc</c> -&gt; <c>Microsoft.AspNetCore.Mvc</c> +
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> (<see cref="SelectListItem"/>) +
    /// <c>Microsoft.AspNetCore.Http</c>; <c>FormCollection</c> -&gt;
    /// <see cref="IFormCollection"/> on the two <see cref="BasePaymentController"/> overrides;
    /// <c>[ChildActionOnly]</c> x4 deleted (deferral 7.3-4 - no counterpart, and these actions
    /// MUST stay invocable because <c>IPaymentMethod</c>'s action/controller triple is how the
    /// host reaches them). The <c>View("~/Plugins/Payments.PayPalDirect/Views/...")</c> paths
    /// are unchanged.
    /// </para>
    /// <para>
    /// <b>Three System.Web APIs in <see cref="WebhookEventsHandler"/> had no counterpart and
    /// each is documented at its own call site:</b> <c>Request.InputStream</c>,
    /// <c>HttpRequestBase.Headers</c> (a <see cref="NameValueCollection"/>, which the PayPal
    /// SDK's signature-validation method demands) and <c>HttpStatusCodeResult</c>.
    /// </para>
    /// <para>
    /// <b>DEFECT FOUND AND FIXED</b> - the <c>HasFormContentType</c> guard in
    /// <see cref="PaymentInfo"/>; see there.
    /// </para>
    /// </remarks>
    public class PaymentPayPalDirectController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;

        #endregion

        #region Ctor

        public PaymentPayPalDirectController(ILocalizationService localizationService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            ISettingService settingService,
            IStoreContext storeContext,
            IStoreService storeService,
            IWorkContext workContext,
            IWebHelper webHelper)
        {
            this._localizationService = localizationService;
            this._logger = logger;
            this._orderProcessingService = orderProcessingService;
            this._orderService = orderService;
            this._settingService = settingService;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._workContext = workContext;
            this._webHelper = webHelper;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Create webhook that receive events for the subscribed event types
        /// </summary>
        /// <returns>Webhook id</returns>
        protected string CreateWebHook()
        {
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            try
            {
                var apiContext = PaypalHelper.GetApiContext(payPalDirectPaymentSettings);
                if (!string.IsNullOrEmpty(payPalDirectPaymentSettings.WebhookId))
                {
                    try
                    {
                        return Webhook.Get(apiContext, payPalDirectPaymentSettings.WebhookId).id;
                    }
                    catch (PayPal.PayPalException) { }
                }

                var currentStore = storeScope > 0 ? _storeService.GetStoreById(storeScope) : _storeContext.CurrentStore;
                var webhook = new Webhook
                {
                    event_types = new List<WebhookEventType> { new WebhookEventType { name = "*" } },
                    url = string.Format("{0}Plugins/PaymentPayPalDirect/Webhook", _webHelper.GetStoreLocation(currentStore.SslEnabled))
                }.Create(apiContext);

                return webhook.id;
            }
            catch (PayPal.PayPalException exc)
            {
                if (exc is PayPal.ConnectionException)
                {
                    var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
                    if (error != null)
                    {
                        _logger.Error(string.Format("PayPal error: {0} ({1})", error.message, error.name));
                        if (error.details != null)
                            error.details.ForEach(x => _logger.Error(string.Format("{0} {1}", x.field, x.issue)));
                    }
                    else
                        _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
                }
                else
                    _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);

                return string.Empty;
            }
        }

        /// <summary>
        /// Copies the current request's headers into a <see cref="NameValueCollection"/>, the
        /// type the PayPal SDK's webhook signature check requires.
        /// </summary>
        /// <remarks>
        /// See the extended note on <see cref="WebhookEventsHandler"/>. Kept as a separate
        /// method rather than inlined so the conversion has one home and can be reasoned about
        /// on its own: it is part of a signature-verification path.
        /// </remarks>
        private NameValueCollection GetRequestHeadersAsNameValueCollection()
        {
            //StringComparer.OrdinalIgnoreCase is NOT needed - NameValueCollection's default
            //comparer is already case-insensitive, matching both System.Web's Headers
            //collection and ASP.NET Core's IHeaderDictionary.
            var headers = new NameValueCollection();
            foreach (var header in Request.Headers)
            {
                //StringValues.ToString() joins multiple values with "," - the same
                //representation System.Web's NameValueCollection-based Headers exposed.
                headers[header.Key] = header.Value.ToString();
            }

            return headers;
        }

        #endregion

        #region Methods

        [AdminAuthorize]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                ClientId = payPalDirectPaymentSettings.ClientId,
                ClientSecret = payPalDirectPaymentSettings.ClientSecret,
                WebhookId = payPalDirectPaymentSettings.WebhookId,
                UseSandbox = payPalDirectPaymentSettings.UseSandbox,
                PassPurchasedItems = payPalDirectPaymentSettings.PassPurchasedItems,
                TransactModeId = (int)payPalDirectPaymentSettings.TransactMode,
                AdditionalFee = payPalDirectPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = payPalDirectPaymentSettings.AdditionalFeePercentage,
                TransactModeValues = payPalDirectPaymentSettings.TransactMode.ToSelectList(),
                ActiveStoreScopeConfiguration = storeScope
            };
            if (storeScope > 0)
            {
                model.ClientId_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.ClientId, storeScope);
                model.ClientSecret_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.ClientSecret, storeScope);
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.UseSandbox, storeScope);
                model.PassPurchasedItems_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.PassPurchasedItems, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.TransactMode, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(payPalDirectPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
            }

            return View("~/Plugins/Payments.PayPalDirect/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("save")]
        [AdminAuthorize]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            //save settings
            payPalDirectPaymentSettings.ClientId = model.ClientId;
            payPalDirectPaymentSettings.ClientSecret = model.ClientSecret;
            payPalDirectPaymentSettings.WebhookId = model.WebhookId;
            payPalDirectPaymentSettings.UseSandbox = model.UseSandbox;
            payPalDirectPaymentSettings.PassPurchasedItems = model.PassPurchasedItems;
            payPalDirectPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            payPalDirectPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalDirectPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.ClientId, model.ClientId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.ClientSecret, model.ClientSecret_OverrideForStore, storeScope, false);
            _settingService.SaveSetting(payPalDirectPaymentSettings, x => x.WebhookId, 0, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.PassPurchasedItems, model.PassPurchasedItems_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalDirectPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("createwebhook")]
        [AdminAuthorize]
        public ActionResult GetWebhookId(ConfigurationModel model)
        {
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>();
            payPalDirectPaymentSettings.WebhookId = CreateWebHook();
            _settingService.SaveSetting(payPalDirectPaymentSettings);

            if (string.IsNullOrEmpty(payPalDirectPaymentSettings.WebhookId))
                ErrorNotification(_localizationService.GetResource("Plugins.Payments.PayPalDirect.WebhookError"));

            return Configure();
        }

        /// <summary>
        /// Renders the payment-info step for this method.
        /// </summary>
        /// <remarks>
        /// <b>DEFECT FOUND AND FIXED - task 12.3. Without the <c>HasFormContentType</c> guard
        /// this action throws on the ordinary GET of the checkout page.</b> 3.90 read
        /// <c>Request.Form</c> unconditionally to repopulate the card fields after a failed
        /// validation round-trip; <c>System.Web</c> returned an EMPTY
        /// <c>NameValueCollection</c> for a request with no form body, whereas ASP.NET Core's
        /// <c>HttpRequest.Form</c> getter THROWS <c>InvalidOperationException</c> ("Incorrect
        /// Content-Type") when <c>HasFormContentType</c> is false.
        ///
        /// This is a CHILD ACTION reached from <c>Views/Checkout/PaymentInfo.cshtml</c> through
        /// <c>IPaymentMethod.GetPaymentInfoRoute</c> and the <c>Html.Action</c> bridge, on the
        /// SAME <c>HttpContext</c> as the parent request - and <c>GET /checkout/paymentinfo</c>
        /// has no form body, so the unguarded read would have produced an HTTP 500 on the
        /// checkout page every time a store used this payment method. Same defect, same fix, as
        /// Payments.Manual (12.2) and Payments.PurchaseOrder (12.5); the guard is the pattern
        /// already established across this migration for this API change.
        ///
        /// The <c>.ToString()</c> calls on the three comparisons are also deliberate: the
        /// indexer yields <c>StringValues</c> (§55.5), and
        /// <c>string.Equals(string, StringComparison)</c> would otherwise bind through the
        /// implicit conversion and compare a comma-joined multi-value field.
        /// </remarks>
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            model.CreditCardTypes = new List<SelectListItem>
            {
                new SelectListItem { Text = "Visa", Value = "visa" },
                new SelectListItem { Text = "Master card", Value = "MasterCard" },
                new SelectListItem { Text = "Discover", Value = "Discover" },
                new SelectListItem { Text = "Amex", Value = "Amex" },
            };

            //years
            for (var i = 0; i < 15; i++)
            {
                var year = (DateTime.Now.Year + i).ToString();
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (var i = 1; i <= 12; i++)
            {
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = i.ToString("D2"),
                    Value = i.ToString(),
                });
            }

            //set postback values
            if (Request.HasFormContentType)
            {
                model.CardNumber = Request.Form["CardNumber"];
                model.CardCode = Request.Form["CardCode"];
                var selectedCcType = model.CreditCardTypes.FirstOrDefault(x => x.Value.Equals(Request.Form["CreditCardType"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedCcType != null)
                    selectedCcType.Selected = true;
                var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireMonth"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedMonth != null)
                    selectedMonth.Selected = true;
                var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(Request.Form["ExpireYear"].ToString(), StringComparison.InvariantCultureIgnoreCase));
                if (selectedYear != null)
                    selectedYear.Selected = true;
            }

            return View("~/Plugins/Payments.PayPalDirect/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest
            { 
                CreditCardType = form["CreditCardType"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"].ToString()),
                CreditCardExpireYear = int.Parse(form["ExpireYear"].ToString()),
                CreditCardCvv2 = form["CardCode"]
            };
        }

        /// <summary>
        /// PayPal webhook endpoint. Reached only through the
        /// <c>Plugin.Payments.PayPalDirect.Webhook</c> route.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Three System.Web APIs replaced, and every one of them touches whether an inbound
        /// webhook is ACCEPTED, so each is spelled out.</b>
        /// </para>
        /// <list type="number">
        /// <item><b><c>Request.InputStream</c> -&gt; <c>Request.Body</c>, read
        /// asynchronously.</b> <c>HttpRequestBase.InputStream</c> is <c>System.Web</c> and has
        /// no counterpart. The read must be async because Kestrel sets
        /// <c>AllowSynchronousIO = false</c> by default (since .NET Core 3.0) and this host does
        /// not override it, so <c>new StreamReader(Request.Body).ReadToEnd()</c> would throw
        /// <c>InvalidOperationException</c> - which, inside this method's
        /// <c>catch (PayPalException)</c>, would NOT have been caught and would have surfaced as
        /// a 500 to PayPal, causing it to retry the notification indefinitely. Making the action
        /// <c>async Task&lt;ActionResult&gt;</c> is safe: the only way in is the route, and
        /// ASP.NET Core invokes an async action transparently. (Deferral <b>12.x-2</b> records
        /// five PRE-EXISTING synchronous <c>Request.Body</c> reads in
        /// <c>Nop.Web</c>/<c>Nop.Admin</c> that hit the same wall - found while deciding this,
        /// not introduced by it.)</item>
        /// <item><b><c>Request.Headers</c> -&gt; a <see cref="NameValueCollection"/> built from
        /// <c>IHeaderDictionary</c>.</b> This is NOT cosmetic and NOT optional:
        /// <c>WebhookEvent.ValidateReceivedEvent(APIContext, NameValueCollection, string,
        /// string)</c> is the PayPal SDK's signature check, its second parameter is typed
        /// <see cref="NameValueCollection"/> (verified by reflection against the real
        /// PayPal 1.8.0 assembly on net10.0), and <c>System.Web</c>'s
        /// <c>HttpRequestBase.Headers</c> WAS one. ASP.NET Core's <c>IHeaderDictionary</c> is
        /// not, and there is no implicit conversion. The SDK reads
        /// <c>PAYPAL-TRANSMISSION-ID</c>, <c>PAYPAL-TRANSMISSION-TIME</c>,
        /// <c>PAYPAL-TRANSMISSION-SIG</c>, <c>PAYPAL-CERT-URL</c> and
        /// <c>PAYPAL-AUTH-ALGO</c> from it, so the copy must be complete and it must join
        /// multi-value headers the way <c>NameValueCollection</c> does - which is exactly what
        /// <c>StringValues.ToString()</c> produces (comma-separated), matching
        /// <c>System.Web</c>. <c>NameValueCollection</c>'s indexer is case-insensitive by
        /// default, as <c>IHeaderDictionary</c>'s is, so the SDK's lookups behave
        /// identically.</item>
        /// <item><b><c>new HttpStatusCodeResult(HttpStatusCode.OK)</c> -&gt;
        /// <c>new StatusCodeResult((int)HttpStatusCode.OK)</c></b>, three sites. Same wire
        /// behaviour: a bare status line with no body. Note 3.90 deliberately answers 200 even
        /// when validation FAILS or an exception is thrown - that is not a bug to fix, it is how
        /// a webhook consumer tells PayPal "delivered, stop retrying"; a non-2xx would make
        /// PayPal redeliver the same event. Preserved exactly.</item>
        /// </list>
        /// </remarks>
        [HttpPost]
        public async Task<ActionResult> WebhookEventsHandler()
        {
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalDirectPaymentSettings = _settingService.LoadSetting<PayPalDirectPaymentSettings>(storeScope);

            try
            {
                string requestBody;
                using (var stream = new StreamReader(Request.Body))
                {
                    requestBody = await stream.ReadToEndAsync();
                }
                var apiContext = PaypalHelper.GetApiContext(payPalDirectPaymentSettings);

                //validate request
                if (!WebhookEvent.ValidateReceivedEvent(apiContext, GetRequestHeadersAsNameValueCollection(), requestBody, payPalDirectPaymentSettings.WebhookId))
                {
                    _logger.Error("PayPal error: webhook event was not validated");
                    return new StatusCodeResult((int)HttpStatusCode.OK);
                }

                var webhook = JsonFormatter.ConvertFromJson<WebhookEvent>(requestBody);

                if (webhook.resource_type.ToLowerInvariant().Equals("sale"))
                {
                    var sale = JsonFormatter.ConvertFromJson<Sale>(webhook.resource.ToString());

                    //recurring payment
                    if (!string.IsNullOrEmpty(sale.billing_agreement_id))
                    {
                        //get agreement
                        var agreement = Agreement.Get(apiContext, sale.billing_agreement_id);
                        var initialOrder = _orderService.GetOrderByGuid(new Guid(agreement.description));
                        if (initialOrder != null)
                        {
                            var recurringPayment = _orderService.SearchRecurringPayments(initialOrderId: initialOrder.Id).FirstOrDefault();
                            if (recurringPayment != null)
                            {
                                if (sale.state.ToLowerInvariant().Equals("completed"))
                                {
                                    if (recurringPayment.RecurringPaymentHistory.Count == 0)
                                    {
                                        //first payment
                                        initialOrder.PaymentStatus = PaymentStatus.Paid;
                                        initialOrder.CaptureTransactionId = sale.id;
                                        _orderService.UpdateOrder(initialOrder);

                                        recurringPayment.RecurringPaymentHistory.Add(new RecurringPaymentHistory
                                        {
                                            RecurringPaymentId = recurringPayment.Id,
                                            OrderId = initialOrder.Id,
                                            CreatedOnUtc = DateTime.UtcNow
                                        });
                                        _orderService.UpdateRecurringPayment(recurringPayment);
                                    }
                                    else
                                    {
                                        //next payments
                                        var orders = _orderService.GetOrdersByIds(recurringPayment.RecurringPaymentHistory.Select(order => order.OrderId).ToArray());
                                        if (!orders.Any(order => !string.IsNullOrEmpty(order.CaptureTransactionId)
                                            && order.CaptureTransactionId.Equals(sale.id, StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            var processPaymentResult = new ProcessPaymentResult
                                            {
                                                NewPaymentStatus = PaymentStatus.Paid,
                                                CaptureTransactionId = sale.id
                                            };
                                            _orderProcessingService.ProcessNextRecurringPayment(recurringPayment, processPaymentResult);
                                        }
                                    }
                                }
                                else if (sale.state.ToLowerInvariant().Equals("denied"))
                                {
                                    //payment denied
                                    _orderProcessingService.ProcessNextRecurringPayment(recurringPayment,
                                        new ProcessPaymentResult { Errors = new[] { webhook.summary }, RecurringPaymentFailed = true });
                                }
                                else
                                    _logger.Error(string.Format("PayPal error: Sale is {0} for the order #{1}", sale.state, initialOrder.Id));
                            }
                        }
                    }
                    else
                    //standard payment
                    {
                        var order = _orderService.GetOrderByGuid(new Guid(sale.invoice_number));
                        if (order != null)
                        {
                            if (sale.state.ToLowerInvariant().Equals("completed"))
                            {
                                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                {
                                    order.CaptureTransactionId = sale.id;
                                    order.CaptureTransactionResult = sale.state;
                                    _orderService.UpdateOrder(order);
                                    _orderProcessingService.MarkOrderAsPaid(order);
                                }
                            }
                            if (sale.state.ToLowerInvariant().Equals("denied"))
                            {
                                var reason = string.Format("Payment is denied. {0}", sale.fmf_details != null ?
                                    string.Format("Based on fraud filter: {0}. {1}", sale.fmf_details.name, sale.fmf_details.description) : string.Empty);
                                order.OrderNotes.Add(new OrderNote
                                {
                                    Note = reason,
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                                _logger.Error(string.Format("PayPal error: {0}", reason));
                            }
                        }
                        else
                            _logger.Error(string.Format("PayPal error: Order with guid {0} was not found", sale.invoice_number));
                    }
                }

                return new StatusCodeResult((int)HttpStatusCode.OK);
            }
            catch (PayPal.PayPalException exc)
            {
                if (exc is PayPal.ConnectionException)
                {
                    var error = JsonFormatter.ConvertFromJson<Error>((exc as PayPal.ConnectionException).Response);
                    if (error != null)
                    {
                        _logger.Error(string.Format("PayPal error: {0} ({1})", error.message, error.name));
                        if (error.details != null)
                            error.details.ForEach(x => _logger.Error(string.Format("{0} {1}", x.field, x.issue)));
                    }
                    else
                        _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
                }
                else
                    _logger.Error(exc.InnerException != null ? exc.InnerException.Message : exc.Message);

                return new StatusCodeResult((int)HttpStatusCode.OK);
            }
        }

        #endregion
    }
}