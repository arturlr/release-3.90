using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Localized route extensions - stub for ASP.NET Core
    /// </summary>
    public static class LocalizedRouteExtensions
    {
        public static IEndpointConventionBuilder MapLocalizedRoute(this IEndpointRouteBuilder endpoints,
            string name, string pattern, object defaults = null)
        {
            return endpoints.MapControllerRoute(
                name: name,
                pattern: pattern,
                defaults: defaults);
        }
    }
}
