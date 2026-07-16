using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Admin
{
    /// <summary>
    /// Represents the Admin area route provider for ASP.NET Core
    /// </summary>
    public class AdminAreaRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(
                name: "Admin_default",
                pattern: "Admin/{controller=Home}/{action=Index}/{id?}",
                defaults: new { area = "Admin" });
        }

        public int Priority
        {
            get { return 1000; } // Admin routes should be registered with high priority
        }
    }
}
