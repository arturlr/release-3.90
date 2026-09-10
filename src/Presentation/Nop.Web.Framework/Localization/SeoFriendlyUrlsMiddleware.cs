using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// Strips the language SEO code (<c>/en</c>) from the incoming request path so that the
    /// ordinary route patterns match, and moves it into <see cref="HttpRequest.PathBase"/> so
    /// that every generated URL carries it back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the inbound half of 3.90's <c>LocalizedRoute</c>.</b> The original did
    /// <c>httpContext.RewritePath(newVirtualPath, true)</c> from
    /// <c>LocalizedRoute.GetRouteData</c>, i.e. during route matching. ASP.NET Core has no
    /// per-route match hook and no <c>RewritePath</c>; path rewriting before routing is a
    /// middleware concern, so this component MUST be registered <b>before</b>
    /// <c>UseRouting()</c>.
    /// </para>
    /// <para>
    /// <b>Scope difference, stated plainly.</b> In 3.90 the rewrite was performed by whichever
    /// <c>LocalizedRoute</c> the route collection happened to test first, and it mutated the
    /// context for the whole of the rest of matching. Since <c>HomePage</c> — a
    /// <c>MapLocalizedRoute</c> — is the first route nopCommerce registers, in practice
    /// <i>every</i> localized request was stripped before anything else could match. Doing it
    /// unconditionally in middleware is therefore equivalent in practice and strictly simpler.
    /// It also means static files are unaffected, because <c>UseStaticFiles()</c> runs earlier
    /// in the pipeline.
    /// </para>
    /// <para>
    /// <b>Shape-only detection is preserved.</b> <see cref="LocalizedUrlExtenstions.IsLocalizedUrl"/>
    /// only checks that the first segment is exactly two characters; it does not check that the
    /// code names an installed language. 3.90 behaved the same way and relied on
    /// <see cref="LanguageSeoCodeAttribute"/> to redirect away from an unknown code. That
    /// division of labour is unchanged.
    /// </para>
    /// </remarks>
    public class SeoFriendlyUrlsMiddleware
    {
        private readonly RequestDelegate _next;

        public SeoFriendlyUrlsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            RewriteLocalizedPath(context);
            await _next(context);
        }

        /// <summary>
        /// Moves a leading language SEO code from <c>Request.Path</c> to
        /// <c>Request.PathBase</c>. Exposed as <c>internal static</c> so the behaviour can be
        /// exercised without a pipeline.
        /// </summary>
        internal static void RewriteLocalizedPath(HttpContext context)
        {
            if (context == null || context.Request == null)
                return;

            if (!LocalizedRoute.SeoFriendlyUrlsForLanguagesEnabled)
                return;

            var request = context.Request;

            //3.90 read System.Web's AppRelativeCurrentExecutionFilePath, which was
            //"~" + the application-relative path, and passed isRawPath: false.
            string virtualPath = "~" + request.Path.Value;
            string applicationPath = request.PathBase.HasValue ? request.PathBase.Value : "/";

            if (!virtualPath.IsLocalizedUrl(applicationPath, false))
                return;

            var seoCode = virtualPath.GetLanguageSeoCodeFromUrl(applicationPath, false);
            if (string.IsNullOrEmpty(seoCode))
                return;

            var segment = new PathString("/" + seoCode);
            PathString remaining;
            if (!request.Path.StartsWithSegments(segment, out remaining))
                return;

            request.PathBase = request.PathBase.Add(segment);
            request.Path = remaining.HasValue ? remaining : new PathString("/");

            //WebWorkContext.GetLanguageFromUrl() reads this instead of re-parsing the path,
            //which no longer contains the code.
            context.Items[LocalizedRoute.LanguageSeoCodeItemKey] = seoCode;
        }
    }
}
