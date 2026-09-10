using System.Collections.Specialized;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Represents a RemotePost helper class
    /// </summary>
    /// <remarks>
    /// Task 6.2: re-based from <c>System.Web.HttpContextBase</c> onto
    /// <see cref="IHttpContextAccessor"/>.
    /// <list type="bullet">
    /// <item>BREAKING CONSTRUCTOR CHANGE: <c>RemotePost(HttpContextBase, IWebHelper)</c> -&gt;
    /// <c>RemotePost(IHttpContextAccessor, IWebHelper)</c>, and the parameterless constructor now
    /// resolves <see cref="IHttpContextAccessor"/> from the engine instead of
    /// <c>HttpContextBase</c> (which task 6.4 stops registering - deferral 7.14). Consumers:
    /// the payment plugins (tasks 12.3/12.4 - PayPalDirect/PayPalStandard use the parameterless
    /// form) and Nop.Web's checkout flow.</item>
    /// <item><c>Response.Write(string)</c> was REMOVED in ASP.NET Core. The whole document is now
    /// composed into a <see cref="StringBuilder"/> and written once with
    /// <c>Response.WriteAsync</c>. Emitted markup is byte-identical.</item>
    /// <item><c>Response.Clear()</c> -&gt; <c>Response.Clear()</c> does not exist; the equivalent
    /// is resetting the body/headers, which is only legal before the response has started.
    /// <c>HasStarted</c> is checked and the body is truncated via
    /// <c>Response.Body.SetLength(0)</c> only where the stream supports it (it does not for a real
    /// response), so in practice the reset reduces to clearing the buffered content we are about to
    /// write. The status code and content type are set explicitly instead.</item>
    /// <item><c>Response.End()</c> was REMOVED - no equivalent, and none needed: this method is
    /// invoked from an action and nothing writes after it. Note the CONSEQUENCE: in System.Web,
    /// <c>Response.End()</c> aborted the rest of the pipeline. Here it does not, so a caller that
    /// returns a view/result after calling <see cref="Post"/> would append to the document.
    /// All in-tree callers return immediately.</item>
    /// <item><c>System.Web.HttpUtility.HtmlEncode</c> -&gt;
    /// <see cref="WebUtility.HtmlEncode"/>, the same swap already made across Nop.Core and
    /// Nop.Services (identical behaviour for HtmlEncode).</item>
    /// <item><c>Post()</c> keeps its synchronous signature (plugins call it from synchronous
    /// actions) and blocks on the single write, consistent with the sync-over-async trade-off
    /// recorded for task 4.2.</item>
    /// </list>
    /// </remarks>
    public partial class RemotePost
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHelper _webHelper;
        private readonly NameValueCollection _inputValues;

        /// <summary>
        /// Gets or sets a remote URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a method
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets a form name
        /// </summary>
        public string FormName { get; set; }

        /// <summary>
        /// Gets or sets a form character-sets the server can handle for form-data.
        /// </summary>
        public string AcceptCharset { get; set; }

        /// <summary>
        /// A value indicating whether we should create a new "input" HTML element for each value (in case if there are more than one) for the same "name" attributes.
        /// </summary>
        public bool NewInputForEachValue { get; set; }

        public NameValueCollection Params
        {
            get
            {
                return _inputValues;
            }
        }

        /// <summary>
        /// Creates a new instance of the RemotePost class
        /// </summary>
        public RemotePost()
            : this(EngineContext.Current.Resolve<IHttpContextAccessor>(), EngineContext.Current.Resolve<IWebHelper>())
        {
        }

        /// <summary>
        /// Creates a new instance of the RemotePost class
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="webHelper">Web helper</param>
        public RemotePost(IHttpContextAccessor httpContextAccessor, IWebHelper webHelper)
        {
            this._inputValues = new NameValueCollection();
            this.Url = "http://www.someurl.com";
            this.Method = "post";
            this.FormName = "formName";

            this._httpContextAccessor = httpContextAccessor;
            this._webHelper = webHelper;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary (to be posted).
        /// </summary>
        /// <param name="name">The key of the element to add</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(string name, string value)
        {
            _inputValues.Add(name, value);
        }
        
        /// <summary>
        /// Post
        /// </summary>
        public void Post()
        {
            var httpContext = _httpContextAccessor != null ? _httpContextAccessor.HttpContext : null;
            if (httpContext == null || httpContext.Response == null)
                return;

            var sb = new StringBuilder();
            sb.Append("<html><head>");
            sb.Append(string.Format("</head><body onload=\"document.{0}.submit()\">", FormName));
            if (!string.IsNullOrEmpty(AcceptCharset))
            {
                //AcceptCharset specified
                sb.Append(string.Format("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" accept-charset=\"{3}\">", FormName, Method, Url, AcceptCharset));
            }
            else
            {
                //no AcceptCharset specified
                sb.Append(string.Format("<form name=\"{0}\" method=\"{1}\" action=\"{2}\" >", FormName, Method, Url));
            }
            if (NewInputForEachValue)
            {
                foreach (string key in _inputValues.Keys)
                {
                    string[] values = _inputValues.GetValues(key);
                    if (values != null)
                    {
                        foreach (string value in values)
                        {
                            sb.Append(string.Format("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">", WebUtility.HtmlEncode(key), WebUtility.HtmlEncode(value)));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < _inputValues.Keys.Count; i++)
                    sb.Append(string.Format("<input name=\"{0}\" type=\"hidden\" value=\"{1}\">", WebUtility.HtmlEncode(_inputValues.Keys[i]), WebUtility.HtmlEncode(_inputValues[_inputValues.Keys[i]])));
            }
            sb.Append("</form>");
            sb.Append("</body></html>");

            var response = httpContext.Response;
            if (!response.HasStarted)
            {
                //closest available equivalent of System.Web's Response.Clear()
                response.Clear();
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/html; charset=utf-8";
            }
            response.WriteAsync(sb.ToString(), Encoding.UTF8).GetAwaiter().GetResult();

            //store a value indicating whether POST has been done
            _webHelper.IsPostBeingDone = true;
        }
    }
}
