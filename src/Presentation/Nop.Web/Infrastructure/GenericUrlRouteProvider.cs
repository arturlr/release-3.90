using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class GenericUrlRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            // Generic URL routes will be configured when SEO-friendly URLs are migrated
        }

        public int Priority => -1000000;
    }
}
