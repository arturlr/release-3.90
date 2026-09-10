using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    //Routes used for backward compatibility with 2.x versions of nopCommerce
    public partial class BackwardCompatibility2XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routes)
        {
            var config = EngineContext.Current.Resolve<NopConfig>();
            if (!config.SupportPreviousNopcommerceVersions)
                return;

            //products
            routes.MapLocalizedRoute("", "p/{productId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectProductById" },
                new { productId = @"\d+" });

            //categories
            routes.MapLocalizedRoute("", "c/{categoryId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectCategoryById" },
                new { categoryId = @"\d+" });

            //manufacturers
            routes.MapLocalizedRoute("", "m/{manufacturerId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectManufacturerById" },
                new { manufacturerId = @"\d+" });

            //news
            routes.MapLocalizedRoute("", "news/{newsItemId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectNewsItemById" },
                new { newsItemId = @"\d+" });

            //blog
            routes.MapLocalizedRoute("", "blog/{blogPostId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectBlogPostById" },
                new { blogPostId = @"\d+" });

            //topic
            routes.MapLocalizedRoute("", "t/{SystemName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectTopicBySystemName" });

            //vendors
            routes.MapLocalizedRoute("", "vendor/{vendorId}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectVendorById" },
                new { vendorId = @"\d+" });
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
