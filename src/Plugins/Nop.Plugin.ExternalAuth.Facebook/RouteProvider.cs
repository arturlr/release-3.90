using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.ExternalAuth.Facebook
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ExternalAuth.Facebook.Login",
                pattern: "Plugins/ExternalAuthFacebook/Login",
                defaults: new { controller = "ExternalAuthFacebook", action = "Login" }
            );

            endpointRouteBuilder.MapControllerRoute(
                name: "Plugin.ExternalAuth.Facebook.LoginCallback",
                pattern: "Plugins/ExternalAuthFacebook/LoginCallback",
                defaults: new { controller = "ExternalAuthFacebook", action = "LoginCallback" }
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
