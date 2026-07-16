using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Seo;

namespace Nop.Web.Framework.Seo
{
    /// <summary>
    /// Provides SEO friendly URL routing as ASP.NET Core middleware.
    /// Intercepts requests with SEO-friendly slugs and rewrites them to the appropriate controller action.
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

            var path = context.Request.Path.Value;
            
            // Skip empty paths, paths with extensions (static files), and multi-segment paths
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                await _next(context);
                return;
            }

            // Remove leading slash
            var slug = path.TrimStart('/');
            
            // Skip if it contains slashes (multi-segment - let normal routing handle it)
            // But allow single-segment paths like /electronics, /build-your-own-computer
            if (slug.Contains('/'))
            {
                await _next(context);
                return;
            }

            // Skip if it has a file extension (static file)
            if (slug.Contains('.'))
            {
                await _next(context);
                return;
            }

            // Skip known controller names
            var lowerSlug = slug.ToLowerInvariant();
            if (lowerSlug == "admin" || lowerSlug == "install" || lowerSlug == "keepalive")
            {
                await _next(context);
                return;
            }

            try
            {
                var urlRecordService = EngineContext.Current.Resolve<IUrlRecordService>();
                var urlRecord = urlRecordService.GetBySlug(slug);

                if (urlRecord == null || !urlRecord.IsActive)
                {
                    await _next(context);
                    return;
                }

                // Rewrite the path based on entity type
                string newPath = null;
                switch (urlRecord.EntityName.ToLowerInvariant())
                {
                    case "product":
                        newPath = $"/Product/ProductDetails?productId={urlRecord.EntityId}";
                        break;
                    case "category":
                        newPath = $"/Catalog/Category?categoryId={urlRecord.EntityId}";
                        break;
                    case "manufacturer":
                        newPath = $"/Catalog/Manufacturer?manufacturerId={urlRecord.EntityId}";
                        break;
                    case "vendor":
                        newPath = $"/Catalog/Vendor?vendorId={urlRecord.EntityId}";
                        break;
                    case "newsitem":
                        newPath = $"/News/NewsItem?newsItemId={urlRecord.EntityId}";
                        break;
                    case "blogpost":
                        newPath = $"/Blog/BlogPost?blogPostId={urlRecord.EntityId}";
                        break;
                    case "topic":
                        newPath = $"/Topic/TopicDetails?topicId={urlRecord.EntityId}";
                        break;
                }

                if (newPath != null)
                {
                    // Split path and query
                    var parts = newPath.Split('?');
                    context.Request.Path = parts[0];
                    if (parts.Length > 1)
                        context.Request.QueryString = new QueryString("?" + parts[1]);
                }
            }
            catch
            {
                // If URL resolution fails, let normal routing handle it
            }

            await _next(context);
        }
    }
}
