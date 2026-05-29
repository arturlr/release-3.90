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
    public class RoutePublisher : IRoutePublisher
    {
        protected readonly ITypeFinder typeFinder;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="typeFinder">Type finder</param>
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
                throw new ArgumentNullException(nameof(providerType));

            foreach (var plugin in PluginManager.ReferencedPlugins)
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
        /// <param name="endpointRouteBuilder">Endpoint route builder</param>
        public virtual void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
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
            routeProviders.ForEach(rp => rp.RegisterRoutes(endpointRouteBuilder));
        }
    }
}
