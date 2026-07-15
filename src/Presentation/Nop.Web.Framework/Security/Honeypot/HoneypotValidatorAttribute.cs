using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Domain.Security;
using Nop.Services.Logging;

namespace Nop.Web.Framework.Security.Honeypot
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class HoneypotValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            var securitySettings = filterContext.HttpContext.RequestServices.GetService<SecuritySettings>();
            if (securitySettings.HoneypotEnabled)
            {
                var form = filterContext.HttpContext.Request.HasFormContentType
                    ? filterContext.HttpContext.Request.Form : null;
                string inputValue = form != null ? form[securitySettings.HoneypotInputName].ToString() : null;

                var isBot = !String.IsNullOrWhiteSpace(inputValue);
                if (isBot)
                {
                    var logger = filterContext.HttpContext.RequestServices.GetService<ILogger>();
                    logger.Warning("A bot detected. Honeypot.");

                    var webHelper = filterContext.HttpContext.RequestServices.GetService<IWebHelper>();
                    string url = webHelper.GetThisPageUrl(true);
                    filterContext.Result = new RedirectResult(url);
                }
            }
        }
    }
}
