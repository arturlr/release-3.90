using System;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework.UI.Paging
{
    /// <summary>
    /// Renders a pager component
    /// </summary>
    public partial class Pager : IHtmlContent
    {
        private readonly IPageableModel _model;
        private readonly ViewContext _viewContext;
        private string _pageQueryName = "page";
        private bool _showTotalSummary;
        private bool _showPagerItems = true;
        private bool _showFirst = true;
        private bool _showPrevious = true;
        private bool _showNext = true;
        private bool _showLast = true;
        private bool _showIndividualPages = true;
        private int _individualPagesDisplayedCount = 5;
        private Func<int, string> _urlBuilder;

        public Pager(IPageableModel model, ViewContext context)
        {
            _model = model;
            _viewContext = context;
        }

        public Pager QueryParam(string value) { _pageQueryName = value; return this; }
        public Pager ShowTotalSummary(bool value) { _showTotalSummary = value; return this; }
        public Pager ShowPagerItems(bool value) { _showPagerItems = value; return this; }
        public Pager ShowFirst(bool value) { _showFirst = value; return this; }
        public Pager ShowPrevious(bool value) { _showPrevious = value; return this; }
        public Pager ShowNext(bool value) { _showNext = value; return this; }
        public Pager ShowLast(bool value) { _showLast = value; return this; }
        public Pager ShowIndividualPages(bool value) { _showIndividualPages = value; return this; }
        public Pager IndividualPagesDisplayedCount(int value) { _individualPagesDisplayedCount = value; return this; }
        public Pager Link(Func<int, string> value) { _urlBuilder = value; return this; }

        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder)
        {
            writer.Write(ToString());
        }

        public override string ToString()
        {
            if (_model.TotalPages <= 1) return string.Empty;
            var sb = new StringBuilder();
            sb.Append("<ul class=\"pagination\">");
            if (_showFirst && _model.PageIndex > 0)
                sb.AppendFormat("<li class=\"first-page\"><a href=\"{0}\">First</a></li>", CreateUrl(1));
            if (_showPrevious && _model.PageIndex > 0)
                sb.AppendFormat("<li class=\"previous-page\"><a href=\"{0}\">Previous</a></li>", CreateUrl(_model.PageIndex));
            if (_showIndividualPages)
            {
                int start = Math.Max(1, _model.PageIndex + 1 - _individualPagesDisplayedCount / 2);
                int end = Math.Min(_model.TotalPages, start + _individualPagesDisplayedCount - 1);
                for (int i = start; i <= end; i++)
                {
                    if (i == _model.PageIndex + 1)
                        sb.AppendFormat("<li class=\"active\"><span>{0}</span></li>", i);
                    else
                        sb.AppendFormat("<li><a href=\"{0}\">{1}</a></li>", CreateUrl(i), i);
                }
            }
            if (_showNext && _model.PageIndex < _model.TotalPages - 1)
                sb.AppendFormat("<li class=\"next-page\"><a href=\"{0}\">Next</a></li>", CreateUrl(_model.PageIndex + 2));
            if (_showLast && _model.PageIndex < _model.TotalPages - 1)
                sb.AppendFormat("<li class=\"last-page\"><a href=\"{0}\">Last</a></li>", CreateUrl(_model.TotalPages));
            sb.Append("</ul>");
            if (_showTotalSummary)
                sb.AppendFormat("<span class=\"pager-summary\">Page {0} of {1} ({2} items)</span>", _model.PageIndex + 1, _model.TotalPages, _model.TotalItems);
            return sb.ToString();
        }

        private string CreateUrl(int page)
        {
            if (_urlBuilder != null) return _urlBuilder(page);
            return $"?{_pageQueryName}={page}";
        }
    }
}
