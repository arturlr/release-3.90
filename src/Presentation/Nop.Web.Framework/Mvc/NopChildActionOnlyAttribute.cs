using System;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Marks an action that exists ONLY to be rendered inline in a view — the replacement marker
    /// for <c>System.Web.Mvc.ChildActionOnlyAttribute</c>, which has no ASP.NET Core counterpart.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Runtime deferral 7.3-4.</b> Task 7.3 deleted <c>[ChildActionOnly]</c> from 48
    /// <c>Nop.Web</c> actions because the attribute does not exist on .NET 10. In MVC 5 it was an
    /// <c>IAuthorizationFilter</c> that threw <c>InvalidOperationException</c> unless
    /// <c>ControllerContext.IsChildAction</c> was true, so an action carrying it was unreachable
    /// by URL. With the attribute gone those 48 actions are matched by the <c>Default</c>
    /// <c>{controller}/{action}/{id?}</c> route, so <c>GET /Common/Footer</c> or
    /// <c>GET /ShoppingCart/OrderSummary</c> returns the bare partial's HTML.
    /// </para>
    /// <para>
    /// <b>This attribute is inert on its own.</b> It carries no behaviour; it is a selector for
    /// <see cref="NopChildActionOnlyConvention"/>, which is what actually removes the action from
    /// inbound route matching. Applying the attribute without registering the convention changes
    /// nothing — <c>NopServiceCollectionExtensions.AddNopFramework</c> registers it, so any host
    /// that calls that method gets both halves.
    /// </para>
    /// <para>
    /// <b>Where to apply it.</b> On an action that is only ever reached through
    /// <c>@Html.Action(...)</c> (task 7.3's <c>ChildActionExtensions</c> bridge) or through a view
    /// component. Do NOT apply it to an action that any URL, script or plugin invokes directly:
    /// the convention removes the endpoint from matching, so such a call would start returning
    /// 404. The correct source of truth for <c>Nop.Web</c> was the pre-task-7.3 tree
    /// (<c>git grep ChildActionOnly 9cb503f</c>, 48 hits); <c>Nop.Admin</c> must use the same
    /// method at task 8.3 rather than guessing.
    /// </para>
    /// <para>
    /// <b>What it deliberately does NOT restore.</b> MVC 5 threw on a direct URL request; this
    /// yields a 404 instead. A 404 is the better answer — it does not distinguish "this action
    /// exists but you may not call it that way" — and it is what the endpoint-routing mechanism
    /// gives us. The observable difference from 3.90 is 500 → 404 on a request that was already
    /// refused.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class NopChildActionOnlyAttribute : Attribute
    {
    }
}
