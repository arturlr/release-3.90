using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Localized route - stub for ASP.NET Core routing compatibility.
    /// In Core, localization is typically handled via middleware or route constraints.
    /// </summary>
    public class LocalizedRoute : IRouter
    {
        private readonly IRouter _target;

        public LocalizedRoute(IRouter target)
        {
            _target = target;
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return _target?.GetVirtualPath(context);
        }

        public System.Threading.Tasks.Task RouteAsync(RouteContext context)
        {
            return _target?.RouteAsync(context) ?? System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
