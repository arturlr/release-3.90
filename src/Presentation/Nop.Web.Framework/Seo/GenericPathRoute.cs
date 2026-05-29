using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using Nop.Services.Seo;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Provides properties and methods for defining a SEO friendly route.
    /// In ASP.NET Core, this is implemented as an IRouter or middleware.
    /// This is a stub implementation for build compatibility.
    /// </summary>
    public partial class GenericPathRoute : IRouter
    {
        private readonly IRouter _target;

        public GenericPathRoute(IRouter target)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            return _target.GetVirtualPath(context);
        }

        public async System.Threading.Tasks.Task RouteAsync(RouteContext context)
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
            {
                await _target.RouteAsync(context);
                return;
            }

            var urlRecordService = EngineContext.Current.Resolve<IUrlRecordService>();
            var slug = context.RouteData.Values["generic_se_name"] as string;
            if (string.IsNullOrEmpty(slug))
            {
                await _target.RouteAsync(context);
                return;
            }

            var urlRecord = urlRecordService.GetBySlugCached(slug);
            if (urlRecord == null)
            {
                context.RouteData.Values["controller"] = "Common";
                context.RouteData.Values["action"] = "PageNotFound";
                await _target.RouteAsync(context);
                return;
            }

            if (!urlRecord.IsActive)
            {
                var activeSlug = urlRecordService.GetActiveSlug(urlRecord.EntityId, urlRecord.EntityName, urlRecord.LanguageId);
                if (string.IsNullOrWhiteSpace(activeSlug))
                {
                    context.RouteData.Values["controller"] = "Common";
                    context.RouteData.Values["action"] = "PageNotFound";
                    await _target.RouteAsync(context);
                    return;
                }

                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var redirectUrl = $"{webHelper.GetStoreLocation()}{activeSlug}";
                context.HttpContext.Response.Redirect(redirectUrl, true);
                context.Handler = httpContext => System.Threading.Tasks.Task.CompletedTask;
                return;
            }

            //process URL
            switch (urlRecord.EntityName.ToLowerInvariant())
            {
                case "product":
                    context.RouteData.Values["controller"] = "Product";
                    context.RouteData.Values["action"] = "ProductDetails";
                    context.RouteData.Values["productid"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "category":
                    context.RouteData.Values["controller"] = "Catalog";
                    context.RouteData.Values["action"] = "Category";
                    context.RouteData.Values["categoryid"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "manufacturer":
                    context.RouteData.Values["controller"] = "Catalog";
                    context.RouteData.Values["action"] = "Manufacturer";
                    context.RouteData.Values["manufacturerid"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "vendor":
                    context.RouteData.Values["controller"] = "Catalog";
                    context.RouteData.Values["action"] = "Vendor";
                    context.RouteData.Values["vendorid"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "newsitem":
                    context.RouteData.Values["controller"] = "News";
                    context.RouteData.Values["action"] = "NewsItem";
                    context.RouteData.Values["newsItemId"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "blogpost":
                    context.RouteData.Values["controller"] = "Blog";
                    context.RouteData.Values["action"] = "BlogPost";
                    context.RouteData.Values["blogPostId"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                case "topic":
                    context.RouteData.Values["controller"] = "Topic";
                    context.RouteData.Values["action"] = "TopicDetails";
                    context.RouteData.Values["topicId"] = urlRecord.EntityId;
                    context.RouteData.Values["SeName"] = urlRecord.Slug;
                    break;
                default:
                    EngineContext.Current.Resolve<IEventPublisher>()
                        .Publish(new CustomUrlRecordEntityNameRequested(context.RouteData, urlRecord));
                    break;
            }

            await _target.RouteAsync(context);
        }
    }
}
