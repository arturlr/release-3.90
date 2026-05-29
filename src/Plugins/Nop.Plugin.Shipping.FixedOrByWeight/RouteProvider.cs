using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Shipping.FixedOrByWeight
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Endpoint routing is configured via attribute routing in .NET Core
        }

        public int Priority
        {
            get { return 0; }
        }
    }
}
