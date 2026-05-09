using System;
using System.Collections.Specialized;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// Fake HTTP request for use in scenarios where a real HttpRequest is not available.
    /// </summary>
    public class FakeHttpRequest
    {
        private readonly NameValueCollection _cookies;
        private readonly NameValueCollection _formParams;
        private readonly NameValueCollection _queryStringParams;
        private readonly NameValueCollection _headers;
        private readonly NameValueCollection _serverVariables;
        private readonly string _relativeUrl;
        private readonly Uri _url;
        private readonly Uri _urlReferrer;
        private readonly string _httpMethod;

        public FakeHttpRequest(string relativeUrl, string method,
            NameValueCollection formParams, NameValueCollection queryStringParams,
            NameValueCollection cookies, NameValueCollection serverVariables)
        {
            _httpMethod = method;
            _relativeUrl = relativeUrl;
            _formParams = formParams ?? new NameValueCollection();
            _queryStringParams = queryStringParams ?? new NameValueCollection();
            _cookies = cookies ?? new NameValueCollection();
            _serverVariables = serverVariables ?? new NameValueCollection();
            _headers = new NameValueCollection();
        }

        public FakeHttpRequest(string relativeUrl, string method, Uri url, Uri urlReferrer,
            NameValueCollection formParams, NameValueCollection queryStringParams,
            NameValueCollection cookies, NameValueCollection serverVariables)
            : this(relativeUrl, method, formParams, queryStringParams, cookies, serverVariables)
        {
            _url = url;
            _urlReferrer = urlReferrer;
        }

        public FakeHttpRequest(string relativeUrl, Uri url, Uri urlReferrer)
            : this(relativeUrl, "GET", url, urlReferrer, null, null, null, null)
        {
        }

        public virtual NameValueCollection ServerVariables
        {
            get { return _serverVariables; }
        }

        public virtual NameValueCollection Form
        {
            get { return _formParams; }
        }

        public virtual NameValueCollection QueryString
        {
            get { return _queryStringParams; }
        }

        public virtual NameValueCollection Headers
        {
            get { return _headers; }
        }

        public virtual NameValueCollection Cookies
        {
            get { return _cookies; }
        }

        public virtual string AppRelativeCurrentExecutionFilePath
        {
            get { return _relativeUrl; }
        }

        public virtual Uri Url
        {
            get { return _url; }
        }

        public virtual Uri UrlReferrer
        {
            get { return _urlReferrer; }
        }

        public virtual string PathInfo
        {
            get { return ""; }
        }

        public virtual string ApplicationPath
        {
            get
            {
                //we know that relative paths always start with ~/
                //ApplicationPath should start with /
                if (_relativeUrl != null && _relativeUrl.StartsWith("~/"))
                    return _relativeUrl.Remove(0, 1);
                return null;
            }
        }

        public virtual string HttpMethod
        {
            get { return _httpMethod; }
        }

        public virtual string UserHostAddress
        {
            get { return null; }
        }

        public virtual string RawUrl
        {
            get { return null; }
        }

        public virtual bool IsSecureConnection
        {
            get { return false; }
        }

        public virtual bool IsAuthenticated
        {
            get { return false; }
        }

        public virtual string[] UserLanguages
        {
            get { return null; }
        }

        public virtual string Path
        {
            get { return _relativeUrl; }
        }
    }
}
