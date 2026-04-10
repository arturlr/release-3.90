using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Stores;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class StoreController(
    IStoreService storeService,
    ISettingService settingService,
    ILanguageService languageService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult Index() => RedirectToAction("List");

    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> StoreList()
    {
        if (!permissionService.Authorize("ManageStores"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var stores = await storeService.GetAllStoresAsync();

        var gridModel = new DataSourceResult
        {
            Data = stores.Select(s => new StoreGridModel
            {
                Id = s.Id,
                Name = s.Name,
                Url = s.Url,
                Hosts = s.Hosts,
                DisplayOrder = s.DisplayOrder
            }),
            Total = stores.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        var model = new StoreModel();
        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(StoreModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var store = new Store
            {
                Name = model.Name,
                Url = EnsureTrailingSlash(model.Url),
                SslEnabled = model.SslEnabled,
                SecureUrl = model.SecureUrl,
                Hosts = model.Hosts,
                DefaultLanguageId = model.DefaultLanguageId,
                DisplayOrder = model.DisplayOrder,
                CompanyName = model.CompanyName,
                CompanyAddress = model.CompanyAddress,
                CompanyPhoneNumber = model.CompanyPhoneNumber,
                CompanyVat = model.CompanyVat
            };
            await storeService.InsertStoreAsync(store);

            customerActivityService.InsertActivity("AddNewStore", $"Added a new store (ID = {store.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = store.Id });

            return RedirectToAction("List");
        }

        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        var store = await storeService.GetStoreByIdAsync(id);
        if (store is null)
            return RedirectToAction("List");

        var model = MapToModel(store);
        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(StoreModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        var store = await storeService.GetStoreByIdAsync(model.Id);
        if (store is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            store.Name = model.Name;
            store.Url = EnsureTrailingSlash(model.Url);
            store.SslEnabled = model.SslEnabled;
            store.SecureUrl = model.SecureUrl;
            store.Hosts = model.Hosts;
            store.DefaultLanguageId = model.DefaultLanguageId;
            store.DisplayOrder = model.DisplayOrder;
            store.CompanyName = model.CompanyName;
            store.CompanyAddress = model.CompanyAddress;
            store.CompanyPhoneNumber = model.CompanyPhoneNumber;
            store.CompanyVat = model.CompanyVat;
            await storeService.UpdateStoreAsync(store);

            customerActivityService.InsertActivity("EditStore", $"Edited a store (ID = {store.Id})");

            if (continueEditing)
                return RedirectToAction("Edit", new { id = store.Id });

            return RedirectToAction("List");
        }

        await PrepareLanguageDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageStores"))
            return Forbid();

        var store = await storeService.GetStoreByIdAsync(id);
        if (store is null)
            return RedirectToAction("List");

        // Can't delete the last store
        var allStores = await storeService.GetAllStoresAsync();
        if (allStores.Count == 1)
            return RedirectToAction("Edit", new { id = store.Id });

        await storeService.DeleteStoreAsync(store);

        // Clean up per-store settings
        var allSettings = await settingService.GetAllSettingsAsync();
        var settingsToDelete = allSettings.Where(s => s.StoreId == id).ToList();
        if (settingsToDelete.Count > 0)
            await settingService.DeleteSettingsAsync(settingsToDelete);

        // If only one store remains, remove its per-store overrides too
        var remainingStores = await storeService.GetAllStoresAsync();
        if (remainingStores.Count == 1)
        {
            settingsToDelete = allSettings.Where(s => s.StoreId == remainingStores[0].Id).ToList();
            if (settingsToDelete.Count > 0)
                await settingService.DeleteSettingsAsync(settingsToDelete);
        }

        customerActivityService.InsertActivity("DeleteStore", $"Deleted a store (ID = {id})");

        return RedirectToAction("List");
    }

    #region Helpers

    private async Task PrepareLanguageDropdownAsync(StoreModel model)
    {
        model.AvailableLanguages.Add(new SelectListItem { Text = "---", Value = "0" });
        var languages = await languageService.GetAllLanguagesAsync(showHidden: true);
        foreach (var lang in languages)
            model.AvailableLanguages.Add(new SelectListItem { Text = lang.Name, Value = lang.Id.ToString() });
    }

    private static StoreModel MapToModel(Store store) => new()
    {
        Id = store.Id,
        Name = store.Name,
        Url = store.Url,
        SslEnabled = store.SslEnabled,
        SecureUrl = store.SecureUrl,
        Hosts = store.Hosts,
        DefaultLanguageId = store.DefaultLanguageId,
        DisplayOrder = store.DisplayOrder,
        CompanyName = store.CompanyName,
        CompanyAddress = store.CompanyAddress,
        CompanyPhoneNumber = store.CompanyPhoneNumber,
        CompanyVat = store.CompanyVat
    };

    private static string? EnsureTrailingSlash(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        return url.EndsWith('/') ? url : url + "/";
    }

    #endregion
}
