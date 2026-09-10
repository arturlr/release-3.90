using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.Seo;

namespace Nop.Web.Controllers
{
    [CheckAffiliate]
    [StoreClosed]
    [PublicStoreAllowNavigation]
    [LanguageSeoCode]
    [NopHttpsRequirement(SslRequirement.NoMatter)]
    [WwwRequirement]
    public abstract partial class BasePublicController : BaseController
    {
        /// <summary>
        /// Serve the "page not found" page for a request whose target does not exist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>REIMPLEMENTED — task 7.3.</b> 3.90 executed a second controller in-process:
        /// <code>
        /// IController errorController = EngineContext.Current.Resolve&lt;CommonController&gt;();
        /// var routeData = new RouteData();
        /// routeData.Values.Add("controller", "Common");
        /// routeData.Values.Add("action", "PageNotFound");
        /// errorController.Execute(new RequestContext(this.HttpContext, routeData));
        /// return new EmptyResult();
        /// </code>
        /// None of that exists in ASP.NET Core: there is no <c>IController</c>, no
        /// <c>IController.Execute</c> and no <c>RequestContext</c>. Re-executing a request
        /// against a different endpoint is a pipeline concern, not a controller one.
        /// </para>
        /// <para>
        /// The replacement is <see cref="NotFoundResult"/>, which produces a bodyless 404.
        /// Task 7.2 registered <c>app.UseStatusCodePagesWithReExecute("/page-not-found")</c>,
        /// and <c>RouteProvider</c> maps <c>page-not-found</c> to
        /// <c>Common/PageNotFound</c> — so the pipeline re-executes exactly the action 3.90
        /// invoked by hand, and renders it <b>at the original URL</b>. The observable result is
        /// the same as 3.90's: HTTP 404 plus the PageNotFound page, with the request URL
        /// unchanged.
        /// </para>
        /// <para>
        /// One difference worth noting: 3.90 emitted the PageNotFound body with whatever status
        /// code <c>CommonController.PageNotFound</c> set (404). Here the 404 is set first and the
        /// re-execution preserves it, so the status code is 404 either way.
        /// </para>
        /// </remarks>
        /// <returns>404 result; the body is supplied by the status-code-pages re-execution</returns>
        protected virtual ActionResult InvokeHttp404()
        {
            return NotFound();
        }

    }
}
