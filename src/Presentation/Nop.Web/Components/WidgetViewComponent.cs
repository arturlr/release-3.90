using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Factories;

namespace Nop.Web.Components
{
    /// <summary>
    /// Renders every widget registered for a widget zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Task 7.3 — closes runtime deferral 32.</b> Task 6.3 rewrote
    /// <c>Nop.Web.Framework.HtmlExtensions.Widget(...)</c> to resolve
    /// <c>IViewComponentHelper</c> and invoke a view component <b>named "Widget"</b>, because
    /// <c>Html.Action("WidgetsByZone", "Widget", …)</c> — which is what 3.90 did — has no
    /// ASP.NET Core counterpart. No such component existed, so all 190
    /// <c>@Html.Widget(...)</c> call sites threw
    /// <c>InvalidOperationException: A view component named 'Widget' could not be found</c> on
    /// the first page render. This is that component. The class name determines the component
    /// name, so <c>WidgetViewComponent</c> resolves as <c>"Widget"</c>.
    /// </para>
    /// <para>
    /// The logic is <c>WidgetController.WidgetsByZone</c> verbatim, including the
    /// "return empty content when no widget is registered" short-circuit.
    /// <c>WidgetController</c> is <b>retained</b> rather than deleted: <c>RouteProvider</c>
    /// registers a named <c>WidgetsByZone</c> route for it, with the comment "we have this
    /// route for performance optimization because named routes are MUCH faster than usual
    /// Html.Action(...)", so the URL endpoint is part of the public surface and a plugin or
    /// script may call it. Both paths now feed off the same
    /// <see cref="IWidgetModelFactory"/>.
    /// </para>
    /// <para>
    /// <b>Two behaviour notes carried over from deferral 32.</b> The <c>area</c> parameter of
    /// <c>Html.Widget</c> is now inert — view components are resolved application-wide, not per
    /// area. And view components do not execute the action-filter pipeline, which is the
    /// property task 6.2 already relied on when it removed the <c>IsChildAction</c> guards from
    /// eleven filters.
    /// </para>
    /// </remarks>
    public class WidgetViewComponent : ViewComponent
    {
        private readonly IWidgetModelFactory _widgetModelFactory;

        public WidgetViewComponent(IWidgetModelFactory widgetModelFactory)
        {
            this._widgetModelFactory = widgetModelFactory;
        }

        /// <summary>
        /// Invoke the view component.
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data handed to each widget</param>
        /// <returns>View component result</returns>
        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            var model = _widgetModelFactory.GetRenderWidgetModels(widgetZone, additionalData);

            //no data?
            if (!model.Any())
                return Content("");

            return View(model);
        }
    }
}
