using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Web.Infrastructure.Cache;
using Nop.Core.Caching;

namespace Nop.Web.Extensions
{
    /// <summary>
    /// HTML extensions - stubbed out for ASP.NET Core migration
    /// Full implementation will be added when views are migrated
    /// </summary>
    public static class HtmlExtensions
    {
        /// <summary>
        /// Get topic system name
        /// </summary>
        public static string GetTopicSeName(this IHtmlHelper html, string systemName)
        {
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var cacheManager = EngineContext.Current.Resolve<ICacheManager>();
            var cacheKey = string.Format(ModelCacheEventConsumer.TOPIC_SENAME_BY_SYSTEMNAME, systemName, workContext.WorkingLanguage.Id, storeContext.CurrentStore.Id);
            var cachedSeName = cacheManager.Get(cacheKey, () =>
            {
                var topicService = EngineContext.Current.Resolve<ITopicService>();
                var topic = topicService.GetTopicBySystemName(systemName, storeContext.CurrentStore.Id);
                if (topic == null)
                    return "";
                return topic.GetSeName();
            });
            return cachedSeName;
        }
    }
}
