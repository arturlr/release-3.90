using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Seo
{
    public static class GenericPathRouteExtensions
    {
        public static IEndpointRouteBuilder MapGenericPathRoute(this IEndpointRouteBuilder endpoints, string name, string pattern, object defaults = null, object constraints = null)
        {
            if (endpoints == null)
                throw new ArgumentNullException("endpoints");
            if (pattern == null)
                throw new ArgumentNullException("pattern");

            endpoints.MapControllerRoute(name, pattern, defaults, constraints);
            return endpoints;
        }

        public static IApplicationBuilder UseGenericPathRouteMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GenericPathRouteMiddleware>();
        }
    }
}
