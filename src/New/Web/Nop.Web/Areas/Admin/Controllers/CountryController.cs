using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.ExportImport;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Directory;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CountryController(
    ICountryService countryService,
    IStateProvinceService stateProvinceService,
    IAddressService addressService,
    IExportManager exportManager,
    IImportManager importManager,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Countries

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        return View(new CountryListModel());
    }

    [HttpPost]
    public async Task<JsonResult> CountryList(CountryListModel model)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var countries = await countryService.GetAllCountriesAsync(showHidden: true);

        if (!string.IsNullOrWhiteSpace(model.SearchCountryName))
            countries = countries.Where(c => c.Name != null && c.Name.Contains(model.SearchCountryName, StringComparison.OrdinalIgnoreCase)).ToList();

        var gridModel = new DataSourceResult
        {
            Data = countries.Select(c => new CountryGridModel
            {
                Id = c.Id,
                Name = c.Name,
                AllowsBilling = c.AllowsBilling,
                AllowsShipping = c.AllowsShipping,
                TwoLetterIsoCode = c.TwoLetterIsoCode,
                ThreeLetterIsoCode = c.ThreeLetterIsoCode,
                NumericIsoCode = c.NumericIsoCode,
                SubjectToVat = c.SubjectToVat,
                Published = c.Published,
                DisplayOrder = c.DisplayOrder
            }),
            Total = countries.Count
        };

        return Json(gridModel);
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        var model = new CountryModel { Published = true, AllowsBilling = true, AllowsShipping = true };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CountryModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var country = new Country
            {
                Name = model.Name,
                AllowsBilling = model.AllowsBilling,
                AllowsShipping = model.AllowsShipping,
                TwoLetterIsoCode = model.TwoLetterIsoCode,
                ThreeLetterIsoCode = model.ThreeLetterIsoCode,
                NumericIsoCode = model.NumericIsoCode,
                SubjectToVat = model.SubjectToVat,
                Published = model.Published,
                DisplayOrder = model.DisplayOrder,
                LimitedToStores = model.LimitedToStores
            };
            await countryService.InsertCountryAsync(country);

            customerActivityService.InsertActivity("AddNewCountry", $"Added a new country (ID = {country.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = country.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        var country = await countryService.GetCountryByIdAsync(id);
        if (country is null)
            return RedirectToAction("List");

        return View(MapCountryToModel(country));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CountryModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        var country = await countryService.GetCountryByIdAsync(model.Id);
        if (country is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            country.Name = model.Name;
            country.AllowsBilling = model.AllowsBilling;
            country.AllowsShipping = model.AllowsShipping;
            country.TwoLetterIsoCode = model.TwoLetterIsoCode;
            country.ThreeLetterIsoCode = model.ThreeLetterIsoCode;
            country.NumericIsoCode = model.NumericIsoCode;
            country.SubjectToVat = model.SubjectToVat;
            country.Published = model.Published;
            country.DisplayOrder = model.DisplayOrder;
            country.LimitedToStores = model.LimitedToStores;
            await countryService.UpdateCountryAsync(country);

            customerActivityService.InsertActivity("EditCountry", $"Edited a country (ID = {country.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = country.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        var country = await countryService.GetCountryByIdAsync(id);
        if (country is null)
            return RedirectToAction("List");

        if (await addressService.GetAddressTotalByCountryIdAsync(country.Id) > 0)
            return RedirectToAction("Edit", new { id = country.Id });

        await countryService.DeleteCountryAsync(country);

        customerActivityService.InsertActivity("DeleteCountry", $"Deleted a country (ID = {id})");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<JsonResult> DeleteSelected(IEnumerable<int> selectedIds)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new { result = false });

        var countries = await countryService.GetCountriesByIdsAsync(selectedIds.ToArray());
        foreach (var country in countries)
        {
            if (await addressService.GetAddressTotalByCountryIdAsync(country.Id) > 0)
                continue;

            await countryService.DeleteCountryAsync(country);
        }

        return Json(new { result = true });
    }

    #endregion

    #region States / provinces

    [HttpPost]
    public async Task<JsonResult> StateProvinceList(int countryId)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var states = await stateProvinceService.GetStateProvincesByCountryIdAsync(countryId, showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = states.Select(s => new StateProvinceModel
            {
                Id = s.Id,
                CountryId = s.CountryId,
                Name = s.Name,
                Abbreviation = s.Abbreviation,
                Published = s.Published,
                DisplayOrder = s.DisplayOrder
            }),
            Total = states.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> StateProvinceAdd(int countryId, StateProvinceModel model)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        await stateProvinceService.InsertStateProvinceAsync(new StateProvince
        {
            CountryId = countryId,
            Name = model.Name ?? string.Empty,
            Abbreviation = model.Abbreviation,
            Published = model.Published,
            DisplayOrder = model.DisplayOrder
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> StateProvinceUpdate(StateProvinceModel model)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var state = await stateProvinceService.GetStateProvinceByIdAsync(model.Id);
        if (state is null)
            return Json(new DataSourceResult { Errors = "State not found" });

        state.Name = model.Name ?? string.Empty;
        state.Abbreviation = model.Abbreviation;
        state.Published = model.Published;
        state.DisplayOrder = model.DisplayOrder;
        await stateProvinceService.UpdateStateProvinceAsync(state);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> StateProvinceDelete(int id)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var state = await stateProvinceService.GetStateProvinceByIdAsync(id);
        if (state is null)
            return Json(new DataSourceResult { Errors = "State not found" });

        if (await addressService.GetAddressTotalByStateProvinceIdAsync(state.Id) > 0)
            return Json(new DataSourceResult { Errors = "The state can't be deleted. It has associated addresses." });

        await stateProvinceService.DeleteStateProvinceAsync(state);

        return Json(new { });
    }

    #endregion

    #region Export / import

    public async Task<IActionResult> ExportCsv()
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        var states = await stateProvinceService.GetStateProvincesAsync(showHidden: true);
        var csv = exportManager.ExportStatesToTxt(states);
        var fileName = $"states_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{CommonHelper.GenerateRandomDigitCode(4)}.txt";

        return File(Encoding.UTF8.GetBytes(csv), MimeTypes.TextCsv, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportCsv(IFormFile? importcsvfile)
    {
        if (!permissionService.Authorize("ManageCountries"))
            return Forbid();

        if (importcsvfile is { Length: > 0 })
        {
            await using var stream = importcsvfile.OpenReadStream();
            await importManager.ImportStatesFromTxtAsync(stream);
        }

        return RedirectToAction("List");
    }

    #endregion

    #region Helpers

    private static CountryModel MapCountryToModel(Country c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        AllowsBilling = c.AllowsBilling,
        AllowsShipping = c.AllowsShipping,
        TwoLetterIsoCode = c.TwoLetterIsoCode,
        ThreeLetterIsoCode = c.ThreeLetterIsoCode,
        NumericIsoCode = c.NumericIsoCode,
        SubjectToVat = c.SubjectToVat,
        Published = c.Published,
        DisplayOrder = c.DisplayOrder,
        LimitedToStores = c.LimitedToStores
    };

    #endregion
}
