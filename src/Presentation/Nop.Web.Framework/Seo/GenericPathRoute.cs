using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using Nop.Services.Seo;
using Nop.Web.Framework.Localization;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Provides SEO friendly URL routing as ASP.NET Core middleware.
    /// </summary>
    public class GenericPathRouteMiddleware
    {
        private readonly RequestDelegate _next;

        public GenericPathRouteMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only process if database is installed
            if (!DataSettingsHelper.DatabaseIsInstalled())
            {
                await _next(context);
                return;
            }

            var slug = context.GetRouteValue("generic_se_name") as string;
            if (string.IsNullOrEmpty(slug))
            {
                await _next(context);
                return;
            }

            var urlRecordService = context.RequestServices.GetService<IUrlRecordService>();
            var urlRecord = urlRecordService.GetBySlugCached(slug);

            if (urlRecord == null)
            {
                // Route to PageNotFound
                context.GetRouteData().Values["controller"] = "Common";
                context.GetRouteData().Values["action"] = "PageNotFound";
                await _next(context);
                return;
            }

            if (!urlRecord.IsActive)
            {
                var activeSlug = urlRecordService.GetActiveSlug(urlRecord.EntityId, urlRecord.EntityName, urlRecord.LanguageId);
                if (string.IsNullOrWhiteSpace(activeSlug))
                {
                    context.GetRouteData().Values["controller"] = "Common";
                    context.GetRouteData().Values["action"] = "PageNotFound";
                    await _next(context);
                    return;
                }

                var webHelper = context.RequestServices.GetService<IWebHelper>();
                context.Response.StatusCode = 301;
                context.Response.Headers["Location"] = string.Format("{0}{1}", webHelper.GetStoreLocation(), activeSlug);
                return;
            }

            var workContext = context.RequestServices.GetService<IWorkContext>();
            var slugForCurrentLanguage = SeoExtensions.GetSeName(urlRecord.EntityId, urlRecord.EntityName, workContext.WorkingLanguage.Id);
            if (!String.IsNullOrEmpty(slugForCurrentLanguage) &&
                !slugForCurrentLanguage.Equals(slug, StringComparison.InvariantCultureIgnoreCase))
            {
                var webHelper = context.RequestServices.GetService<IWebHelper>();
                context.Response.StatusCode = 302;
                context.Response.Headers["Location"] = string.Format("{0}{1}", webHelper.GetStoreLocation(), slugForCurrentLanguage);
                return;
            }

            var routeData = context.GetRouteData();
            switch (urlRecord.EntityName.ToLowerInvariant())
            {
                case "product":
                    routeData.Values["controller"] = "Product";
                    routeData.Values["action"] = "ProductDetails";
                    routeData.Values["productid"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "category":
                    routeData.Values["controller"] = "Catalog";
                    routeData.Values["action"] = "Category";
                    routeData.Values["categoryid"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "manufacturer":
                    routeData.Values["controller"] = "Catalog";
                    routeData.Values["action"] = "Manufacturer";
                    routeData.Values["manufacturerid"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "vendor":
                    routeData.Values["controller"] = "Catalog";
                    routeData.Values["action"] = "Vendor";
                    routeData.Values["vendorid"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "newsitem":
                    routeData.Values["controller"] = "News";
                    routeData.Values["action"] = "NewsItem";
                    routeData.Values["newsItemId"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "blogpost":
                    routeData.Values["controller"] = "Blog";
                    routeData.Values["action"] = "BlogPost";
                    routeData.Values["blogPostId"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "topic":
                    routeData.Values["controller"] = "Topic";
                    routeData.Values["action"] = "TopicDetails";
                    routeData.Values["topicId"] = urlRecord.EntityId;
                    routeData.Values["SeName"] = urlRecord.Slug;
                    break;
                default:
                    EngineContext.Current.Resolve<IEventPublisher>()
                        .Publish(new CustomUrlRecordEntityNameRequested(routeData, urlRecord));
                    break;
            }

            await _next(context);
        }
    }
}
