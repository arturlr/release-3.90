using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    [TestFixture]
    public abstract class RoutesTestsBase
    {
        protected IEndpointRouteBuilder _endpointRouteBuilder;
        protected IServiceProvider _serviceProvider;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddRouting();
            _serviceProvider = services.BuildServiceProvider();
            _endpointRouteBuilder = new TestEndpointRouteBuilder(_serviceProvider);

            var routeProvider = new Nop.Web.Infrastructure.RouteProvider();
            routeProvider.RegisterRoutes(_endpointRouteBuilder);
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    /// <summary>
    /// A minimal IEndpointRouteBuilder implementation for testing route registration.
    /// </summary>
    internal class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<EndpointDataSource> _dataSources = new List<EndpointDataSource>();

        public TestEndpointRouteBuilder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public ICollection<EndpointDataSource> DataSources => _dataSources;

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(_serviceProvider);
        }
    }
}
