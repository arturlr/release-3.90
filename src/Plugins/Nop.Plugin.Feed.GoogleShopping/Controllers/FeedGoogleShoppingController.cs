using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Stores;
using Nop.Core.Plugins;
using Nop.Plugin.Feed.GoogleShopping.Domain;
using Nop.Plugin.Feed.GoogleShopping.Models;
using Nop.Plugin.Feed.GoogleShopping.Services;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Feed.GoogleShopping.Controllers
{
    /// <remarks>
    /// Task 11.2 substitutions — all of them decisions tasks 7.3 (§30), 8.3 (§55) and 10.x already
    /// made and recorded:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c> plus
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> for <see cref="SelectListItem"/>.</item>
    /// <item><c>using System.Web;</c> DELETED — see <see cref="Configure()"/>, where
    /// <c>HttpRuntime.AppDomainAppPath</c> is replaced.</item>
    /// <item><c>[ChildActionOnly]</c> → <c>[NopChildActionOnly]</c> on all three
    /// <c>Configure</c>-named actions (deferral 7.3-4, resolved at 8.3). Without it
    /// <c>GET /FeedGoogleShopping/Configure</c> would serve the bare admin settings panel over
    /// the <c>Default</c> route. <b>The two grid actions are deliberately NOT marked</b>:
    /// <c>GoogleProductList</c> and <c>GoogleProductUpdate</c> are called by URL from the Kendo
    /// grid in <c>Views/Configure.cshtml</c> (through <c>Url.Action</c>), so suppressing their
    /// endpoints would 404 the grid. They were never <c>[ChildActionOnly]</c> in 3.90 either — the
    /// marker follows 3.90's own attribute placement rather than a guess.</item>
    /// <item><c>Json(gridModel)</c> — unchanged; this file never used
    /// <c>JsonRequestBehavior.AllowGet</c>.</item>
    /// <item><c>View("~/Plugins/Feed.GoogleShopping/Views/Configure.cshtml", model)</c> — call
    /// site UNCHANGED, thanks to the project file's <c>Content</c>/<c>Link</c> block.</item>
    /// </list>
    /// Needed no edit: <c>[AdminAuthorize]</c>, <c>[AdminAntiForgery]</c>,
    /// <c>[FormValueRequired]</c>, <c>[HttpPost, ActionName("Configure")]</c>,
    /// <c>ErrorForKendoGridJson</c>, <c>DataSourceRequest</c>/<c>DataSourceResult</c>,
    /// <c>NullJsonResult</c>, <c>SuccessNotification</c>/<c>ErrorNotification</c> — every one
    /// ported in place by tasks 6.2/6.3 with its signature intact.
    /// <para>
    /// <b>THE THREE <c>Configure</c> ACTIONS ARE WHY TASK 11.1 HAD TO FIX THE
    /// <c>Html.Action</c> BRIDGE (runtime deferral 11.x-1), AND THIS PLUGIN IS THE SHARPEST
    /// CASE IN THE SOLUTION.</b> Two of them are <c>[HttpPost]</c> with the SAME action name and
    /// the SAME single parameter, distinguished only by <c>[FormValueRequired("save")]</c> versus
    /// <c>[FormValueRequired("generate")]</c> — i.e. by which submit button was pressed. The
    /// pre-11.1 bridge ignored <c>[HttpPost]</c> and <c>[FormValueRequired]</c> entirely and broke
    /// the tie by "fewest parameters", so pressing either button re-ran the parameterless GET and
    /// nothing happened at all; had the tie fallen the other way, "Save" could have generated the
    /// feed. The bridge now evaluates <c>IActionConstraint</c>s the way MVC 5's
    /// <c>ActionMethodSelector</c> did.
    /// </para>
    /// </remarks>
    [AdminAuthorize]
    public class FeedGoogleShoppingController : BasePluginController
    {
        private readonly IGoogleService _googleService;
        private readonly IProductService _productService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly IStoreService _storeService;
        private readonly GoogleShoppingSettings _googleShoppingSettings;
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;

        public FeedGoogleShoppingController(IGoogleService googleService,
            IProductService productService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            IPluginFinder pluginFinder,
            ILogger logger,
            IWebHelper webHelper,
            IStoreService storeService,
            GoogleShoppingSettings googleShoppingSettings,
            ISettingService settingService,
            IPermissionService permissionService)
        {
            this._googleService = googleService;
            this._productService = productService;
            this._currencyService = currencyService;
            this._localizationService = localizationService;
            this._pluginFinder = pluginFinder;
            this._logger = logger;
            this._webHelper = webHelper;
            this._storeService = storeService;
            this._googleShoppingSettings = googleShoppingSettings;
            this._settingService = settingService;
            this._permissionService = permissionService;
        }

        [NopChildActionOnly]
        public ActionResult Configure()
        {
            var model = new FeedGoogleShoppingModel();
            model.ProductPictureSize = _googleShoppingSettings.ProductPictureSize;
            model.PassShippingInfoWeight = _googleShoppingSettings.PassShippingInfoWeight;
            model.PassShippingInfoDimensions = _googleShoppingSettings.PassShippingInfoDimensions;
            model.PricesConsiderPromotions = _googleShoppingSettings.PricesConsiderPromotions;
            //stores
            model.StoreId = _googleShoppingSettings.StoreId;
            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            foreach (var s in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //currencies
            model.CurrencyId = _googleShoppingSettings.CurrencyId;
            foreach (var c in _currencyService.GetAllCurrencies())
                model.AvailableCurrencies.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //Google categories
            model.DefaultGoogleCategory = _googleShoppingSettings.DefaultGoogleCategory;
            model.AvailableGoogleCategories.Add(new SelectListItem {Text = "Select a category", Value = ""});
            foreach (var gc in _googleService.GetTaxonomyList())
                model.AvailableGoogleCategories.Add(new SelectListItem { Text = gc, Value = gc });

            //file paths
            //TASK 11.2: the probe and the URL both go through GoogleShoppingFeedFile, which is
            //where the two silent 3.90 defects are documented - HttpRuntime.AppDomainAppPath has
            //no net10.0 counterpart, and "content\\files\\exportimport" is one directory name with
            //the wrong casing on a case-sensitive filesystem, so File.Exists returned false
            //forever and the administrator never saw a feed URL. The probe and the WRITE in
            //GoogleShoppingService.GenerateStaticFile now share one expression by construction.
            foreach (var store in _storeService.GetAllStores())
            {
                var localFilePath = GoogleShoppingFeedFile.PhysicalPath(store.Id, _googleShoppingSettings.StaticFileName);
                if (System.IO.File.Exists(localFilePath))
                    model.GeneratedFiles.Add(new FeedGoogleShoppingModel.GeneratedFileModel
                    {
                        StoreName = store.Name,
                        FileUrl = GoogleShoppingFeedFile.Url(_webHelper, store.Id, _googleShoppingSettings.StaticFileName)
                    });
            }

            return View("~/Plugins/Feed.GoogleShopping/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [NopChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(FeedGoogleShoppingModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            //save settings
            _googleShoppingSettings.ProductPictureSize = model.ProductPictureSize;
            _googleShoppingSettings.PassShippingInfoWeight = model.PassShippingInfoWeight;
            _googleShoppingSettings.PassShippingInfoDimensions = model.PassShippingInfoDimensions;
            _googleShoppingSettings.PricesConsiderPromotions = model.PricesConsiderPromotions;
            _googleShoppingSettings.CurrencyId = model.CurrencyId;
            _googleShoppingSettings.StoreId = model.StoreId;
            _googleShoppingSettings.DefaultGoogleCategory = model.DefaultGoogleCategory;
            _settingService.SaveSetting(_googleShoppingSettings);

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            //redisplay the form
            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [NopChildActionOnly]
        [FormValueRequired("generate")]
        public ActionResult GenerateFeed(FeedGoogleShoppingModel model)
        {
            try
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("PromotionFeed.Froogle");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                //plugin
                var plugin = pluginDescriptor.Instance() as GoogleShoppingService;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var stores = new List<Store>();
                var storeById = _storeService.GetStoreById(_googleShoppingSettings.StoreId);
                if (storeById != null)
                    stores.Add(storeById);
                else
                    stores.AddRange(_storeService.GetAllStores());

                foreach (var store in stores)
                    plugin.GenerateStaticFile(store);

                SuccessNotification(_localizationService.GetResource("Plugins.Feed.GoogleShopping.SuccessResult"));
            }
            catch (Exception exc)
            {
                ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Configure();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GoogleProductList(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return ErrorForKendoGridJson("Access denied");

            var products = _productService.SearchProducts(pageIndex: command.Page - 1,
                pageSize: command.PageSize, showHidden: true);
            var productsModel = products
                .Select(x =>
                            {
                                var gModel = new FeedGoogleShoppingModel.GoogleProductModel
                                {
                                    ProductId = x.Id,
                                    ProductName = x.Name

                                };
                                var googleProduct = _googleService.GetByProductId(x.Id);
                                if (googleProduct != null)
                                {
                                    gModel.GoogleCategory = googleProduct.Taxonomy;
                                    gModel.Gender = googleProduct.Gender;
                                    gModel.AgeGroup = googleProduct.AgeGroup;
                                    gModel.Color = googleProduct.Color;
                                    gModel.GoogleSize = googleProduct.Size;
                                    gModel.CustomGoods = googleProduct.CustomGoods;
                                }

                                return gModel;
                            })
                .ToList();

            var gridModel = new DataSourceResult
            {
                Data = productsModel,
                Total = products.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult GoogleProductUpdate(FeedGoogleShoppingModel.GoogleProductModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
                return Content("Access denied");

            var googleProduct = _googleService.GetByProductId(model.ProductId);
            if (googleProduct != null)
            {

                googleProduct.Taxonomy = model.GoogleCategory;
                googleProduct.Gender = model.Gender;
                googleProduct.AgeGroup = model.AgeGroup;
                googleProduct.Color = model.Color;
                googleProduct.Size = model.GoogleSize;
                googleProduct.CustomGoods = model.CustomGoods;
                _googleService.UpdateGoogleProductRecord(googleProduct);
            }
            else
            {
                //insert
                googleProduct = new GoogleProductRecord
                {
                    ProductId = model.ProductId,
                    Taxonomy = model.GoogleCategory,
                    Gender = model.Gender,
                    AgeGroup = model.AgeGroup,
                    Color = model.Color,
                    Size = model.GoogleSize,
                    CustomGoods = model.CustomGoods
                };
                _googleService.InsertGoogleProductRecord(googleProduct);
            }

            return new NullJsonResult();
        }
    }
}
