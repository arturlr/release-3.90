using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Messages;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class MessageTemplateController(
    IMessageTemplateService messageTemplateService,
    IEmailAccountService emailAccountService,
    IMessageTokenProvider messageTokenProvider,
    IWorkflowMessageService workflowMessageService,
    IStoreService storeService,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public async Task<IActionResult> List()
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var model = new MessageTemplateListModel();
        model.AvailableStores.Add(new MessageTemplateListModel.StoreSelectItem { Id = 0, Name = "All" });
        foreach (var s in await storeService.GetAllStoresAsync())
            model.AvailableStores.Add(new MessageTemplateListModel.StoreSelectItem { Id = s.Id, Name = s.Name });

        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> MessageTemplateList(int searchStoreId = 0)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var templates = await messageTemplateService.GetAllMessageTemplatesAsync(searchStoreId);
        var allStores = await storeService.GetAllStoresAsync();
        var storeDict = allStores.ToDictionary(s => s.Id, s => s.Name ?? string.Empty);

        var gridModel = new DataSourceResult
        {
            Data = templates.Select(t => new MessageTemplateGridModel
            {
                Id = t.Id,
                Name = t.Name,
                IsActive = t.IsActive,
                ListOfStores = !t.LimitedToStores
                    ? "All"
                    : string.Join(", ", storeDict.Values)
            }),
            Total = templates.Count
        };

        return Json(gridModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(id);
        if (mt is null)
            return RedirectToAction("List");

        var model = MapToModel(mt);
        model.AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfAllowedTokens(mt.GetTokenGroups()));
        await PrepareEmailAccountDropdownAsync(model);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(MessageTemplateModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(model.Id);
        if (mt is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            mt.BccEmailAddresses = model.BccEmailAddresses;
            mt.Subject = model.Subject;
            mt.Body = model.Body;
            mt.IsActive = model.IsActive;
            mt.EmailAccountId = model.EmailAccountId;
            mt.AttachedDownloadId = model.HasAttachedDownload ? model.AttachedDownloadId : 0;
            mt.DelayBeforeSend = model.SendImmediately ? null : model.DelayBeforeSend;
            mt.DelayPeriodId = model.DelayPeriodId;
            mt.LimitedToStores = model.LimitedToStores;

            await messageTemplateService.UpdateMessageTemplateAsync(mt);

            customerActivityService.InsertActivity("EditMessageTemplate",
                $"Edited a message template (ID = {mt.Id})");

            return continueEditing
                ? RedirectToAction("Edit", new { id = mt.Id })
                : RedirectToAction("List");
        }

        model.AllowedTokens = string.Join(", ", messageTokenProvider.GetListOfAllowedTokens(mt.GetTokenGroups()));
        await PrepareEmailAccountDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(id);
        if (mt is null)
            return RedirectToAction("List");

        await messageTemplateService.DeleteMessageTemplateAsync(mt);

        customerActivityService.InsertActivity("DeleteMessageTemplate",
            $"Deleted a message template (ID = {id})");

        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<IActionResult> CopyTemplate(int id)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(id);
        if (mt is null)
            return RedirectToAction("List");

        var copy = await messageTemplateService.CopyMessageTemplateAsync(mt);
        return RedirectToAction("Edit", new { id = copy.Id });
    }

    public async Task<IActionResult> TestTemplate(int id)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(id);
        if (mt is null)
            return RedirectToAction("List");

        var allTokens = messageTokenProvider.GetListOfAllowedTokens(mt.GetTokenGroups()).ToList();
        var usedTokens = allTokens.Where(t =>
            (mt.Subject?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (mt.Body?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        return View(new TestMessageTemplateModel
        {
            Id = mt.Id,
            Tokens = usedTokens
        });
    }

    [HttpPost]
    public async Task<IActionResult> SendTestTemplate(TestMessageTemplateModel model)
    {
        if (!permissionService.Authorize("ManageMessageTemplates"))
            return Forbid();

        var mt = await messageTemplateService.GetMessageTemplateByIdAsync(model.Id);
        if (mt is null)
            return RedirectToAction("List");

        var tokens = new List<Token>();
        foreach (var key in Request.Form.Keys)
        {
            if (!key.StartsWith("token_", StringComparison.OrdinalIgnoreCase))
                continue;

            var tokenKey = key["token_".Length..].Replace("%", "");
            var stringValue = Request.Form[key].ToString();

            object tokenValue;
            if (bool.TryParse(stringValue, out var boolVal))
                tokenValue = boolVal;
            else if (int.TryParse(stringValue, out var intVal))
                tokenValue = intVal;
            else if (decimal.TryParse(stringValue, out var decVal))
                tokenValue = decVal;
            else
                tokenValue = stringValue;

            tokens.Add(new Token(tokenKey, tokenValue));
        }

        try
        {
            await workflowMessageService.SendTestEmailAsync(mt.Id, model.SendTo ?? string.Empty, tokens, model.LanguageId);
        }
        catch
        {
            // Test email failure is non-critical
        }

        return RedirectToAction("Edit", new { id = mt.Id });
    }

    #region Helpers

    private static MessageTemplateModel MapToModel(MessageTemplate mt) => new()
    {
        Id = mt.Id,
        Name = mt.Name,
        BccEmailAddresses = mt.BccEmailAddresses,
        Subject = mt.Subject,
        Body = mt.Body,
        IsActive = mt.IsActive,
        SendImmediately = !mt.DelayBeforeSend.HasValue,
        DelayBeforeSend = mt.DelayBeforeSend,
        DelayPeriodId = mt.DelayPeriodId,
        AttachedDownloadId = mt.AttachedDownloadId,
        HasAttachedDownload = mt.AttachedDownloadId > 0,
        EmailAccountId = mt.EmailAccountId,
        LimitedToStores = mt.LimitedToStores
    };

    private async Task PrepareEmailAccountDropdownAsync(MessageTemplateModel model)
    {
        foreach (var ea in await emailAccountService.GetAllEmailAccountsAsync())
            model.AvailableEmailAccounts.Add(new MessageTemplateModel.EmailAccountSelectItem
            {
                Id = ea.Id,
                DisplayName = $"{ea.DisplayName} ({ea.Email})"
            });
    }

    #endregion
}
