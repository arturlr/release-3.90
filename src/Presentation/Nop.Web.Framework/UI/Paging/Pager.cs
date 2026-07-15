using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;

namespace Nop.Web.Framework.UI.Paging
{
    public partial class Pager : IHtmlContent
    {
        protected readonly IPageableModel model;
        protected readonly ViewContext viewContext;
        protected string pageQueryName = "page";
        protected bool showTotalSummary;
        protected bool showPagerItems = true;
        protected bool showFirst = true;
        protected bool showPrevious = true;
        protected bool showNext = true;
        protected bool showLast = true;
        protected bool showIndividualPages = true;
        protected int individualPagesDisplayedCount = 5;
        protected Func<int, string> urlBuilder;

        public Pager(IPageableModel model, ViewContext context)
        {
            this.model = model;
            this.viewContext = context;
            this.urlBuilder = CreateDefaultUrl;
        }

        public Pager QueryParam(string value) { this.pageQueryName = value; return this; }
        public Pager ShowTotalSummary(bool value) { this.showTotalSummary = value; return this; }
        public Pager ShowPagerItems(bool value) { this.showPagerItems = value; return this; }
        public Pager ShowFirst(bool value) { this.showFirst = value; return this; }
        public Pager ShowPrevious(bool value) { this.showPrevious = value; return this; }
        public Pager ShowNext(bool value) { this.showNext = value; return this; }
        public Pager ShowLast(bool value) { this.showLast = value; return this; }
        public Pager ShowIndividualPages(bool value) { this.showIndividualPages = value; return this; }
        public Pager IndividualPagesDisplayedCount(int value) { this.individualPagesDisplayedCount = value; return this; }
        public Pager Link(Func<int, string> value) { this.urlBuilder = value; return this; }

        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder)
        {
            var html = ToHtmlString();
            if (!string.IsNullOrEmpty(html))
                writer.Write(html);
        }

        public virtual string ToHtmlString()
        {
            if (model.TotalItems == 0) return null;
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            var links = new StringBuilder();

            if (showTotalSummary && (model.TotalPages > 0))
            {
                links.Append("<li class=\"total-summary\">");
                links.Append(string.Format(localizationService.GetResource("Pager.CurrentPage"), model.PageIndex + 1, model.TotalPages, model.TotalItems));
                links.Append("</li>");
            }
            if (showPagerItems && (model.TotalPages > 1))
            {
                if (showFirst && (model.PageIndex >= 3) && (model.TotalPages > individualPagesDisplayedCount))
                    links.Append(CreatePageLink(1, localizationService.GetResource("Pager.First"), "first-page"));
                if (showPrevious && model.PageIndex > 0)
                    links.Append(CreatePageLink(model.PageIndex, localizationService.GetResource("Pager.Previous"), "previous-page"));
                if (showIndividualPages)
                {
                    int first = GetFirstIndividualPageIndex();
                    int last = GetLastIndividualPageIndex();
                    for (int i = first; i <= last; i++)
                    {
                        if (model.PageIndex == i)
                            links.AppendFormat("<li class=\"current-page\"><span>{0}</span></li>", (i + 1));
                        else
                            links.Append(CreatePageLink(i + 1, (i + 1).ToString(), "individual-page"));
                    }
                }
                if (showNext && (model.PageIndex + 1) < model.TotalPages)
                    links.Append(CreatePageLink(model.PageIndex + 2, localizationService.GetResource("Pager.Next"), "next-page"));
                if (showLast && ((model.PageIndex + 3) < model.TotalPages) && (model.TotalPages > individualPagesDisplayedCount))
                    links.Append(CreatePageLink(model.TotalPages, localizationService.GetResource("Pager.Last"), "last-page"));
            }
            var result = links.ToString();
            if (!String.IsNullOrEmpty(result)) result = "<ul>" + result + "</ul>";
            return result;
        }

        protected virtual int GetFirstIndividualPageIndex()
        {
            if ((model.TotalPages < individualPagesDisplayedCount) || ((model.PageIndex - (individualPagesDisplayedCount / 2)) < 0)) return 0;
            if ((model.PageIndex + (individualPagesDisplayedCount / 2)) >= model.TotalPages) return (model.TotalPages - individualPagesDisplayedCount);
            return (model.PageIndex - (individualPagesDisplayedCount / 2));
        }
        protected virtual int GetLastIndividualPageIndex()
        {
            int num = individualPagesDisplayedCount / 2;
            if ((individualPagesDisplayedCount % 2) == 0) num--;
            if ((model.TotalPages < individualPagesDisplayedCount) || ((model.PageIndex + num) >= model.TotalPages)) return (model.TotalPages - 1);
            if ((model.PageIndex - (individualPagesDisplayedCount / 2)) < 0) return (individualPagesDisplayedCount - 1);
            return (model.PageIndex + num);
        }
        protected virtual string CreatePageLink(int pageNumber, string text, string cssClass)
        {
            var li = new TagBuilder("li");
            if (!String.IsNullOrWhiteSpace(cssClass)) li.AddCssClass(cssClass);
            var a = new TagBuilder("a");
            a.InnerHtml.Append(text);
            a.MergeAttribute("href", urlBuilder(pageNumber));
            using (var sw = new System.IO.StringWriter())
            {
                a.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
                li.InnerHtml.AppendHtml(sw.ToString());
            }
            using (var sw = new System.IO.StringWriter())
            {
                li.WriteTo(sw, System.Text.Encodings.Web.HtmlEncoder.Default);
                return sw.ToString();
            }
        }
        protected virtual string CreateDefaultUrl(int pageNumber)
        {
            var request = viewContext.HttpContext.Request;
            var url = request.Path.Value;
            var query = request.QueryString.Value ?? "";
            // Simple query string manipulation for page parameter
            if (pageNumber > 1)
            {
                if (query.Contains(pageQueryName + "="))
                    url += System.Text.RegularExpressions.Regex.Replace(query, pageQueryName + @"=\d+", pageQueryName + "=" + pageNumber);
                else
                    url += (string.IsNullOrEmpty(query) ? "?" : query + "&") + pageQueryName + "=" + pageNumber;
            }
            else
            {
                url += query;
            }
            return url;
        }
    }
}
