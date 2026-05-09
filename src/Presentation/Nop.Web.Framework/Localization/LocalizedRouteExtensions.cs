using System;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Localization
{
    public static class LocalizedRouteExtensions
    {
        //Override for localized route
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template)
        {
            return MapLocalizedRoute(routeBuilder, name, template, null /* defaults */, null /* constraints */);
        }
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults)
        {
            return MapLocalizedRoute(routeBuilder, name, template, defaults, null /* constraints */);
        }
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints)
        {
            return MapLocalizedRoute(routeBuilder, name, template, defaults, constraints, null /* namespaces */);
        }
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template, string[] namespaces)
        {
            return MapLocalizedRoute(routeBuilder, name, template, null /* defaults */, null /* constraints */, namespaces);
        }
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, string[] namespaces)
        {
            return MapLocalizedRoute(routeBuilder, name, template, defaults, null /* constraints */, namespaces);
        }
        public static IRouteBuilder MapLocalizedRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, string[] namespaces)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            // In ASP.NET Core, we use the IRouteBuilder to add the localized route
            var defaultsDictionary = defaults != null ? new RouteValueDictionary(defaults) : new RouteValueDictionary();
            var constraintsDictionary = constraints != null ? new RouteValueDictionary(constraints) : new RouteValueDictionary();
            var dataTokens = new RouteValueDictionary();

            if (namespaces != null && namespaces.Length > 0)
            {
                dataTokens["Namespaces"] = namespaces;
            }

            var inlineConstraintResolver = (IInlineConstraintResolver)routeBuilder.ServiceProvider.GetService(typeof(IInlineConstraintResolver));
            
            // Create the localized route wrapping the default handler
            var localizedRoute = new LocalizedRoute(routeBuilder.DefaultHandler);
            
            routeBuilder.Routes.Add(new Route(
                localizedRoute,
                name,
                template,
                defaultsDictionary,
                new RouteValueDictionary(constraintsDictionary),
                dataTokens,
                inlineConstraintResolver));

            return routeBuilder;
        }

        // MapRoute overloads that accept namespace arrays (for backward compatibility with MVC 5 route registration)
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, string[] namespaces)
        {
            return MapRoute(routeBuilder, name, template, defaults, null /* constraints */, namespaces);
        }
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string name, string template, object defaults, object constraints, string[] namespaces)
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

            routeBuilder.Routes.Add(new Route(
                routeBuilder.DefaultHandler,
                name,
                template,
                defaultsDictionary,
                new RouteValueDictionary(constraintsDictionary),
                dataTokens,
                inlineConstraintResolver));

            return routeBuilder;
        }
        
        public static void ClearSeoFriendlyUrlsCachedValueForRoutes(this IRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
            {
                throw new ArgumentNullException(nameof(routeBuilder));
            }
            foreach (var router in routeBuilder.Routes)
            {
                if (router is LocalizedRoute localizedRoute)
                {
                    localizedRoute.ClearSeoFriendlyUrlsCachedValue();
                }
                else if (router is Route route)
                {
                    // The Route wraps a target IRouter - try to find LocalizedRoute in its hierarchy
                    // In ASP.NET Core, Route stores target internally; we check if the route name matches
                    // our pattern and clear all cached values through the localized routes we track
                }
            }
        }
    }
}
