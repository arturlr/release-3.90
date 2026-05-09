using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Principal;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// Fake HTTP context for use in scenarios where a real HttpContext is not available
    /// (e.g., scheduled tasks, unit tests).
    /// </summary>
    public class FakeHttpContext
    {
        private readonly IDictionary _items;
        private IPrincipal _principal;
        private FakeHttpRequest _request;
        private FakeHttpResponse _response;
        private FakeHttpSessionState _session;

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
            NameValueCollection queryStringParams, NameValueCollection cookies,
            Dictionary<string, object> sessionItems, NameValueCollection serverVariables)
            : this(relativeUrl, null, principal, formParams, queryStringParams, cookies, sessionItems, serverVariables)
        {
        }

        public FakeHttpContext(string relativeUrl, string method,
            IPrincipal principal, NameValueCollection formParams,
            NameValueCollection queryStringParams, NameValueCollection cookies,
            Dictionary<string, object> sessionItems, NameValueCollection serverVariables)
        {
            _principal = principal;
            _request = new FakeHttpRequest(relativeUrl, method, formParams, queryStringParams, cookies, serverVariables);
            _response = new FakeHttpResponse();
            _session = new FakeHttpSessionState(sessionItems);
            _items = new Hashtable();
        }

        public virtual FakeHttpRequest Request
        {
            get { return _request; }
        }

        public void SetRequest(FakeHttpRequest request)
        {
            _request = request;
        }

        public virtual FakeHttpResponse Response
        {
            get { return _response; }
        }

        public void SetResponse(FakeHttpResponse response)
        {
            _response = response;
        }

        public virtual IPrincipal User
        {
            get { return _principal; }
            set { _principal = value; }
        }

        public virtual FakeHttpSessionState Session
        {
            get { return _session; }
        }

        public virtual IDictionary Items
        {
            get { return _items; }
        }

        public virtual bool SkipAuthorization { get; set; }

        public virtual object GetService(Type serviceType)
        {
            return null;
        }
    }
}
