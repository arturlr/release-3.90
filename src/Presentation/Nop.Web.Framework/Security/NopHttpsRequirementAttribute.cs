using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters.
    /// <c>FilterAttribute, IAuthorizationFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter</c>;
    /// <c>AuthorizationContext</c> -&gt; <see cref="AuthorizationFilterContext"/>;
    /// <c>Request.HttpMethod</c> -&gt; <c>Request.Method</c>; the <c>IsChildAction</c> guard is
    /// removed (View Components do not execute filters).
    /// SECURITY-RELEVANT: the SSL redirect decisions are unchanged, and still driven by
    /// <c>SecuritySettings.ForceSslForAllPages</c> / <c>Store.SslEnabled</c> /
    /// <c>IWebHelper.IsCurrentConnectionSecured()</c>. Note that
    /// <c>IsCurrentConnectionSecured()</c> reads <c>HttpRequest.IsHttps</c> behind a
    /// reverse proxy only if the host adds forwarded-headers middleware - task 6.4/7.2 owns that,
    /// and without it a TLS-terminating proxy makes every request look insecure, producing a
    /// redirect loop rather than an insecure page.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class NopHttpsRequirementAttribute : Attribute, IAuthorizationFilter
    {
        public NopHttpsRequirementAttribute(SslRequirement sslRequirement)
        {
            this.SslRequirement = sslRequirement;
        }
        public virtual void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            // only redirect for GET requests, 
            // otherwise the browser might not propagate the verb and request body correctly.
            if (!String.Equals(filterContext.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;
            
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;
            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (securitySettings.ForceSslForAllPages)
                //all pages are forced to be SSL no matter of the specified value
                this.SslRequirement = SslRequirement.Yes;
            
            switch (this.SslRequirement)
            {
                case SslRequirement.Yes:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (!currentConnectionSecured)
                        {
                            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                            if (storeContext.CurrentStore.SslEnabled)
                            {
                                //redirect to HTTPS version of page
                                string url = webHelper.GetThisPageUrl(true, true);

                                //301 (permanent) redirection
                                filterContext.Result = new RedirectResult(url, true);
                            }
                        }
                    }
                    break;
                case SslRequirement.No:
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var currentConnectionSecured = webHelper.IsCurrentConnectionSecured();
                        if (currentConnectionSecured)
                        {
                            //redirect to HTTP version of page
                            string url = webHelper.GetThisPageUrl(true, false);
                            //301 (permanent) redirection
                            filterContext.Result = new RedirectResult(url, true);
                        }
                    }
                    break;
                case SslRequirement.NoMatter:
                    {
                        //do nothing
                    }
                    break;
                default:
                    throw new NopException("Not supported SslProtected parameter");
            }
        }

        public SslRequirement SslRequirement { get; set; }
    }
}
