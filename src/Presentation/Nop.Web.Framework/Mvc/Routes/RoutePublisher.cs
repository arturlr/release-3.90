using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;

namespace Nop.Web.Framework.Mvc.Routes
{
    /// <summary>
    /// Route publisher
    /// </summary>
    /// <remarks>
    /// Task 6.4: <c>RouteCollection</c> → <see cref="IEndpointRouteBuilder"/>. Discovery,
    /// the "ignore not-installed plugins" filter and the descending-priority ordering are
    /// unchanged.
    /// </remarks>
    public class RoutePublisher : IRoutePublisher
    {
        protected readonly ITypeFinder typeFinder;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="typeFinder"></param>
        public RoutePublisher(ITypeFinder typeFinder)
        {
            this.typeFinder = typeFinder;
        }

        /// <summary>
        /// Find a plugin descriptor by some type which is located into its assembly
        /// </summary>
        /// <param name="providerType">Provider type</param>
        /// <returns>Plugin descriptor</returns>
        protected virtual PluginDescriptor FindPlugin(Type providerType)
        {
            if (providerType == null)
                throw new ArgumentNullException("providerType");

            //NOTE (task 6.4): PluginManager.ReferencedPlugins is null until
            //PluginManager.Initialize() runs, which no longer happens automatically
            //(runtime deferral 1.1, owned by task 7.2). 3.90 could not observe this because
            //[PreApplicationStartMethod] guaranteed initialization before Application_Start.
            //Guard rather than NullReferenceException: with no plugin list, no provider can
            //be attributed to a plugin, so every discovered provider is treated as
            //"not from a plugin" and registered - which is the same outcome 3.90 produced
            //for the Nop.Web/Nop.Admin providers.
            var referencedPlugins = PluginManager.ReferencedPlugins;
            if (referencedPlugins == null)
                return null;

            foreach (var plugin in referencedPlugins)
            {
                if (plugin.ReferencedAssembly == null)
                    continue;

                if (plugin.ReferencedAssembly.FullName == providerType.Assembly.FullName)
                    return plugin;
            }

            return null;
        }

        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Endpoint route builder</param>
        public virtual void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            if (routeBuilder == null)
                throw new ArgumentNullException("routeBuilder");

            var routeProviderTypes = typeFinder.FindClassesOfType<IRouteProvider>();
            var routeProviders = new List<IRouteProvider>();
            foreach (var providerType in routeProviderTypes)
            {
                //Ignore not installed plugins
                var plugin = FindPlugin(providerType);
                if (plugin != null && !plugin.Installed)
                    continue;

                var provider = Activator.CreateInstance(providerType) as IRouteProvider;
                routeProviders.Add(provider);
            }
            routeProviders = routeProviders.OrderByDescending(rp => rp.Priority).ToList();
            routeProviders.ForEach(rp => rp.RegisterRoutes(routeBuilder));
        }
    }
}
