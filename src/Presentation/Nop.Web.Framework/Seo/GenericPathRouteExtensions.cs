using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Extensions for generic path routing in ASP.NET Core.
    /// In ASP.NET Core, routing is handled via endpoint routing.
    /// This is a stub for build compatibility.
    /// </summary>
    public static class GenericPathRouteExtensions
    {
        /// <summary>
        /// Maps a generic path route for SEO-friendly URLs
        /// </summary>
        public static IEndpointConventionBuilder MapGenericPathRoute(this IEndpointRouteBuilder endpoints,
            string name, string pattern, object defaults = null)
        {
            return endpoints.MapControllerRoute(
                name: name,
                pattern: pattern,
                defaults: defaults);
        }
    }
}
