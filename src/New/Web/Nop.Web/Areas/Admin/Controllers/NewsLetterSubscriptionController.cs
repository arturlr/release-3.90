using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Services.Customers;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public class NewsLetterSubscriptionController(
    INewsLetterSubscriptionService newsLetterSubscriptionService,
    IDateTimeHelper dateTimeHelper,
    IPermissionService permissionService,
    IStoreService storeService,
    ICustomerService customerService,
    IExportManager exportManager,
    IImportManager importManager) : BaseAdminController
{
    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Forbid();

        var model = new NewsLetterSubscriptionListModel();
        await PrepareDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> SubscriptionList(NewsLetterSubscriptionListModel model, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        bool? isActive = model.ActiveId switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        var startDateUtc = model.StartDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, dateTimeHelper.CurrentTimeZone)
            : (DateTime?)null;
        var endDateUtc = model.EndDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : (DateTime?)null;

        var subscriptions = await newsLetterSubscriptionService.GetAllNewsLetterSubscriptionsAsync(
            model.SearchEmail, startDateUtc, endDateUtc,
            model.StoreId, isActive, model.CustomerRoleId,
            command.Page - 1, command.PageSize);

        var stores = await storeService.GetAllStoresAsync();
        var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

        return Json(new DataSourceResult
        {
            Data = subscriptions.Select(s => new NewsLetterSubscriptionModel
            {
                Id = s.Id,
                Email = s.Email,
                Active = s.Active,
                StoreName = storeDict.TryGetValue(s.StoreId, out var name) ? name : "Unknown",
                CreatedOn = dateTimeHelper.ConvertToUserTime(s.CreatedOnUtc, DateTimeKind.Utc)
            }),
            Total = subscriptions.TotalCount
        });
    }

    [HttpPost]
    public async Task<JsonResult> SubscriptionUpdate(NewsLetterSubscriptionModel model)
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        if (!ModelState.IsValid)
        {
            return Json(new DataSourceResult
            {
                Errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage))
            });
        }

        var subscription = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByIdAsync(model.Id);
        if (subscription is null)
            return Json(new DataSourceResult { Errors = "Subscription not found" });

        subscription.Email = model.Email;
        subscription.Active = model.Active;
        await newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(subscription, publishSubscriptionEvents: false);

        return Json(new { });
    }

    [HttpPost]
    public async Task<JsonResult> SubscriptionDelete(int id)
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var subscription = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByIdAsync(id);
        if (subscription is not null)
            await newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(subscription, publishSubscriptionEvents: false);

        return Json(new { });
    }

    [HttpPost]
    public async Task<IActionResult> ExportCsv(NewsLetterSubscriptionListModel model)
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Forbid();

        bool? isActive = model.ActiveId switch
        {
            1 => true,
            2 => false,
            _ => null
        };

        var startDateUtc = model.StartDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.StartDate.Value, dateTimeHelper.CurrentTimeZone)
            : (DateTime?)null;
        var endDateUtc = model.EndDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.EndDate.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : (DateTime?)null;

        var subscriptions = await newsLetterSubscriptionService.GetAllNewsLetterSubscriptionsAsync(
            model.SearchEmail, startDateUtc, endDateUtc,
            model.StoreId, isActive, model.CustomerRoleId);

        var result = exportManager.ExportNewsletterSubscribersToTxt(subscriptions);
        var fileName = $"newsletter_emails_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{CommonHelper.GenerateRandomDigitCode(4)}.txt";

        return File(Encoding.UTF8.GetBytes(result), MimeTypes.TextCsv, fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportCsv(IFormFile? importcsvfile)
    {
        if (!permissionService.Authorize("ManageNewsletterSubscribers"))
            return Forbid();

        if (importcsvfile is { Length: > 0 })
        {
            await importManager.ImportNewsletterSubscribersFromTxtAsync(importcsvfile.OpenReadStream());
        }

        return RedirectToAction("List");
    }

    private async Task PrepareDropdownsAsync(NewsLetterSubscriptionListModel model)
    {
        model.AvailableStores.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var store in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });

        model.ActiveList.Add(new SelectListItem { Value = "0", Text = "All" });
        model.ActiveList.Add(new SelectListItem { Value = "1", Text = "Active only" });
        model.ActiveList.Add(new SelectListItem { Value = "2", Text = "Not active only" });

        model.AvailableCustomerRoles.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var role in await customerService.GetAllCustomerRolesAsync(showHidden: true))
            model.AvailableCustomerRoles.Add(new SelectListItem { Text = role.Name, Value = role.Id.ToString() });
    }
}
