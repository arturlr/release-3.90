using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Re-executes nopCommerce's <c>PageNotFound</c> page for a 404 — and ONLY for a 404, so every
    /// other refused request keeps its own status code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Runtime deferral 7.7-1.</b> Task 7.2 replaced <c>Global.asax</c>'s
    /// <c>Application_Error</c> 404 branch with a bare
    /// <c>app.UseStatusCodePagesWithReExecute("/page-not-found")</c>. That middleware fires for
    /// <b>any</b> status in the 400–599 range whose response has no body, and the re-executed
    /// <c>CommonController.PageNotFound</c> then assigns <c>Response.StatusCode = 404</c>. So a
    /// deliberate 400, 403 or 405 was silently rewritten to "404 Page not found".
    /// </para>
    /// <para>
    /// Task 7.2 recorded the imprecision but judged only "the uncommon bare 403/400" affected.
    /// Task 7.7 measured otherwise: it hits <b>every</b> antiforgery refusal on the public store —
    /// <c>PublicAntiForgeryAttribute</c> sets <c>BadRequestResult</c>, which is bodiless — so
    /// <c>POST /register</c> and <c>POST /contactus</c> answered 404 while <c>POST /login</c> (no
    /// <c>[PublicAntiForgery]</c>) answered 200. It is not a security hole: the request IS refused
    /// and the action does NOT run. It is simply the wrong answer, and misleading to any client
    /// that distinguishes the two. <c>Nop.Admin</c> is antiforgery-heavy throughout, so it would
    /// hit task 8.3 broadly, which is why this is fixed here in <c>Nop.Web.Framework</c> rather
    /// than in <c>Nop.Web</c>'s <c>Program.cs</c>.
    /// </para>
    /// <para>
    /// <b>How, and why this way.</b> <c>StatusCodePagesMiddleware</c> exposes an
    /// <see cref="IStatusCodePagesFeature"/> for exactly this purpose: it installs the feature
    /// before calling the rest of the pipeline and, on the way back out, does nothing when
    /// <c>Enabled</c> is false. <see cref="UseNopStatusCodePages"/> therefore registers the stock
    /// re-execute middleware and then, <b>immediately inside it</b>, a filter that clears
    /// <c>Enabled</c> whenever the final status code is not 404. Because the filter is inside, it
    /// runs first on the way back out and the decision is taken with the final status code in
    /// hand.
    /// </para>
    /// <para>
    /// Rejected alternatives: reimplementing re-execution by hand (it has to preserve
    /// <c>PathBase</c>, the original path and query through
    /// <c>IStatusCodeReExecuteFeature</c>, and clear the matched endpoint and route values —
    /// re-deriving that correctly is pure risk); and writing a placeholder body for non-404
    /// statuses so the stock middleware skips them (it works, because the middleware also skips a
    /// response that already has a body, but it changes what is sent on the wire in order to
    /// influence control flow).
    /// </para>
    /// <para>
    /// <b>Both behaviours are preserved and both are asserted by the smoke suite:</b> a genuine
    /// 404 still re-executes to nopCommerce's <c>PageNotFound</c> view at the original URL, and a
    /// bodiless non-404 keeps its status code.
    /// </para>
    /// </remarks>
    public static class NopStatusCodePagesExtensions
    {
        /// <summary>
        /// The path <c>RouteProvider</c> registers for <c>Common/PageNotFound</c>.
        /// </summary>
        public const string PageNotFoundPath = "/page-not-found";

        /// <summary>
        /// Registers 404 re-execution, scoped to 404 only. Call it where task 7.2 called
        /// <c>UseStatusCodePagesWithReExecute</c> — after the exception handler and the error
        /// logger, before the static-file middleware.
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="pathFormat">
        /// Path to re-execute for a 404. Defaults to <see cref="PageNotFoundPath"/>.
        /// </param>
        public static IApplicationBuilder UseNopStatusCodePages(this IApplicationBuilder app,
            string pathFormat = PageNotFoundPath)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            //outer: the stock middleware, unchanged - it owns re-execution, which is fiddly to
            //reproduce and pointless to reproduce
            app.UseStatusCodePagesWithReExecute(pathFormat);

            //inner: opt the response out of re-execution unless it is a 404. MUST be registered
            //immediately after (i.e. INSIDE) the call above.
            app.UseMiddleware<NopStatusCodePagesScopeMiddleware>();

            return app;
        }
    }

    /// <summary>
    /// Clears <see cref="IStatusCodePagesFeature.Enabled"/> for every response that is not a 404,
    /// so the surrounding <c>StatusCodePagesMiddleware</c> leaves it alone.
    /// </summary>
    /// <remarks>
    /// Register it only through
    /// <see cref="NopStatusCodePagesExtensions.UseNopStatusCodePages"/>: on its own it does nothing
    /// useful, and placed anywhere other than immediately inside the status-code-pages middleware
    /// it has no effect at all.
    /// </remarks>
    public class NopStatusCodePagesScopeMiddleware
    {
        private readonly RequestDelegate _next;

        public NopStatusCodePagesScopeMiddleware(RequestDelegate next)
        {
            if (next == null)
                throw new ArgumentNullException("next");

            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                return;

            //not a 404 - keep whatever status the pipeline produced. A 500 is already handled by
            //UseExceptionHandler (which writes ErrorPage.htm, i.e. a bodied response the stock
            //middleware would skip anyway); this covers the bodiless cases, of which the
            //antiforgery 400 is by far the most common.
            var feature = context.Features.Get<IStatusCodePagesFeature>();
            if (feature != null)
                feature.Enabled = false;
        }
    }
}
