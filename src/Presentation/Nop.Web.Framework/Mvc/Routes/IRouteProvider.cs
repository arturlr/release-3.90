using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Mvc.Routes
{
    public interface IRouteProvider
    {
        void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder);

        int Priority { get; }
    }
}
