using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public partial class EmailAccountController(
    IEmailAccountService emailAccountService,
    IEmailSender emailSender,
    ISettingService settingService,
    IStoreContext storeContext,
    EmailAccountSettings emailAccountSettings,
    ICustomerActivityService customerActivityService,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        return View();
    }

    [HttpPost]
    public async Task<JsonResult> EmailAccountList()
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        var accounts = await emailAccountService.GetAllEmailAccountsAsync();
        var gridModel = new DataSourceResult
        {
            Data = accounts.Select(ea => new EmailAccountModel
            {
                Id = ea.Id,
                Email = ea.Email,
                DisplayName = ea.DisplayName,
                Host = ea.Host,
                Port = ea.Port,
                Username = ea.Username,
                EnableSsl = ea.EnableSsl,
                UseDefaultCredentials = ea.UseDefaultCredentials,
                IsDefaultEmailAccount = ea.Id == emailAccountSettings.DefaultEmailAccountId
            }),
            Total = accounts.Count
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsDefaultEmail(int id)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var account = await emailAccountService.GetEmailAccountByIdAsync(id);
        if (account is null)
            return Json(new { result = false });

        emailAccountSettings.DefaultEmailAccountId = account.Id;
        await settingService.SaveSettingAsync(emailAccountSettings);

        return Json(new { result = true });
    }

    public IActionResult Create()
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        return View(new EmailAccountModel { Port = 25 });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EmailAccountModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        if (ModelState.IsValid)
        {
            var emailAccount = new EmailAccount
            {
                Email = model.Email,
                DisplayName = model.DisplayName,
                Host = model.Host,
                Port = model.Port,
                Username = model.Username,
                Password = model.Password,
                EnableSsl = model.EnableSsl,
                UseDefaultCredentials = model.UseDefaultCredentials
            };
            await emailAccountService.InsertEmailAccountAsync(emailAccount);

            customerActivityService.InsertActivity("AddNewEmailAccount",
                $"Added a new email account (ID = {emailAccount.Id})");

            return continueEditing
                ? RedirectToAction("Edit", new { id = emailAccount.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(id);
        if (emailAccount is null)
            return RedirectToAction("List");

        return View(new EmailAccountModel
        {
            Id = emailAccount.Id,
            Email = emailAccount.Email,
            DisplayName = emailAccount.DisplayName,
            Host = emailAccount.Host,
            Port = emailAccount.Port,
            Username = emailAccount.Username,
            EnableSsl = emailAccount.EnableSsl,
            UseDefaultCredentials = emailAccount.UseDefaultCredentials,
            IsDefaultEmailAccount = emailAccount.Id == emailAccountSettings.DefaultEmailAccountId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EmailAccountModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(model.Id);
        if (emailAccount is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            emailAccount.Email = model.Email;
            emailAccount.DisplayName = model.DisplayName;
            emailAccount.Host = model.Host;
            emailAccount.Port = model.Port;
            emailAccount.Username = model.Username;
            emailAccount.EnableSsl = model.EnableSsl;
            emailAccount.UseDefaultCredentials = model.UseDefaultCredentials;
            await emailAccountService.UpdateEmailAccountAsync(emailAccount);

            customerActivityService.InsertActivity("EditEmailAccount",
                $"Edited an email account (ID = {emailAccount.Id})");

            return continueEditing
                ? RedirectToAction("Edit", new { id = emailAccount.Id })
                : RedirectToAction("List");
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(int id, string? password)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(id);
        if (emailAccount is null)
            return RedirectToAction("List");

        emailAccount.Password = password;
        await emailAccountService.UpdateEmailAccountAsync(emailAccount);

        return RedirectToAction("Edit", new { id = emailAccount.Id });
    }

    [HttpPost]
    public async Task<IActionResult> SendTestEmail(int id, string? sendTestEmailTo)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(id);
        if (emailAccount is null)
            return RedirectToAction("List");

        if (string.IsNullOrWhiteSpace(sendTestEmailTo) || !CommonHelper.IsValidEmail(sendTestEmailTo))
            return RedirectToAction("Edit", new { id = emailAccount.Id });

        try
        {
            var subject = storeContext.CurrentStore.Name + ". Testing email functionality.";
            await emailSender.SendEmailAsync(emailAccount, subject, "Email works fine.",
                emailAccount.Email ?? string.Empty, emailAccount.DisplayName ?? string.Empty,
                sendTestEmailTo.Trim(), string.Empty);
        }
        catch
        {
            // Send test email failure is non-critical — redirect back to edit
        }

        return RedirectToAction("Edit", new { id = emailAccount.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageEmailAccounts"))
            return Forbid();

        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(id);
        if (emailAccount is null)
            return RedirectToAction("List");

        try
        {
            await emailAccountService.DeleteEmailAccountAsync(emailAccount);

            customerActivityService.InsertActivity("DeleteEmailAccount",
                $"Deleted an email account (ID = {id})");

            return RedirectToAction("List");
        }
        catch
        {
            // DeleteEmailAccountAsync throws NopException if last account — redirect back to edit
            return RedirectToAction("Edit", new { id = emailAccount.Id });
        }
    }
}
