using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.ExternalAuth.Facebook
{
    /// <summary>
    /// Registers this plugin's two storefront routes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 11.1. The mechanical port runtime-deferrals.md §17.4a describes, applied twice:
    /// <c>RouteCollection</c> → <see cref="IEndpointRouteBuilder"/>, <c>MapRoute</c> →
    /// <c>MapControllerRoute</c>, and the <c>string[] namespaces</c> argument dropped (no ASP.NET
    /// Core counterpart — controller discovery is application-part based). Route names, URL
    /// patterns and defaults are 3.90's, unchanged.
    /// </para>
    /// <para>
    /// <b>Both names are load-bearing, and one of them twice over.</b>
    /// <c>Plugin.ExternalAuth.Facebook.Login</c> is resolved by
    /// <c>Url.RouteUrl(...)</c> in <c>Views/PublicInfo.cshtml</c> — it is the href of the login
    /// button itself, so a rename breaks the button with no compile error.
    /// <c>Plugin.ExternalAuth.Facebook.LoginCallback</c> is never named in code, but its
    /// <b>pattern</b> is: <c>FacebookProviderAuthorizer.GenerateLocalCallbackUri</c> builds
    /// <c>{store}plugins/externalauthFacebook/logincallback/</c> by hand and sends it to Facebook
    /// as <c>redirect_uri</c>, so the pattern must keep matching that string. Endpoint routing
    /// matches path segments case-insensitively, so the casing difference between the two is
    /// harmless — as it was in MVC 5.
    /// </para>
    /// <para>
    /// Both patterns are fully literal, so neither can tie on precedence with another and
    /// §17.4a gotcha 3's <c>AmbiguousMatchException</c> cannot arise. <c>Priority</c> stays
    /// 3.90's 0.
    /// </para>
    /// <para>
    /// <b>These endpoints only exist while the plugin is INSTALLED</b> — <c>RoutePublisher</c>
    /// skips an uninstalled plugin's provider, which is 3.90's own filter. A missing
    /// <c>/plugins/externalauthfacebook/login</c> on a store where the method has not been
    /// activated is that, not a porting defect.
    /// </para>
    /// </remarks>
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.ExternalAuth.Facebook.Login",
                 "Plugins/ExternalAuthFacebook/Login",
                 new { controller = "ExternalAuthFacebook", action = "Login" }
            );

            routeBuilder.MapControllerRoute("Plugin.ExternalAuth.Facebook.LoginCallback",
                 "Plugins/ExternalAuthFacebook/LoginCallback",
                 new { controller = "ExternalAuthFacebook", action = "LoginCallback" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
