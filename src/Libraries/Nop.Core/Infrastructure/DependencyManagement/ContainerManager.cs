using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Core.Infrastructure.DependencyManagement
{
    /// <summary>
    /// Container manager - wraps IServiceProvider for service resolution.
    /// In ASP.NET Core, scoped resolution is handled by the framework's
    /// built-in request scope. This class provides a compatibility layer
    /// for code that still uses the service locator pattern.
    /// </summary>
    public class ContainerManager
    {
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="serviceProvider">The application service provider</param>
        public ContainerManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets or sets the service provider
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get => _serviceProvider;
            set => _serviceProvider = value;
        }

        /// <summary>
        /// Resolve a service
        /// </summary>
        /// <typeparam name="T">Type of service</typeparam>
        /// <param name="key">Optional key (not used in standard DI)</param>
        /// <returns>Resolved service</returns>
        public virtual T Resolve<T>(string key = "") where T : class
        {
            if (string.IsNullOrEmpty(key))
                return _serviceProvider.GetService<T>();

            // Keyed services support in .NET 8+
            return _serviceProvider.GetKeyedService<T>(key);
        }

        /// <summary>
        /// Resolve a service by type
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <returns>Resolved service</returns>
        public virtual object Resolve(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        /// <summary>
        /// Resolve all implementations of a service
        /// </summary>
        /// <typeparam name="T">Type of service</typeparam>
        /// <returns>All resolved services</returns>
        public virtual T[] ResolveAll<T>()
        {
            return _serviceProvider.GetServices<T>().ToArray();
        }

        /// <summary>
        /// Resolve an unregistered service by attempting constructor injection
        /// </summary>
        /// <typeparam name="T">Type of service</typeparam>
        /// <returns>Resolved service</returns>
        public virtual T ResolveUnregistered<T>() where T : class
        {
            return ResolveUnregistered(typeof(T)) as T;
        }

        /// <summary>
        /// Resolve an unregistered service by attempting constructor injection
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <returns>Resolved service</returns>
        public virtual object ResolveUnregistered(Type type)
        {
            Exception innerException = null;
            var constructors = type.GetConstructors();
            foreach (var constructor in constructors)
            {
                try
                {
                    var parameters = constructor.GetParameters();
                    var parameterInstances = new List<object>();
                    foreach (var parameter in parameters)
                    {
                        var service = Resolve(parameter.ParameterType);
                        if (service == null)
                            throw new NopException("Unknown dependency");
                        parameterInstances.Add(service);
                    }
                    return Activator.CreateInstance(type, parameterInstances.ToArray());
                }
                catch (Exception ex)
                {
                    innerException = ex;
                }
            }
            throw new NopException("No constructor was found that had all the dependencies satisfied.", innerException);
        }

        /// <summary>
        /// Try to resolve a service
        /// </summary>
        /// <param name="serviceType">Type of service</param>
        /// <param name="instance">Resolved instance</param>
        /// <returns>True if resolved successfully</returns>
        public virtual bool TryResolve(Type serviceType, out object instance)
        {
            instance = _serviceProvider.GetService(serviceType);
            return instance != null;
        }

        /// <summary>
        /// Check whether some service is registered (can be resolved)
        /// </summary>
        /// <param name="serviceType">Type of service</param>
        /// <returns>True if registered</returns>
        public virtual bool IsRegistered(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType) != null;
        }

        /// <summary>
        /// Resolve optional service (returns null if not registered)
        /// </summary>
        /// <param name="serviceType">Type of service</param>
        /// <returns>Resolved service or null</returns>
        public virtual object ResolveOptional(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }
}
