using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Executes a SEO redirect decided by <see cref="SlugRouteTransformer"/> during route
    /// matching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MUST be registered immediately <b>after</b> <c>UseRouting()</c>. Middleware placed
    /// there runs whether or not an endpoint was matched, which is exactly the position
    /// needed: the transformer signals "redirect instead of serving" by returning <c>null</c>
    /// (no endpoint) and parking the target on <see cref="HttpContext.Items"/>.
    /// </para>
    /// <para>
    /// This replaces <c>Response.Status = "301 Moved Permanently"; Response.RedirectLocation
    /// = ...; Response.End();</c> inside 3.90's <c>GenericPathRoute.GetRouteData</c>. The
    /// pipeline is short-circuited (<c>_next</c> is not called), which is what
    /// <c>Response.End()</c> achieved.
    /// </para>
    /// </remarks>
    public class SlugRedirectMiddleware
    {
        private readonly RequestDelegate _next;

        public SlugRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            object pending;
            if (context.Items != null &&
                context.Items.TryGetValue(SlugRouteTransformer.RedirectItemKey, out pending))
            {
                var redirect = pending as SlugRouteTransformer.PendingRedirect;
                if (redirect != null && !context.Response.HasStarted)
                {
                    context.Response.Redirect(redirect.Location, redirect.Permanent);
                    return;
                }
            }

            await _next(context);
        }
    }
}
