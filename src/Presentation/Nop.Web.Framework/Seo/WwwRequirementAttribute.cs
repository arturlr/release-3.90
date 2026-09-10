using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Seo;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters.
    /// <list type="bullet">
    /// <item><c>FilterAttribute, IAuthorizationFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter</c>;
    /// <c>AuthorizationContext</c> -&gt; <see cref="AuthorizationFilterContext"/>;
    /// <c>Request.HttpMethod</c> -&gt; <c>Request.Method</c>; the <c>IsChildAction</c> guard is
    /// removed (View Components do not execute filters).</item>
    /// <item><c>HttpRequestBase.IsLocal</c> has NO ASP.NET Core equivalent. It is reproduced with
    /// <see cref="IsLocalRequest"/> below, which applies the same two tests System.Web used:
    /// the remote address is a loopback address, or the remote address equals the local
    /// address.</item>
    /// </list>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class WwwRequirementAttribute : Attribute, IAuthorizationFilter
    {
        /// <summary>
        /// Replacement for <c>System.Web.HttpRequestBase.IsLocal</c>
        /// </summary>
        private static bool IsLocalRequest(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var connection = httpContext.Connection;
            if (connection == null)
                return false;

            var remoteIp = connection.RemoteIpAddress;
            if (remoteIp == null)
            {
                //no remote address at all - System.Web treated an in-process request as local
                return true;
            }

            if (IPAddress.IsLoopback(remoteIp))
                return true;

            var localIp = connection.LocalIpAddress;
            return localIp != null && remoteIp.Equals(localIp);
        }

        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            // only redirect for GET requests, 
            // otherwise the browser might not propagate the verb and request body correctly.
            if (!String.Equals(filterContext.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            //ignore this rule for localhost
            if (IsLocalRequest(filterContext.HttpContext))
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
                        bool startsWith3W = url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase);
                        if (!startsWith3W)
                        {
                            url = url.Replace("https://", "https://www.");

                            //301 (permanent) redirection
                            filterContext.Result = new RedirectResult(url, true);
                        }
                    }
                    else
                    {
                        bool startsWith3W = url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase);
                        if (!startsWith3W)
                        {
                            url = url.Replace("http://", "http://www.");

                            //301 (permanent) redirection
                            filterContext.Result = new RedirectResult(url, true);
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
                        bool startsWith3W = url.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase);
                        if (startsWith3W)
                        {
                            url = url.Replace("https://www.", "https://");

                            //301 (permanent) redirection
                            filterContext.Result = new RedirectResult(url, true);
                        }
                    }
                    else
                    {
                        bool startsWith3W = url.StartsWith("http://www.", StringComparison.OrdinalIgnoreCase);
                        if (startsWith3W)
                        {
                            url = url.Replace("http://www.", "http://");

                            //301 (permanent) redirection
                            filterContext.Result = new RedirectResult(url, true);
                        }
                    }
                }
                    break;
                case WwwRequirement.NoMatter:
                {
                    //do nothing
                }
                break;
                default:
                    throw new NopException("Not supported WwwRequirement parameter");
            }
        }
    }
}
