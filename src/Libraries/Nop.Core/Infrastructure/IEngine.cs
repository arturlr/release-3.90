using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Classes implementing this interface can serve as a portal for the 
    /// various services composing the Nop engine. Edit functionality, modules
    /// and implementations access most Nop functionality through this 
    /// interface.
    /// </summary>
    public interface IEngine
    {
        /// <summary>
        /// Configure services for the application (called during startup)
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);

        /// <summary>
        /// Configure the HTTP request pipeline (called during startup)
        /// </summary>
        /// <param name="application">Application builder</param>
        void ConfigureRequestPipeline(IApplicationBuilder application);

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <typeparam name="T">Type of resolved service</typeparam>
        /// <returns>Resolved service</returns>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <param name="type">Type of resolved service</param>
        /// <returns>Resolved service</returns>
        object Resolve(Type type);

        /// <summary>
        /// Resolve all implementations of a service
        /// </summary>
        /// <typeparam name="T">Type of resolved services</typeparam>
        /// <returns>Collection of resolved services</returns>
        T[] ResolveAll<T>();

        /// <summary>
        /// Resolve unregistered service by attempting constructor injection
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <returns>Resolved service</returns>
        object ResolveUnregistered(Type type);

        /// <summary>
        /// Gets the service provider
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
