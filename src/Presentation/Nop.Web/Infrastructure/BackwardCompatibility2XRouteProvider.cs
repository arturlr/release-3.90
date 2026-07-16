using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    //Routes used for backward compatibility with 2.x versions of nopCommerce
    public partial class BackwardCompatibility2XRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            var config = EngineContext.Current.Resolve<NopConfig>();
            if (!config.SupportPreviousNopcommerceVersions)
                return;

            //products
            endpointRouteBuilder.MapControllerRoute("", "p/{productId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectProductById" });

            //categories
            endpointRouteBuilder.MapControllerRoute("", "c/{categoryId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectCategoryById" });

            //manufacturers
            endpointRouteBuilder.MapControllerRoute("", "m/{manufacturerId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectManufacturerById" });

            //news
            endpointRouteBuilder.MapControllerRoute("", "news/{newsItemId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectNewsItemById" });

            //blog
            endpointRouteBuilder.MapControllerRoute("", "blog/{blogPostId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectBlogPostById" });

            //topic
            endpointRouteBuilder.MapControllerRoute("", "t/{SystemName}",
                new { controller = "BackwardCompatibility2X", action = "RedirectTopicBySystemName" });

            //vendors
            endpointRouteBuilder.MapControllerRoute("", "vendor/{vendorId:int}/{SeName?}",
                new { controller = "BackwardCompatibility2X", action = "RedirectVendorById" });
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
