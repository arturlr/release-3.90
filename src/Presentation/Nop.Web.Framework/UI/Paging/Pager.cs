//Contributor : MVCContrib

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;

namespace Nop.Web.Framework.UI.Paging
{
	/// <summary>
    /// Renders a pager component from an IPageableModel datasource.
	/// </summary>
    /// <remarks>
    /// Ported in task 6.3. <c>System.Web.IHtmlString</c> → <see cref="IHtmlContent"/>
    /// (<see cref="WriteTo"/> added), <c>System.Web.Mvc.ViewContext</c> →
    /// <see cref="Microsoft.AspNetCore.Mvc.Rendering.ViewContext"/>, and the whole of
    /// <see cref="CreateDefaultUrl"/> re-based off the current request — see the note there.
    /// The instance method <see cref="ToHtmlString"/> is retained so existing view call sites are
    /// unaffected.
    /// </remarks>
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
        protected bool renderEmptyParameters = true;
        protected int individualPagesDisplayedCount = 5;
        protected Func<int, string> urlBuilder;
        protected IList<string> booleanParameterNames;

		public Pager(IPageableModel model, ViewContext context)
		{
            this.model = model;
            this.viewContext = context;
            this.urlBuilder = CreateDefaultUrl;
            this.booleanParameterNames = new List<string>();
		}

		protected ViewContext ViewContext 
		{
			get { return viewContext; }
		}
        
        public Pager QueryParam(string value)
		{
            this.pageQueryName = value;
			return this;
		}
        public Pager ShowTotalSummary(bool value)
        {
            this.showTotalSummary = value;
            return this;
        }
        public Pager ShowPagerItems(bool value)
        {
            this.showPagerItems = value;
            return this;
        }
        public Pager ShowFirst(bool value)
        {
            this.showFirst = value;
            return this;
        }
        public Pager ShowPrevious(bool value)
        {
            this.showPrevious = value;
            return this;
        }
        public Pager ShowNext(bool value)
        {
            this.showNext = value;
            return this;
        }
        public Pager ShowLast(bool value)
        {
            this.showLast = value;
            return this;
        }
        public Pager ShowIndividualPages(bool value)
        {
            this.showIndividualPages = value;
            return this;
        }
        public Pager RenderEmptyParameters(bool value)
        {
            this.renderEmptyParameters = value;
            return this;
        }
        public Pager IndividualPagesDisplayedCount(int value)
        {
            this.individualPagesDisplayedCount = value;
            return this;
        }
		public Pager Link(Func<int, string> value)
		{
            this.urlBuilder = value;
			return this;
		}
        //little hack here due to ugly MVC implementation
        //find more info here: http://www.mindstorminteractive.com/topics/jquery-fix-asp-net-mvc-checkbox-truefalse-value/
        public Pager BooleanParameterName(string paramName)
        {
            booleanParameterNames.Add(paramName);
            return this;
        }

        public override string ToString()
        {
            return ToHtmlString();
        }

