using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc.Routes;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Infrastructure
{
    //Routes used for backward compatibility with 2.x versions of nopCommerce
    public partial class BackwardCompatibility2XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IRouteBuilder routes)
        {
            var config = EngineContext.Current.Resolve<NopConfig>();
            if (!config.SupportPreviousNopcommerceVersions)
                return;

            //products
            routes.MapLocalizedRoute("", "p/{productId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectProductById", SeName = "" },
                new { productId = @"\d+" },
                new[] { "Nop.Web.Controllers" });

            //categories
            routes.MapLocalizedRoute("", "c/{categoryId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectCategoryById", SeName = "" },
                new { categoryId = @"\d+" },
                new[] { "Nop.Web.Controllers" });

            //manufacturers
            routes.MapLocalizedRoute("", "m/{manufacturerId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectManufacturerById", SeName = "" },
                new { manufacturerId = @"\d+" },
                new[] { "Nop.Web.Controllers" });

            //news
            routes.MapLocalizedRoute("", "news/{newsItemId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectNewsItemById", SeName = "" },
                new { newsItemId = @"\d+" },
                new[] { "Nop.Web.Controllers" });

            //blog
            routes.MapLocalizedRoute("", "blog/{blogPostId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectBlogPostById", SeName = "" },
                new { blogPostId = @"\d+" },
                new[] { "Nop.Web.Controllers" });

            //topic
            routes.MapLocalizedRoute("", "t/{SystemName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectTopicBySystemName" },
                new[] { "Nop.Web.Controllers" });

            //vendors
            routes.MapLocalizedRoute("", "vendor/{vendorId}/{SeName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectVendorById", SeName = "" },
                new { vendorId = @"\d+" },
                new[] { "Nop.Web.Controllers" });
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
