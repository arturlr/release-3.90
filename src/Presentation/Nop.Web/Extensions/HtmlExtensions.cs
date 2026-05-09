using System;
using System.Text;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Web.Framework.UI.Paging;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Boards;
using Nop.Web.Models.Common;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Html;

using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Web.Extensions
{
    public static class HtmlExtensions
    {
        /// <summary>
        /// BBCode editor
        /// </summary>
        /// <typeparam name="TModel">Model</typeparam>
        /// <param name="html">HTML Helper</param>
        /// <param name="name">Name</param>
        /// <returns>Editor</returns>
        public static HtmlString BBCodeEditor<TModel>(this IHtmlHelper<TModel> html, string name)
        {
            var sb = new StringBuilder();

            var storeLocation = EngineContext.Current.Resolve<IWebHelper>().GetStoreLocation();
            string bbEditorWebRoot = String.Format("{0}Content/", storeLocation);

            sb.AppendFormat("<script src=\"{0}Content/BBEditor/ed.js\" type=\"{1}\"></script>", storeLocation, MimeTypes.TextJavascript);
            sb.AppendLine();
            sb.AppendFormat("<script language=\"javascript\" type=\"{0}\">", MimeTypes.TextJavascript);
            sb.AppendLine();
            sb.AppendFormat("edToolbar('{0}','{1}');", name, bbEditorWebRoot);
            sb.AppendLine();
            sb.Append("</script>");
            sb.AppendLine();

            return new HtmlString(sb.ToString());
        }

        //we have two pagers:
        //The first one can have custom routes
        //The second one just adds query string parameter
        public static HtmlString Pager<TModel>(this IHtmlHelper<TModel> html, PagerModel model)
        {
            if (model.TotalRecords == 0)
                return null;

            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            var links = new StringBuilder();
            if (model.ShowTotalSummary && (model.TotalPages > 0))
            {
                links.Append("<li class=\"total-summary\">");
                links.Append(string.Format(model.CurrentPageText, model.PageIndex + 1, model.TotalPages, model.TotalRecords));
                links.Append("</li>");
            }
            if (model.ShowPagerItems && (model.TotalPages > 1))
            {
                if (model.ShowFirst)
                {
                    //first page
                    if ((model.PageIndex >= 3) && (model.TotalPages > model.IndividualPagesDisplayedCount))
                    {
                        model.RouteValues.page = 1;

                        links.Append("<li class=\"first-page\">");
                        if (model.UseRouteLinks)
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.FirstPageTitle"), model.FirstButtonText));
                        }
                        else
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.FirstPageTitle"), model.FirstButtonText));
                        }
                        links.Append("</li>");
                    }
                }
                if (model.ShowPrevious)
                {
                    //previous page
                    if (model.PageIndex > 0)
                    {
                        model.RouteValues.page = (model.PageIndex);

                        links.Append("<li class=\"previous-page\">");
                        if (model.UseRouteLinks)
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.PreviousPageTitle"), model.PreviousButtonText));
                        }
                        else
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.PreviousPageTitle"), model.PreviousButtonText));
                        }
                        links.Append("</li>");
                    }
                }
                if (model.ShowIndividualPages)
                {
                    //individual pages
                    int firstIndividualPageIndex = model.GetFirstIndividualPageIndex();
                    int lastIndividualPageIndex = model.GetLastIndividualPageIndex();
                    for (int i = firstIndividualPageIndex; i <= lastIndividualPageIndex; i++)
                    {
                        if (model.PageIndex == i)
                        {
                            links.AppendFormat("<li class=\"current-page\"><span>{0}</span></li>", (i + 1));
                        }
                        else
                        {
                            model.RouteValues.page = (i + 1);

                            links.Append("<li class=\"individual-page\">");
                            if (model.UseRouteLinks)
                            {
                                links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", String.Format(localizationService.GetResource("Pager.PageLinkTitle"), (i + 1)), (i + 1).ToString()));
                            }
                            else
                            {
                                links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", String.Format(localizationService.GetResource("Pager.PageLinkTitle"), (i + 1)), (i + 1).ToString()));
                            }
                            links.Append("</li>");
                        }
                    }
                }
                if (model.ShowNext)
                {
                    //next page
                    if ((model.PageIndex + 1) < model.TotalPages)
                    {
                        model.RouteValues.page = (model.PageIndex + 2);

                        links.Append("<li class=\"next-page\">");
                        if (model.UseRouteLinks)
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.NextPageTitle"), model.NextButtonText));
                        }
                        else
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.NextPageTitle"), model.NextButtonText));
                        }
                        links.Append("</li>");
                    }
                }
                if (model.ShowLast)
                {
                    //last page
                    if (((model.PageIndex + 3) < model.TotalPages) && (model.TotalPages > model.IndividualPagesDisplayedCount))
                    {
                        model.RouteValues.page = model.TotalPages;

                        links.Append("<li class=\"last-page\">");
                        if (model.UseRouteLinks)
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.LastPageTitle"), model.LastButtonText));
                        }
                        else
                        {
                            links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", localizationService.GetResource("Pager.LastPageTitle"), model.LastButtonText));
                        }
                        links.Append("</li>");
                    }
                }
            }
            var result = links.ToString();
            if (!String.IsNullOrEmpty(result))
            {
                result = "<ul>" + result + "</ul>";
            }
            return new HtmlString(result);
        }
        public static HtmlString ForumTopicSmallPager<TModel>(this IHtmlHelper<TModel> html, ForumTopicRowModel model)
        {
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            var forumTopicId = model.Id;
            var forumTopicSlug = model.SeName;
            var totalPages = model.TotalPostPages;

            if (totalPages > 0)
            {
                var links = new StringBuilder();

                if (totalPages <= 4)
                {
                    for (int x = 1; x <= totalPages; x++)
                    {
                        links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", String.Format(localizationService.GetResource("Pager.PageLinkTitle"), x.ToString()), x.ToString()));
                        if (x < totalPages)
                        {
                            links.Append(", ");
                        }
                    }
                }
                else
                {
                    links.Append(string.Format("<a href=\"#\" title=\"{0}\">1</a>", String.Format(localizationService.GetResource("Pager.PageLinkTitle"), 1)));
                    links.Append(" ... ");

                    for (int x = (totalPages - 2); x <= totalPages; x++)
                    {
                        links.Append(string.Format("<a href=\"#\" title=\"{0}\">{1}</a>", String.Format(localizationService.GetResource("Pager.PageLinkTitle"), x.ToString()), x.ToString()));

                        if (x < totalPages)
                        {
                            links.Append(", ");
                        }
                    }
                }

                // Inserts the topic page links into the localized string ([Go to page: {0}])
                return new HtmlString(String.Format(localizationService.GetResource("Forum.Topics.GotoPostPager"), links.ToString()));
            }
            return new HtmlString(string.Empty);
        }
        public static Pager Pager(this IHtmlHelper helper, IPageableModel pagination)
        {
            return new Pager(pagination, helper.ViewContext);
        }

        /// <summary>
        /// Get topic system name
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="html">HTML helper</param>
        /// <param name="systemName">System name</param>
        /// <returns>Topic SEO Name</returns>
        public static string GetTopicSeName<T>(this IHtmlHelper<T> html, string systemName)
        {
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();

            //static cache manager
            var cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("nop_cache_static");
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
