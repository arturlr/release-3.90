using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Mvc.Routes
{
    /// <summary>
    /// Route publisher
    /// </summary>
    public interface IRoutePublisher
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Endpoint route builder</param>
        void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder);
    }
}
