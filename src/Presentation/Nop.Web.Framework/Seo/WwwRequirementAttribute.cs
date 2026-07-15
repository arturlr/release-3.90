using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Seo;

namespace Nop.Web.Framework.Seo
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class WwwRequirementAttribute : Attribute, IAuthorizationFilter
    {
        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (!String.Equals(filterContext.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var seoSettings = filterContext.HttpContext.RequestServices.GetService<SeoSettings>();

            switch (seoSettings.WwwRequirement)
            {
                case WwwRequirement.WithWww:
                    {
                        var webHelper = filterContext.HttpContext.RequestServices.GetService<IWebHelper>();
                        string url = webHelper.GetThisPageUrl(true);
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            if (!url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("https://", "https://www.");
                                filterContext.Result = new RedirectResult(url, true);
                            }
                        }
                        else
                        {
                            if (!url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("http://", "http://www.");
                                filterContext.Result = new RedirectResult(url, true);
                            }
                        }
                    }
                    break;
                case WwwRequirement.WithoutWww:
                    {
                        var webHelper = filterContext.HttpContext.RequestServices.GetService<IWebHelper>();
                        string url = webHelper.GetThisPageUrl(true);
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            if (url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("https://www.", "https://");
                                filterContext.Result = new RedirectResult(url, true);
                            }
                        }
                        else
                        {
                            if (url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace("http://www.", "http://");
                                filterContext.Result = new RedirectResult(url, true);
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
