using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Pickup.PickupInStore.Domain;
using Nop.Plugin.Pickup.PickupInStore.Models;
using Nop.Plugin.Pickup.PickupInStore.Services;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;

namespace Nop.Plugin.Pickup.PickupInStore.Controllers
{
    /// <remarks>
    /// Task 13.1 substitutions — all decisions tasks 7.3 (§30), 8.3 (§55) and groups 10/11 already
    /// made and recorded:
    /// <list type="bullet">
    /// <item><c>using System.Web.Mvc;</c> → <c>using Microsoft.AspNetCore.Mvc;</c> plus
    /// <c>Microsoft.AspNetCore.Mvc.Rendering</c> for <see cref="SelectListItem"/>.</item>
    /// <item><c>[ChildActionOnly]</c> → <c>[NopChildActionOnly]</c> on <see cref="Configure()"/>
    /// only — it is the action <c>PickupInStoreProvider.GetConfigurationRoute</c> names and that the
    /// admin plugin list renders as a child action. Without the marker,
    /// <c>GET /PickupInStore/Configure</c> would serve the bare admin panel over the Default route.
    /// <b>The other actions are deliberately NOT marked:</b> <c>List</c> and <c>Delete</c> are called
    /// by URL from the Kendo grid in <c>Configure.cshtml</c>, and <c>Create</c>/<c>Edit</c> are
    /// reached by the plugin's own named routes (RouteProvider) from the grid's popup buttons —
    /// suppressing any of their endpoints would break the page. None of them were
    /// <c>[ChildActionOnly]</c> in 3.90 either.</item>
    /// <item><c>View("~/Plugins/Pickup.PickupInStore/Views/Xyz.cshtml", model)</c> — call sites
    /// UNCHANGED, thanks to the project file's <c>Content</c>/<c>Link</c> block.</item>
    /// </list>
    /// Needed no edit: <c>[AdminAuthorize]</c>, <c>[AdminAntiForgery]</c>, <c>ErrorForKendoGridJson</c>,
    /// <c>DataSourceRequest</c>/<c>DataSourceResult</c>, <c>NullJsonResult</c>, <c>Content(...)</c>,
    /// <c>RedirectToAction(...)</c>, <c>Json(...)</c> (this file never used
    /// <c>JsonRequestBehavior.AllowGet</c>) — each ported in place by tasks 6.2/6.3 with its
    /// signature intact.
    /// </remarks>
    [AdminAuthorize]
    public class PickupInStoreController : BasePluginController
    {
        #region Fields

        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStorePickupPointService _storePickupPointService;
        private readonly IStoreService _storeService;

        #endregion

        #region Ctor

        public PickupInStoreController(IAddressService addressService,
            ICountryService countryService,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            IStateProvinceService stateProvinceService,
            IStorePickupPointService storePickupPointService,
            IStoreService storeService)
        {
            this._addressService = addressService;
            this._countryService = countryService;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._stateProvinceService = stateProvinceService;
            this._storePickupPointService = storePickupPointService;
            this._storeService = storeService;
        }

        #endregion

        #region Methods

        [NopChildActionOnly]
        public ActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            return View("~/Plugins/Pickup.PickupInStore/Views/Configure.cshtml");
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult List(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return ErrorForKendoGridJson("Access denied");

            var pickupPoints = _storePickupPointService.GetAllStorePickupPoints();
            var model = pickupPoints.Select(x =>
            {
                var store = _storeService.GetStoreById(x.StoreId);
                return new StorePickupPointModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    OpeningHours = x.OpeningHours,
                    PickupFee = x.PickupFee,
                    StoreName = store != null ? store.Name
                        : x.StoreId == 0 ? _localizationService.GetResource("Admin.Configuration.Settings.StoreScope.AllStores") : string.Empty
                };
            }).ToList();

            return Json(new DataSourceResult
            {
                Data = model,
                Total = pickupPoints.TotalCount
            });
        }

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var model = new StorePickupPointModel();

