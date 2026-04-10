using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Models.Newsletter;

namespace Nop.Web.Controllers;

public class NewsletterController(
    ILocalizationService localizationService,
    IWorkContext workContext,
    INewsLetterSubscriptionService newsLetterSubscriptionService,
    IWorkflowMessageService workflowMessageService,
    IStoreContext storeContext,
    CustomerSettings customerSettings) : BasePublicController
{
    public IActionResult NewsletterBox()
    {
        if (customerSettings.HideNewsletterBlock)
            return Content("");

        var model = new NewsletterBoxModel
        {
            AllowToUnsubscribe = customerSettings.NewsletterBlockAllowToUnsubscribe
        };
        return PartialView(model);
    }

    [HttpPost]
    public async Task<IActionResult> SubscribeNewsletter(string email, bool subscribe)
    {
        string result;
        var success = false;

        if (!CommonHelper.IsValidEmail(email))
        {
            result = await localizationService.GetResourceAsync("Newsletter.Email.Wrong");
        }
        else
        {
            email = email.Trim();
            var storeId = storeContext.CurrentStore.Id;
            var subscription = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByEmailAndStoreIdAsync(email, storeId);

            if (subscription != null)
            {
                if (subscribe)
                {
                    if (!subscription.Active)
                        await workflowMessageService.SendNewsLetterSubscriptionActivationMessageAsync(subscription, workContext.WorkingLanguage.Id);
                    result = await localizationService.GetResourceAsync("Newsletter.SubscribeEmailSent");
                }
                else
                {
                    if (subscription.Active)
                        await workflowMessageService.SendNewsLetterSubscriptionDeactivationMessageAsync(subscription, workContext.WorkingLanguage.Id);
                    result = await localizationService.GetResourceAsync("Newsletter.UnsubscribeEmailSent");
                }
            }
            else if (subscribe)
            {
                subscription = new NewsLetterSubscription
                {
                    NewsLetterSubscriptionGuid = Guid.NewGuid(),
                    Email = email,
                    Active = false,
                    StoreId = storeId,
                    CreatedOnUtc = DateTime.UtcNow
                };
                await newsLetterSubscriptionService.InsertNewsLetterSubscriptionAsync(subscription);
                await workflowMessageService.SendNewsLetterSubscriptionActivationMessageAsync(subscription, workContext.WorkingLanguage.Id);
                result = await localizationService.GetResourceAsync("Newsletter.SubscribeEmailSent");
            }
            else
            {
                result = await localizationService.GetResourceAsync("Newsletter.UnsubscribeEmailSent");
            }

            success = true;
        }

        return Json(new { Success = success, Result = result });
    }

    public async Task<IActionResult> SubscriptionActivation(Guid token, bool active)
    {
        var subscription = await newsLetterSubscriptionService.GetNewsLetterSubscriptionByGuidAsync(token);
        if (subscription == null)
            return RedirectToAction("Index", "Home");

        if (active)
        {
            subscription.Active = true;
            await newsLetterSubscriptionService.UpdateNewsLetterSubscriptionAsync(subscription);
        }
        else
        {
            await newsLetterSubscriptionService.DeleteNewsLetterSubscriptionAsync(subscription);
        }

        var model = new SubscriptionActivationModel
        {
            Result = active
                ? await localizationService.GetResourceAsync("Newsletter.ResultActivated")
                : await localizationService.GetResourceAsync("Newsletter.ResultDeactivated")
        };
        return View(model);
    }
}
