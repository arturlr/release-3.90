using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC filters. SECURITY-SENSITIVE: the IP allow-list logic
    /// is unchanged, including the "empty list means no restriction" default and the
    /// already-on-the-access-denied-page guard that prevents a redirect loop.
    /// <c>System.Web.Mvc.ActionFilterAttribute</c> -&gt;
    /// <c>Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute</c>;
    /// <c>HttpRequestBase</c> -&gt; <see cref="HttpRequest"/>; the <c>IsChildAction</c> guard is
    /// removed (View Components do not execute action filters). The IP itself comes from
    /// <c>IWebHelper.GetCurrentIpAddress()</c>, already re-based on ASP.NET Core in task 2.4.
    /// </summary>
    public class AdminValidateIpAddressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            HttpRequest request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            bool ok = false;
            var ipAddresses = EngineContext.Current.Resolve<SecuritySettings>().AdminAreaAllowedIpAddresses;
            if (ipAddresses != null && ipAddresses.Any())
            {
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                foreach (string ip in ipAddresses)
                    if (ip.Equals(webHelper.GetCurrentIpAddress(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        ok = true;
                        break;
                    }
            }
            else
            {
                //no restrictions
                ok = true;
            }

            if (!ok)
            {
                //ensure that it's not 'Access denied' page
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var thisPageUrl = webHelper.GetThisPageUrl(false);
                if (!thisPageUrl.StartsWith(string.Format("{0}admin/security/accessdenied", webHelper.GetStoreLocation()), StringComparison.InvariantCultureIgnoreCase))
                {
                    //redirect to 'Access denied' page
                    filterContext.Result = new RedirectResult(webHelper.GetStoreLocation() + "admin/security/accessdenied");
                    //filterContext.Result = RedirectToAction("AccessDenied", "Security");
                }
            }
        }
    }
}
