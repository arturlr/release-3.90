using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Services.Affiliates;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Security;
using Nop.Services.Topics;

namespace Nop.Web.Framework.Filters;

/// <summary>
/// Updates customer's LastActivityDateUtc on GET requests (throttled to once per minute).
/// </summary>
public sealed class CustomerLastActivityFilter(IWorkContext workContext, ICustomerService customerService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            return;

        var customer = workContext.CurrentCustomer;
        if (customer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
        {
            customer.LastActivityDateUtc = DateTime.UtcNow;
            await customerService.UpdateCustomerAsync(customer);
        }
    }
}

/// <summary>
/// Redirects to "StoreClosed" page when store is closed, unless user has AccessClosedStore permission
/// or the topic is marked AccessibleWhenStoreClosed.
/// </summary>
public sealed class StoreClosedFilter(
    StoreInformationSettings storeInformationSettings,
    IPermissionService permissionService,
    ITopicService topicService,
    IStoreContext storeContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!storeInformationSettings.StoreClosed)
        {
            await next();
            return;
        }

        // Topics accessible when store is closed
        var routeController = context.RouteData.Values["controller"]?.ToString();
        var routeAction = context.RouteData.Values["action"]?.ToString();
        if (string.Equals(routeController, "Topic", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(routeAction, "TopicDetails", StringComparison.OrdinalIgnoreCase))
        {
            var topics = await topicService.GetAllTopicsAsync(storeContext.CurrentStore.Id);
            var allowedTopicIds = topics.Where(t => t.AccessibleWhenStoreClosed).Select(t => t.Id).ToHashSet();
            if (context.RouteData.Values.TryGetValue("topicId", out var topicIdObj) &&
                int.TryParse(topicIdObj?.ToString(), out var topicId) &&
                allowedTopicIds.Contains(topicId))
            {
                await next();
                return;
            }
        }

        if (permissionService.Authorize(StandardPermissionProvider.AccessClosedStore))
        {
            await next();
            return;
        }

        context.Result = new RedirectToRouteResult("StoreClosed", null);
    }
}

/// <summary>
/// Stores the customer's current IP address on GET requests.
/// </summary>
public sealed class StoreIpAddressFilter(IWorkContext workContext, IWebHelper webHelper, ICustomerService customerService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            return;

        var currentIpAddress = webHelper.GetCurrentIpAddress();
        if (!string.IsNullOrEmpty(currentIpAddress))
        {
            var customer = workContext.CurrentCustomer;
            if (!string.Equals(currentIpAddress, customer.LastIpAddress, StringComparison.OrdinalIgnoreCase))
            {
                customer.LastIpAddress = currentIpAddress;
                await customerService.UpdateCustomerAsync(customer);
            }
        }
    }
}

/// <summary>
/// Stores the last visited page URL in a generic attribute on GET requests.
/// </summary>
public sealed class StoreLastVisitedPageFilter(
    IWorkContext workContext,
    IWebHelper webHelper,
    IGenericAttributeService genericAttributeService,
    CustomerSettings customerSettings) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
            return;

        if (!customerSettings.StoreLastVisitedPage)
            return;

        var pageUrl = webHelper.GetThisPageUrl(true);
        if (string.IsNullOrEmpty(pageUrl))
            return;

        var customer = workContext.CurrentCustomer;
        var previousPageUrl = await customer.GetAttributeAsync<string>(
            SystemCustomerAttributeNames.LastVisitedPage, genericAttributeService);

        if (!string.Equals(pageUrl, previousPageUrl, StringComparison.Ordinal))
            await genericAttributeService.SaveAttributeAsync(customer, SystemCustomerAttributeNames.LastVisitedPage, pageUrl);
    }
}

/// <summary>
/// Ensures URL contains language SEO code when SeoFriendlyUrlsForLanguagesEnabled is true.
/// Deferred — requires localized URL routing infrastructure. Currently a no-op placeholder.
/// </summary>
public sealed class LanguageSeoCodeFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Language SEO code enforcement requires localized URL routing infrastructure
        // (LocalizedRoute, LocalizedUrlExtensions) which will be built with the routing system.
        // For now, pass through.
        await next();
    }
}

/// <summary>
/// Checks PublicStoreAllowNavigation permission. Returns 403 if not authorized.
/// </summary>
public sealed class PublicStoreAllowNavigationFilter(IPermissionService permissionService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (permissionService.Authorize(StandardPermissionProvider.PublicStoreAllowNavigation))
        {
            await next();
            return;
        }

        context.Result = new StatusCodeResult(403);
    }
}

/// <summary>
/// Redirects to ChangePassword page when the customer's password has expired.
/// </summary>
public sealed class ValidatePasswordFilter(
    IWorkContext workContext,
    ICustomerService customerService,
    CustomerSettings customerSettings) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var actionName = context.RouteData.Values["action"]?.ToString();
        if (string.Equals(actionName, "ChangePassword", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        if (customerSettings.PasswordLifetime <= 0)
        {
            await next();
            return;
        }

        var customer = workContext.CurrentCustomer;

        // Only check registered customers
        var registeredRole = await customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
        if (registeredRole == null)
        {
            await next();
            return;
        }

        var roleIds = await customerService.GetCustomerRoleIdsAsync(customer);
        if (!roleIds.Contains(registeredRole.Id))
        {
            await next();
            return;
        }

        var currentPassword = await customerService.GetCurrentPasswordAsync(customer.Id);
        if (currentPassword == null)
        {
            await next();
            return;
        }

        if (currentPassword.CreatedOnUtc.AddDays(customerSettings.PasswordLifetime) < DateTime.UtcNow)
        {
            context.Result = new RedirectToRouteResult("CustomerChangePassword", null);
            return;
        }

        await next();
    }
}

/// <summary>
/// Checks for affiliate ID or friendly URL name in query string and associates the customer with the affiliate.
/// </summary>
public sealed class CheckAffiliateFilter(
    IWorkContext workContext,
    IAffiliateService affiliateService,
    ICustomerService customerService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        var request = context.HttpContext.Request;
        Affiliate? affiliate = null;

        // Try by ID
        if (request.Query.TryGetValue("affiliateid", out var affiliateIdStr) &&
            int.TryParse(affiliateIdStr, out var affiliateId) && affiliateId > 0)
        {
            affiliate = await affiliateService.GetAffiliateByIdAsync(affiliateId);
        }
        // Try by friendly URL name
        else if (request.Query.TryGetValue("affiliate", out var friendlyUrlName) &&
                 !string.IsNullOrEmpty(friendlyUrlName))
        {
            affiliate = await affiliateService.GetAffiliateByFriendlyUrlNameAsync(friendlyUrlName!);
        }

        if (affiliate is { Deleted: false, Active: true })
        {
            var customer = workContext.CurrentCustomer;
            if (customer.AffiliateId != affiliate.Id)
            {
                customer.AffiliateId = affiliate.Id;
                await customerService.UpdateCustomerAsync(customer);
            }
        }
    }
}
