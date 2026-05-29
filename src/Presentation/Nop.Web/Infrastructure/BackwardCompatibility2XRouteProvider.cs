using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class BackwardCompatibility2XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Backward compatibility routes will be configured when views are migrated
        }

        public int Priority => -1000;
    }
}
