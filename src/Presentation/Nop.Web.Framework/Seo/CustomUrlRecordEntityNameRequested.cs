using Microsoft.AspNetCore.Routing;
using Nop.Services.Seo;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Event to handle unknow URL record entity names
    /// </summary>
    /// <remarks>
    /// <b>BREAKING CHANGE — task 6.4.</b> The first constructor argument and the corresponding
    /// property changed from <c>System.Web.Routing.RouteData</c> to
    /// <see cref="RouteValueDictionary"/>, and the property was renamed
    /// <c>RouteData</c> → <see cref="RouteValues"/>.
    /// <para>
    /// <c>Microsoft.AspNetCore.Routing.RouteData</c> does exist, so keeping the old type name
    /// would have compiled — but it would have broken the extension point silently.
    /// <see cref="SlugRouteTransformer"/> works with a <see cref="RouteValueDictionary"/>, and
    /// <c>new RouteData(values)</c> <i>copies</i> the dictionary, so a consumer writing
    /// <c>RouteData.Values["controller"] = "MyController"</c> would have mutated a throwaway
    /// copy and had no effect. Exposing the live dictionary keeps the event's whole purpose —
    /// "developers could insert their own types" — working.
    /// </para>
    /// <para>
    /// There is no consumer of this event anywhere in this solution (verified by grep across
    /// Nop.Web, Nop.Admin and all 20 plugins), so nothing in tree has to change. A third-party
    /// consumer must change <c>e.RouteData.Values[...]</c> to <c>e.RouteValues[...]</c>.
    /// </para>
    /// </remarks>
    public class CustomUrlRecordEntityNameRequested
    {
        public CustomUrlRecordEntityNameRequested(RouteValueDictionary routeValues, UrlRecordService.UrlRecordForCaching urlRecord)
        {
            this.RouteValues = routeValues;
            this.UrlRecord = urlRecord;
        }

        /// <summary>
        /// The live route values that will be handed back to MVC for action selection.
        /// Mutate this to route the slug at your own controller/action.
        /// </summary>
        public RouteValueDictionary RouteValues { get; private set; }

        public UrlRecordService.UrlRecordForCaching UrlRecord { get; private set; }
    }
}
