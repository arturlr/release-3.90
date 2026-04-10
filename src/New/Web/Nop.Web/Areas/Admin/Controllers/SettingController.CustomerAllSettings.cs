using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Web.Areas.Admin.Models.Settings;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class SettingController
{
    #region CustomerUser

    public async Task<IActionResult> CustomerUser()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var cs = await settingService.LoadSettingAsync<CustomerSettings>(storeScope);
        var ads = await settingService.LoadSettingAsync<AddressSettings>(storeScope);
        var dts = await settingService.LoadSettingAsync<DateTimeSettings>(storeScope);
        var eas = await settingService.LoadSettingAsync<ExternalAuthenticationSettings>(storeScope);

        var model = new CustomerUserSettingsModel();
        model.CustomerSettings = new CustomerSettingsModel
        {
            UsernamesEnabled = cs.UsernamesEnabled,
            AllowUsersToChangeUsernames = cs.AllowUsersToChangeUsernames,
            CheckUsernameAvailabilityEnabled = cs.CheckUsernameAvailabilityEnabled,
            UserRegistrationType = (int)cs.UserRegistrationType,
            AllowCustomersToUploadAvatars = cs.AllowCustomersToUploadAvatars,
            DefaultAvatarEnabled = cs.DefaultAvatarEnabled,
            ShowCustomersLocation = cs.ShowCustomersLocation,
            ShowCustomersJoinDate = cs.ShowCustomersJoinDate,
            AllowViewingProfiles = cs.AllowViewingProfiles,
            NotifyNewCustomerRegistration = cs.NotifyNewCustomerRegistration,
            HideDownloadableProductsTab = cs.HideDownloadableProductsTab,
            HideBackInStockSubscriptionsTab = cs.HideBackInStockSubscriptionsTab,
            DownloadableProductsValidateUser = cs.DownloadableProductsValidateUser,
            CustomerNameFormat = (int)cs.CustomerNameFormat,
            NewsletterEnabled = cs.NewsletterEnabled,
            NewsletterTickedByDefault = cs.NewsletterTickedByDefault,
            HideNewsletterBlock = cs.HideNewsletterBlock,
            NewsletterBlockAllowToUnsubscribe = cs.NewsletterBlockAllowToUnsubscribe,
            OnlineCustomerMinutes = cs.OnlineCustomerMinutes,
            StoreLastVisitedPage = cs.StoreLastVisitedPage,
            EnteringEmailTwice = cs.EnteringEmailTwice,
            GenderEnabled = cs.GenderEnabled,
            DateOfBirthEnabled = cs.DateOfBirthEnabled,
            DateOfBirthRequired = cs.DateOfBirthRequired,
            CompanyEnabled = cs.CompanyEnabled,
            CompanyRequired = cs.CompanyRequired,
            StreetAddressEnabled = cs.StreetAddressEnabled,
            StreetAddressRequired = cs.StreetAddressRequired,
            StreetAddress2Enabled = cs.StreetAddress2Enabled,
            StreetAddress2Required = cs.StreetAddress2Required,
            ZipPostalCodeEnabled = cs.ZipPostalCodeEnabled,
            ZipPostalCodeRequired = cs.ZipPostalCodeRequired,
            CityEnabled = cs.CityEnabled,
            CityRequired = cs.CityRequired,
            CountryEnabled = cs.CountryEnabled,
            CountryRequired = cs.CountryRequired,
            StateProvinceEnabled = cs.StateProvinceEnabled,
            StateProvinceRequired = cs.StateProvinceRequired,
            PhoneEnabled = cs.PhoneEnabled,
            PhoneRequired = cs.PhoneRequired,
            FaxEnabled = cs.FaxEnabled,
            FaxRequired = cs.FaxRequired,
            AcceptPrivacyPolicyEnabled = cs.AcceptPrivacyPolicyEnabled
        };
        model.AddressSettings = new AddressSettingsModel
        {
            CompanyEnabled = ads.CompanyEnabled,
            CompanyRequired = ads.CompanyRequired,
            StreetAddressEnabled = ads.StreetAddressEnabled,
            StreetAddressRequired = ads.StreetAddressRequired,
            StreetAddress2Enabled = ads.StreetAddress2Enabled,
            StreetAddress2Required = ads.StreetAddress2Required,
            ZipPostalCodeEnabled = ads.ZipPostalCodeEnabled,
            ZipPostalCodeRequired = ads.ZipPostalCodeRequired,
            CityEnabled = ads.CityEnabled,
            CityRequired = ads.CityRequired,
            CountryEnabled = ads.CountryEnabled,
            StateProvinceEnabled = ads.StateProvinceEnabled,
            PhoneEnabled = ads.PhoneEnabled,
            PhoneRequired = ads.PhoneRequired,
            FaxEnabled = ads.FaxEnabled,
            FaxRequired = ads.FaxRequired
        };
        model.DateTimeSettings.AllowCustomersToSetTimeZone = dts.AllowCustomersToSetTimeZone;
        model.DateTimeSettings.DefaultStoreTimeZoneId = dateTimeHelper.DefaultStoreTimeZone.Id;
        foreach (var tz in dateTimeHelper.GetSystemTimeZones())
        {
            model.DateTimeSettings.AvailableTimeZones.Add(new DateTimeSettingsModel.TimeZoneSelectItem
            {
                Text = tz.DisplayName,
                Value = tz.Id,
                Selected = tz.Id.Equals(dateTimeHelper.DefaultStoreTimeZone.Id, StringComparison.OrdinalIgnoreCase)
            });
        }
        model.ExternalAuthenticationSettings.AutoRegisterEnabled = eas.AutoRegisterEnabled;
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CustomerUser(CustomerUserSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var cs = await settingService.LoadSettingAsync<CustomerSettings>(storeScope);
        cs.UsernamesEnabled = model.CustomerSettings.UsernamesEnabled;
        cs.AllowUsersToChangeUsernames = model.CustomerSettings.AllowUsersToChangeUsernames;
        cs.CheckUsernameAvailabilityEnabled = model.CustomerSettings.CheckUsernameAvailabilityEnabled;
        cs.UserRegistrationType = (UserRegistrationType)model.CustomerSettings.UserRegistrationType;
        cs.AllowCustomersToUploadAvatars = model.CustomerSettings.AllowCustomersToUploadAvatars;
        cs.DefaultAvatarEnabled = model.CustomerSettings.DefaultAvatarEnabled;
        cs.ShowCustomersLocation = model.CustomerSettings.ShowCustomersLocation;
        cs.ShowCustomersJoinDate = model.CustomerSettings.ShowCustomersJoinDate;
        cs.AllowViewingProfiles = model.CustomerSettings.AllowViewingProfiles;
        cs.NotifyNewCustomerRegistration = model.CustomerSettings.NotifyNewCustomerRegistration;
        cs.HideDownloadableProductsTab = model.CustomerSettings.HideDownloadableProductsTab;
        cs.HideBackInStockSubscriptionsTab = model.CustomerSettings.HideBackInStockSubscriptionsTab;
        cs.DownloadableProductsValidateUser = model.CustomerSettings.DownloadableProductsValidateUser;
        cs.CustomerNameFormat = (CustomerNameFormat)model.CustomerSettings.CustomerNameFormat;
        cs.NewsletterEnabled = model.CustomerSettings.NewsletterEnabled;
        cs.NewsletterTickedByDefault = model.CustomerSettings.NewsletterTickedByDefault;
        cs.HideNewsletterBlock = model.CustomerSettings.HideNewsletterBlock;
        cs.NewsletterBlockAllowToUnsubscribe = model.CustomerSettings.NewsletterBlockAllowToUnsubscribe;
        cs.OnlineCustomerMinutes = model.CustomerSettings.OnlineCustomerMinutes;
        cs.StoreLastVisitedPage = model.CustomerSettings.StoreLastVisitedPage;
        cs.EnteringEmailTwice = model.CustomerSettings.EnteringEmailTwice;
        cs.GenderEnabled = model.CustomerSettings.GenderEnabled;
        cs.DateOfBirthEnabled = model.CustomerSettings.DateOfBirthEnabled;
        cs.DateOfBirthRequired = model.CustomerSettings.DateOfBirthRequired;
        cs.CompanyEnabled = model.CustomerSettings.CompanyEnabled;
        cs.CompanyRequired = model.CustomerSettings.CompanyRequired;
        cs.StreetAddressEnabled = model.CustomerSettings.StreetAddressEnabled;
        cs.StreetAddressRequired = model.CustomerSettings.StreetAddressRequired;
        cs.StreetAddress2Enabled = model.CustomerSettings.StreetAddress2Enabled;
        cs.StreetAddress2Required = model.CustomerSettings.StreetAddress2Required;
        cs.ZipPostalCodeEnabled = model.CustomerSettings.ZipPostalCodeEnabled;
        cs.ZipPostalCodeRequired = model.CustomerSettings.ZipPostalCodeRequired;
        cs.CityEnabled = model.CustomerSettings.CityEnabled;
        cs.CityRequired = model.CustomerSettings.CityRequired;
        cs.CountryEnabled = model.CustomerSettings.CountryEnabled;
        cs.CountryRequired = model.CustomerSettings.CountryRequired;
        cs.StateProvinceEnabled = model.CustomerSettings.StateProvinceEnabled;
        cs.StateProvinceRequired = model.CustomerSettings.StateProvinceRequired;
        cs.PhoneEnabled = model.CustomerSettings.PhoneEnabled;
        cs.PhoneRequired = model.CustomerSettings.PhoneRequired;
        cs.FaxEnabled = model.CustomerSettings.FaxEnabled;
        cs.FaxRequired = model.CustomerSettings.FaxRequired;
        cs.AcceptPrivacyPolicyEnabled = model.CustomerSettings.AcceptPrivacyPolicyEnabled;
        await settingService.SaveSettingAsync(cs);

        var ads = await settingService.LoadSettingAsync<AddressSettings>(storeScope);
        ads.CompanyEnabled = model.AddressSettings.CompanyEnabled; ads.CompanyRequired = model.AddressSettings.CompanyRequired;
        ads.StreetAddressEnabled = model.AddressSettings.StreetAddressEnabled; ads.StreetAddressRequired = model.AddressSettings.StreetAddressRequired;
        ads.StreetAddress2Enabled = model.AddressSettings.StreetAddress2Enabled; ads.StreetAddress2Required = model.AddressSettings.StreetAddress2Required;
        ads.ZipPostalCodeEnabled = model.AddressSettings.ZipPostalCodeEnabled; ads.ZipPostalCodeRequired = model.AddressSettings.ZipPostalCodeRequired;
        ads.CityEnabled = model.AddressSettings.CityEnabled; ads.CityRequired = model.AddressSettings.CityRequired;
        ads.CountryEnabled = model.AddressSettings.CountryEnabled; ads.StateProvinceEnabled = model.AddressSettings.StateProvinceEnabled;
        ads.PhoneEnabled = model.AddressSettings.PhoneEnabled; ads.PhoneRequired = model.AddressSettings.PhoneRequired;
        ads.FaxEnabled = model.AddressSettings.FaxEnabled; ads.FaxRequired = model.AddressSettings.FaxRequired;
        await settingService.SaveSettingAsync(ads);

        var dts = await settingService.LoadSettingAsync<DateTimeSettings>(storeScope);
        dts.DefaultStoreTimeZoneId = model.DateTimeSettings.DefaultStoreTimeZoneId;
        dts.AllowCustomersToSetTimeZone = model.DateTimeSettings.AllowCustomersToSetTimeZone;
        await settingService.SaveSettingAsync(dts);

        var eas = await settingService.LoadSettingAsync<ExternalAuthenticationSettings>(storeScope);
        eas.AutoRegisterEnabled = model.ExternalAuthenticationSettings.AutoRegisterEnabled;
        await settingService.SaveSettingAsync(eas);

        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("CustomerUser");
    }

    #endregion

    #region RewardPoints

    public async Task<IActionResult> RewardPoints()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<RewardPointsSettings>(storeScope);
        var model = new RewardPointsSettingsModel
        {
            ActiveStoreScopeConfiguration = storeScope,
            Enabled = s.Enabled,
            ExchangeRate = s.ExchangeRate,
            MinimumRewardPointsToUse = s.MinimumRewardPointsToUse,
            PointsForRegistration = s.PointsForRegistration,
            PointsForPurchases_Amount = s.PointsForPurchases_Amount,
            PointsForPurchases_Points = s.PointsForPurchases_Points,
            ActivationDelay = s.ActivationDelay,
            ActivationDelayPeriodId = s.ActivationDelayPeriodId,
            DisplayHowMuchWillBeEarned = s.DisplayHowMuchWillBeEarned,
            PointsAccumulatedForAllStores = s.PointsAccumulatedForAllStores,
            PageSize = s.PageSize
        };
        if (storeScope > 0)
        {
            model.Enabled_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.Enabled, storeScope);
            model.ExchangeRate_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ExchangeRate, storeScope);
            model.MinimumRewardPointsToUse_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.MinimumRewardPointsToUse, storeScope);
            model.PointsForRegistration_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PointsForRegistration, storeScope);
            model.PointsForPurchases_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PointsForPurchases_Amount, storeScope);
            model.ActivationDelay_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.ActivationDelay, storeScope);
            model.DisplayHowMuchWillBeEarned_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.DisplayHowMuchWillBeEarned, storeScope);
            model.PageSize_OverrideForStore = await settingService.SettingExistsAsync(s, x => x.PageSize, storeScope);
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RewardPoints(RewardPointsSettingsModel model)
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        var storeScope = await GetActiveStoreScopeAsync();
        var s = await settingService.LoadSettingAsync<RewardPointsSettings>(storeScope);
        s.Enabled = model.Enabled; s.ExchangeRate = model.ExchangeRate; s.MinimumRewardPointsToUse = model.MinimumRewardPointsToUse;
        s.PointsForRegistration = model.PointsForRegistration; s.PointsForPurchases_Amount = model.PointsForPurchases_Amount;
        s.PointsForPurchases_Points = model.PointsForPurchases_Points; s.ActivationDelay = model.ActivationDelay;
        s.ActivationDelayPeriodId = model.ActivationDelayPeriodId; s.DisplayHowMuchWillBeEarned = model.DisplayHowMuchWillBeEarned;
        s.PointsAccumulatedForAllStores = model.PointsAccumulatedForAllStores; s.PageSize = model.PageSize;

        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ExchangeRate, model.ExchangeRate_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.MinimumRewardPointsToUse, model.MinimumRewardPointsToUse_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PointsForRegistration, model.PointsForRegistration_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PointsForPurchases_Amount, model.PointsForPurchases_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PointsForPurchases_Points, model.PointsForPurchases_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ActivationDelay, model.ActivationDelay_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.ActivationDelayPeriodId, model.ActivationDelay_OverrideForStore, storeScope, false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.DisplayHowMuchWillBeEarned, model.DisplayHowMuchWillBeEarned_OverrideForStore, storeScope, false);
        await settingService.SaveSettingAsync(s, x => x.PointsAccumulatedForAllStores, clearCache: false);
        await settingService.SaveSettingOverridablePerStoreAsync(s, x => x.PageSize, model.PageSize_OverrideForStore, storeScope, false);
        await settingService.ClearCacheAsync();
        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        SuccessNotification("The settings have been updated successfully.");
        return RedirectToAction("RewardPoints");
    }

    #endregion

    #region AllSettings

    public IActionResult AllSettings()
    {
        if (!permissionService.Authorize("ManageSettings")) return Forbid();
        return View();
    }

    [HttpPost]
    public async Task<JsonResult> AllSettings(DataSourceRequest command, AllSettingsListModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var allSettings = await settingService.GetAllSettingsAsync();
        var query = allSettings.AsQueryable();

        if (!string.IsNullOrEmpty(model.SearchSettingName))
            query = query.Where(s => (s.Name ?? "").Contains(model.SearchSettingName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(model.SearchSettingValue))
            query = query.Where(s => (s.Value ?? "").Contains(model.SearchSettingValue, StringComparison.OrdinalIgnoreCase));

        var settings = query.ToList();
        var gridData = new List<SettingModel>();
        foreach (var x in settings)
        {
            string storeName;
            if (x.StoreId == 0)
                storeName = "All stores";
            else
            {
                var store = await storeService.GetStoreByIdAsync(x.StoreId);
                storeName = store?.Name ?? "Unknown";
            }
            gridData.Add(new SettingModel { Id = x.Id, Name = x.Name, Value = x.Value, Store = storeName, StoreId = x.StoreId });
        }

        var page = command.Page > 0 ? command.Page : 1;
        var pageSize = command.PageSize > 0 ? command.PageSize : 20;
        return Json(new DataSourceResult
        {
            Data = gridData.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Total = gridData.Count
        });
    }

    [HttpPost]
    public async Task<JsonResult> SettingUpdate(SettingModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        model.Name = model.Name?.Trim();
        model.Value = model.Value?.Trim();

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = "Invalid model" });

        var setting = await settingService.GetSettingByIdAsync(model.Id);
        if (setting is null)
            return Json(new DataSourceResult { Errors = "Setting not found" });

        if (!string.Equals(setting.Name, model.Name, StringComparison.OrdinalIgnoreCase) || setting.StoreId != model.StoreId)
            await settingService.DeleteSettingAsync(setting);

        await settingService.SetSettingAsync(model.Name ?? "", model.Value ?? "", model.StoreId);
        customerActivityService.InsertActivity("EditSettings", "Edited settings");
        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> SettingAdd(SettingModel model)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        model.Name = model.Name?.Trim();
        model.Value = model.Value?.Trim();

        if (!ModelState.IsValid)
            return Json(new DataSourceResult { Errors = "Invalid model" });

        await settingService.SetSettingAsync(model.Name ?? "", model.Value ?? "", model.StoreId);
        customerActivityService.InsertActivity("AddNewSetting", $"Added a new setting ({model.Name})");
        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> SettingDelete(int id)
    {
        if (!permissionService.Authorize("ManageSettings"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var setting = await settingService.GetSettingByIdAsync(id);
        if (setting is not null)
        {
            await settingService.DeleteSettingAsync(setting);
            customerActivityService.InsertActivity("DeleteSetting", $"Deleted a setting ({setting.Name})");
        }
        return Json(new { });
    }

    #endregion
}
