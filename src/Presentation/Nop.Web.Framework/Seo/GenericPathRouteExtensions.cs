using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Seo
{
    public static class GenericPathRouteExtensions
    {
        public static IEndpointRouteBuilder MapGenericPathRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template)
        {
            return MapGenericPathRoute(endpointRouteBuilder, name, template, null, null);
        }

        public static IEndpointRouteBuilder MapGenericPathRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults)
        {
            return MapGenericPathRoute(endpointRouteBuilder, name, template, defaults, null);
        }

        public static IEndpointRouteBuilder MapGenericPathRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, object constraints)
        {
            if (endpointRouteBuilder == null)
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            // Ignore constraints if it's a string array (MVC5 namespaces pattern)
            object routeConstraints = null;
            if (constraints != null && constraints is not string[] && constraints is not Array)
                routeConstraints = constraints;

            endpointRouteBuilder.MapControllerRoute(
                name: name,
                pattern: template,
                defaults: defaults,
                constraints: routeConstraints);

            return endpointRouteBuilder;
        }
    }
}
