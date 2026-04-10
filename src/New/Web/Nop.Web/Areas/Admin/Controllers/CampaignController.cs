using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public class CampaignController(
    ICampaignService campaignService,
    IEmailAccountService emailAccountService,
    INewsLetterSubscriptionService newsLetterSubscriptionService,
    IMessageTokenProvider messageTokenProvider,
    ICustomerService customerService,
    IStoreService storeService,
    IStoreContext storeContext,
    IDateTimeHelper dateTimeHelper,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService,
    EmailAccountSettings emailAccountSettings) : BaseAdminController
{
    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var model = new CampaignListModel();
        await PrepareStoreDropdownAsync(model.AvailableStores);
        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> CampaignList(CampaignListModel model, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var campaigns = await campaignService.GetAllCampaignsAsync(model.SearchStoreId);

        return Json(new DataSourceResult
        {
            Data = campaigns.Select(c => new CampaignGridModel
            {
                Id = c.Id,
                Name = c.Name,
                Subject = c.Subject,
                CreatedOn = dateTimeHelper.ConvertToUserTime(c.CreatedOnUtc, DateTimeKind.Utc),
                DontSendBeforeDate = c.DontSendBeforeDateUtc.HasValue
                    ? dateTimeHelper.ConvertToUserTime(c.DontSendBeforeDateUtc.Value, DateTimeKind.Utc)
                    : null
            }),
            Total = campaigns.Count
        });
    }

    public async Task<IActionResult> Create()
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var model = new CampaignModel
        {
            AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfCampaignAllowedTokens()),
            EmailAccountId = emailAccountSettings.DefaultEmailAccountId
        };
        await PrepareDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CampaignModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var campaign = new Campaign
            {
                Name = model.Name,
                Subject = model.Subject,
                Body = model.Body,
                StoreId = model.StoreId,
                CustomerRoleId = model.CustomerRoleId,
                CreatedOnUtc = DateTime.UtcNow,
                DontSendBeforeDateUtc = model.DontSendBeforeDate.HasValue
                    ? dateTimeHelper.ConvertToUtcTime(model.DontSendBeforeDate.Value)
                    : null
            };
            await campaignService.InsertCampaignAsync(campaign);

            customerActivityService.InsertActivity("AddNewCampaign", "Added a new campaign (ID = {0})", campaign.Id);

            return continueEditing
                ? RedirectToAction("Edit", new { id = campaign.Id })
                : RedirectToAction("List");
        }

        model.AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfCampaignAllowedTokens());
        await PrepareDropdownsAsync(model);
        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var campaign = await campaignService.GetCampaignByIdAsync(id);
        if (campaign is null)
            return RedirectToAction("List");

        var model = MapCampaignToModel(campaign);
        model.AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfCampaignAllowedTokens());
        model.EmailAccountId = emailAccountSettings.DefaultEmailAccountId;
        await PrepareDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CampaignModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var campaign = await campaignService.GetCampaignByIdAsync(model.Id);
        if (campaign is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            campaign.Name = model.Name;
            campaign.Subject = model.Subject;
            campaign.Body = model.Body;
            campaign.StoreId = model.StoreId;
            campaign.CustomerRoleId = model.CustomerRoleId;
            campaign.DontSendBeforeDateUtc = model.DontSendBeforeDate.HasValue
                ? dateTimeHelper.ConvertToUtcTime(model.DontSendBeforeDate.Value)
                : null;
            await campaignService.UpdateCampaignAsync(campaign);

            customerActivityService.InsertActivity("EditCampaign", "Edited a campaign (ID = {0})", campaign.Id);

            return continueEditing
                ? RedirectToAction("Edit", new { id = campaign.Id })
                : RedirectToAction("List");
        }

        model.AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfCampaignAllowedTokens());
        await PrepareDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SendTestEmail(int id, string? testEmail, int emailAccountId)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var campaign = await campaignService.GetCampaignByIdAsync(id);
        if (campaign is null)
            return RedirectToAction("List");

        if (!CommonHelper.IsValidEmail(testEmail))
        {
            // redirect back — wrong email
            return RedirectToAction("Edit", new { id = campaign.Id });
        }

        try
        {
            var emailAccount = await GetEmailAccountAsync(emailAccountId);
            var subscription = await newsLetterSubscriptionService
                .GetNewsLetterSubscriptionByEmailAndStoreIdAsync(testEmail!, storeContext.CurrentStore.Id);

            if (subscription is not null)
                await campaignService.SendCampaignAsync(campaign, emailAccount, [subscription]);
            else
                await campaignService.SendCampaignAsync(campaign, emailAccount, testEmail!);
        }
        catch
        {
            // error sending — redirect back
        }

        return RedirectToAction("Edit", new { id = campaign.Id });
    }

    [HttpPost]
    public async Task<IActionResult> SendMassEmail(int id, int customerRoleId, int emailAccountId)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var campaign = await campaignService.GetCampaignByIdAsync(id);
        if (campaign is null)
            return RedirectToAction("List");

        try
        {
            var emailAccount = await GetEmailAccountAsync(emailAccountId);

            var store = await storeService.GetStoreByIdAsync(campaign.StoreId);
            var storeId = store is not null ? store.Id : 0;
            var subscriptions = await newsLetterSubscriptionService
                .GetAllNewsLetterSubscriptionsAsync(storeId: storeId, customerRoleId: customerRoleId, isActive: true);

            await campaignService.SendCampaignAsync(campaign, emailAccount, subscriptions);
        }
        catch
        {
            // error sending — redirect back
        }

        return RedirectToAction("Edit", new { id = campaign.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageCampaigns"))
            return Forbid();

        var campaign = await campaignService.GetCampaignByIdAsync(id);
        if (campaign is null)
            return RedirectToAction("List");

        await campaignService.DeleteCampaignAsync(campaign);

        customerActivityService.InsertActivity("DeleteCampaign", "Deleted a campaign (ID = {0})", campaign.Id);

        return RedirectToAction("List");
    }

    // --- helpers ---

    private CampaignModel MapCampaignToModel(Campaign campaign) => new()
    {
        Id = campaign.Id,
        Name = campaign.Name,
        Subject = campaign.Subject,
        Body = campaign.Body,
        StoreId = campaign.StoreId,
        CustomerRoleId = campaign.CustomerRoleId,
        DontSendBeforeDate = campaign.DontSendBeforeDateUtc.HasValue
            ? dateTimeHelper.ConvertToUserTime(campaign.DontSendBeforeDateUtc.Value, DateTimeKind.Utc)
            : null
    };

    private async Task PrepareStoreDropdownAsync(IList<SelectListItem> items)
    {
        items.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var store in await storeService.GetAllStoresAsync())
            items.Add(new SelectListItem { Text = store.Name, Value = store.Id.ToString() });
    }

    private async Task PrepareDropdownsAsync(CampaignModel model)
    {
        await PrepareStoreDropdownAsync(model.AvailableStores);

        model.AvailableCustomerRoles.Add(new SelectListItem { Text = "All", Value = "0" });
        foreach (var role in await customerService.GetAllCustomerRolesAsync(showHidden: true))
            model.AvailableCustomerRoles.Add(new SelectListItem { Text = role.Name, Value = role.Id.ToString() });

        foreach (var ea in await emailAccountService.GetAllEmailAccountsAsync())
            model.AvailableEmailAccounts.Add(new SelectListItem
            {
                Text = $"{ea.DisplayName} ({ea.Email})",
                Value = ea.Id.ToString()
            });
    }

    private async Task<EmailAccount> GetEmailAccountAsync(int emailAccountId)
    {
        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(emailAccountId)
            ?? await emailAccountService.GetEmailAccountByIdAsync(emailAccountSettings.DefaultEmailAccountId);
        return emailAccount ?? throw new NopException("Email account could not be loaded");
    }
}
