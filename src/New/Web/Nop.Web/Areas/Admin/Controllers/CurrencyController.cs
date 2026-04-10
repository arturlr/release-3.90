using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Directory;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Directory;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class CurrencyController(
    ICurrencyService currencyService,
    CurrencySettings currencySettings,
    ISettingService settingService,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        // Live rates and exchange rate provider selection depend on plugin system [2.10] — deferred
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> CurrencyList()
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var currencies = await currencyService.GetAllCurrenciesAsync(showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = currencies.Select(c => new CurrencyGridModel
            {
                Id = c.Id,
                Name = c.Name,
                CurrencyCode = c.CurrencyCode,
                Rate = c.Rate,
                IsPrimaryExchangeRateCurrency = c.Id == currencySettings.PrimaryExchangeRateCurrencyId,
                IsPrimaryStoreCurrency = c.Id == currencySettings.PrimaryStoreCurrencyId,
                Published = c.Published,
                DisplayOrder = c.DisplayOrder
            }),
            Total = currencies.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<JsonResult> ApplyRate(string currencyCode, decimal rate)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Json(new { result = false });

        var currency = await currencyService.GetCurrencyByCodeAsync(currencyCode);
        if (currency is not null)
        {
            currency.Rate = rate;
            currency.UpdatedOnUtc = DateTime.UtcNow;
            await currencyService.UpdateCurrencyAsync(currency);
        }

        return Json(new { result = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsPrimaryExchangeRateCurrency(int id)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        currencySettings.PrimaryExchangeRateCurrencyId = id;
        await settingService.SaveSettingAsync(currencySettings);

        return Json(new { result = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsPrimaryStoreCurrency(int id)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        currencySettings.PrimaryStoreCurrencyId = id;
        await settingService.SaveSettingAsync(currencySettings);

        return Json(new { result = true });
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        var model = new CurrencyModel { Published = true, Rate = 1 };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CurrencyModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var currency = new Currency
            {
                Name = model.Name,
                CurrencyCode = model.CurrencyCode,
                Rate = model.Rate,
                DisplayLocale = model.DisplayLocale,
                CustomFormatting = model.CustomFormatting,
                Published = model.Published,
                DisplayOrder = model.DisplayOrder,
                RoundingTypeId = model.RoundingTypeId,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            await currencyService.InsertCurrencyAsync(currency);

            customerActivityService.InsertActivity("AddNewCurrency", $"Added a new currency (ID = {currency.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = currency.Id });

            return RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        var currency = await currencyService.GetCurrencyByIdAsync(id);
        if (currency is null)
            return RedirectToAction("List");

        var model = new CurrencyModel
        {
            Id = currency.Id,
            Name = currency.Name,
            CurrencyCode = currency.CurrencyCode,
            Rate = currency.Rate,
            DisplayLocale = currency.DisplayLocale,
            CustomFormatting = currency.CustomFormatting,
            Published = currency.Published,
            DisplayOrder = currency.DisplayOrder,
            RoundingTypeId = currency.RoundingTypeId,
            CreatedOn = dateTimeHelper.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CurrencyModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        var currency = await currencyService.GetCurrencyByIdAsync(model.Id);
        if (currency is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            // Prevent unpublishing the last published currency
            var allCurrencies = await currencyService.GetAllCurrenciesAsync();
            if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id && !model.Published)
                return RedirectToAction("Edit", new { id = currency.Id });

            currency.Name = model.Name;
            currency.CurrencyCode = model.CurrencyCode;
            currency.Rate = model.Rate;
            currency.DisplayLocale = model.DisplayLocale;
            currency.CustomFormatting = model.CustomFormatting;
            currency.Published = model.Published;
            currency.DisplayOrder = model.DisplayOrder;
            currency.RoundingTypeId = model.RoundingTypeId;
            currency.UpdatedOnUtc = DateTime.UtcNow;
            await currencyService.UpdateCurrencyAsync(currency);

            customerActivityService.InsertActivity("EditCurrency", $"Edited a currency (ID = {currency.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = currency.Id });

            return RedirectToAction("List");
        }

        model.CreatedOn = dateTimeHelper.ConvertToUserTime(currency.CreatedOnUtc, DateTimeKind.Utc);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCurrencies"))
            return Forbid();

        var currency = await currencyService.GetCurrencyByIdAsync(id);
        if (currency is null)
            return RedirectToAction("List");

        // Can't delete primary currencies
        if (currency.Id == currencySettings.PrimaryStoreCurrencyId ||
            currency.Id == currencySettings.PrimaryExchangeRateCurrencyId)
            return RedirectToAction("Edit", new { id = currency.Id });

        // Ensure at least one published currency remains
        var allCurrencies = await currencyService.GetAllCurrenciesAsync();
        if (allCurrencies.Count == 1 && allCurrencies[0].Id == currency.Id)
            return RedirectToAction("Edit", new { id = currency.Id });

        await currencyService.DeleteCurrencyAsync(currency);

        customerActivityService.InsertActivity("DeleteCurrency", $"Deleted a currency (ID = {id})");

        return RedirectToAction("List");
    }
}
