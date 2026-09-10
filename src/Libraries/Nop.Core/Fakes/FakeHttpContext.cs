using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// An <see cref="HttpContext"/> that can be constructed outside of a request, used both
    /// by unit tests and by the runtime when nopCommerce code runs without a real request
    /// (scheduled tasks, installation).
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 5): re-based from <c>System.Web.HttpContextBase</c> onto
    /// ASP.NET Core's <see cref="HttpContext"/>. Because
    /// <see cref="DefaultHttpContext"/> already supplies working
    /// <see cref="HttpContext.Request"/>, <see cref="HttpContext.Response"/> and
    /// <see cref="HttpContext.Items"/> implementations, the hand-rolled
    /// <c>FakeHttpRequest</c>, <c>FakeHttpResponse</c> and <c>FakeHttpSessionState</c> classes
    /// were removed; this type just seeds the built-in request with the supplied values.
    /// <see cref="DefaultHttpContext"/> is sealed, so it is composed rather than inherited and
    /// every <see cref="HttpContext"/> member delegates to it - <c>x is FakeHttpContext</c>
    /// checks (Nop.Web.Framework) therefore keep working.
    ///
    /// Constructor arity and parameter order are unchanged so existing call sites keep
    /// compiling, but three parameter types had to move to ASP.NET Core equivalents:
    ///   * <c>HttpCookieCollection cookies</c>            -&gt; <c>IDictionary&lt;string, string&gt;</c>
    ///   * <c>SessionStateItemCollection sessionItems</c>  -&gt; <c>IDictionary&lt;string, object&gt;</c>
    ///   * <c>IPrincipal principal</c> is still accepted but is projected onto
    ///     <see cref="ClaimsPrincipal"/>, which is what <see cref="HttpContext.User"/> exposes.
    /// <c>NameValueCollection</c> is retained for form/query/server-variable seeds so that
    /// existing test call sites are untouched.
    /// </remarks>
    public class FakeHttpContext : HttpContext
    {
        #region Fields

        private readonly DefaultHttpContext _inner = new DefaultHttpContext();
        private readonly string _relativeUrl;

        #endregion

        #region Ctor

        public static FakeHttpContext Root()
        {
            return new FakeHttpContext("~/");
        }

        public FakeHttpContext(string relativeUrl, string method)
            : this(relativeUrl, method, null, null, null, null, null, null)
        {
        }

        public FakeHttpContext(string relativeUrl)
            : this(relativeUrl, null, null, null, null, null, null, null)
        {
        }

        public FakeHttpContext(string relativeUrl,
            IPrincipal principal, NameValueCollection formParams,
            NameValueCollection queryStringParams, IDictionary<string, string> cookies,
            IDictionary<string, object> sessionItems, NameValueCollection serverVariables)
            : this(relativeUrl, null, principal, formParams, queryStringParams, cookies, sessionItems, serverVariables)
        {
        }

        public FakeHttpContext(string relativeUrl, string method,
            IPrincipal principal, NameValueCollection formParams,
            NameValueCollection queryStringParams, IDictionary<string, string> cookies,
            IDictionary<string, object> sessionItems, NameValueCollection serverVariables)
        {
            _relativeUrl = relativeUrl;

            ApplyUrl(relativeUrl);

            if (!string.IsNullOrEmpty(method))
                Request.Method = method;

            if (principal != null)
                User = principal as ClaimsPrincipal ?? new ClaimsPrincipal(principal);

            //classic server variables were request headers with '-' replaced by '_' and an
            //optional HTTP_ prefix; seed them as headers so IWebHelper.ServerVariables resolves
            //them (see WebHelper.ServerVariableNameToHeaderName)
            ApplyToHeaders(serverVariables);

            if (queryStringParams != null)
            {
                Request.Query = new QueryCollection(ToStringValuesDictionary(queryStringParams));
                Request.QueryString = BuildQueryString(queryStringParams);
            }

            if (formParams != null)
                Request.Form = new FormCollection(ToStringValuesDictionary(formParams));

            if (cookies != null && cookies.Count > 0)
            {
                //Request.Cookies is parsed from the Cookie header
                Request.Headers["Cookie"] = new StringValues(cookies
                    .Select(c => string.Format("{0}={1}", c.Key, c.Value))
                    .ToArray());
            }

            Session = new FakeSession(sessionItems);
        }

        #endregion

        #region HttpContext members (delegated to the composed DefaultHttpContext)

        public override IFeatureCollection Features
        {
            get { return _inner.Features; }
        }

        public override HttpRequest Request
        {
            get { return _inner.Request; }
        }

        public override HttpResponse Response
        {
            get { return _inner.Response; }
        }

        public override ConnectionInfo Connection
        {
            get { return _inner.Connection; }
        }

        public override WebSocketManager WebSockets
        {
            get { return _inner.WebSockets; }
        }

        public override ClaimsPrincipal User
        {
            get { return _inner.User; }
            set { _inner.User = value; }
        }

        public override IDictionary<object, object> Items
        {
            get { return _inner.Items; }
            set { _inner.Items = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return _inner.RequestServices; }
            set { _inner.RequestServices = value; }
        }

        public override CancellationToken RequestAborted
        {
            get { return _inner.RequestAborted; }
            set { _inner.RequestAborted = value; }
        }

        public override string TraceIdentifier
        {
            get { return _inner.TraceIdentifier; }
            set { _inner.TraceIdentifier = value; }
        }

        public override ISession Session
        {
            get { return _inner.Session; }
            set { _inner.Session = value; }
        }

        public override void Abort()
        {
            _inner.Abort();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Splits an app-relative url ("~/somepath") into PathBase / Path the same way
        /// System.Web's <c>Request.ApplicationPath</c> and <c>Request.Path</c> used to
        /// </summary>
        private void ApplyUrl(string relativeUrl)
        {
            //we know that relative paths always start with ~/, and ApplicationPath started with /
            var applicationPath = "/";
            if (!string.IsNullOrEmpty(relativeUrl) && relativeUrl.StartsWith("~/"))
                applicationPath = relativeUrl.Remove(0, 1);

            Request.PathBase = new PathString(applicationPath.TrimEnd('/'));
            Request.Path = new PathString("/");
            Request.Scheme = "http";
        }

        private void ApplyToHeaders(NameValueCollection values)
        {
            if (values == null)
                return;

            foreach (var key in values.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
            {
                var headerName = key.StartsWith("HTTP_", StringComparison.OrdinalIgnoreCase)
                    ? key.Substring("HTTP_".Length).Replace('_', '-')
                    : key;

                Request.Headers[headerName] = new StringValues(values.GetValues(key));
            }
        }

        private static Dictionary<string, StringValues> ToStringValuesDictionary(NameValueCollection values)
        {
            var result = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            if (values == null)
                return result;

            foreach (var key in values.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
                result[key] = new StringValues(values.GetValues(key));

            return result;
        }

        private static QueryString BuildQueryString(NameValueCollection values)
        {
            if (values == null || values.Count == 0)
                return QueryString.Empty;

            var builder = new QueryBuilder();
            foreach (var key in values.AllKeys.Where(k => !string.IsNullOrEmpty(k)))
            {
                foreach (var value in values.GetValues(key) ?? new string[0])
                    builder.Add(key, value ?? string.Empty);
            }

            return builder.ToQueryString();
        }

        #endregion

        #region Properties

        /// <summary>
        /// The app-relative url this context was created for (e.g. "~/")
        /// </summary>
        public string RelativeUrl
        {
            get { return _relativeUrl; }
        }

        #endregion
    }
}
