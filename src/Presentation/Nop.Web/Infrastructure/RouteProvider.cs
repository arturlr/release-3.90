using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Routes are configured in Program.cs via conventional routing
            // Named routes will be added here when views are migrated
        }

        public int Priority => 0;
    }
}
