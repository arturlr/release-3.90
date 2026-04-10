using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Messages;
using Nop.Services.Helpers;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Models.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;

namespace Nop.Web.Areas.Admin.Controllers;

public class QueuedEmailController(
    IQueuedEmailService queuedEmailService,
    IEmailAccountService emailAccountService,
    IDateTimeHelper dateTimeHelper,
    IPermissionService permissionService) : BaseAdminController
{
    public IActionResult List()
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        return View(new QueuedEmailListModel());
    }

    [HttpPost]
    public async Task<JsonResult> QueuedEmailList(QueuedEmailListModel model, DataSourceRequest command)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Json(new DataSourceResult { Errors = "Access denied" });

        DateTime? startDateUtc = model.SearchStartDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.SearchStartDate.Value, dateTimeHelper.CurrentTimeZone)
            : null;
        DateTime? endDateUtc = model.SearchEndDate.HasValue
            ? dateTimeHelper.ConvertToUtcTime(model.SearchEndDate.Value, dateTimeHelper.CurrentTimeZone).AddDays(1)
            : null;

        var queuedEmails = await queuedEmailService.SearchEmailsAsync(
            model.SearchFromEmail, model.SearchToEmail,
            startDateUtc, endDateUtc,
            model.SearchLoadNotSent, false, model.SearchMaxSentTries, true,
            command.Page - 1, command.PageSize);

        var gridModel = new DataSourceResult
        {
            Data = queuedEmails.Select(qe => new QueuedEmailGridModel
            {
                Id = qe.Id,
                From = qe.From,
                To = qe.To,
                Subject = qe.Subject,
                CreatedOn = dateTimeHelper.ConvertToUserTime(qe.CreatedOnUtc, DateTimeKind.Utc),
                SentTries = qe.SentTries,
                SentOn = qe.SentOnUtc.HasValue
                    ? dateTimeHelper.ConvertToUserTime(qe.SentOnUtc.Value, DateTimeKind.Utc)
                    : null,
                PriorityName = qe.Priority.ToString()
            }),
            Total = queuedEmails.TotalCount
        };

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> GoToEmailByNumber(QueuedEmailListModel model)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        var queuedEmail = await queuedEmailService.GetQueuedEmailByIdAsync(model.GoDirectlyToNumber);
        if (queuedEmail is null)
            return RedirectToAction("List");

        return RedirectToAction("Edit", new { id = queuedEmail.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        var email = await queuedEmailService.GetQueuedEmailByIdAsync(id);
        if (email is null)
            return RedirectToAction("List");

        return View(await PrepareQueuedEmailModelAsync(email));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(QueuedEmailModel model, bool continueEditing = false)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        var email = await queuedEmailService.GetQueuedEmailByIdAsync(model.Id);
        if (email is null)
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            email.PriorityId = (int)Enum.Parse<QueuedEmailPriority>(model.PriorityName ?? "Low");
            email.From = model.From;
            email.FromName = model.FromName;
            email.To = model.To;
            email.ToName = model.ToName;
            email.ReplyTo = model.ReplyTo;
            email.ReplyToName = model.ReplyToName;
            email.CC = model.CC;
            email.Bcc = model.Bcc;
            email.Subject = model.Subject;
            email.Body = model.Body;
            email.DontSendBeforeDateUtc = model.SendImmediately || !model.DontSendBeforeDate.HasValue
                ? null
                : dateTimeHelper.ConvertToUtcTime(model.DontSendBeforeDate.Value);
            await queuedEmailService.UpdateQueuedEmailAsync(email);

            return continueEditing
                ? RedirectToAction("Edit", new { id = email.Id })
                : RedirectToAction("List");
        }

        return View(await PrepareQueuedEmailModelAsync(email));
    }

    [HttpPost]
    public async Task<IActionResult> Requeue(int id, bool sendImmediately = true, DateTime? dontSendBeforeDate = null)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        var queuedEmail = await queuedEmailService.GetQueuedEmailByIdAsync(id);
        if (queuedEmail is null)
            return RedirectToAction("List");

        var requeuedEmail = new QueuedEmail
        {
            PriorityId = queuedEmail.PriorityId,
            From = queuedEmail.From,
            FromName = queuedEmail.FromName,
            To = queuedEmail.To,
            ToName = queuedEmail.ToName,
            ReplyTo = queuedEmail.ReplyTo,
            ReplyToName = queuedEmail.ReplyToName,
            CC = queuedEmail.CC,
            Bcc = queuedEmail.Bcc,
            Subject = queuedEmail.Subject,
            Body = queuedEmail.Body,
            AttachmentFilePath = queuedEmail.AttachmentFilePath,
            AttachmentFileName = queuedEmail.AttachmentFileName,
            AttachedDownloadId = queuedEmail.AttachedDownloadId,
            CreatedOnUtc = DateTime.UtcNow,
            EmailAccountId = queuedEmail.EmailAccountId,
            DontSendBeforeDateUtc = sendImmediately || !dontSendBeforeDate.HasValue
                ? null
                : dateTimeHelper.ConvertToUtcTime(dontSendBeforeDate.Value)
        };
        await queuedEmailService.InsertQueuedEmailAsync(requeuedEmail);

        return RedirectToAction("Edit", new { id = requeuedEmail.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        var email = await queuedEmailService.GetQueuedEmailByIdAsync(id);
        if (email is null)
            return RedirectToAction("List");

        await queuedEmailService.DeleteQueuedEmailAsync(email);
        return RedirectToAction("List");
    }

    [HttpPost]
    public async Task<JsonResult> DeleteSelected(ICollection<int>? selectedIds)
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Json(new { Result = false });

        if (selectedIds is { Count: > 0 })
        {
            var emails = await queuedEmailService.GetQueuedEmailsByIdsAsync(selectedIds.ToArray());
            await queuedEmailService.DeleteQueuedEmailsAsync(emails);
        }

        return Json(new { Result = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteAll()
    {
        if (!permissionService.Authorize("ManageMessageQueue"))
            return Forbid();

        await queuedEmailService.DeleteAllEmailsAsync();
        return RedirectToAction("List");
    }

    private async Task<QueuedEmailModel> PrepareQueuedEmailModelAsync(QueuedEmail email)
    {
        var emailAccount = await emailAccountService.GetEmailAccountByIdAsync(email.EmailAccountId);

        return new QueuedEmailModel
        {
            Id = email.Id,
            PriorityName = email.Priority.ToString(),
            From = email.From,
            FromName = email.FromName,
            To = email.To,
            ToName = email.ToName,
            ReplyTo = email.ReplyTo,
            ReplyToName = email.ReplyToName,
            CC = email.CC,
            Bcc = email.Bcc,
            Subject = email.Subject,
            Body = email.Body,
            AttachmentFilePath = email.AttachmentFilePath,
            AttachmentFileName = email.AttachmentFileName,
            AttachedDownloadId = email.AttachedDownloadId,
            CreatedOn = dateTimeHelper.ConvertToUserTime(email.CreatedOnUtc, DateTimeKind.Utc),
            SendImmediately = !email.DontSendBeforeDateUtc.HasValue,
            DontSendBeforeDate = email.DontSendBeforeDateUtc.HasValue
                ? dateTimeHelper.ConvertToUserTime(email.DontSendBeforeDateUtc.Value, DateTimeKind.Utc)
                : null,
            SentTries = email.SentTries,
            SentOn = email.SentOnUtc.HasValue
                ? dateTimeHelper.ConvertToUserTime(email.SentOnUtc.Value, DateTimeKind.Utc)
                : null,
            EmailAccountId = email.EmailAccountId,
            EmailAccountName = emailAccount is not null
                ? $"{emailAccount.DisplayName} ({emailAccount.Email})"
                : string.Empty
        };
    }
}
