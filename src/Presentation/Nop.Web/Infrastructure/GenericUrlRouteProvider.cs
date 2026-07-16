using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Web.Infrastructure
{
    public partial class GenericUrlRouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //generic URLs
            endpointRouteBuilder.MapControllerRoute("GenericUrl", "{generic_se_name}",
                new { controller = "Common", action = "GenericUrl" });

            //define this routes to use in UI views (in case if you want to customize some of them later)
            endpointRouteBuilder.MapControllerRoute("Product", "{SeName}",
                new { controller = "Product", action = "ProductDetails" });

            endpointRouteBuilder.MapControllerRoute("Category", "{SeName}",
                new { controller = "Catalog", action = "Category" });

            endpointRouteBuilder.MapControllerRoute("Manufacturer", "{SeName}",
                new { controller = "Catalog", action = "Manufacturer" });

            endpointRouteBuilder.MapControllerRoute("Vendor", "{SeName}",
                new { controller = "Catalog", action = "Vendor" });

            endpointRouteBuilder.MapControllerRoute("NewsItem", "{SeName}",
                new { controller = "News", action = "NewsItem" });

            endpointRouteBuilder.MapControllerRoute("BlogPost", "{SeName}",
                new { controller = "Blog", action = "BlogPost" });

            endpointRouteBuilder.MapControllerRoute("Topic", "{SeName}",
                new { controller = "Topic", action = "TopicDetails" });
        }

        public int Priority
        {
            get
            {
                //it should be the last route
                //we do not set it to -int.MaxValue so it could be overridden (if required)
                return -1000000;
            }
        }
    }
}
