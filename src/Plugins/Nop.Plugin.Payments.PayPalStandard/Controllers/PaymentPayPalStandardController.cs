using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.PayPalStandard.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.PayPalStandard.Controllers
{
    /// <remarks>
    /// <para>
    /// Task 12.4 substitutions, reusing decisions tasks 7.3 (§30), 8.3 (§55) and 10.1 (§83.9)
    /// already recorded: <c>System.Web.Mvc</c> -&gt; <c>Microsoft.AspNetCore.Mvc</c> +
    /// <c>Microsoft.AspNetCore.Http</c>; <c>FormCollection</c> -&gt;
    /// <see cref="IFormCollection"/> (four sites - the two
    /// <see cref="BasePaymentController"/> overrides plus the <see cref="PDTHandler"/> and
    /// <see cref="CancelOrder"/> action parameters); <c>[ChildActionOnly]</c> x2 deleted
    /// (deferral 7.3-4); <c>Json(x, JsonRequestBehavior.AllowGet)</c> -&gt; <c>Json(x)</c> x2;
    /// <c>[ValidateInput(false)]</c> x3 deleted. The
    /// <c>View("~/Plugins/Payments.PayPalStandard/Views/...")</c> paths are unchanged.
    /// </para>
    /// <para>
    /// <b><c>[ValidateInput(false)]</c> x3 DELETED - a SECURITY-RELEVANT RELAXATION, recorded.</b>
    /// It existed only to opt OUT of ASP.NET request validation, which ASP.NET Core does not
    /// have (deferral <b>7.3-3</b>, recorded at 42 Nop.Web sites and 24 admin ones). The
    /// direction here is worth stating precisely, because these three are unusual: 3.90 marked
    /// <see cref="PDTHandler"/>, <see cref="IPNHandler"/> and <see cref="RoundingWarning"/>
    /// EXEMPT because PayPal's callbacks carry arbitrary text in fields like
    /// <c>payment_status</c>, <c>memo</c> and address lines that request validation would have
    /// rejected outright. So these three actions were never screened in 3.90 either - their
    /// exposure is unchanged, not widened. What protects them is unchanged too, and it is the
    /// part that matters: <see cref="IPNHandler"/> accepts nothing until
    /// <c>PayPalStandardPaymentProcessor.VerifyIpn</c> has POSTed the raw body back to PayPal
    /// and got <c>VERIFIED</c>, and <see cref="PDTHandler"/> accepts nothing until
    /// <c>GetPdtDetails</c> has done the same with the PDT token. Both round trips are intact.
    /// </para>
    /// <para>
    /// <b><c>JsonRequestBehavior.AllowGet</c> x2 -&gt; nothing.</b> The type does not exist in
    /// ASP.NET Core: there is no JSON-hijacking guard and therefore no opt-out from one. Note
    /// the direction - MVC 5's default was <c>DenyGet</c> and both call sites explicitly opted
    /// out, so the ported behaviour is what 3.90 asked for. <see cref="RoundingWarning"/> is
    /// fetched by <c>$.ajax</c> from the admin Configure view and returns a localized warning
    /// string with no customer data in it.
    /// </para>
    /// </remarks>
    public class PaymentPayPalStandardController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly PaymentSettings _paymentSettings;
        private readonly PayPalStandardPaymentSettings _payPalStandardPaymentSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public PaymentPayPalStandardController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IPaymentService paymentService, 
            IOrderService orderService, 
            IOrderProcessingService orderProcessingService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger, 
            IWebHelper webHelper,
            PaymentSettings paymentSettings,
            PayPalStandardPaymentSettings payPalStandardPaymentSettings,
            ShoppingCartSettings shoppingCartSettings)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._paymentSettings = paymentSettings;
            this._payPalStandardPaymentSettings = payPalStandardPaymentSettings;
            this._shoppingCartSettings = shoppingCartSettings;
        }
        
        [AdminAuthorize]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalStandardPaymentSettings = _settingService.LoadSetting<PayPalStandardPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.UseSandbox = payPalStandardPaymentSettings.UseSandbox;
            model.BusinessEmail = payPalStandardPaymentSettings.BusinessEmail;
            model.PdtToken = payPalStandardPaymentSettings.PdtToken;
            model.PdtValidateOrderTotal = payPalStandardPaymentSettings.PdtValidateOrderTotal;
            model.AdditionalFee = payPalStandardPaymentSettings.AdditionalFee;
            model.AdditionalFeePercentage = payPalStandardPaymentSettings.AdditionalFeePercentage;
            model.PassProductNamesAndTotals = payPalStandardPaymentSettings.PassProductNamesAndTotals;
            model.EnableIpn = payPalStandardPaymentSettings.EnableIpn;
            model.IpnUrl = payPalStandardPaymentSettings.IpnUrl;
            model.AddressOverride = payPalStandardPaymentSettings.AddressOverride;
            model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage = payPalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.UseSandbox, storeScope);
                model.BusinessEmail_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.BusinessEmail, storeScope);
                model.PdtToken_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PdtToken, storeScope);
                model.PdtValidateOrderTotal_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PdtValidateOrderTotal, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.PassProductNamesAndTotals_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, storeScope);
                model.EnableIpn_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.EnableIpn, storeScope);
                model.IpnUrl_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.IpnUrl, storeScope);
                model.AddressOverride_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.AddressOverride, storeScope);
                model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore = _settingService.SettingExists(payPalStandardPaymentSettings, x => x.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage, storeScope);
            }

            return View("~/Plugins/Payments.PayPalStandard/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var payPalStandardPaymentSettings = _settingService.LoadSetting<PayPalStandardPaymentSettings>(storeScope);

            //save settings
            payPalStandardPaymentSettings.UseSandbox = model.UseSandbox;
            payPalStandardPaymentSettings.BusinessEmail = model.BusinessEmail;
            payPalStandardPaymentSettings.PdtToken = model.PdtToken;
            payPalStandardPaymentSettings.PdtValidateOrderTotal = model.PdtValidateOrderTotal;
            payPalStandardPaymentSettings.AdditionalFee = model.AdditionalFee;
            payPalStandardPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            payPalStandardPaymentSettings.PassProductNamesAndTotals = model.PassProductNamesAndTotals;
            payPalStandardPaymentSettings.EnableIpn = model.EnableIpn;
            payPalStandardPaymentSettings.IpnUrl = model.IpnUrl;
            payPalStandardPaymentSettings.AddressOverride = model.AddressOverride;
            payPalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage = model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.BusinessEmail, model.BusinessEmail_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.PdtToken, model.PdtToken_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.PdtValidateOrderTotal, model.PdtValidateOrderTotal_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.PassProductNamesAndTotals, model.PassProductNamesAndTotals_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.EnableIpn, model.EnableIpn_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.IpnUrl, model.IpnUrl_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.AddressOverride, model.AddressOverride_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(payPalStandardPaymentSettings, x => x.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage, model.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate PayPal rounding
        public ActionResult RoundingWarning(bool passProductNamesAndTotals)
        {
            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = _localizationService.GetResource("Plugins.Payments.PayPalStandard.RoundingWarning") });

            return Json(new { Result = string.Empty });
        }

        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.PayPalStandard/Views/PaymentInfo.cshtml");
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        /// <remarks>
        /// The <see cref="IFormCollection"/> parameter is UNUSED by the body (the transaction
        /// token comes from <c>_webHelper.QueryString&lt;string&gt;("tx")</c>) and is kept only
        /// because it is part of 3.90's action signature. It binds safely on the GET that PayPal
        /// redirects the customer back with: ASP.NET Core's <c>FormCollectionModelBinder</c>
        /// checks <c>HasFormContentType</c> and supplies an EMPTY collection rather than
        /// throwing, which is what <c>System.Web</c>'s binder did too.
        /// </remarks>
        public ActionResult PDTHandler(IFormCollection form)
        {
            var tx = _webHelper.QueryString<string>("tx");
            Dictionary<string, string> values;
            string response;

            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.PayPalStandard") as PayPalStandardPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("PayPal Standard module cannot be loaded");

            if (processor.GetPdtDetails(tx, out values, out response))
            {
                string orderNumber = string.Empty;
                values.TryGetValue("custom", out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    decimal mc_gross = decimal.Zero;
                    try
                    {
                        mc_gross = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                    }
                    catch (Exception exc)
                    {
                        _logger.Error("PayPal PDT. Error getting mc_gross", exc);
                    }

                    string payer_status = string.Empty;
                    values.TryGetValue("payer_status", out payer_status);
                    string payment_status = string.Empty;
                    values.TryGetValue("payment_status", out payment_status);
                    string pending_reason = string.Empty;
                    values.TryGetValue("pending_reason", out pending_reason);
                    string mc_currency = string.Empty;
                    values.TryGetValue("mc_currency", out mc_currency);
                    string txn_id = string.Empty;
                    values.TryGetValue("txn_id", out txn_id);
                    string payment_type = string.Empty;
                    values.TryGetValue("payment_type", out payment_type);
                    string payer_id = string.Empty;
                    values.TryGetValue("payer_id", out payer_id);
                    string receiver_id = string.Empty;
                    values.TryGetValue("receiver_id", out receiver_id);
                    string invoice = string.Empty;
                    values.TryGetValue("invoice", out invoice);
                    string payment_fee = string.Empty;
                    values.TryGetValue("payment_fee", out payment_fee);

                    var sb = new StringBuilder();
                    sb.AppendLine("Paypal PDT:");
                    sb.AppendLine("mc_gross: " + mc_gross);
                    sb.AppendLine("Payer status: " + payer_status);
                    sb.AppendLine("Payment status: " + payment_status);
                    sb.AppendLine("Pending reason: " + pending_reason);
                    sb.AppendLine("mc_currency: " + mc_currency);
                    sb.AppendLine("txn_id: " + txn_id);
                    sb.AppendLine("payment_type: " + payment_type);
                    sb.AppendLine("payer_id: " + payer_id);
                    sb.AppendLine("receiver_id: " + receiver_id);
                    sb.AppendLine("invoice: " + invoice);
                    sb.AppendLine("payment_fee: " + payment_fee);

                    var newPaymentStatus = PaypalHelper.GetPaymentStatus(payment_status, pending_reason);
                    sb.AppendLine("New payment status: " + newPaymentStatus);

                    //order note
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = sb.ToString(),
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);

                    //load settings for a chosen store scope
                    var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
                    var payPalStandardPaymentSettings = _settingService.LoadSetting<PayPalStandardPaymentSettings>(storeScope);

                    //validate order total
                    var orderTotalSentToPayPal = order.GetAttribute<decimal?>("OrderTotalSentToPayPal");
                    if (payPalStandardPaymentSettings.PdtValidateOrderTotal && orderTotalSentToPayPal.HasValue && mc_gross != orderTotalSentToPayPal.Value)
                    {
                        string errorStr = string.Format("PayPal PDT. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal, order.Id);
                        //log
                        _logger.Error(errorStr);
                        //order note
                        order.OrderNotes.Add(new OrderNote
                        {
                            Note = errorStr,
                            DisplayToCustomer = false,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);

                        return RedirectToAction("Index", "Home", new { area = "" });
                    }
                    //clear attribute
                    if (orderTotalSentToPayPal.HasValue)
                        _genericAttributeService.SaveAttribute<decimal?>(order, "OrderTotalSentToPayPal", null);

                    //mark order as paid
                    if (newPaymentStatus == PaymentStatus.Paid)
                    {
                        if (_orderProcessingService.CanMarkOrderAsPaid(order))
                        {
                            order.AuthorizationTransactionId = txn_id;
                            _orderService.UpdateOrder(order);

                            _orderProcessingService.MarkOrderAsPaid(order);
                        }
                    }
                }

                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id});
            }
            else
            {
                string orderNumber = string.Empty;
                values.TryGetValue("custom", out orderNumber);
                Guid orderNumberGuid = Guid.Empty;
                try
                {
                    orderNumberGuid = new Guid(orderNumber);
                }
                catch { }
                Order order = _orderService.GetOrderByGuid(orderNumberGuid);
                if (order != null)
                {
                    //order note
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = "PayPal PDT failed. " + response,
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                }
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        /// <summary>
        /// PayPal Instant Payment Notification endpoint.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Task 12.4 - <c>Request.BinaryRead(Request.ContentLength)</c> has no ASP.NET Core
        /// counterpart</b> (<c>HttpRequestBase.BinaryRead</c> and the <c>int ContentLength</c>
        /// property are both <c>System.Web</c>). The replacement reads <c>Request.Body</c> to
        /// completion, which is strictly MORE correct than what it replaces: 3.90's single
        /// <c>BinaryRead(n)</c> was the same latent truncation shape task 4.2 fixed in
        /// <c>Nop.Services</c>' <c>Media.Extensions</c> and task 7.3 in the two upload actions -
        /// it trusted a client-declared length and one read call. Reading to completion cannot
        /// truncate, and for a well-formed IPN the resulting string is byte-identical.
        /// </para>
        /// <para>
        /// <b>The action became <c>async</c>, and that is required rather than stylistic.</b>
        /// Kestrel sets <c>AllowSynchronousIO = false</c> by default (since .NET Core 3.0) and
        /// this host does not override it, so a synchronous read of <c>Request.Body</c> throws
        /// <c>InvalidOperationException</c>. There is no in-process caller to break: the only
        /// way in is the <c>Plugin.Payments.PayPalStandard.IPNHandler</c> route, and ASP.NET
        /// Core invokes an async action transparently. (Deferral <b>12.x-2</b> records that five
        /// PRE-EXISTING sites in <c>Nop.Web</c>/<c>Nop.Admin</c> still do a synchronous
        /// <c>Request.Body.CopyTo</c> and would hit the same wall - found while deciding this,
        /// not introduced by it, and out of this task's scope.)
        /// </para>
        /// <para>
        /// <b><c>Encoding.ASCII</c> is KEPT deliberately.</b> UTF-8 would be the modern choice
        /// and would decode more characters, but the decoded string is fed straight into
        /// <c>VerifyIpn</c>, which POSTs it back to PayPal as <c>cmd=_notify-validate&amp;...</c>
        /// and requires a BYTE-FOR-BYTE echo of the original notification for the signature
        /// check to succeed. Changing the decoding would change what is echoed and could make a
        /// genuine IPN fail validation - a behaviour change on the payment-authenticity path.
        /// 3.90 used ASCII; so does this.
        /// </para>
        /// </remarks>
        public async Task<ActionResult> IPNHandler()
        {
            string strRequest;
            using (var reader = new StreamReader(Request.Body, Encoding.ASCII))
            {
                strRequest = await reader.ReadToEndAsync();
            }
            Dictionary<string, string> values;

            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.PayPalStandard") as PayPalStandardPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("PayPal Standard module cannot be loaded");

            if (processor.VerifyIpn(strRequest, out values))
            {
                #region values
                decimal mc_gross = decimal.Zero;
                try
                {
                    mc_gross = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
                }
                catch { }

                string payer_status = string.Empty;
                values.TryGetValue("payer_status", out payer_status);
                string payment_status = string.Empty;
                values.TryGetValue("payment_status", out payment_status);
                string pending_reason = string.Empty;
                values.TryGetValue("pending_reason", out pending_reason);
                string mc_currency = string.Empty;
                values.TryGetValue("mc_currency", out mc_currency);
                string txn_id = string.Empty;
                values.TryGetValue("txn_id", out txn_id);
                string txn_type = string.Empty;
                values.TryGetValue("txn_type", out txn_type);
                string rp_invoice_id = string.Empty;
                values.TryGetValue("rp_invoice_id", out rp_invoice_id);
                string payment_type = string.Empty;
                values.TryGetValue("payment_type", out payment_type);
                string payer_id = string.Empty;
                values.TryGetValue("payer_id", out payer_id);
                string receiver_id = string.Empty;
                values.TryGetValue("receiver_id", out receiver_id);
                string invoice = string.Empty;
                values.TryGetValue("invoice", out invoice);
                string payment_fee = string.Empty;
                values.TryGetValue("payment_fee", out payment_fee);

                #endregion

                var sb = new StringBuilder();
                sb.AppendLine("Paypal IPN:");
                foreach (KeyValuePair<string, string> kvp in values)
                {
                    sb.AppendLine(kvp.Key + ": " + kvp.Value);
                }

                var newPaymentStatus = PaypalHelper.GetPaymentStatus(payment_status, pending_reason);
                sb.AppendLine("New payment status: " + newPaymentStatus);

                switch (txn_type)
                {
                    case "recurring_payment_profile_created":
                        //do nothing here
                        break;
                    #region Recurring payment
                    case "recurring_payment":
                        {
                            Guid orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(rp_invoice_id);
                            }
                            catch
                            {
                            }

                            var initialOrder = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (initialOrder != null)
                            {
                                var recurringPayments = _orderService.SearchRecurringPayments(initialOrderId: initialOrder.Id);
                                foreach (var rp in recurringPayments)
                                {
                                    switch (newPaymentStatus)
                                    {
                                        case PaymentStatus.Authorized:
                                        case PaymentStatus.Paid:
                                            {
                                                var recurringPaymentHistory = rp.RecurringPaymentHistory;
                                                if (!recurringPaymentHistory.Any())
                                                {
                                                    //first payment
                                                    var rph = new RecurringPaymentHistory
                                                    {
                                                        RecurringPaymentId = rp.Id,
                                                        OrderId = initialOrder.Id,
                                                        CreatedOnUtc = DateTime.UtcNow
                                                    };
                                                    rp.RecurringPaymentHistory.Add(rph);
                                                    _orderService.UpdateRecurringPayment(rp);
                                                }
                                                else
                                                {
                                                    //next payments
                                                    var processPaymentResult = new ProcessPaymentResult();
                                                    processPaymentResult.NewPaymentStatus = newPaymentStatus;
                                                    if (newPaymentStatus == PaymentStatus.Authorized)
                                                        processPaymentResult.AuthorizationTransactionId = txn_id;
                                                    else
                                                        processPaymentResult.CaptureTransactionId = txn_id;

                                                    _orderProcessingService.ProcessNextRecurringPayment(rp, processPaymentResult);
                                                }
                                            }
                                            break;
                                        case PaymentStatus.Voided:
                                            //failed payment
                                            var failedPaymentResult = new ProcessPaymentResult
                                            {
                                                Errors = new[] { string.Format("PayPal IPN. Recurring payment is {0} .", payment_status) },
                                                RecurringPaymentFailed = true
                                            };
                                            _orderProcessingService.ProcessNextRecurringPayment(rp, failedPaymentResult);
                                            break;
                                    }
                                }

                                //this.OrderService.InsertOrderNote(newOrder.OrderId, sb.ToString(), DateTime.UtcNow);
                                _logger.Information("PayPal IPN. Recurring info", new NopException(sb.ToString()));
                            }
                            else
                            {
                                _logger.Error("PayPal IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }                       
                        break;
                    case "recurring_payment_failed":
                        var orderGuid = Guid.Empty;
                        if (Guid.TryParse(rp_invoice_id, out orderGuid))
                        {
                            var initialOrder = _orderService.GetOrderByGuid(orderGuid);
                            if (initialOrder != null)
                            {
                                var recurringPayment = _orderService.SearchRecurringPayments(initialOrderId: initialOrder.Id).FirstOrDefault();
                                //failed payment
                                if (recurringPayment != null)
                                    _orderProcessingService.ProcessNextRecurringPayment(recurringPayment, new ProcessPaymentResult { Errors = new[] { txn_type }, RecurringPaymentFailed = true });
                            }
                        }
                        break;
                    #endregion
                    default:
                        #region Standard payment
                        {
                            string orderNumber = string.Empty;
                            values.TryGetValue("custom", out orderNumber);
                            Guid orderNumberGuid = Guid.Empty;
                            try
                            {
                                orderNumberGuid = new Guid(orderNumber);
                            }
                            catch
                            {
                            }

                            var order = _orderService.GetOrderByGuid(orderNumberGuid);
                            if (order != null)
                            {

                                //order note
                                order.OrderNotes.Add(new OrderNote
                                {
                                    Note = sb.ToString(),
                                    DisplayToCustomer = false,
                                    CreatedOnUtc = DateTime.UtcNow
                                });
                                _orderService.UpdateOrder(order);

                                switch (newPaymentStatus)
                                {
                                    case PaymentStatus.Pending:
                                        {
                                        }
                                        break;
                                    case PaymentStatus.Authorized:
                                        {
                                            //validate order total
                                            if (Math.Round(mc_gross, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //valid
                                                if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                                                {
                                                    _orderProcessingService.MarkAsAuthorized(order);
                                                }
                                            }
                                            else
                                            {
                                                //not valid
                                                string errorStr = string.Format("PayPal IPN. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal, order.Id);
                                                //log
                                                _logger.Error(errorStr);
                                                //order note
                                                order.OrderNotes.Add(new OrderNote
                                                {
                                                    Note = errorStr,
                                                    DisplayToCustomer = false,
                                                    CreatedOnUtc = DateTime.UtcNow
                                                });
                                                _orderService.UpdateOrder(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Paid:
                                        {
                                            //validate order total
                                            if (Math.Round(mc_gross, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //valid
                                                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                                                {
                                                    order.AuthorizationTransactionId = txn_id;
                                                    _orderService.UpdateOrder(order);

                                                    _orderProcessingService.MarkOrderAsPaid(order);
                                                }
                                            }
                                            else
                                            {
                                                //not valid
                                                string errorStr = string.Format("PayPal IPN. Returned order total {0} doesn't equal order total {1}. Order# {2}.", mc_gross, order.OrderTotal, order.Id);
                                                //log
                                                _logger.Error(errorStr);
                                                //order note
                                                order.OrderNotes.Add(new OrderNote
                                                {
                                                    Note = errorStr,
                                                    DisplayToCustomer = false,
                                                    CreatedOnUtc = DateTime.UtcNow
                                                });
                                                _orderService.UpdateOrder(order);
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Refunded:
                                        {
                                            var totalToRefund = Math.Abs(mc_gross);
                                            if (totalToRefund > 0 && Math.Round(totalToRefund, 2).Equals(Math.Round(order.OrderTotal, 2)))
                                            {
                                                //refund
                                                if (_orderProcessingService.CanRefundOffline(order))
                                                {
                                                    _orderProcessingService.RefundOffline(order);
                                                }
                                            }
                                            else
                                            {
                                                //partial refund
                                                if (_orderProcessingService.CanPartiallyRefundOffline(order, totalToRefund))
                                                {
                                                    _orderProcessingService.PartiallyRefundOffline(order, totalToRefund);
                                                }
                                            }
                                        }
                                        break;
                                    case PaymentStatus.Voided:
                                        {
                                            if (_orderProcessingService.CanVoidOffline(order))
                                            {
                                                _orderProcessingService.VoidOffline(order);
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            else
                            {
                                _logger.Error("PayPal IPN. Order is not found", new NopException(sb.ToString()));
                            }
                        }
                        #endregion
                        break;
                }
            }
            else
            {
                _logger.Error("PayPal IPN failed.", new NopException(strRequest));
            }

            //nothing should be rendered to visitor
            return Content("");
        }

        public ActionResult CancelOrder(IFormCollection form)
        {
            if (_payPalStandardPaymentSettings.ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage)
            {
                var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                    customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                    .FirstOrDefault();
                if (order != null)
                {
                    return RedirectToRoute("OrderDetails", new { orderId = order.Id });
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}