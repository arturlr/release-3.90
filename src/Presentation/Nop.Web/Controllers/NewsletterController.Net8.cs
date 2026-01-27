using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class NewsletterController : BasePublicController
    {
        private readonly INewsLetterSubscriptionService _subscriptionService;

        public NewsletterController(
            IWorkContext workContext,
            INewsLetterSubscriptionService subscriptionService) : base(workContext)
        {
            _subscriptionService = subscriptionService;
        }

        // POST: /Newsletter/Subscribe
        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Email is required" });

            var existing = await _subscriptionService.GetNewsLetterSubscriptionByEmailAsync(email);
            if (existing != null && existing.Active)
                return Json(new { success = false, message = "Already subscribed" });

            var subscription = new Core.Domain.Messages.NewsLetterSubscription
            {
                Email = email,
                Active = true,
                CreatedOnUtc = DateTime.UtcNow,
                NewsLetterSubscriptionGuid = Guid.NewGuid()
            };

            await _subscriptionService.InsertNewsLetterSubscriptionAsync(subscription);
            return Json(new { success = true, message = "Subscribed successfully" });
        }

        // GET: /Newsletter/Unsubscribe
        public async Task<IActionResult> Unsubscribe(string email)
        {
            var subscription = await _subscriptionService.GetNewsLetterSubscriptionByEmailAsync(email);
            if (subscription != null)
            {
                subscription.Active = false;
                await _subscriptionService.UpdateNewsLetterSubscriptionAsync(subscription);
            }

            return Content("Unsubscribed successfully");
        }
    }
}
