using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Widgets.GoogleAnalytics.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Widgets.GoogleAnalytics.Controllers
{
    /// <remarks>
    /// Task 15.2 substitutions — decisions tasks 7.3 (§30), 8.3 (§55) and 10.x/11.x already made:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c> plus
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> for <see cref="SelectListItem"/>.</item>
    /// <item><c>[ChildActionOnly]</c> → <c>[NopChildActionOnly]</c> on both <c>Configure</c>
    /// overloads and on <c>PublicInfo</c> (deferral 7.3-4). All three are reached only through the
    /// <c>Html.Action</c> bridge — the two <c>Configure</c>s from the admin plugin-config page,
    /// <c>PublicInfo</c> from the storefront widget zone — so their endpoints are suppressed for
    /// direct URL matching, exactly as MVC 5's <c>[ChildActionOnly]</c> did.</item>
    /// <item><c>View("~/Plugins/Widgets.GoogleAnalytics/Views/Configure.cshtml", model)</c> — call
    /// site UNCHANGED, thanks to the project file's <c>Content</c>/<c>Link</c> block.</item>
    /// </list>
    /// <c>[AdminAuthorize]</c>, <c>[HttpPost]</c>, <c>Content(...)</c>,
    /// <c>GetActiveStoreScopeConfiguration</c>, <c>SuccessNotification</c> needed no edit.
    /// <para>
    /// <b>THE ONE REAL <c>System.Web</c> DEPENDENCY: reading the CURRENT request's route in
    /// <see cref="PublicInfo"/>.</b> 3.90 did
    /// <c>((System.Web.UI.Page)this.HttpContext.CurrentHandler).RouteData</c> to discover which
    /// controller/action the storefront was actually rendering, so it could emit the richer
    /// e-commerce tracking script only on the checkout "completed" page. <c>HttpContext.CurrentHandler</c>
    /// and <c>System.Web.UI.Page</c> do not exist in ASP.NET Core. The faithful replacement is
    /// <c>HttpContext.Request.RouteValues</c> (the endpoint-routing values of the AMBIENT request):
    /// <c>PublicInfo</c> runs through the <c>Html.Action</c> bridge, which reuses the parent
    /// request's <c>HttpContext</c>, so <c>Request.RouteValues</c> holds the storefront page's own
    /// <c>controller</c>/<c>action</c> — which is precisely what <c>CurrentHandler.RouteData</c>
    /// gave 3.90. (The bridge builds a fresh child <c>RouteData</c> for the invoked action, so
    /// <c>this.RouteData</c> would report <c>WidgetsGoogleAnalytics</c>/<c>PublicInfo</c> instead —
    /// hence <c>Request.RouteValues</c>, not <c>ControllerContext.RouteData</c>.)
    /// </para>
    /// </remarks>
    public class WidgetsGoogleAnalyticsController : BasePluginController
    {
        private const string ORDER_ALREADY_PROCESSED_ATTRIBUTE_NAME = "GoogleAnalytics.OrderAlreadyProcessed";
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly ICategoryService _categoryService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly ILocalizationService _localizationService;
        private readonly IGenericAttributeService _genericAttributeService;

        public WidgetsGoogleAnalyticsController(IWorkContext workContext,
            IStoreContext storeContext, 
            IStoreService storeService,
            ISettingService settingService, 
            IOrderService orderService, 
            ILogger logger, 
            ICategoryService categoryService,
            IProductAttributeParser productAttributeParser,
            ILocalizationService localizationService,
            IGenericAttributeService genericAttributeService)
        {
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._orderService = orderService;
            this._logger = logger;
            this._categoryService = categoryService;
            this._productAttributeParser = productAttributeParser;
            this._localizationService = localizationService;
            this._genericAttributeService = genericAttributeService;
        }

        [AdminAuthorize]
        [NopChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsSettings>(storeScope);
            var model = new ConfigurationModel();
            model.GoogleId = googleAnalyticsSettings.GoogleId;
            model.TrackingScript = googleAnalyticsSettings.TrackingScript;
            model.EcommerceScript = googleAnalyticsSettings.EcommerceScript;
            model.EcommerceDetailScript = googleAnalyticsSettings.EcommerceDetailScript;
            model.IncludingTax = googleAnalyticsSettings.IncludingTax;
            model.ZoneId = googleAnalyticsSettings.WidgetZone;
            model.AvailableZones.Add(new SelectListItem() { Text = "Before body end html tag", Value = "body_end_html_tag_before" });
            model.AvailableZones.Add(new SelectListItem() { Text = "Head html tag", Value = "head_html_tag" });

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.GoogleId_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.GoogleId, storeScope);
                model.TrackingScript_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.TrackingScript, storeScope);
                model.EcommerceScript_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.EcommerceScript, storeScope);
                model.EcommerceDetailScript_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.EcommerceDetailScript, storeScope);
                model.IncludingTax_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.IncludingTax, storeScope);
                model.ZoneId_OverrideForStore = _settingService.SettingExists(googleAnalyticsSettings, x => x.WidgetZone, storeScope);
            }

            return View("~/Plugins/Widgets.GoogleAnalytics/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [NopChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsSettings>(storeScope);
            googleAnalyticsSettings.GoogleId = model.GoogleId;
            googleAnalyticsSettings.TrackingScript = model.TrackingScript;
            googleAnalyticsSettings.EcommerceScript = model.EcommerceScript;
            googleAnalyticsSettings.EcommerceDetailScript = model.EcommerceDetailScript;
            googleAnalyticsSettings.IncludingTax = model.IncludingTax;
            googleAnalyticsSettings.WidgetZone = model.ZoneId;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.GoogleId, model.GoogleId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.TrackingScript, model.TrackingScript_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.EcommerceScript, model.EcommerceScript_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.EcommerceDetailScript, model.EcommerceDetailScript_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.IncludingTax, model.IncludingTax_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(googleAnalyticsSettings, x => x.WidgetZone, model.ZoneId_OverrideForStore, storeScope, false);
            
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [NopChildActionOnly]
        public ActionResult PublicInfo(string widgetZone, object additionalData = null)
        {
            string globalScript = "";

            try
            {
                //TASK 15.2: 3.90 read
                //   ((System.Web.UI.Page)this.HttpContext.CurrentHandler).RouteData
                //to discover which storefront page is being rendered. System.Web.UI.Page /
                //HttpContext.CurrentHandler do not exist in ASP.NET Core. This widget is invoked
                //through the Html.Action bridge, which reuses the PARENT request's HttpContext, so
                //Request.RouteValues holds the storefront page's own controller/action - the
                //faithful replacement. (this.RouteData would be the bridge's child route data
                //naming this controller/action instead.)
                var routeValues = HttpContext.Request.RouteValues;
                var controller = routeValues.ContainsKey("controller") ? routeValues["controller"] : null;
                var action = routeValues.ContainsKey("action") ? routeValues["action"] : null;

                if (controller == null || action == null)
                    return Content("");

                //Special case, if we are in last step of checkout, we can use order total for conversion value
                if (controller.ToString().Equals("checkout", StringComparison.InvariantCultureIgnoreCase) &&
                    action.ToString().Equals("completed", StringComparison.InvariantCultureIgnoreCase))
                {
                    var lastOrder = GetLastOrder();
                    globalScript += GetEcommerceScript(lastOrder);
                }
                else
                {
                    globalScript += GetEcommerceScript(null);
                }
            }
            catch (Exception ex)
            {
                _logger.InsertLog(Core.Domain.Logging.LogLevel.Error, "Error creating scripts for google ecommerce tracking", ex.ToString());
            }
            return Content(globalScript);
        }

        private Order GetLastOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            return order;
        }
        
        //<script type="text/javascript"> 

        //var _gaq = _gaq || []; 
        //_gaq.push(['_setAccount', 'UA-XXXXX-X']); 
        //_gaq.push(['_trackPageview']); 
        //_gaq.push(['_addTrans', 
        //'1234',           // order ID - required 
        //'Acme Clothing',  // affiliation or store name 
        //'11.99',          // total - required 
        //'1.29',           // tax 
        //'5',              // shipping 
        //'San Jose',       // city 
        //'California',     // state or province 
        //'USA'             // country 
        //]); 

        //// add item might be called for every item in the shopping cart 
        //// where your ecommerce engine loops through each item in the cart and 
        //// prints out _addItem for each 
        //_gaq.push(['_addItem', 
        //'1234',           // order ID - required 
        //'DD44',           // SKU/code - required 
        //'T-Shirt',        // product name 
        //'Green Medium',   // category or variation 
        //'11.99',          // unit price - required 
        //'1'               // quantity - required 
        //]); 
        //_gaq.push(['_trackTrans']); //submits transaction to the Analytics servers 

        //(function() { 
        //var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true; 
        //ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js'; 
        //var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s); 
        //})(); 

        //</script>
        private string GetEcommerceScript(Order order)
        {
            var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsSettings>(_storeContext.CurrentStore.Id);
            var analyticsTrackingScript = googleAnalyticsSettings.TrackingScript + "\n";
            analyticsTrackingScript = analyticsTrackingScript.Replace("{GOOGLEID}", googleAnalyticsSettings.GoogleId);

            //ensure that ecommerce tracking code is renderred only once (avoid duplicated data in Google Analytics)
            if (order != null && !order.GetAttribute<bool>(ORDER_ALREADY_PROCESSED_ATTRIBUTE_NAME))
            {
                var usCulture = new CultureInfo("en-US");

                var analyticsEcommerceScript = googleAnalyticsSettings.EcommerceScript + "\n";
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{GOOGLEID}", googleAnalyticsSettings.GoogleId);
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{ORDERID}", order.Id.ToString());
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{SITE}", _storeContext.CurrentStore.Url.Replace("http://", "").Replace("/", ""));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{TOTAL}", order.OrderTotal.ToString("0.00", usCulture));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{TAX}", order.OrderTax.ToString("0.00", usCulture));
                var orderShipping = googleAnalyticsSettings.IncludingTax ? order.OrderShippingInclTax : order.OrderShippingExclTax;
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{SHIP}", orderShipping.ToString("0.00", usCulture));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{CITY}", order.BillingAddress == null ? "" : FixIllegalJavaScriptChars(order.BillingAddress.City));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{STATEPROVINCE}", order.BillingAddress == null || order.BillingAddress.StateProvince == null ? "" : FixIllegalJavaScriptChars(order.BillingAddress.StateProvince.Name));
                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{COUNTRY}", order.BillingAddress == null || order.BillingAddress.Country == null ? "" : FixIllegalJavaScriptChars(order.BillingAddress.Country.Name));

                var sb = new StringBuilder();
                foreach (var item in order.OrderItems)
                {
                    string analyticsEcommerceDetailScript = googleAnalyticsSettings.EcommerceDetailScript;
                    //get category
                    string category = "";
                    var defaultProductCategory = _categoryService.GetProductCategoriesByProductId(item.ProductId).FirstOrDefault();
                    if (defaultProductCategory != null)
                        category = defaultProductCategory.Category.Name;
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{ORDERID}", item.OrderId.ToString());
                    //The SKU code is a required parameter for every item that is added to the transaction
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{PRODUCTSKU}", FixIllegalJavaScriptChars(item.Product.FormatSku(item.AttributesXml, _productAttributeParser)));
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{PRODUCTNAME}", FixIllegalJavaScriptChars(item.Product.Name));
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{CATEGORYNAME}", FixIllegalJavaScriptChars(category));
                    var unitPrice = googleAnalyticsSettings.IncludingTax ? item.UnitPriceInclTax : item.UnitPriceExclTax;
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{UNITPRICE}", unitPrice.ToString("0.00", usCulture));
                    analyticsEcommerceDetailScript = analyticsEcommerceDetailScript.Replace("{QUANTITY}", item.Quantity.ToString());
                    sb.AppendLine(analyticsEcommerceDetailScript);
                }

                analyticsEcommerceScript = analyticsEcommerceScript.Replace("{DETAILS}", sb.ToString());

                analyticsTrackingScript = analyticsTrackingScript.Replace("{ECOMMERCE}", analyticsEcommerceScript);

                _genericAttributeService.SaveAttribute(order, ORDER_ALREADY_PROCESSED_ATTRIBUTE_NAME, true);
            }
            else
            {
                analyticsTrackingScript = analyticsTrackingScript.Replace("{ECOMMERCE}", "");
            }

            return analyticsTrackingScript;
        }

        private string FixIllegalJavaScriptChars(string text)
        {
            if (String.IsNullOrEmpty(text))
                return text;

            //replace ' with \' (http://stackoverflow.com/questions/4292761/need-to-url-encode-labels-when-tracking-events-with-google-analytics)
            text = text.Replace("'", "\\'");
            return text;
        }
    }
}