using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Localization;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class LanguageController(
    ILanguageService languageService,
    ILocalizationService localizationService,
    ICurrencyService currencyService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    #region Languages

    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> LanguageList()
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);

        var gridModel = new DataSourceResult
        {
            Data = languages.Select(l => new LanguageGridModel
            {
                Id = l.Id,
                Name = l.Name,
                LanguageCulture = l.LanguageCulture,
                UniqueSeoCode = l.UniqueSeoCode,
                Published = l.Published,
                DisplayOrder = l.DisplayOrder
            }),
            Total = languages.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var model = new LanguageModel { Published = true };
        await PrepareCurrencyDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(LanguageModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var language = new Language
            {
                Name = model.Name ?? string.Empty,
                LanguageCulture = model.LanguageCulture,
                UniqueSeoCode = model.UniqueSeoCode,
                FlagImageFileName = model.FlagImageFileName,
                Rtl = model.Rtl,
                DefaultCurrencyId = model.DefaultCurrencyId,
                Published = model.Published,
                DisplayOrder = model.DisplayOrder
            };
            await languageService.InsertLanguageAsync(language);

            customerActivityService.InsertActivity("AddNewLanguage", $"Added a new language (ID = {language.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = language.Id });

            return RedirectToAction("List");
        }

        await PrepareCurrencyDropdownAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var language = await languageService.GetLanguageByIdAsync(id);
        if (language is null)
            return RedirectToAction("List");

        var model = new LanguageModel
        {
            Id = language.Id,
            Name = language.Name,
            LanguageCulture = language.LanguageCulture,
            UniqueSeoCode = language.UniqueSeoCode,
            FlagImageFileName = language.FlagImageFileName,
            Rtl = language.Rtl,
            DefaultCurrencyId = language.DefaultCurrencyId,
            Published = language.Published,
            DisplayOrder = language.DisplayOrder
        };

        await PrepareCurrencyDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(LanguageModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var language = await languageService.GetLanguageByIdAsync(model.Id);
        if (language is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            // ensure at least one published language
            var allLanguages = await languageService.GetAllLanguagesAsync();
            if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id && !model.Published)
            {
                ErrorNotification("At least one published language is required.");
                return RedirectToAction("Edit", new { id = language.Id });
            }

            language.Name = model.Name ?? string.Empty;
            language.LanguageCulture = model.LanguageCulture;
            language.UniqueSeoCode = model.UniqueSeoCode;
            language.FlagImageFileName = model.FlagImageFileName;
            language.Rtl = model.Rtl;
            language.DefaultCurrencyId = model.DefaultCurrencyId;
            language.Published = model.Published;
            language.DisplayOrder = model.DisplayOrder;
            await languageService.UpdateLanguageAsync(language);

            customerActivityService.InsertActivity("EditLanguage", $"Edited a language (ID = {language.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = language.Id });

            return RedirectToAction("List");
        }

        await PrepareCurrencyDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var language = await languageService.GetLanguageByIdAsync(id);
        if (language is null)
            return RedirectToAction("List");

        // ensure at least one published language
        var allLanguages = await languageService.GetAllLanguagesAsync();
        if (allLanguages.Count == 1 && allLanguages[0].Id == language.Id)
        {
            ErrorNotification("You cannot delete the only language.");
            return RedirectToAction("Edit", new { id = language.Id });
        }

        await languageService.DeleteLanguageAsync(language);

        customerActivityService.InsertActivity("DeleteLanguage", $"Deleted a language (ID = {id})");

        return RedirectToAction("List");
    }

    #endregion

    #region Resources

    [HttpPost]
    public async Task<JsonResult> Resources(int languageId, DataSourceRequest command, string? searchResourceName, string? searchResourceValue)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var allResources = await localizationService.GetAllResourceValuesAsync(languageId);

        var query = allResources
            .OrderBy(x => x.Key)
            .AsEnumerable();

        if (!string.IsNullOrEmpty(searchResourceName))
            query = query.Where(l => l.Key.Contains(searchResourceName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(searchResourceValue))
            query = query.Where(l => l.Value.Value.Contains(searchResourceValue, StringComparison.OrdinalIgnoreCase));

        var resources = query.Select(x => new LanguageResourceModel
        {
            LanguageId = languageId,
            Id = x.Value.Key,
            Name = x.Key,
            Value = x.Value.Value
        }).ToList();

        var total = resources.Count;
        var pageIndex = command.Page > 0 ? command.Page - 1 : 0;
        var pageSize = command.PageSize > 0 ? command.PageSize : 20;
        var pagedData = resources.Skip(pageIndex * pageSize).Take(pageSize);

        return Json(new DataSourceResult { Data = pagedData, Total = total });
    }

    [HttpPost]
    public async Task<JsonResult> ResourceUpdate(LanguageResourceModel model)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        model.Name = model.Name?.Trim();
        model.Value = model.Value?.Trim();

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var resource = await localizationService.GetLocaleStringResourceByIdAsync(model.Id);
        if (resource is null)
            return Json(new DataSourceResult { Errors = "Resource not found" });

        // check for duplicate name
        if (!string.Equals(resource.ResourceName, model.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await localizationService.GetLocaleStringResourceByNameAsync(model.Name!, model.LanguageId, false);
            if (existing is not null && existing.Id != resource.Id)
                return Json(new DataSourceResult { Errors = $"Resource name '{model.Name}' already exists." });
        }

        resource.ResourceName = model.Name!;
        resource.ResourceValue = model.Value ?? string.Empty;
        await localizationService.UpdateLocaleStringResourceAsync(resource);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ResourceAdd(int languageId, LanguageResourceModel model)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        model.Name = model.Name?.Trim();
        model.Value = model.Value?.Trim();

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

        var existing = await localizationService.GetLocaleStringResourceByNameAsync(model.Name!, languageId, false);
        if (existing is not null)
            return Json(new DataSourceResult { Errors = $"Resource name '{model.Name}' already exists." });

        await localizationService.InsertLocaleStringResourceAsync(new LocaleStringResource
        {
            LanguageId = languageId,
            ResourceName = model.Name!,
            ResourceValue = model.Value ?? string.Empty
        });

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> ResourceDelete(int id)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var resource = await localizationService.GetLocaleStringResourceByIdAsync(id);
        if (resource is null)
            return Json(new DataSourceResult { Errors = "Resource not found" });

        await localizationService.DeleteLocaleStringResourceAsync(resource);

        return Json(new { });
    }

    #endregion

    #region Export / Import

    public async Task<IActionResult> ExportXml(int id)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var language = await languageService.GetLanguageByIdAsync(id);
        if (language is null)
            return RedirectToAction("List");

        var xml = await localizationService.ExportResourcesToXmlAsync(language);
        var bytes = System.Text.Encoding.UTF8.GetBytes(xml);
        return File(bytes, "application/xml", "language_pack.xml");
    }

    [HttpPost]
    public async Task<IActionResult> ImportXml(int id, IFormFile? importxmlfile)
    {
        if (!permissionService.Authorize("ManageLanguages"))
            return Forbid();

        var language = await languageService.GetLanguageByIdAsync(id);
        if (language is null)
            return RedirectToAction("List");

        if (importxmlfile is not null && importxmlfile.Length > 0)
        {
            using var reader = new StreamReader(importxmlfile.OpenReadStream(), System.Text.Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            await localizationService.ImportResourcesFromXmlAsync(language, content);
        }

        return RedirectToAction("Edit", new { id = language.Id });
    }

    #endregion

    #region Helpers

    private async Task PrepareCurrencyDropdownAsync(LanguageModel model)
    {
        model.AvailableCurrencies.Add(new SelectListItem { Text = "---", Value = "0" });
        foreach (var currency in await currencyService.GetAllCurrenciesAsync(showHidden: true))
            model.AvailableCurrencies.Add(new SelectListItem { Text = currency.Name, Value = currency.Id.ToString() });
    }

    #endregion
}
