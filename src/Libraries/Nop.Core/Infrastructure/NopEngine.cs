using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Infrastructure.Mapper;

namespace Nop.Core.Infrastructure
{
    /// <summary>
    /// Represents the Nop engine that provides access to the application's services
    /// </summary>
    public class NopEngine : IEngine
    {
        #region Fields

        private IServiceProvider _serviceProvider;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the service provider
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider;

        #endregion

        #region Utilities

        /// <summary>
        /// Get IServiceProvider
        /// </summary>
        /// <returns>IServiceProvider</returns>
        protected IServiceProvider GetServiceProvider()
        {
            // Try to get the current HttpContext service provider for scoped services
            if (ServiceProvider == null)
                return null;

            var accessor = ServiceProvider.GetService<IHttpContextAccessor>();
            var context = accessor?.HttpContext;
            return context?.RequestServices ?? ServiceProvider;
        }

        /// <summary>
        /// Run startup tasks
        /// </summary>
        protected virtual void RunStartupTasks()
        {
            var typeFinder = Resolve<ITypeFinder>();
            var startUpTaskTypes = typeFinder.FindClassesOfType<IStartupTask>();
            var startUpTasks = new List<IStartupTask>();

            foreach (var startUpTaskType in startUpTaskTypes)
                startUpTasks.Add((IStartupTask)Activator.CreateInstance(startUpTaskType));

            // Sort by order
            startUpTasks = startUpTasks.OrderBy(st => st.Order).ToList();

            foreach (var startUpTask in startUpTasks)
                startUpTask.Execute();
        }

        /// <summary>
        /// Register dependencies using Autofac
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="nopConfig">Nop configuration</param>
        protected virtual void RegisterDependencies(IServiceCollection services, ITypeFinder typeFinder, NopConfig nopConfig)
        {
            // Create an Autofac container builder
            var builder = new ContainerBuilder();

            // Register engine and config
            builder.RegisterInstance(this).As<IEngine>().SingleInstance();
            builder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();
            builder.RegisterInstance(nopConfig).As<NopConfig>().SingleInstance();

            // Find dependency registrars provided by other assemblies
            var drTypes = typeFinder.FindClassesOfType<IDependencyRegistrar>();
            var drInstances = new List<IDependencyRegistrar>();
            foreach (var drType in drTypes)
                drInstances.Add((IDependencyRegistrar)Activator.CreateInstance(drType));

            // Sort
            drInstances = drInstances.OrderBy(t => t.Order).ToList();

            // Register all dependencies
            foreach (var dependencyRegistrar in drInstances)
                dependencyRegistrar.Register(builder, typeFinder, nopConfig);

            // Populate Autofac container from IServiceCollection
            builder.Populate(services);

            // Build the container and create the service provider
            var container = builder.Build();
            _serviceProvider = new AutofacServiceProvider(container);
        }

        /// <summary>
        /// Register and configure AutoMapper
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="typeFinder">Type finder</param>
        protected virtual void RegisterMapperConfiguration(IServiceCollection services, ITypeFinder typeFinder)
        {
            // Find mapper configurations provided by other assemblies
            var mcTypes = typeFinder.FindClassesOfType<IMapperConfiguration>();
            var mcInstances = new List<IMapperConfiguration>();
            foreach (var mcType in mcTypes)
                mcInstances.Add((IMapperConfiguration)Activator.CreateInstance(mcType));

            // Sort
            mcInstances = mcInstances.OrderBy(t => t.Order).ToList();

            // Get Profile instances and initialize (AutoMapper 13+ pattern)
            var profiles = mcInstances
                .Select(mc => mc.GetProfile())
                .ToList();

            AutoMapperConfiguration.Init(profiles);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Configure services for the application
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Bind NopConfig from configuration
            var nopConfig = new NopConfig();
            configuration.GetSection("Nop").Bind(nopConfig);

            // Create type finder
            var typeFinder = new WebAppTypeFinder();

            // Register mapper configurations
            RegisterMapperConfiguration(services, typeFinder);

            // Register dependencies
            RegisterDependencies(services, typeFinder, nopConfig);

            // Run startup tasks
            if (!nopConfig.IgnoreStartupTasks)
            {
                RunStartupTasks();
            }
        }

        /// <summary>
        /// Configure the HTTP request pipeline
        /// </summary>
        /// <param name="application">Application builder</param>
        public void ConfigureRequestPipeline(IApplicationBuilder application)
        {
            // Update service provider to use the app's built provider
            _serviceProvider = application.ApplicationServices;
        }

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <typeparam name="T">Type of resolved service</typeparam>
        /// <returns>Resolved service</returns>
        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Resolve dependency
        /// </summary>
        /// <param name="type">Type of resolved service</param>
        /// <returns>Resolved service</returns>
        public object Resolve(Type type)
        {
            var sp = GetServiceProvider();
            return sp?.GetService(type);
        }

        /// <summary>
        /// Resolve all implementations of a service
        /// </summary>
        /// <typeparam name="T">Type of resolved services</typeparam>
        /// <returns>Collection of resolved services</returns>
        public T[] ResolveAll<T>()
        {
            var sp = GetServiceProvider();
            return sp?.GetServices<T>()?.ToArray() ?? Array.Empty<T>();
        }

        /// <summary>
        /// Resolve unregistered service
        /// </summary>
        /// <param name="type">Type of service</param>
        /// <returns>Resolved service</returns>
        public object ResolveUnregistered(Type type)
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

        #endregion
    }
}