        /// <summary>
        /// Write the pager markup.
        /// </summary>
        /// <remarks>
        /// The <see cref="IHtmlContent"/> member that replaces <c>IHtmlString.ToHtmlString()</c>
        /// as the framework-facing contract. The markup is already encoded, so it is written
        /// verbatim and <paramref name="encoder"/> is intentionally unused - matching
        /// <c>IHtmlString</c> semantics.
        /// </remarks>
        public virtual void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            var html = ToHtmlString();
            if (!string.IsNullOrEmpty(html))
                writer.Write(html);
        }

		public virtual string ToHtmlString()
		{
            if (model.TotalItems == 0) 
				return null;
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
                if (showFirst)
                {
                    //first page
                    if ((model.PageIndex >= 3) && (model.TotalPages > individualPagesDisplayedCount))
                    {
                        links.Append(CreatePageLink(1, localizationService.GetResource("Pager.First"), "first-page"));
                    }
                }
                if (showPrevious)
                {
                    //previous page
                    if (model.PageIndex > 0)
                    {
                        links.Append(CreatePageLink(model.PageIndex, localizationService.GetResource("Pager.Previous"), "previous-page"));
                    }
                }
                if (showIndividualPages)
                {
                    //individual pages
                    int firstIndividualPageIndex = GetFirstIndividualPageIndex();
                    int lastIndividualPageIndex = GetLastIndividualPageIndex();
                    for (int i = firstIndividualPageIndex; i <= lastIndividualPageIndex; i++)
                    {
                        if (model.PageIndex == i)
                        {
                            links.AppendFormat("<li class=\"current-page\"><span>{0}</span></li>", (i + 1));
                        }
                        else
                        {
                            links.Append(CreatePageLink(i + 1, (i + 1).ToString(), "individual-page"));
                        }
                    }
                }
                if (showNext)
                {
                    //next page
                    if ((model.PageIndex + 1) < model.TotalPages)
                    {
                        links.Append(CreatePageLink(model.PageIndex + 2, localizationService.GetResource("Pager.Next"), "next-page"));
                    }
                }
                if (showLast)
                {
                    //last page
                    if (((model.PageIndex + 3) < model.TotalPages) && (model.TotalPages > individualPagesDisplayedCount))
                    {
                        links.Append(CreatePageLink(model.TotalPages, localizationService.GetResource("Pager.Last"), "last-page"));
                    }
                }
            }

            var result = links.ToString();
            if (!String.IsNullOrEmpty(result))
            {
                result = "<ul>" + result + "</ul>";
            }
            return result;
		}
	    public virtual bool IsEmpty()
	    {
            var html = ToString();
	        return string.IsNullOrEmpty(html);
	    }

        protected virtual int GetFirstIndividualPageIndex()
        {
            if ((model.TotalPages < individualPagesDisplayedCount) ||
                ((model.PageIndex - (individualPagesDisplayedCount / 2)) < 0))
            {
                return 0;
            }
            if ((model.PageIndex + (individualPagesDisplayedCount / 2)) >= model.TotalPages)
            {
                return (model.TotalPages - individualPagesDisplayedCount);
            }
            return (model.PageIndex - (individualPagesDisplayedCount / 2));
        }
        protected virtual int GetLastIndividualPageIndex()
        {
            int num = individualPagesDisplayedCount / 2;
            if ((individualPagesDisplayedCount % 2) == 0)
            {
                num--;
            }
            if ((model.TotalPages < individualPagesDisplayedCount) ||
                ((model.PageIndex + num) >= model.TotalPages))
            {
                return (model.TotalPages - 1);
            }
            if ((model.PageIndex - (individualPagesDisplayedCount / 2)) < 0)
            {
                return (individualPagesDisplayedCount - 1);
            }
            return (model.PageIndex + num);
        }
		protected virtual string CreatePageLink(int pageNumber, string text, string cssClass)
		{
            var liBuilder = new TagBuilder("li");
            if (!String.IsNullOrWhiteSpace(cssClass))
                liBuilder.AddCssClass(cssClass);

			var aBuilder = new TagBuilder("a");
            //SetInnerText -> InnerHtml.SetContent: both HTML-encode
            aBuilder.InnerHtml.SetContent(text);
            aBuilder.MergeAttribute("href", urlBuilder(pageNumber));
            aBuilder.TagRenderMode = TagRenderMode.Normal;

            //3.90 did "liBuilder.InnerHtml += aBuilder", relying on TagBuilder.ToString()
            liBuilder.InnerHtml.AppendHtml(aBuilder);

            return liBuilder.ToHtmlString(TagRenderMode.Normal);
		}

        /// <summary>
        /// Build the URL for a page number by re-emitting the current request with a modified
        /// page query-string parameter.
        /// </summary>
        /// <remarks>
        /// <b>This is a reimplementation, not a rename.</b> 3.90 called
        /// <c>UrlHelper.GenerateUrl(null, null, null, routeValues, RouteTable.Routes,
        /// viewContext.RequestContext, includeImplicitMvcValues: true)</c>: it took the query
        /// string, folded in the implicit action/controller/area of the current request, and asked
        /// the global <c>RouteTable</c> to regenerate a matching URL. ASP.NET Core has neither a
        /// static route table nor <c>UrlHelper.GenerateUrl</c>, and <c>LinkGenerator</c> is not
        /// equivalent: nopCommerce's SEO URLs are produced by <c>GenericPathRoute</c> (task 6.4),
        /// so round-tripping through link generation could not reproduce a slug-based path.
        ///
        /// Since every value fed into <c>routeValues</c> here comes from the QUERY STRING and the
        /// only thing that changes is the page parameter, the current path is by definition the
        /// correct path. This therefore keeps the current <c>PathBase + Path</c> and rebuilds only
        /// the query string. For the SEO routes that matter (<c>/category-slug?pagenumber=2</c>)
        /// the output is the same string 3.90 produced, and it no longer depends on a route
        /// existing that can regenerate the URL.
        ///
        /// Values are URL-encoded via <see cref="QueryString"/>, where MVC 5's
        /// <c>GenerateUrl</c> also encoded them. The <c>renderEmptyParameters</c> hack and its
        /// <c>IWebHelper.ModifyQueryString</c> follow-up are preserved verbatim.
        /// </remarks>
        protected virtual string CreateDefaultUrl(int pageNumber)
		{
			var routeValues = new RouteValueDictionary();

            var request = viewContext.HttpContext.Request;

            var parametersWithEmptyValues = new List<string>();
			foreach (var key in request.Query.Keys.Where(key => key != null))
			{
                var value = request.Query[key].ToString();
                if (renderEmptyParameters && String.IsNullOrEmpty(value))
			    {
                    //we store query string parameters with empty values separately
                    //we need to do it because they are not properly processed in the UrlHelper.GenerateUrl method (dropped for some reasons)
                    parametersWithEmptyValues.Add(key);
			    }
			    else
                {
                    if (booleanParameterNames.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                    {
                        //little hack here due to ugly MVC implementation
                        //find more info here: http://www.mindstorminteractive.com/topics/jquery-fix-asp-net-mvc-checkbox-truefalse-value/
                        if (!String.IsNullOrEmpty(value) && value.Equals("true,false", StringComparison.InvariantCultureIgnoreCase))
                        {
                            value = "true";
                        }
                    }
                    routeValues[key] = value;
			    }
			}

            if (pageNumber > 1)
            {
                routeValues[pageQueryName] = pageNumber;
            }
            else
            {
                //SEO. we do not render pageindex query string parameter for the first page
                if (routeValues.ContainsKey(pageQueryName))
                {
                    routeValues.Remove(pageQueryName);
                }
            }

            var queryBuilder = new QueryBuilder();
            foreach (var routeValue in routeValues)
                queryBuilder.Add(routeValue.Key, routeValue.Value?.ToString() ?? string.Empty);

            var url = request.PathBase.Add(request.Path).ToString() + queryBuilder.ToQueryString().ToString();

            if (renderEmptyParameters && parametersWithEmptyValues.Any())
            {
                //we add such parameters manually because UrlHelper.GenerateUrl() ignores them
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                foreach (var key in parametersWithEmptyValues)
                {
                    url = webHelper.ModifyQueryString(url, key + "=", null);
                }
            }
			return url;
		}
	}
}