            model.Address.AvailableCountries.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
            foreach (var country in _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = country.Name, Value = country.Id.ToString() });

            var states = !model.Address.CountryId.HasValue ? new List<StateProvince>()
                : _stateProvinceService.GetStateProvincesByCountryId(model.Address.CountryId.Value, showHidden: true);
            if (states.Any())
            {
                model.Address.AvailableStates.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectState"), Value = "0" });
                foreach (var state in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = state.Name, Value = state.Id.ToString() });
            }
            else
                model.Address.AvailableStates.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });

            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Configuration.Settings.StoreScope.AllStores"), Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

            return View("~/Plugins/Pickup.PickupInStore/Views/Create.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Create(string btnId, string formId, StorePickupPointModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var address = new Address
            {
                Address1 = model.Address.Address1,
                City = model.Address.City,
                CountryId = model.Address.CountryId,
                StateProvinceId = model.Address.StateProvinceId,
                ZipPostalCode = model.Address.ZipPostalCode,
                CreatedOnUtc = DateTime.UtcNow
            };
            _addressService.InsertAddress(address);

            var pickupPoint = new StorePickupPoint
            {
                Name = model.Name,
                Description = model.Description,
                AddressId = address.Id,
                OpeningHours = model.OpeningHours,
                PickupFee = model.PickupFee,
                StoreId = model.StoreId
            };
            _storePickupPointService.InsertStorePickupPoint(pickupPoint);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Pickup.PickupInStore/Views/Create.cshtml", model);
        }

        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var pickupPoint = _storePickupPointService.GetStorePickupPointById(id);
            if (pickupPoint == null)
                return RedirectToAction("Configure");

            var model = new StorePickupPointModel
            {
                Id = pickupPoint.Id,
                Name = pickupPoint.Name,
                Description = pickupPoint.Description,
                OpeningHours = pickupPoint.OpeningHours,
                PickupFee = pickupPoint.PickupFee,
                StoreId = pickupPoint.StoreId
            };

            var address = _addressService.GetAddressById(pickupPoint.AddressId);
            if (address != null)
            {
                model.Address = new AddressModel
                {
                    Address1 = address.Address1,
                    City = address.City,
                    CountryId = address.CountryId,
                    StateProvinceId = address.StateProvinceId,
                    ZipPostalCode = address.ZipPostalCode
                };
            }

            model.Address.AvailableCountries.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectCountry"), Value = "0" });
            foreach (var country in _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = country.Name, Value = country.Id.ToString(), Selected = (address != null && country.Id == address.CountryId) });

            var states = !model.Address.CountryId.HasValue ? new List<StateProvince>()
                : _stateProvinceService.GetStateProvincesByCountryId(model.Address.CountryId.Value, showHidden: true);
            if (states.Any())
            {
                model.Address.AvailableStates.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.SelectState"), Value = "0" });
                foreach (var state in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = state.Name, Value = state.Id.ToString(), Selected = (address != null && state.Id == address.StateProvinceId) });
            }
            else
                model.Address.AvailableStates.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Address.OtherNonUS"), Value = "0" });

            model.AvailableStores.Add(new SelectListItem { Text = _localizationService.GetResource("Admin.Configuration.Settings.StoreScope.AllStores"), Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString(), Selected = store.Id == model.StoreId });

            return View("~/Plugins/Pickup.PickupInStore/Views/Edit.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Edit(string btnId, string formId, StorePickupPointModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var pickupPoint = _storePickupPointService.GetStorePickupPointById(model.Id);
            if (pickupPoint == null)
                return RedirectToAction("Configure");

            var address = _addressService.GetAddressById(pickupPoint.AddressId) ?? new Address { CreatedOnUtc = DateTime.UtcNow };
            address.Address1 = model.Address.Address1;
            address.City = model.Address.City;
            address.CountryId = model.Address.CountryId;
            address.StateProvinceId = model.Address.StateProvinceId;
            address.ZipPostalCode = model.Address.ZipPostalCode;
            if (address.Id > 0)
                _addressService.UpdateAddress(address);
            else
                _addressService.InsertAddress(address);

            pickupPoint.Name = model.Name;
            pickupPoint.Description = model.Description;
            pickupPoint.AddressId = address.Id;
            pickupPoint.OpeningHours = model.OpeningHours;
            pickupPoint.PickupFee = model.PickupFee;
            pickupPoint.StoreId = model.StoreId;
            _storePickupPointService.UpdateStorePickupPoint(pickupPoint);

            ViewBag.RefreshPage = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View("~/Plugins/Pickup.PickupInStore/Views/Edit.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return Content("Access denied");

            var pickupPoint = _storePickupPointService.GetStorePickupPointById(id);
            if (pickupPoint == null)
                return RedirectToAction("Configure");

            var address = _addressService.GetAddressById(pickupPoint.AddressId);
            if (address != null)
                _addressService.DeleteAddress(address);
            _storePickupPointService.DeleteStorePickupPoint(pickupPoint);

            return new NullJsonResult();
        }

        #endregion
    }
}
