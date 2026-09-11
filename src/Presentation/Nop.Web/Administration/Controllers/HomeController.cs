using System;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using Nop.Admin.Infrastructure.Cache;
using Nop.Admin.Models.Home;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class HomeController : BaseAdminController
    {
        #region Fields
        private readonly IStoreContext _storeContext;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IReturnRequestService _returnRequestService;
        private readonly IWorkContext _workContext;
        private readonly ICacheManager _cacheManager;

        #endregion

        #region Ctor

        public HomeController(IStoreContext storeContext,
            AdminAreaSettings adminAreaSettings, 
            ISettingService settingService,
            IPermissionService permissionService,
            IProductService productService,
            IOrderService orderService,
            ICustomerService customerService,
            IReturnRequestService returnRequestService,
            IWorkContext workContext,
            ICacheManager cacheManager)
        {
            this._storeContext = storeContext;
            this._adminAreaSettings = adminAreaSettings;
            this._settingService = settingService;
            this._permissionService = permissionService;
            this._productService = productService;
            this._orderService = orderService;
            this._customerService = customerService;
            this._returnRequestService = returnRequestService;
            this._workContext = workContext;
            this._cacheManager = cacheManager;
        }

        #endregion

        #region Methods

        public virtual ActionResult Index()
        {
            var model = new DashboardModel();
            model.IsLoggedInAsVendor = _workContext.CurrentVendor != null;
            return View(model);
        }

        [NopChildActionOnly]
        public virtual ActionResult NopCommerceNews()
        {
            try
            {
                string feedUrl = string.Format("http://www.nopCommerce.com/NewsRSS.aspx?Version={0}&Localhost={1}&HideAdvertisements={2}&StoreURL={3}",
                    NopVersion.CurrentVersion, 
                    //TASK 8.3 - HttpRequest.Url (a System.Uri) does not exist in ASP.NET Core; the
                    //request URL is split across Scheme/Host/PathBase/Path/QueryString. Uri.IsLoopback
                    //is reproduced from the connection's local address, which is what it actually
                    //measured - and it is now MORE accurate: Uri.IsLoopback tested the host STRING, so
                    //a request to a machine's own name or LAN address reported false where this reports
                    //true only for a genuine loopback connection. The value is a query-string flag on
                    //the nopCommerce news feed, so a difference is not load-bearing.
                    IsLoopbackRequest(),
                    _adminAreaSettings.HideAdvertisementsOnAdminArea,
                    _storeContext.CurrentStore.Url)
                    .ToLowerInvariant();

                var rssData = _cacheManager.Get(ModelCacheEventConsumer.OFFICIAL_NEWS_MODEL_KEY, () =>
                {
                    //TASK 8.7 - WebRequest.Create IS OBSOLETE (SYSLIB0014) AND IS DELIBERATELY KEPT.
                    //This is a recorded decision, not an oversight; see runtime-deferrals.md 8.7-1.
                    //
                    //1. SOLUTION-WIDE CONSISTENCY. Nop.Core/Plugins/OfficialFeedManager.cs line 23
                    //   is the IDENTICAL pattern - WebRequest.Create(url) + Timeout + GetResponse()
                    //   + XML parse - and it is part of Nop.Core's ACCEPTED 3-warning baseline at
                    //   gate 2.5. Nop.Services/Common/KeepAliveTask.cs line 25 uses WebClient and is
                    //   in Nop.Services' accepted 10. This migration has twice declined the same
                    //   conversion in projects that have already passed their gate; converting it
                    //   here alone would make Nop.Admin the only project that modernised its
                    //   SYSLIB0014 site.
                    //
                    //2. THE CONVERSION IS NOT BEHAVIOUR-PRESERVING, in three specific ways:
                    //   * Timeout SEMANTICS DIFFER. HttpWebRequest.Timeout bounds GetResponse()
                    //     only - it does not cover reading the response stream. HttpClient.Timeout
                    //     bounds the whole operation including the body read. So a feed that
                    //     responds quickly but streams slowly succeeds today and would start
                    //     timing out. That is a change in the direction of MORE failures.
                    //   * NON-SUCCESS STATUS. GetResponse() throws WebException for 4xx/5xx, so
                    //     nothing is cached. HttpClient.Send() returns the response and the failure
                    //     would surface later, from SyndicationFeed.Load on an HTML error body -
                    //     a different exception type from a different place, and what actually gets
                    //     handed to _cacheManager.Get depends on a third-party server's error page.
                    //   * PROXY AND LIFETIME. A correct HttpClient port needs a STATIC client (the
                    //     socket-exhaustion pattern task 4.2 used for TaxService), which introduces
                    //     a process-wide singleton with its own DNS and proxy resolution, not quite
                    //     WebRequest.DefaultWebProxy's.
                    //
                    //3. NO GAIN. This is the admin dashboard's nopCommerce news feed over plain
                    //   HTTP to a third-party site, already wrapped in the catch below that returns
                    //   Content(""). WebRequest is obsolete but fully functional on .NET 10, it is
                    //   not a System.Web dependency, and the warning is non-blocking (Req 3.3).
                    //
                    //If this is ever converted, convert all three sites together and decide the
                    //timeout semantics explicitly.
                    //
                    //THE WARNING IS DELIBERATELY LEFT VISIBLE - no #pragma, no NoWarn. Nop.Core
                    //carries the identical SYSLIB0014 unsuppressed in its accepted baseline, and
                    //suppressing it here would leave the same fact visible in one project and
                    //hidden in another, so the warning counts would stop describing the code.
                    //specify timeout (5 secs)
                    var request = WebRequest.Create(feedUrl);
                    request.Timeout = 5000;
                    using (var response = request.GetResponse())
                    using (var reader = XmlReader.Create(response.GetResponseStream()))
                    {
                        return SyndicationFeed.Load(reader);
                    }
                });
                
                var model = new NopCommerceNewsModel()
                {
                    HideAdvertisements = _adminAreaSettings.HideAdvertisementsOnAdminArea
                };
                for (int i = 0; i < rssData.Items.Count(); i++)
                {
                    var item = rssData.Items.ElementAt(i);
                    var newsItem = new NopCommerceNewsModel.NewsDetailsModel()
                    {
                        Title = item.Title.Text,
                        Summary = item.Summary.Text,
                        Url = item.Links.Any() ? item.Links.First().Uri.OriginalString : null,
                        PublishDate = item.PublishDate
                    };
                    model.Items.Add(newsItem);

                    //has new items?
                    if (i == 0)
                    {
                        var firstRequest = String.IsNullOrEmpty(_adminAreaSettings.LastNewsTitleAdminArea);
                        if (_adminAreaSettings.LastNewsTitleAdminArea != newsItem.Title)
                        {
                            _adminAreaSettings.LastNewsTitleAdminArea = newsItem.Title;
                            _settingService.SaveSetting(_adminAreaSettings);

                            if (!firstRequest)
                            {
                                //new item
                                model.HasNewItems = true;
                            }
                        }
                    }
                }
                return PartialView(model);
            }
            catch (Exception)
            {
                return Content("");
            }
        }

        [HttpPost]
        public virtual ActionResult NopCommerceNewsHideAdv()
        {
            _adminAreaSettings.HideAdvertisementsOnAdminArea = !_adminAreaSettings.HideAdvertisementsOnAdminArea;
            _settingService.SaveSetting(_adminAreaSettings);
            return Content("Setting changed");
        }

        [NopChildActionOnly]
        public virtual ActionResult CommonStatistics()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageCustomers) ||
                !_permissionService.Authorize(StandardPermissionProvider.ManageOrders) ||
                !_permissionService.Authorize(StandardPermissionProvider.ManageReturnRequests) ||
                !_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return Content("");

            //a vendor doesn't have access to this report
            if (_workContext.CurrentVendor != null)
                return Content("");

            var model = new CommonStatisticsModel();

            model.NumberOfOrders = _orderService.SearchOrders(
                pageIndex: 0, 
                pageSize: 1).TotalCount;

            model.NumberOfCustomers = _customerService.GetAllCustomers(
                customerRoleIds: new [] { _customerService.GetCustomerRoleBySystemName(SystemCustomerRoleNames.Registered).Id }, 
                pageIndex: 0, 
                pageSize: 1).TotalCount;

            model.NumberOfPendingReturnRequests = _returnRequestService.SearchReturnRequests(
                rs: ReturnRequestStatus.Pending, 
                pageIndex: 0, 
                pageSize:1).TotalCount;

            model.NumberOfLowStockProducts = _productService.GetLowStockProducts(0, 0, 1).TotalCount +
                                             _productService.GetLowStockProductCombinations(0, 0, 1).TotalCount;

            return PartialView(model);
        }

        /// <summary>
        /// Whether the current request arrived over a loopback connection.
        /// </summary>
        /// <remarks>
        /// TASK 8.3: replaces <c>HttpRequest.Url.IsLoopback</c>. <c>HttpRequest.Url</c> (a
        /// <see cref="System.Uri"/>) has no ASP.NET Core counterpart, and
        /// <c>ConnectionInfo.LocalIpAddress</c> is the accurate source for the question actually
        /// being asked. Returns <c>false</c> when there is no connection information (an in-process
        /// or test request), which is the safer default for the news-feed flag this feeds.
        /// </remarks>
        protected virtual bool IsLoopbackRequest()
        {
            var localIp = HttpContext.Connection.LocalIpAddress;
            return localIp != null && System.Net.IPAddress.IsLoopback(localIp);
        }

        #endregion
    }
}
