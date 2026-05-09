using System;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Events;
using Nop.Services.Seo;
using Nop.Web.Framework.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Provides properties and methods for defining a SEO friendly route, and for getting information about the route.
    /// In ASP.NET Core, this is implemented as an IRouter.
    /// </summary>
    public partial class GenericPathRoute : LocalizedRoute
    {
        #region Constructors

        public GenericPathRoute(IRouter target)
            : base(target)
        {
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Route the request based on SEO-friendly URL slugs.
        /// </summary>
        public override async Task RouteAsync(RouteContext context)
        {
            // First, let the base (localized) route process the request
            await base.RouteAsync(context);

            if (context.Handler != null && DataSettingsHelper.DatabaseIsInstalled())
            {
                var routeData = context.RouteData;
                var slug = routeData.Values["generic_se_name"] as string;

                if (!string.IsNullOrEmpty(slug))
                {
                    var urlRecordService = EngineContext.Current.Resolve<IUrlRecordService>();
                    var urlRecord = urlRecordService.GetBySlugCached(slug);

                    if (urlRecord == null)
                    {
                        routeData.Values["controller"] = "Common";
                        routeData.Values["action"] = "PageNotFound";
                        return;
                    }

                    if (!urlRecord.IsActive)
                    {
                        var activeSlug = urlRecordService.GetActiveSlug(urlRecord.EntityId, urlRecord.EntityName, urlRecord.LanguageId);
                        if (string.IsNullOrWhiteSpace(activeSlug))
                        {
                            routeData.Values["controller"] = "Common";
                            routeData.Values["action"] = "PageNotFound";
                            return;
                        }

                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var redirectUrl = string.Format("{0}{1}", webHelper.GetStoreLocation(), activeSlug);
                        context.HttpContext.Response.StatusCode = 301;
                        context.HttpContext.Response.Headers["Location"] = redirectUrl;
                        return;
                    }

                    var workContext = EngineContext.Current.Resolve<IWorkContext>();
                    var slugForCurrentLanguage = SeoExtensions.GetSeName(urlRecord.EntityId, urlRecord.EntityName, workContext.WorkingLanguage.Id);
                    if (!String.IsNullOrEmpty(slugForCurrentLanguage) &&
                        !slugForCurrentLanguage.Equals(slug, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                        var redirectUrl = string.Format("{0}{1}", webHelper.GetStoreLocation(), slugForCurrentLanguage);
                        context.HttpContext.Response.StatusCode = 302;
                        context.HttpContext.Response.Headers["Location"] = redirectUrl;
                        return;
                    }

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
                }
            }
        }

        #endregion
    }
}
