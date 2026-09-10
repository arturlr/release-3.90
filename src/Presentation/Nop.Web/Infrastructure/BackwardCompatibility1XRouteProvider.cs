using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// Routes used for backward compatibility with 1.x versions of nopCommerce.
    /// </summary>
    /// <remarks>
    /// Task 7.3: all eleven routes were registered with the name <c>""</c>. Endpoint route
    /// names are indexed by <c>RouteValuesAddressScheme</c>, so eleven identically-named
    /// routes are at best pointless and at worst a duplicate-name failure. They are
    /// registered as <c>null</c> (unnamed) instead, which is what an empty name effectively
    /// meant in 3.90 and is the same normalisation
    /// <c>Nop.Web.Framework.Localization.LocalizedRouteExtensions.MapLocalizedRoute</c>
    /// applies. Nothing generates URLs to these routes by name — they exist only to redirect
    /// inbound legacy <c>.aspx</c> URLs.
    /// </remarks>
    public partial class BackwardCompatibility1XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routes)
        {
            var config = EngineContext.Current.Resolve<NopConfig>();
            if (!config.SupportPreviousNopcommerceVersions)
                return;

            //all old aspx URLs
            routes.MapControllerRoute(null, "{oldfilename}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "GeneralRedirect" });
            
            //products
            routes.MapControllerRoute(null, "products/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectProduct"});
            
            //categories
            routes.MapControllerRoute(null, "category/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectCategory" });

            //manufacturers
            routes.MapControllerRoute(null, "manufacturer/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectManufacturer" });

            //product tags
            routes.MapControllerRoute(null, "producttag/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectProductTag" });

            //news
            routes.MapControllerRoute(null, "news/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectNewsItem" });

            //blog posts
            routes.MapControllerRoute(null, "blog/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectBlogPost" });

            //topics
            routes.MapControllerRoute(null, "topic/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectTopic" });

            //forums
            routes.MapControllerRoute(null, "boards/fg/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectForumGroup" });
            routes.MapControllerRoute(null, "boards/f/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectForum" });
            routes.MapControllerRoute(null, "boards/t/{id}.aspx",
                            new { controller = "BackwardCompatibility1X", action = "RedirectForumTopic" });
        }

        public int Priority
        {
            get
            {
                //register it after all other IRouteProvider are processed
                return -1000;
            }
        }
    }
}
