using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Core
{
    /// <summary>
    /// Represents a common helper
    /// </summary>
    /// <remarks>
    /// Task 2.4 (design section 5): reimplemented against
    /// <see cref="Microsoft.AspNetCore.Http.HttpContext"/> reached through
    /// <see cref="IHttpContextAccessor"/>, replacing the <c>System.Web.HttpContextBase</c>
    /// the class was originally constructed with. Every <see cref="IWebHelper"/> member is
    /// preserved; the ASP.NET Core equivalents used for each are documented inline.
    /// </remarks>
    public partial class WebHelper : IWebHelper
    {
        #region Fields 

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string[] _staticFileExtensions;

        #endregion

        #region Constructor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        public WebHelper(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._staticFileExtensions = new[] { ".axd", ".ashx", ".bmp", ".css", ".gif", ".htm", ".html", ".ico", ".jpeg", ".jpg", ".js", ".png", ".rar", ".zip" };
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets the current HTTP context, or null when there is no current request
        /// </summary>
        protected virtual HttpContext HttpContext
        {
            get { return _httpContextAccessor == null ? null : _httpContextAccessor.HttpContext; }
        }

        /// <summary>
        /// Gets a value indicating whether a request is currently available
        /// </summary>
        protected virtual bool IsRequestAvailable()
        {
            var httpContext = HttpContext;
            if (httpContext == null)
                return false;

            try
            {
                if (httpContext.Request == null)
                    return false;
            }
            catch
            {
                //the features backing HttpContext.Request are released once the request completes
                return false;
            }

            return true;
        }

        /// <summary>
        /// Translates a classic ASP.NET server variable name to an ASP.NET Core header name
        /// </summary>
        /// <param name="name">Server variable name, e.g. <c>HTTP_X_FORWARDED_PROTO</c></param>
        /// <returns>Header name, e.g. <c>X-FORWARDED-PROTO</c></returns>
        protected virtual string ServerVariableNameToHeaderName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            //HTTP_ prefixed server variables were verbatim request headers with '-' replaced by '_'
            if (name.StartsWith("HTTP_", StringComparison.OrdinalIgnoreCase))
                return name.Substring("HTTP_".Length).Replace('_', '-');

            return name;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get URL referrer
        /// </summary>
        /// <returns>URL referrer</returns>
        public virtual string GetUrlReferrer()
        {
            string referrerUrl = string.Empty;

            if (!IsRequestAvailable())
                return referrerUrl;

            //System.Web exposed this as Request.UrlReferrer; in ASP.NET Core it is the raw header
            var referrer = HttpContext.Request.Headers["Referer"].ToString();
            if (string.IsNullOrEmpty(referrer))
                return referrerUrl;

            //the legacy implementation returned only the path and query part
            Uri referrerUri;
            if (Uri.TryCreate(referrer, UriKind.Absolute, out referrerUri))
                return referrerUri.PathAndQuery;

            return referrer;
        }

        /// <summary>
        /// Get context IP address
        /// </summary>
        /// <returns>URL referrer</returns>
        public virtual string GetCurrentIpAddress()
        {
            if (!IsRequestAvailable())
                return string.Empty;

            var result = "";
            try
            {
                var request = HttpContext.Request;

                //The X-Forwarded-For (XFF) HTTP header field is a de facto standard
                //for identifying the originating IP address of a client
                //connecting to a web server through an HTTP proxy or load balancer.
                var forwardedHttpHeader = "X-FORWARDED-FOR";
                //Task 2.4: ConfigurationManager.AppSettings -> IConfiguration (see NopConfigurationManager)
                var configuredForwardedHttpHeader = NopConfigurationManager.GetAppSetting("ForwardedHTTPheader");
                if (!String.IsNullOrEmpty(configuredForwardedHttpHeader))
                {
                    //but in some cases server use other HTTP header
                    //in these cases an administrator can specify a custom Forwarded HTTP header
                    //e.g. CF-Connecting-IP, X-FORWARDED-PROTO, etc
                    forwardedHttpHeader = configuredForwardedHttpHeader;
                }

                //it's used for identifying the originating IP address of a client connecting to a web server
                //through an HTTP proxy or load balancer.
                //ASP.NET Core header lookup is already case-insensitive
                var xff = request.Headers[forwardedHttpHeader].ToString();

                //if you want to exclude private IP addresses, then see http://stackoverflow.com/questions/2577496/how-can-i-get-the-clients-ip-address-in-asp-net-mvc
                if (!String.IsNullOrEmpty(xff))
                {
                    string lastIp = xff.Split(new[] { ',' }).FirstOrDefault();
                    result = lastIp;
                }

                //System.Web's Request.UserHostAddress maps to the connection's remote IP
                if (String.IsNullOrEmpty(result) && HttpContext.Connection != null &&
                    HttpContext.Connection.RemoteIpAddress != null)
                {
                    result = HttpContext.Connection.RemoteIpAddress.ToString();
                }
            }
            catch
            {
                return result;
            }

            //some validation
            if (result == "::1")
                result = "127.0.0.1";
            //remove port
            if (!String.IsNullOrEmpty(result))
            {
                int index = result.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);
                if (index > 0)
                    result = result.Substring(0, index);
            }
            return result;

        }

        /// <summary>
        /// Gets this page name
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <returns>Page name</returns>
        public virtual string GetThisPageUrl(bool includeQueryString)
        {
            bool useSsl = IsCurrentConnectionSecured();
            return GetThisPageUrl(includeQueryString, useSsl);
        }

        /// <summary>
        /// Gets this page name
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <param name="useSsl">Value indicating whether to get SSL protected page</param>
        /// <returns>Page name</returns>
        public virtual string GetThisPageUrl(bool includeQueryString, bool useSsl)
        {
            if (!IsRequestAvailable())
                return string.Empty;
            
            //get the host considering using SSL
            var url = GetStoreHost(useSsl).TrimEnd('/');

            var request = HttpContext.Request;

            //get full URL with or without query string.
            //System.Web's RawUrl == PathBase + Path + QueryString; Path included the app path
            url += request.PathBase.ToString() + request.Path.ToString();
            if (includeQueryString)
                url += request.QueryString.ToString();

            return url.ToLowerInvariant();
        }

        /// <summary>
        /// Gets a value indicating whether current connection is secured
        /// </summary>
        /// <returns>true - secured, false - not secured</returns>
        public virtual bool IsCurrentConnectionSecured()
        {
            bool useSsl = false;
            if (IsRequestAvailable())
            {
                //when your hosting uses a load balancer on their server then the Request.IsHttps is never got set to true

                //Task 2.4: ConfigurationManager.AppSettings -> IConfiguration (see NopConfigurationManager)
                var useHttpClusterHttps = NopConfigurationManager.GetAppSetting("Use_HTTP_CLUSTER_HTTPS");
                var useHttpXForwardedProto = NopConfigurationManager.GetAppSetting("Use_HTTP_X_FORWARDED_PROTO");

                //1. use HTTP_CLUSTER_HTTPS?
                if (!string.IsNullOrEmpty(useHttpClusterHttps) && Convert.ToBoolean(useHttpClusterHttps))
                {
                    useSsl = ServerVariables("HTTP_CLUSTER_HTTPS") == "on";
                }
                //2. use HTTP_X_FORWARDED_PROTO?
                else if (!string.IsNullOrEmpty(useHttpXForwardedProto) && Convert.ToBoolean(useHttpXForwardedProto))
                {
                    useSsl = string.Equals(ServerVariables("HTTP_X_FORWARDED_PROTO"), "https", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    //System.Web's Request.IsSecureConnection
                    useSsl = HttpContext.Request.IsHttps;
                }
            }

            return useSsl;
        }

        /// <summary>
        /// Gets server variable by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Server variable</returns>
        /// <remarks>
        /// ASP.NET Core has no <c>Request.ServerVariables</c> collection. Classic <c>HTTP_*</c>
        /// server variable names are mapped back onto the request header they were derived
        /// from (<c>HTTP_X_FORWARDED_PROTO</c> -&gt; <c>X-FORWARDED-PROTO</c>); any other name
        /// is looked up verbatim as a header. Non-header server variables
        /// (<c>SERVER_SOFTWARE</c>, <c>LOCAL_ADDR</c>, ...) are therefore no longer resolvable
        /// and return the empty string, exactly as an unknown variable did before.
        /// </remarks>
        public virtual string ServerVariables(string name)
        {
            string result = string.Empty;

            try
            {
                if (!IsRequestAvailable())
                    return result;

                //put this method is try-catch 
                //as described here http://www.nopcommerce.com/boards/t/21356/multi-store-roadmap-lets-discuss-update-done.aspx?p=6#90196
                var headerName = ServerVariableNameToHeaderName(name);
                var value = HttpContext.Request.Headers[headerName].ToString();
                if (!string.IsNullOrEmpty(value))
                    result = value;
            }
            catch
            {
                result = string.Empty;
            }
            return result;
        }

        /// <summary>
        /// Gets store host location
        /// </summary>
        /// <param name="useSsl">Use SSL</param>
        /// <returns>Store host location</returns>
        public virtual string GetStoreHost(bool useSsl)
        {
            var result = "";
            var httpHost = ServerVariables("HTTP_HOST");
            if (!String.IsNullOrEmpty(httpHost))
            {
                result = "http://" + httpHost;
                if (!result.EndsWith("/"))
                    result += "/";
            }

            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                #region Database is installed

                //let's resolve IWorkContext  here.
                //Do not inject it via constructor  because it'll cause circular references
                var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                var currentStore = storeContext.CurrentStore;
                if (currentStore == null)
                    throw new Exception("Current store cannot be loaded");

                if (String.IsNullOrWhiteSpace(httpHost))
                {
                    //HTTP_HOST variable is not available.
                    //This scenario is possible only when HttpContext is not available (for example, running in a schedule task)
                    //in this case use URL of a store entity configured in admin area
                    result = currentStore.Url;
                    if (!result.EndsWith("/"))
                        result += "/";
                }

                if (useSsl)
                {
                    result = !String.IsNullOrWhiteSpace(currentStore.SecureUrl) ?
                        //Secure URL specified. 
                        //So a store owner don't want it to be detected automatically.
                        //In this case let's use the specified secure URL
                        currentStore.SecureUrl :
                        //Secure URL is not specified.
                        //So a store owner wants it to be detected automatically.
                        result.Replace("http:/", "https:/");
                }
                else
                {
                    if (currentStore.SslEnabled && !String.IsNullOrWhiteSpace(currentStore.SecureUrl))
                    {
                        //SSL is enabled in this store and secure URL is specified.
                        //So a store owner don't want it to be detected automatically.
                        //In this case let's use the specified non-secure URL
                        result = currentStore.Url;
                    }
                }
                #endregion
            }
            else
            {
                #region Database is not installed
                if (useSsl)
                {
                    //Secure URL is not specified.
                    //So a store owner wants it to be detected automatically.
                    result = result.Replace("http:/", "https:/");
                }
                #endregion
            }


            if (!result.EndsWith("/"))
                result += "/";
            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Gets store location
        /// </summary>
        /// <returns>Store location</returns>
        public virtual string GetStoreLocation()
        {
            bool useSsl = IsCurrentConnectionSecured();
            return GetStoreLocation(useSsl);
        }

        /// <summary>
        /// Gets store location
        /// </summary>
        /// <param name="useSsl">Use SSL</param>
        /// <returns>Store location</returns>
        public virtual string GetStoreLocation(bool useSsl)
        {
            string result = GetStoreHost(useSsl);
            if (result.EndsWith("/"))
                result = result.Substring(0, result.Length - 1);
            if (IsRequestAvailable())
            {
                //System.Web's Request.ApplicationPath is PathBase in ASP.NET Core
                result = result + HttpContext.Request.PathBase.ToString();
            }
            if (!result.EndsWith("/"))
                result += "/";

            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that needn't be processed by the cms engine.
        /// </summary>
        /// <param name="request">HTTP Request</param>
        /// <returns>True if the request targets a static resource file.</returns>
        /// <remarks>
        /// These are the file extensions considered to be static resources:
        /// .css
        ///	.gif
        /// .png 
        /// .jpg
        /// .jpeg
        /// .js
        /// .axd
        /// .ashx
        /// </remarks>
        public virtual bool IsStaticResource(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            //System.Web's VirtualPathUtility.GetExtension has no ASP.NET Core counterpart;
            //Path.GetExtension gives the same answer for a request path
            string path = request.Path.ToString();
            string extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension)) return false;

            return _staticFileExtensions.Contains(extension);
        }

        /// <summary>
        /// Modifies query string
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="queryStringModification">Query string modification</param>
        /// <param name="anchor">Anchor</param>
        /// <returns>New url</returns>
        public virtual string ModifyQueryString(string url, string queryStringModification, string anchor)
        {
            if (url == null)
                url = string.Empty;
            url = url.ToLowerInvariant();

            if (queryStringModification == null)
                queryStringModification = string.Empty;
            queryStringModification = queryStringModification.ToLowerInvariant();

            if (anchor == null)
                anchor = string.Empty;
            anchor = anchor.ToLowerInvariant();


            string str = string.Empty;
            string str2 = string.Empty;
            if (url.Contains("#"))
            {
                str2 = url.Substring(url.IndexOf("#") + 1);
                url = url.Substring(0, url.IndexOf("#"));
            }
            if (url.Contains("?"))
            {
                str = url.Substring(url.IndexOf("?") + 1);
                url = url.Substring(0, url.IndexOf("?"));
            }
            if (!string.IsNullOrEmpty(queryStringModification))
            {
                if (!string.IsNullOrEmpty(str))
                {
                    var dictionary = new Dictionary<string, string>();
                    foreach (string str3 in str.Split(new[] { '&' }))
                    {
                        if (!string.IsNullOrEmpty(str3))
                        {
                            string[] strArray = str3.Split(new[] { '=' });
                            if (strArray.Length == 2)
                            {
                                if (!dictionary.ContainsKey(strArray[0]))
                                {
                                    //do not add value if it already exists
                                    //two the same query parameters? theoretically it's not possible.
                                    //but MVC has some ugly implementation for checkboxes and we can have two values
                                    //find more info here: http://www.mindstorminteractive.com/topics/jquery-fix-asp-net-mvc-checkbox-truefalse-value/
                                    //we do this validation just to ensure that the first one is not overridden
                                    dictionary[strArray[0]] = strArray[1];
                                }
                            }
                            else
                            {
                                dictionary[str3] = null;
                            }
                        }
                    }
                    foreach (string str4 in queryStringModification.Split(new[] { '&' }))
                    {
                        if (!string.IsNullOrEmpty(str4))
                        {
                            string[] strArray2 = str4.Split(new[] { '=' });
                            if (strArray2.Length == 2)
                            {
                                dictionary[strArray2[0]] = strArray2[1];
                            }
                            else
                            {
                                dictionary[str4] = null;
                            }
                        }
                    }
                    var builder = new StringBuilder();
                    foreach (string str5 in dictionary.Keys)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append("&");
                        }
                        builder.Append(str5);
                        if (dictionary[str5] != null)
                        {
                            builder.Append("=");
                            builder.Append(dictionary[str5]);
                        }
                    }
                    str = builder.ToString();
                }
                else
                {
                    str = queryStringModification;
                }
            }
            if (!string.IsNullOrEmpty(anchor))
            {
                str2 = anchor;
            }
            return (url + (string.IsNullOrEmpty(str) ? "" : ("?" + str)) + (string.IsNullOrEmpty(str2) ? "" : ("#" + str2))).ToLowerInvariant();
        }

        /// <summary>
        /// Remove query string from url
        /// </summary>
        /// <param name="url">Url to modify</param>
        /// <param name="queryString">Query string to remove</param>
        /// <returns>New url</returns>
        public virtual string RemoveQueryString(string url, string queryString)
        {
            if (url == null)
                url = string.Empty;
            url = url.ToLowerInvariant();

            if (queryString == null)
                queryString = string.Empty;
            queryString = queryString.ToLowerInvariant();


            string str = string.Empty;
            if (url.Contains("?"))
            {
                str = url.Substring(url.IndexOf("?") + 1);
                url = url.Substring(0, url.IndexOf("?"));
            }
            if (!string.IsNullOrEmpty(queryString))
            {
                if (!string.IsNullOrEmpty(str))
                {
                    var dictionary = new Dictionary<string, string>();
                    foreach (string str3 in str.Split(new[] { '&' }))
                    {
                        if (!string.IsNullOrEmpty(str3))
                        {
                            string[] strArray = str3.Split(new[] { '=' });
                            if (strArray.Length == 2)
                            {
                                dictionary[strArray[0]] = strArray[1];
                            }
                            else
                            {
                                dictionary[str3] = null;
                            }
                        }
                    }
                    dictionary.Remove(queryString);

                    var builder = new StringBuilder();
                    foreach (string str5 in dictionary.Keys)
                    {
                        if (builder.Length > 0)
                        {
                            builder.Append("&");
                        }
                        builder.Append(str5);
                        if (dictionary[str5] != null)
                        {
                            builder.Append("=");
                            builder.Append(dictionary[str5]);
                        }
                    }
                    str = builder.ToString();
                }
            }
            return (url + (string.IsNullOrEmpty(str) ? "" : ("?" + str)));
        }

        /// <summary>
        /// Gets query string value by name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">Parameter name</param>
        /// <returns>Query string value</returns>
        public virtual T QueryString<T>(string name)
        {
            string queryParam = null;
            if (IsRequestAvailable())
            {
                var value = HttpContext.Request.Query[name].ToString();
                if (!string.IsNullOrEmpty(value))
                    queryParam = value;
            }

            if (!String.IsNullOrEmpty(queryParam))
                return CommonHelper.To<T>(queryParam);

            return default(T);
        }

        /// <summary>
        /// Restart application domain
        /// </summary>
        /// <param name="makeRedirect">A value indicating whether we should made redirection after restart</param>
        /// <param name="redirectUrl">Redirect URL; empty string if you want to redirect to the current page URL</param>
        /// <remarks>
        /// SEMANTIC CHANGE (task 2.4). .NET Core has no unloadable AppDomain, so
        /// <c>HttpRuntime.UnloadAppDomain()</c> and the medium-trust "touch web.config /
        /// global.asax" fallbacks are gone (there is also no <c>global.asax</c> in ASP.NET Core
        /// and touching <c>web.config</c> does not recycle Kestrel). The closest supported
        /// behaviour is a graceful host shutdown via
        /// <see cref="IHostApplicationLifetime.StopApplication"/>; the process must then be
        /// restarted by whatever supervises it (ANCM, systemd, a container orchestrator).
        /// <see cref="IHostApplicationLifetime"/> is only resolvable once the ASP.NET Core host
        /// wires nopCommerce's container into it (tasks 6.4 / 7.2); until then, and in any
        /// non-hosted process, this throws <see cref="NopException"/> rather than silently
        /// pretending to have restarted.
        /// </remarks>
        public virtual void RestartAppDomain(bool makeRedirect = false, string redirectUrl = "")
        {
            IHostApplicationLifetime applicationLifetime = null;
            try
            {
                applicationLifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
            }
            catch
            {
                //not registered - fall through to the exception below
            }

            if (applicationLifetime == null)
            {
                throw new NopException("nopCommerce needs to be restarted due to a configuration change, but was unable to do so." + Environment.NewLine +
                    "On .NET the application domain cannot be unloaded in-process; a host restart is required." + Environment.NewLine +
                    "To enable automatic restarts, register Microsoft.Extensions.Hosting.IHostApplicationLifetime with the nopCommerce container and " +
                    "run the application under a supervisor that restarts the process (for example ASP.NET Core Module, systemd or a container orchestrator).");
            }

            // If setting up extensions/modules requires a restart, it's very unlikely the
            // current request can be processed correctly.  So, we redirect to the same URL, so that the
            // new request will come to the newly started host.
            var httpContext = HttpContext;
            if (httpContext != null && makeRedirect)
            {
                if (String.IsNullOrEmpty(redirectUrl))
                    redirectUrl = GetThisPageUrl(true);

                //ASP.NET Core's Redirect has no "endResponse" counterpart - the pipeline
                //short-circuits once the response has started
                httpContext.Response.Redirect(redirectUrl);
            }

            applicationLifetime.StopApplication();
        }

        /// <summary>
        /// Gets a value that indicates whether the client is being redirected to a new location
        /// </summary>
        /// <remarks>
        /// ASP.NET Core has no <c>HttpResponse.IsRequestBeingRedirected</c>; the equivalent
        /// signal is a 3xx redirect status code on the response.
        /// </remarks>
        public virtual bool IsRequestBeingRedirected
        {
            get
            {
                var httpContext = HttpContext;
                if (httpContext == null || httpContext.Response == null)
                    return false;

                var redirectionStatusCodes = new[]
                {
                    StatusCodes.Status301MovedPermanently,
                    StatusCodes.Status302Found,
                    StatusCodes.Status303SeeOther,
                    StatusCodes.Status307TemporaryRedirect,
                    StatusCodes.Status308PermanentRedirect
                };

                return redirectionStatusCodes.Contains(httpContext.Response.StatusCode);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the client is being redirected to a new location using POST
        /// </summary>
        public virtual bool IsPostBeingDone
        {
            get
            {
                var httpContext = HttpContext;
                if (httpContext == null)
                    return false;

                object value;
                if (!httpContext.Items.TryGetValue("nop.IsPOSTBeingDone", out value) || value == null)
                    return false;

                return Convert.ToBoolean(value);
            }
            set
            {
                var httpContext = HttpContext;
                if (httpContext == null)
                    return;

                httpContext.Items["nop.IsPOSTBeingDone"] = value;
            }
        }

        #endregion
    }
}
