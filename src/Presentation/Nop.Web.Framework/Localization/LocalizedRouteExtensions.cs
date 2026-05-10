using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Localization
{
    public static class LocalizedRouteExtensions
    {
        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template)
        {
            return MapLocalizedRoute(endpointRouteBuilder, name, template, null, null);
        }

        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults)
        {
            return MapLocalizedRoute(endpointRouteBuilder, name, template, defaults, null);
        }

        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, object constraints)
        {
            return MapLocalizedRoute(endpointRouteBuilder, name, template, defaults, constraints, null);
        }

        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, string[] namespaces)
        {
            return MapLocalizedRoute(endpointRouteBuilder, name, template, null, null, namespaces);
        }

        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, string[] namespaces)
        {
            return MapLocalizedRoute(endpointRouteBuilder, name, template, defaults, null, namespaces);
        }

        public static IEndpointRouteBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, object constraints, string[] namespaces)
        {
            if (endpointRouteBuilder == null)
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var conventionBuilder = endpointRouteBuilder.MapControllerRoute(
                name: name,
                pattern: template,
                defaults: defaults,
                constraints: constraints);

            return endpointRouteBuilder;
        }

        public static IEndpointRouteBuilder MapRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults)
        {
            if (endpointRouteBuilder == null)
                throw new ArgumentNullException(nameof(endpointRouteBuilder));

            endpointRouteBuilder.MapControllerRoute(name: name, pattern: template, defaults: defaults);
            return endpointRouteBuilder;
        }

        public static IEndpointRouteBuilder MapRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, string[] namespaces)
        {
            return MapRoute(endpointRouteBuilder, name, template, defaults, null, namespaces);
        }

        public static IEndpointRouteBuilder MapRoute(this IEndpointRouteBuilder endpointRouteBuilder, string name, string template, object defaults, object constraints, string[] namespaces)
        {
            if (endpointRouteBuilder == null)
                throw new ArgumentNullException(nameof(endpointRouteBuilder));
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            endpointRouteBuilder.MapControllerRoute(
                name: name,
                pattern: template,
                defaults: defaults,
                constraints: constraints);

            return endpointRouteBuilder;
        }
    }
}
