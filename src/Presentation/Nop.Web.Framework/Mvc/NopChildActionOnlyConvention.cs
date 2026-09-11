using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Removes every action marked <see cref="NopChildActionOnlyAttribute"/> from inbound route
    /// matching, while leaving it fully invocable in every other way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Runtime deferral 7.3-4 — the mechanism.</b> This is the same tool task 7.3 used for
    /// <c>GenericUrlRouteProvider</c>'s seven name-only routes:
    /// <see cref="SuppressMatchingMetadata"/>, i.e. <c>ISuppressMatchingMetadata</c>. The routing
    /// matcher builder skips any endpoint carrying it, so the endpoint is never a match candidate;
    /// <b>nothing else changes</b>, because only <c>ISuppressLinkGenerationMetadata</c> affects
    /// link generation and neither affects the <c>ActionDescriptor</c> collection.
    /// </para>
    /// <para>
    /// <b>Why NOT <c>.WithOrder(...)</c>, and why not "just remove the Default route".</b>
    /// runtime-deferrals.md §28.1 records a measured finding that ordering tricks are the wrong
    /// tool here: <c>MapControllerRoute</c> already assigns an auto-incrementing
    /// <c>Endpoint.Order</c>, and forcing a shared order collapses distinct orders into one and
    /// produces <c>AmbiguousMatchException</c>. Ordering also cannot express "never match" — only
    /// "match later". Deleting the <c>Default</c> route would remove far more than the 48 actions.
    /// Suppression is the only mechanism that says exactly what is meant.
    /// </para>
    /// <para>
    /// <b>The critical property, verified by execution rather than assumed.</b> These actions are
    /// still invoked through task 7.3's <c>Html.Action</c> bridge
    /// (<c>Nop.Web.Framework/ChildActionExtensions.cs</c>), which looks the action up in
    /// <c>IActionDescriptorCollectionProvider.ActionDescriptors</c> and invokes the method by
    /// reflection — it never goes near the matcher. Suppressing matching therefore cannot hide an
    /// action from the bridge. That reasoning is sound but it is exactly the kind of claim this
    /// migration has been burned by, so the smoke suite asserts both halves directly: the marked
    /// action's endpoints all carry <c>ISuppressMatchingMetadata</c>, AND its
    /// <c>ControllerActionDescriptor</c> is still present in the collection the bridge queries.
    /// If it were not, the home page's ~15 child actions would stop rendering.
    /// </para>
    /// <para>
    /// <b>Scope of the suppression is the ACTION, not the route.</b> Every endpoint MVC builds for
    /// a marked action is suppressed — the one from the <c>Default</c> conventional route and any
    /// from an explicit <c>MapControllerRoute</c> that targets it. That is intended: a
    /// <c>[ChildActionOnly]</c> action was unreachable by URL in 3.90 no matter which route
    /// reached it. One in-tree case is affected and it is faithful: <c>RouteProvider</c> registers
    /// <c>widgetsbyzone/</c> for <c>Widget/WidgetsByZone</c>, which also carried
    /// <c>[ChildActionOnly]</c> in 3.90 — so that URL answered <b>500</b> there, and answers 404
    /// now. See the remarks on <c>Nop.Web.Components.WidgetViewComponent</c>.
    /// </para>
    /// <para>
    /// Registered by <c>NopServiceCollectionExtensions.AddNopFramework</c>, so <c>Nop.Web</c>,
    /// <c>Nop.Admin</c> (task 8.3) and every plugin share one implementation. It lives in
    /// <c>Nop.Web.Framework</c> for that reason — the alternative was implementing it twice, and
    /// the admin surface is where the exposure is worse.
    /// </para>
    /// </remarks>
    public class NopChildActionOnlyConvention : IActionModelConvention
    {
        /// <summary>
        /// Apply the convention to one action.
        /// </summary>
        /// <param name="action">Action model</param>
        public virtual void Apply(ActionModel action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (!IsChildActionOnly(action))
                return;

            foreach (var selector in action.Selectors)
            {
                //idempotent: a selector that already declares suppression (e.g. because a future
                //task adds the metadata another way) must not accumulate duplicates
                if (selector.EndpointMetadata.OfType<ISuppressMatchingMetadata>().Any())
                    continue;

                selector.EndpointMetadata.Add(new SuppressMatchingMetadata());
            }
        }

        /// <summary>
        /// Is this action marked <see cref="NopChildActionOnlyAttribute"/>, directly or through its
        /// controller?
        /// </summary>
        /// <remarks>
        /// The controller is checked as well as the method so a whole controller of child actions
        /// (a plausible shape in <c>Nop.Admin</c>) can be marked once. <c>ActionModel.Attributes</c>
        /// is built from <c>MethodInfo.GetCustomAttributes(inherit: true)</c>, so an override of a
        /// <c>public virtual</c> nopCommerce action inherits the marker — which is what a plugin
        /// subclassing a controller needs.
        /// </remarks>
        protected virtual bool IsChildActionOnly(ActionModel action)
        {
            if (action.Attributes.OfType<NopChildActionOnlyAttribute>().Any())
                return true;

            return action.Controller != null &&
                   action.Controller.Attributes.OfType<NopChildActionOnlyAttribute>().Any();
        }
    }
}
