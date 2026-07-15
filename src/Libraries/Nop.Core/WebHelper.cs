using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Core
{
    /// <summary>
    /// Represents a common helper
    /// </summary>
    public partial class WebHelper : IWebHelper
    {
        #region Fields 

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IConfiguration _configuration;
        private readonly string[] _staticFileExtensions;

        #endregion

        #region Constructor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="hostApplicationLifetime">Host application lifetime</param>
        /// <param name="configuration">Configuration</param>
        public WebHelper(IHttpContextAccessor httpContextAccessor,
            IHostApplicationLifetime hostApplicationLifetime,
            IConfiguration configuration)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._hostApplicationLifetime = hostApplicationLifetime;
            this._configuration = configuration;
            this._staticFileExtensions = new[] { ".axd", ".ashx", ".bmp", ".css", ".gif", ".htm", ".html", ".ico", ".jpeg", ".jpg", ".js", ".png", ".rar", ".zip" };
        }

        #endregion

        #region Utilities

        protected virtual bool IsRequestAvailable(HttpContext httpContext)
        {
            if (httpContext == null)
                return false;

            try
            {
                if (httpContext.Request == null)
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        protected virtual bool TryWriteWebConfig()
        {
            try
            {
                // Touch web.config to force an application restart.
                File.SetLastWriteTimeUtc(CommonHelper.MapPath("~/web.config"), DateTime.UtcNow);
                return true;
            }
            catch
            {
                return false;
            }
        }

        protected virtual bool TryWriteGlobalAsax()
        {
            try
            {
                //When a new plugin is dropped in the Plugins folder and is installed into nopCommerce, 
                //even if the plugin has registered routes for its controllers, 
                //these routes will not be working as the MVC framework couldn't 
                //find the new controller types and couldn't instantiate the requested controller. 
                //That's why you get these nasty errors 
                //i.e "Controller does not implement IController".
                //The issue is described here: http://www.nopcommerce.com/boards/t/10969/nop-20-plugin.aspx?p=4#51318
                //The solution is to touch global.asax file
                File.SetLastWriteTimeUtc(CommonHelper.MapPath("~/global.asax"), DateTime.UtcNow);
                return true;
            }
            catch
            {
                return false;
            }
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

            var httpContext = _httpContextAccessor.HttpContext;
            //URL referrer is null in some case (for example, in IE 8)
            if (IsRequestAvailable(httpContext))
            {
                var referrer = httpContext.Request.Headers["Referer"].FirstOrDefault();
                if (!string.IsNullOrEmpty(referrer))
                {
                    if (Uri.TryCreate(referrer, UriKind.Absolute, out var referrerUri))
                    {
                        referrerUrl = referrerUri.PathAndQuery;
                    }
                    else
                    {
                        referrerUrl = referrer;
                    }
                }
            }

            return referrerUrl;
        }

        /// <summary>
        /// Get context IP address
        /// </summary>
        /// <returns>URL referrer</returns>
        public virtual string GetCurrentIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (!IsRequestAvailable(httpContext))
                return string.Empty;

            var result = "";
            try
            {
                if (httpContext.Request.Headers != null)
                {
                    //The X-Forwarded-For (XFF) HTTP header field is a de facto standard
                    //for identifying the originating IP address of a client
                    //connecting to a web server through an HTTP proxy or load balancer.
                    var forwardedHttpHeader = "X-FORWARDED-FOR";
                    if (!string.IsNullOrEmpty(_configuration["ForwardedHTTPheader"]))
                    {
                        //but in some cases server use other HTTP header
                        //in these cases an administrator can specify a custom Forwarded HTTP header
                        //e.g. CF-Connecting-IP, X-FORWARDED-PROTO, etc
                        forwardedHttpHeader = _configuration["ForwardedHTTPheader"];
                    }

                    //it's used for identifying the originating IP address of a client connecting to a web server
                    //through an HTTP proxy or load balancer. 
                    string xff = httpContext.Request.Headers.Keys
                        .Where(x => forwardedHttpHeader.Equals(x, StringComparison.InvariantCultureIgnoreCase))
                        .Select(k => (string)httpContext.Request.Headers[k])
                        .FirstOrDefault();

                    //if you want to exclude private IP addresses, then see http://stackoverflow.com/questions/2577496/how-can-i-get-the-clients-ip-address-in-asp-net-mvc
                    if (!string.IsNullOrEmpty(xff))
                    {
                        string lastIp = xff.Split(new[] { ',' }).FirstOrDefault();
                        result = lastIp;
                    }
                }

                if (string.IsNullOrEmpty(result) && httpContext.Connection.RemoteIpAddress != null)
                {
                    result = httpContext.Connection.RemoteIpAddress.ToString();
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
            if (!string.IsNullOrEmpty(result))
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
            var httpContext = _httpContextAccessor.HttpContext;
            if (!IsRequestAvailable(httpContext))
                return string.Empty;
            
            //get the host considering using SSL
            var url = GetStoreHost(useSsl).TrimEnd('/');

            //get full URL with or without query string
            if (includeQueryString)
            {
                url += httpContext.Request.Path.ToString() + httpContext.Request.QueryString.ToString();
            }
            else
            {
                url += httpContext.Request.Path.ToString();
            }

            return url.ToLowerInvariant();
        }

        /// <summary>
        /// Gets a value indicating whether current connection is secured
        /// </summary>
        /// <returns>true - secured, false - not secured</returns>
        public virtual bool IsCurrentConnectionSecured()
        {
            bool useSsl = false;
            var httpContext = _httpContextAccessor.HttpContext;
            if (IsRequestAvailable(httpContext))
            {
                //when your hosting uses a load balancer on their server then the Request.IsHttps is never got set to true

                //1. use HTTP_CLUSTER_HTTPS?
                if (!string.IsNullOrEmpty(_configuration["Use_HTTP_CLUSTER_HTTPS"]) &&
                   Convert.ToBoolean(_configuration["Use_HTTP_CLUSTER_HTTPS"]))
                {
                    useSsl = ServerVariables("HTTP_CLUSTER_HTTPS") == "on";
                }
                //2. use HTTP_X_FORWARDED_PROTO?
                else if (!string.IsNullOrEmpty(_configuration["Use_HTTP_X_FORWARDED_PROTO"]) &&
                   Convert.ToBoolean(_configuration["Use_HTTP_X_FORWARDED_PROTO"]))
                {
                    useSsl = string.Equals(ServerVariables("HTTP_X_FORWARDED_PROTO"), "https", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    useSsl = httpContext.Request.IsHttps;
                }
            }

            return useSsl;
        }

        /// <summary>
        /// Gets server variable by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Server variable</returns>
        public virtual string ServerVariables(string name)
        {
            string result = string.Empty;

            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (!IsRequestAvailable(httpContext))
                    return result;

                //In ASP.NET Core, server variables are accessed via headers or connection info.
                //Map common server variable names to their ASP.NET Core equivalents.
                if (name.Equals("HTTP_HOST", StringComparison.OrdinalIgnoreCase))
                {
                    result = httpContext.Request.Host.ToString();
                }
                else if (name.Equals("HTTP_X_FORWARDED_PROTO", StringComparison.OrdinalIgnoreCase))
                {
                    result = httpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? string.Empty;
                }
                else if (name.Equals("HTTP_CLUSTER_HTTPS", StringComparison.OrdinalIgnoreCase))
                {
                    result = httpContext.Request.Headers["X-Cluster-Https"].FirstOrDefault() ?? string.Empty;
                }
                else if (name.Equals("REMOTE_ADDR", StringComparison.OrdinalIgnoreCase))
                {
                    result = httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                }
                else if (name.Equals("SERVER_NAME", StringComparison.OrdinalIgnoreCase))
                {
                    result = httpContext.Request.Host.Host;
                }
                else if (name.Equals("SERVER_PORT", StringComparison.OrdinalIgnoreCase))
                {
                    result = (httpContext.Request.Host.Port ?? (httpContext.Request.IsHttps ? 443 : 80)).ToString();
                }
                else if (name.StartsWith("HTTP_", StringComparison.OrdinalIgnoreCase))
                {
                    // Convert HTTP_HEADER_NAME to Header-Name format
                    var headerName = name.Substring(5).Replace("_", "-");
                    result = httpContext.Request.Headers[headerName].FirstOrDefault() ?? string.Empty;
                }
                else
                {
                    // Try as a header directly
                    result = httpContext.Request.Headers[name].FirstOrDefault() ?? string.Empty;
                }
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
            if (!string.IsNullOrEmpty(httpHost))
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

                if (string.IsNullOrWhiteSpace(httpHost))
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
                    result = !string.IsNullOrWhiteSpace(currentStore.SecureUrl) ?
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
                    if (currentStore.SslEnabled && !string.IsNullOrWhiteSpace(currentStore.SecureUrl))
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

            var httpContext = _httpContextAccessor.HttpContext;
            if (IsRequestAvailable(httpContext))
            {
                result = result + httpContext.Request.PathBase.ToString();
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
        public virtual bool IsStaticResource(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            string path = url;
            // If it's a full URL, extract the path portion
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    path = new Uri(url).AbsolutePath;
                }
                catch
                {
                    return false;
                }
            }

            string extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
                return false;

            return _staticFileExtensions.Contains(extension.ToLowerInvariant());
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
            var httpContext = _httpContextAccessor.HttpContext;
            if (IsRequestAvailable(httpContext) && httpContext.Request.Query.ContainsKey(name))
                queryParam = httpContext.Request.Query[name];

            if (!string.IsNullOrEmpty(queryParam))
                return CommonHelper.To<T>(queryParam);

            return default(T);
        }

        /// <summary>
        /// Restart application domain
        /// </summary>
        /// <param name="makeRedirect">A value indicating whether we should made redirection after restart</param>
        /// <param name="redirectUrl">Redirect URL; empty string if you want to redirect to the current page URL</param>
        public virtual void RestartAppDomain(bool makeRedirect = false, string redirectUrl = "")
        {
            // In .NET Core, we use IHostApplicationLifetime to stop the application.
            // The hosting environment (e.g., IIS, systemd, Docker) is responsible for restarting.
            _hostApplicationLifetime.StopApplication();

            // If making a redirect, set the response before the app stops.
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null && makeRedirect)
            {
                if (string.IsNullOrEmpty(redirectUrl))
                    redirectUrl = GetThisPageUrl(true);
                httpContext.Response.Redirect(redirectUrl);
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the client is being redirected to a new location
        /// </summary>
        public virtual bool IsRequestBeingRedirected
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return false;

                var response = httpContext.Response;
                //ASP.NET Core uses status codes 301, 302, 307, 308 for redirects
                return response.StatusCode == StatusCodes.Status301MovedPermanently
                    || response.StatusCode == StatusCodes.Status302Found
                    || response.StatusCode == StatusCodes.Status307TemporaryRedirect
                    || response.StatusCode == StatusCodes.Status308PermanentRedirect;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the client is being redirected to a new location using POST
        /// </summary>
        public virtual bool IsPostBeingDone
        {
            get
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return false;

                if (httpContext.Items["nop.IsPOSTBeingDone"] == null)
                    return false;
                return Convert.ToBoolean(httpContext.Items["nop.IsPOSTBeingDone"]);
            }
            set
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    httpContext.Items["nop.IsPOSTBeingDone"] = value;
                }
            }
        }

        #endregion
    }
}
