using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    public class AdminValidateIpAddressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null || context.HttpContext == null)
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
                ok = true;
            }

            if (!ok)
            {
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var thisPageUrl = webHelper.GetThisPageUrl(false);
                if (!thisPageUrl.StartsWith(string.Format("{0}admin/security/accessdenied", webHelper.GetStoreLocation()), StringComparison.InvariantCultureIgnoreCase))
                {
                    context.Result = new RedirectResult(webHelper.GetStoreLocation() + "admin/security/accessdenied");
                }
            }
        }
    }
}
