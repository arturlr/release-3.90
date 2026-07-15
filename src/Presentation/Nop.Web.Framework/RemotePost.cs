using System.Collections.Specialized;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
    public partial class RemotePost
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly NameValueCollection _inputValues;

        public string Url { get; set; }
        public string Method { get; set; }
        public string FormName { get; set; }
        public string AcceptCharset { get; set; }
        public bool NewInputForEachValue { get; set; }

        public NameValueCollection Params
        {
            get { return _inputValues; }
        }

        public RemotePost()
            : this(EngineContext.Current.Resolve<IHttpContextAccessor>(), EngineContext.Current.Resolve<IWebHelper>())
        {
        }

        public RemotePost(IHttpContextAccessor httpContextAccessor, IWebHelper webHelper)
        {
            this._inputValues = new NameValueCollection();
            this.Url = "http://www.someurl.com";
            this.Method = "post";
            this.FormName = "formName";
            this._httpContextAccessor = httpContextAccessor;
            this._webHelper = webHelper;
        }

        public void Add(string name, string value)
        {
            _inputValues.Add(name, value);
        }

        public void Post()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var response = httpContext.Response;
            response.Clear();

            var sb = new StringBuilder();
            sb.Append("<html><head>");
            sb.AppendFormat("</head><body onload=\"document.{0}.submit()\">", FormName);
            if (!string.IsNullOrEmpty(AcceptCharset))
                sb.AppendFormat("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" accept-charset=\"{3}\">", FormName, Method, Url, AcceptCharset);
            else
                sb.AppendFormat("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" >", FormName, Method, Url);

            if (NewInputForEachValue)
            {
                foreach (string key in _inputValues.Keys)
                {
                    string[] values = _inputValues.GetValues(key);
                    if (values != null)
                    {
                        foreach (string value in values)
                        {
                            sb.AppendFormat("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">",
                                WebUtility.HtmlEncode(key), WebUtility.HtmlEncode(value));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < _inputValues.Keys.Count; i++)
                    sb.AppendFormat("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">",
                        WebUtility.HtmlEncode(_inputValues.Keys[i]),
                        WebUtility.HtmlEncode(_inputValues[_inputValues.Keys[i]]));
            }
            sb.Append("</form>");
            sb.Append("</body></html>");

            response.ContentType = "text/html";
            response.WriteAsync(sb.ToString()).GetAwaiter().GetResult();

            _webHelper.IsPostBeingDone = true;
        }
    }
}
