using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Models;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Controllers
{
    /// <remarks>
    /// Task 15.1 substitutions — all of them decisions tasks 7.3 (§30), 8.3 (§55) and 10.x/11.x
    /// already made and recorded:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c> plus
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> for <see cref="SelectListItem"/>.</item>
    /// <item><c>using System.Web.Routing;</c> DELETED — see <see cref="OnActionExecuting"/>, where
    /// the <c>Initialize(RequestContext)</c> override it existed for is replaced.</item>
    /// <item><c>[ChildActionOnly]</c> on <c>Configure</c> → <c>[NopChildActionOnly]</c>
    /// (deferral 7.3-4, resolved at 8.3). Without it <c>GET /FixedOrByCountryStateZip/Configure</c>
    /// would serve the bare admin panel over the <c>Default</c> route. The grid/save actions are
    /// deliberately NOT marked: they are called by URL from the Kendo grids and the AJAX buttons
    /// in <c>Views/_FixedRate.cshtml</c> / <c>Views/_CountryStateZip.cshtml</c>, so suppressing
    /// their endpoints would break the page. They were never <c>[ChildActionOnly]</c> in 3.90
    /// either.</item>
    /// <item><c>Json(new { Result = true }, JsonRequestBehavior.AllowGet)</c> → <c>Json(new {
    /// Result = true })</c>. <c>JsonRequestBehavior</c> does not exist in ASP.NET Core (there is
    /// no JSON-hijacking guard and therefore no opt-out); this call site explicitly opted out of
    /// <c>DenyGet</c>, so the ported behaviour is what 3.90 asked for (§55.5).</item>
    /// <item><c>View("~/Plugins/Tax.FixedOrByCountryStateZip/Views/Configure.cshtml", model)</c> —
    /// call site UNCHANGED, thanks to the project file's <c>Content</c>/<c>Link</c> block.</item>
    /// </list>
    /// Needed no edit: <c>[AdminAuthorize]</c>, <c>[AdminAntiForgery]</c>,
    /// <c>ErrorForKendoGridJson</c>, <c>DataSourceRequest</c>/<c>DataSourceResult</c>,
    /// <c>NullJsonResult</c>, <c>[NonAction]</c>, <c>Content(...)</c> — every one ported in place
    /// by tasks 6.2/6.3 with its signature intact.
    /// <para>
    /// <b>THE TELERIK-CULTURE HACK MOVES FROM <c>Initialize</c> TO <c>OnActionExecuting</c>.</b>
    /// 3.90 overrode <c>System.Web.Mvc.Controller.Initialize(RequestContext)</c> to force
    /// <c>en-US</c> before the Kendo grid parsed decimals. ASP.NET Core's
    /// <c>Microsoft.AspNetCore.Mvc.Controller</c> has no <c>Initialize</c> seam; the equivalent —
    /// the same one <c>Nop.Admin</c>'s <c>BaseAdminController</c> uses (task 8.3) — is
    /// <see cref="OnActionExecuting"/>, which the base controller already implements as
    /// <see cref="IActionFilter"/> so no registration is needed. <c>CommonHelper.SetTelerikCulture</c>
    /// survived the migration in <c>Nop.Core</c> unchanged.
    /// </para>
    /// </remarks>
    [AdminAuthorize]
    public class FixedOrByCountryStateZipController : BasePluginController
    {
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryStateZipService _taxRateService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly FixedOrByCountryStateZipTaxSettings _countryStateZipSettings;

        public FixedOrByCountryStateZipController(ITaxCategoryService taxCategoryService,
            ICountryService countryService, 
            IStateProvinceService stateProvinceService,
            ICountryStateZipService taxRateService,
            IPermissionService permissionService,
            IStoreService storeService,
            ISettingService settingService,
            FixedOrByCountryStateZipTaxSettings countryStateZipSettings)
        {
            this._taxCategoryService = taxCategoryService;
            this._countryService = countryService;
            this._stateProvinceService = stateProvinceService;
            this._taxRateService = taxRateService;
            this._permissionService = permissionService;
            this._storeService = storeService;
            this._settingService = settingService;
            this._countryStateZipSettings = countryStateZipSettings;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //little hack here
            //always set culture to 'en-US' (Telerik/Kendo UI has a bug related to editing decimal
            //values in other cultures). 3.90 did this in an Initialize(RequestContext) override,
            //which ASP.NET Core's Controller does not have; OnActionExecuting is the equivalent
            //seam (same one Nop.Admin's BaseAdminController uses).
            CommonHelper.SetTelerikCulture();

            base.OnActionExecuting(context);
        }

        [NopChildActionOnly]
        public ActionResult Configure()
        {
            var taxCategories = _taxCategoryService.GetAllTaxCategories();
            if (!taxCategories.Any())
                return Content("No tax categories can be loaded");

            var model = new ConfigurationModel { CountryStateZipEnabled = _countryStateZipSettings.CountryStateZipEnabled };
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            var stores = _storeService.GetAllStores();
            foreach (var s in stores)
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //tax categories
            foreach (var tc in taxCategories)
                model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString() });
            //countries
            var countries = _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            var defaultCountry = countries.FirstOrDefault();
            if (defaultCountry != null)
            {
                var states = _stateProvinceService.GetStateProvincesByCountryId(defaultCountry.Id);
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            }

            return View("~/Plugins/Tax.FixedOrByCountryStateZip/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public ActionResult SaveMode(bool value)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            //save settings
            _countryStateZipSettings.CountryStateZipEnabled = value;
            _settingService.SaveSetting(_countryStateZipSettings);

            //task 15.1: JsonRequestBehavior.AllowGet dropped - see the remarks on this class
            return Json(new
            {
                Result = true
            });
        }

        #region Fixed tax

        [NonAction]
        protected decimal GetFixedTaxRateValue(int taxCategoryId)
        {
            var rate = _settingService.GetSettingByKey<decimal>(string.Format("Tax.TaxProvider.FixedOrByCountryStateZip.TaxCategoryId{0}", taxCategoryId));
            return rate;
        }

        [HttpPost]
        public ActionResult FixedRatesList(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return ErrorForKendoGridJson("Access denied");

            var taxRateModels = _taxCategoryService.GetAllTaxCategories().Select(taxCategory => new FixedTaxRateModel
            {
                TaxCategoryId = taxCategory.Id,
                TaxCategoryName = taxCategory.Name,
                Rate = GetFixedTaxRateValue(taxCategory.Id)
            }).ToList();

            var gridModel = new DataSourceResult
            {
                Data = taxRateModels,
                Total = taxRateModels.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public ActionResult FixedRateUpdate(FixedTaxRateModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxCategoryId = model.TaxCategoryId;
            var rate = model.Rate;

            _settingService.SetSetting(string.Format("Tax.TaxProvider.FixedOrByCountryStateZip.TaxCategoryId{0}", taxCategoryId), rate);

            return new NullJsonResult();
        }
        
        #endregion

        #region Tax by country/state/zip

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult RatesByCountryStateZipList(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return ErrorForKendoGridJson("Access denied");

            var records = _taxRateService.GetAllTaxRates(command.Page - 1, command.PageSize);
            var taxRatesModel = records
                .Select(x =>
                {
                    var m = new CountryStateZipModel
                    {
                        Id = x.Id,
                        StoreId = x.StoreId,
                        TaxCategoryId = x.TaxCategoryId,
                        CountryId = x.CountryId,
                        StateProvinceId = x.StateProvinceId,
                        Zip = x.Zip,
                        Percentage = x.Percentage,
                    };
                    //store
                    var store = _storeService.GetStoreById(x.StoreId);
                    m.StoreName = store != null ? store.Name : "*";
                    //tax category
                    var tc = _taxCategoryService.GetTaxCategoryById(x.TaxCategoryId);
                    m.TaxCategoryName = tc != null ? tc.Name : "";
                    //country
                    var c = _countryService.GetCountryById(x.CountryId);
                    m.CountryName = c != null ? c.Name : "Unavailable";
                    //state
                    var s = _stateProvinceService.GetStateProvinceById(x.StateProvinceId);
                    m.StateProvinceName = s != null ? s.Name : "*";
                    //zip
                    m.Zip = !string.IsNullOrEmpty(x.Zip) ? x.Zip : "*";
                    return m;
                }).ToList();

            var gridModel = new DataSourceResult
            {
                Data = taxRatesModel,
                Total = records.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult AddRateByCountryStateZip(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = new TaxRate
            {
                StoreId = model.AddStoreId,
                TaxCategoryId = model.AddTaxCategoryId,
                CountryId = model.AddCountryId,
                StateProvinceId = model.AddStateProvinceId,
                Zip = model.AddZip,
                Percentage = model.AddPercentage
            };
            _taxRateService.InsertTaxRate(taxRate);

            return Json(new { Result = true });
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult UpdateRateByCountryStateZip(CountryStateZipModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = _taxRateService.GetTaxRateById(model.Id);
            taxRate.Zip = model.Zip == "*" ? null : model.Zip;
            taxRate.Percentage = model.Percentage;
            _taxRateService.UpdateTaxRate(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult DeleteRateByCountryStateZip(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = _taxRateService.GetTaxRateById(id);
            if (taxRate != null)
                _taxRateService.DeleteTaxRate(taxRate);

            return new NullJsonResult();
        }
        
        #endregion
    }
}
