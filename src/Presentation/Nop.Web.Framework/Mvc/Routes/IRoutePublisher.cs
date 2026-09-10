using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Mvc.Routes
{
    /// <summary>
    /// Route publisher — discovers every <see cref="IRouteProvider"/> in the loaded
    /// assemblies and invokes them in descending <see cref="IRouteProvider.Priority"/> order.
    /// </summary>
    /// <remarks>
    /// Task 6.4: the parameter changed from <c>System.Web.Routing.RouteCollection</c> to
    /// <see cref="IEndpointRouteBuilder"/>. The host calls this from inside
    /// <c>UseEndpoints(...)</c> — see
    /// <c>Nop.Web.Framework.Infrastructure.NopApplicationBuilderExtensions.UseNopEndpoints</c>.
    /// </remarks>
    public interface IRoutePublisher
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Endpoint route builder</param>
        void RegisterRoutes(IEndpointRouteBuilder routeBuilder);
    }
}
