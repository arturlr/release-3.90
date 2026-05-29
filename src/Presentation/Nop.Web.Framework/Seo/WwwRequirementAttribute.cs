using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Seo;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Seo
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class WwwRequirementAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            // only redirect for GET requests
            if (!string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            // ignore for localhost
            var connection = context.HttpContext.Connection;
            if (connection.RemoteIpAddress != null && connection.RemoteIpAddress.ToString() == "127.0.0.1")
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var seoSettings = EngineContext.Current.Resolve<SeoSettings>();

            switch (seoSettings.WwwRequirement)
            {
                case WwwRequirement.WithWww:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        string url = webHelper.GetThisPageUrl(true);
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            if (!url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("https://", "https://www.");
                                context.Result = new RedirectResult(url, true);
                            }
                        }
                        else
                        {
                            if (!url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("http://", "http://www.");
                                context.Result = new RedirectResult(url, true);
                            }
                        }
                    }
                    break;
                case WwwRequirement.WithoutWww:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        string url = webHelper.GetThisPageUrl(true);
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            if (url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("https://www.", "https://");
                                context.Result = new RedirectResult(url, true);
                            }
                        }
                        else
                        {
                            if (url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("http://www.", "http://");
                                context.Result = new RedirectResult(url, true);
                            }
                        }
                    }
                    break;
                case WwwRequirement.NoMatter:
                    break;
                default:
                    throw new NopException("Not supported WwwRequirement parameter");
            }
        }
    }
}
