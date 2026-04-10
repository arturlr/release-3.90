using System.Net;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Services.Customers;
using Nop.Services.Forums;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.PrivateMessages;

namespace Nop.Web.Controllers;

public class PrivateMessagesController(
    IForumService forumService,
    ICustomerService customerService,
    ICustomerActivityService customerActivityService,
    ILocalizationService localizationService,
    IWorkContext workContext,
    IStoreContext storeContext,
    ForumSettings forumSettings) : BasePublicController
{
    // --- Index (Inbox + Sent Items inlined) ---

    public async Task<IActionResult> Index(int? pageInbox, int? pageSent, string? tab)
    {
        if (!forumSettings.AllowPrivateMessages)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var storeId = storeContext.CurrentStore.Id;
        var pageSize = forumSettings.PrivateMessagesPageSize > 0 ? forumSettings.PrivateMessagesPageSize : 10;

        var inboxPage = pageInbox ?? 0;
        var sentPage = pageSent ?? 0;

        var inbox = await forumService.GetAllPrivateMessagesAsync(
            storeId, 0, customer.Id, null, false, null, string.Empty, inboxPage, pageSize);
        var sent = await forumService.GetAllPrivateMessagesAsync(
            storeId, customer.Id, 0, null, null, false, string.Empty, sentPage, pageSize);

        var model = new PrivateMessageIndexModel
        {
            SentItemsTabSelected = string.Equals(tab, "sent", StringComparison.OrdinalIgnoreCase),
            InboxPage = inboxPage,
            SentItemsPage = sentPage,
            InboxMessages = MapMessageList(inbox, inboxPage),
            SentMessages = MapMessageList(sent, sentPage)
        };

        return View(model);
    }

    // --- Inbox actions ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteInboxPM([FromForm] IEnumerable<int> inboxIds)
    {
        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        foreach (var id in inboxIds)
        {
            var pm = await forumService.GetPrivateMessageByIdAsync(id);
            if (pm?.ToCustomerId == customer.Id)
            {
                pm.IsDeletedByRecipient = true;
                await forumService.UpdatePrivateMessageAsync(pm);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkUnread([FromForm] IEnumerable<int> inboxIds)
    {
        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        foreach (var id in inboxIds)
        {
            var pm = await forumService.GetPrivateMessageByIdAsync(id);
            if (pm?.ToCustomerId == customer.Id)
            {
                pm.IsRead = false;
                await forumService.UpdatePrivateMessageAsync(pm);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // --- Sent Items actions ---

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSentPM([FromForm] IEnumerable<int> sentIds)
    {
        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        foreach (var id in sentIds)
        {
            var pm = await forumService.GetPrivateMessageByIdAsync(id);
            if (pm?.FromCustomerId == customer.Id)
            {
                pm.IsDeletedByAuthor = true;
                await forumService.UpdatePrivateMessageAsync(pm);
            }
        }

        return RedirectToAction(nameof(Index), new { tab = "sent" });
    }

    // --- Send PM ---

    public async Task<IActionResult> SendPM(int toCustomerId, int? replyToMessageId)
    {
        if (!forumSettings.AllowPrivateMessages)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var customerTo = await customerService.GetCustomerByIdAsync(toCustomerId);
        if (customerTo == null || !await IsRegisteredAsync(customerTo))
            return RedirectToAction(nameof(Index));

        var model = new SendPrivateMessageModel { ToCustomerId = customerTo.Id, CustomerToName = customerTo.Email };

        if (replyToMessageId.HasValue)
        {
            var replyTo = await forumService.GetPrivateMessageByIdAsync(replyToMessageId.Value);
            if (replyTo != null &&
                (replyTo.ToCustomerId == customer.Id || replyTo.FromCustomerId == customer.Id))
            {
                model.ReplyToMessageId = replyTo.Id;
                model.Subject = replyTo.Subject?.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) == true
                    ? replyTo.Subject
                    : $"Re: {replyTo.Subject}";
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPM(SendPrivateMessageModel model)
    {
        if (!forumSettings.AllowPrivateMessages)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        // Resolve recipient
        Customer? toCustomer = null;
        PrivateMessage? replyToPM = null;

        if (model.ReplyToMessageId > 0)
        {
            replyToPM = await forumService.GetPrivateMessageByIdAsync(model.ReplyToMessageId);
            if (replyToPM != null &&
                (replyToPM.ToCustomerId == customer.Id || replyToPM.FromCustomerId == customer.Id))
            {
                toCustomer = await customerService.GetCustomerByIdAsync(
                    replyToPM.FromCustomerId == customer.Id ? replyToPM.ToCustomerId : replyToPM.FromCustomerId);
            }
            else
            {
                return RedirectToAction(nameof(Index));
            }
        }
        else
        {
            toCustomer = await customerService.GetCustomerByIdAsync(model.ToCustomerId);
        }

        if (toCustomer == null || !await IsRegisteredAsync(toCustomer))
            return RedirectToAction(nameof(Index));

        if (ModelState.IsValid)
        {
            try
            {
                var subject = model.Subject ?? string.Empty;
                if (forumSettings.PMSubjectMaxLength > 0 && subject.Length > forumSettings.PMSubjectMaxLength)
                    subject = subject[..forumSettings.PMSubjectMaxLength];

                var text = model.Message ?? string.Empty;
                if (forumSettings.PMTextMaxLength > 0 && text.Length > forumSettings.PMTextMaxLength)
                    text = text[..forumSettings.PMTextMaxLength];

                var pm = new PrivateMessage
                {
                    StoreId = storeContext.CurrentStore.Id,
                    ToCustomerId = toCustomer.Id,
                    FromCustomerId = customer.Id,
                    Subject = subject,
                    Text = text,
                    IsDeletedByAuthor = false,
                    IsDeletedByRecipient = false,
                    IsRead = false,
                    CreatedOnUtc = DateTime.UtcNow
                };

                await forumService.InsertPrivateMessageAsync(pm);

                customerActivityService.InsertActivity("PublicStore.SendPM",
                    await localizationService.GetResourceAsync("ActivityLog.PublicStore.SendPM"),
                    toCustomer.Email ?? string.Empty);

                return RedirectToAction(nameof(Index), new { tab = "sent" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
        }

        // Re-display form on error
        model.ToCustomerId = toCustomer.Id;
        model.CustomerToName = toCustomer.Email;
        return View(model);
    }

    // --- View PM ---

    public async Task<IActionResult> ViewPM(int privateMessageId)
    {
        if (!forumSettings.AllowPrivateMessages)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var pm = await forumService.GetPrivateMessageByIdAsync(privateMessageId);
        if (pm == null || (pm.ToCustomerId != customer.Id && pm.FromCustomerId != customer.Id))
            return RedirectToAction(nameof(Index));

        if (!pm.IsRead && pm.ToCustomerId == customer.Id)
        {
            pm.IsRead = true;
            await forumService.UpdatePrivateMessageAsync(pm);
        }

        var model = new PrivateMessageModel
        {
            Id = pm.Id,
            FromCustomerId = pm.FromCustomerId,
            ToCustomerId = pm.ToCustomerId,
            Subject = pm.Subject,
            Message = FormatText(pm.Text),
            CreatedOn = pm.CreatedOnUtc,
            IsRead = pm.IsRead
        };

        return View(model);
    }

    // --- Delete PM ---

    public async Task<IActionResult> DeletePM(int privateMessageId)
    {
        if (!forumSettings.AllowPrivateMessages)
            return RedirectToAction("Index", "Home");

        var customer = workContext.CurrentCustomer;
        if (!await IsRegisteredAsync(customer))
            return Challenge();

        var pm = await forumService.GetPrivateMessageByIdAsync(privateMessageId);
        if (pm != null)
        {
            if (pm.FromCustomerId == customer.Id)
            {
                pm.IsDeletedByAuthor = true;
                await forumService.UpdatePrivateMessageAsync(pm);
            }

            if (pm.ToCustomerId == customer.Id)
            {
                pm.IsDeletedByRecipient = true;
                await forumService.UpdatePrivateMessageAsync(pm);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // --- Helpers ---

    private async Task<bool> IsRegisteredAsync(Customer customer)
    {
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(
            SystemCustomerRoleNames.Registered);
        if (registeredRole == null) return false;
        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        return roleIds.Contains(registeredRole.Id);
    }

    private static PrivateMessageListModel MapMessageList(IPagedList<PrivateMessage> messages, int currentPage)
    {
        var model = new PrivateMessageListModel
        {
            CurrentPage = currentPage,
            TotalPages = messages.TotalPages
        };

        foreach (var pm in messages)
        {
            model.Messages.Add(new PrivateMessageModel
            {
                Id = pm.Id,
                FromCustomerId = pm.FromCustomerId,
                ToCustomerId = pm.ToCustomerId,
                Subject = pm.Subject,
                CreatedOn = pm.CreatedOnUtc,
                IsRead = pm.IsRead
            });
        }

        return model;
    }

    private static string FormatText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return WebUtility.HtmlEncode(text).Replace("\n", "<br />");
    }
}
