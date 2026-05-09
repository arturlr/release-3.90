using System;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Seo
{
    public static class GenericPathRouteExtensions
    {
        //Override for generic path route
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template)
        {
            return MapGenericPathRoute(routeBuilder, name, template, null /* defaults */, null /* constraints */);
        }
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults)
        {
            return MapGenericPathRoute(routeBuilder, name, template, defaults, null /* constraints */);
        }
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints)
        {
            return MapGenericPathRoute(routeBuilder, name, template, defaults, constraints, null /* namespaces */);
        }
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template, string[] namespaces)
        {
            return MapGenericPathRoute(routeBuilder, name, template, null /* defaults */, null /* constraints */, namespaces);
        }
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, string[] namespaces)
        {
            return MapGenericPathRoute(routeBuilder, name, template, defaults, null /* constraints */, namespaces);
        }
        public static IRouteBuilder MapGenericPathRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, string[] namespaces)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var defaultsDictionary = defaults != null ? new RouteValueDictionary(defaults) : new RouteValueDictionary();
            var constraintsDictionary = constraints != null ? new RouteValueDictionary(constraints) : new RouteValueDictionary();
            var dataTokens = new RouteValueDictionary();

            if (namespaces != null && namespaces.Length > 0)
            {
                dataTokens["Namespaces"] = namespaces;
            }

            var inlineConstraintResolver = (IInlineConstraintResolver)routeBuilder.ServiceProvider.GetService(typeof(IInlineConstraintResolver));

            // Create the generic path route wrapping the default handler
            var genericPathRoute = new GenericPathRoute(routeBuilder.DefaultHandler);

            routeBuilder.Routes.Add(new Route(
                genericPathRoute,
                name,
                template,
                defaultsDictionary,
                new RouteValueDictionary(constraintsDictionary),
                dataTokens,
                inlineConstraintResolver));

            return routeBuilder;
        }
    }
}
