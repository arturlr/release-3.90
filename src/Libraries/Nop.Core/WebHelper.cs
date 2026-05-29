using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Core
{
    /// <summary>
    /// Represents a web helper
    /// </summary>
    public partial class WebHelper : IWebHelper
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly NopConfig _nopConfig;
        private readonly string[] _staticFileExtensions = { ".axd", ".ashx", ".bmp", ".css", ".gif", ".htm", ".html", ".ico", ".jpeg", ".jpg", ".js", ".png", ".rar", ".zip", ".svg", ".woff", ".woff2", ".ttf", ".eot" };

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="hostEnvironment">Host environment</param>
        /// <param name="nopConfig">Nop configuration</param>
        public WebHelper(IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostEnvironment,
            NopConfig nopConfig)
        {
            _httpContextAccessor = httpContextAccessor;
            _hostEnvironment = hostEnvironment;
            _nopConfig = nopConfig;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Check whether current HTTP request is available
        /// </summary>
        /// <returns>True if available; otherwise false</returns>
        protected virtual bool IsRequestAvailable()
        {
            try
            {
                if (_httpContextAccessor?.HttpContext?.Request == null)
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get URL referrer
        /// </summary>
        /// <returns>URL referrer</returns>
        public virtual string GetUrlReferrer()
        {
            if (!IsRequestAvailable())
                return string.Empty;

            return _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.Referer].ToString();
        }

        /// <summary>
        /// Get context IP address
        /// </summary>
        /// <returns>IP address string</returns>
        public virtual string GetCurrentIpAddress()
        {
            if (!IsRequestAvailable())
                return string.Empty;

            var result = string.Empty;
            try
            {
                var request = _httpContextAccessor.HttpContext.Request;

                // Check for forwarded header
                var forwardedHeader = string.IsNullOrEmpty(_nopConfig.ForwardedHttpHeader)
                    ? "X-Forwarded-For"
                    : _nopConfig.ForwardedHttpHeader;

                if (request.Headers.TryGetValue(forwardedHeader, out StringValues forwardedValues) &&
                    !StringValues.IsNullOrEmpty(forwardedValues))
                {
                    var forwardedIp = forwardedValues.FirstOrDefault();
                    if (!string.IsNullOrEmpty(forwardedIp))
                    {
                        // Take the first IP in case of multiple proxies
                        result = forwardedIp.Split(',').FirstOrDefault()?.Trim();
                    }
                }

                // Fall back to connection remote IP
                if (string.IsNullOrEmpty(result))
                {
                    var remoteIp = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress;
                    if (remoteIp != null)
                    {
                        result = remoteIp.MapToIPv4().ToString();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }

            // Some validation
            if (result == "::1")
                result = "127.0.0.1";

            // Remove port if present
            if (!string.IsNullOrEmpty(result) && IPAddress.TryParse(result, out _))
            {
                // Already valid IP, nothing to strip
            }
            else if (!string.IsNullOrEmpty(result))
            {
                var index = result.IndexOf(":", StringComparison.InvariantCultureIgnoreCase);
                if (index > 0)
                    result = result.Substring(0, index);
            }

            return result;
        }

        /// <summary>
        /// Gets this page URL
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <returns>Page URL</returns>
        public virtual string GetThisPageUrl(bool includeQueryString)
        {
            bool useSsl = IsCurrentConnectionSecured();
            return GetThisPageUrl(includeQueryString, useSsl);
        }

        /// <summary>
        /// Gets this page URL
        /// </summary>
        /// <param name="includeQueryString">Value indicating whether to include query strings</param>
        /// <param name="useSsl">Value indicating whether to get SSL protected page</param>
        /// <returns>Page URL</returns>
        public virtual string GetThisPageUrl(bool includeQueryString, bool useSsl)
        {
            if (!IsRequestAvailable())
                return string.Empty;

            // Get the host considering using SSL
            var url = GetStoreHost(useSsl).TrimEnd('/');
            var request = _httpContextAccessor.HttpContext.Request;

            // Get full URL with or without query string
            if (includeQueryString)
            {
                url += request.GetEncodedPathAndQuery();
            }
            else
            {
                url += request.Path;
            }

            return url.ToLowerInvariant();
        }

        /// <summary>
        /// Gets a value indicating whether current connection is secured
        /// </summary>
        /// <returns>true - secured, false - not secured</returns>
        public virtual bool IsCurrentConnectionSecured()
        {
            if (!IsRequestAvailable())
                return false;

            // Check HTTP_CLUSTER_HTTPS
            if (_nopConfig.UseHttpClusterHttps)
            {
                if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("HTTP_CLUSTER_HTTPS", out var clusterHttps))
                    return clusterHttps.ToString().Equals("on", StringComparison.OrdinalIgnoreCase);
            }

            // Check X-Forwarded-Proto
            if (_nopConfig.UseHttpXForwardedProto)
            {
                if (_httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto))
                    return forwardedProto.ToString().Equals("https", StringComparison.OrdinalIgnoreCase);
            }

            return _httpContextAccessor.HttpContext.Request.IsHttps;
        }

        /// <summary>
        /// Gets server variable by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Server variable</returns>
        public virtual string ServerVariables(string name)
        {
            if (!IsRequestAvailable())
                return string.Empty;

            // In ASP.NET Core, server variables are available through features or headers
            // The most common ones map to request headers
            var request = _httpContextAccessor.HttpContext.Request;

            return name?.ToUpperInvariant() switch
            {
                "HTTP_HOST" => request.Host.ToString(),
                "REMOTE_ADDR" => _httpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                "SERVER_NAME" => request.Host.Host,
                "SERVER_PORT" => request.Host.Port?.ToString() ?? (request.IsHttps ? "443" : "80"),
                "HTTPS" => request.IsHttps ? "on" : "off",
                "HTTP_X_FORWARDED_PROTO" => request.Headers["X-Forwarded-Proto"].ToString(),
                "HTTP_CLUSTER_HTTPS" => request.Headers["HTTP_CLUSTER_HTTPS"].ToString(),
                _ => request.Headers.TryGetValue(name, out var value) ? value.ToString() : string.Empty
            };
        }

        /// <summary>
        /// Gets store host location
        /// </summary>
        /// <param name="useSsl">Use SSL</param>
        /// <returns>Store host location</returns>
        public virtual string GetStoreHost(bool useSsl)
        {
            var result = string.Empty;
            var httpHost = IsRequestAvailable() ? _httpContextAccessor.HttpContext.Request.Host.Value : string.Empty;

            if (!string.IsNullOrEmpty(httpHost))
            {
                result = (IsCurrentConnectionSecured() ? "https://" : "http://") + httpHost;
                if (!result.EndsWith("/"))
                    result += "/";
            }

            if (DataSettingsHelper.DatabaseIsInstalled())
            {
                // Let's resolve IStoreContext here
                var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                var currentStore = storeContext?.CurrentStore;
                if (currentStore == null)
                    throw new Exception("Current store cannot be loaded");

                if (string.IsNullOrWhiteSpace(httpHost))
                {
                    // HTTP_HOST variable is not available (e.g., running in a schedule task)
                    result = currentStore.Url;
                    if (!result.EndsWith("/"))
                        result += "/";
                }

                if (useSsl)
                {
                    result = !string.IsNullOrWhiteSpace(currentStore.SecureUrl)
                        ? currentStore.SecureUrl
                        : result.Replace("http://", "https://");
                }
                else
                {
                    if (currentStore.SslEnabled && !string.IsNullOrWhiteSpace(currentStore.SecureUrl))
                    {
                        result = currentStore.Url;
                    }
                }
            }
            else
            {
                if (useSsl)
                {
                    result = result.Replace("http://", "https://");
                }
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
                var pathBase = _httpContextAccessor.HttpContext.Request.PathBase.Value;
                if (!string.IsNullOrEmpty(pathBase))
                    result += pathBase;
            }

            if (!result.EndsWith("/"))
                result += "/";

            return result.ToLowerInvariant();
        }

        /// <summary>
        /// Returns true if the requested resource is one of the typical resources that needn't be processed by the cms engine.
        /// </summary>
        /// <returns>True if the request targets a static resource file.</returns>
        public virtual bool IsStaticResource()
        {
            if (!IsRequestAvailable())
                return false;

            string path = _httpContextAccessor.HttpContext.Request.Path;
            if (string.IsNullOrEmpty(path))
                return false;

            var extension = Path.GetExtension(path);
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
                                    dictionary[strArray[0]] = strArray[1];
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
                            builder.Append("&");
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
                            builder.Append("&");
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
        /// <typeparam name="T">Returned value type</typeparam>
        /// <param name="name">Parameter name</param>
        /// <returns>Query string value</returns>
        public virtual T QueryString<T>(string name)
        {
            if (!IsRequestAvailable())
                return default(T);

            if (_httpContextAccessor.HttpContext.Request.Query.TryGetValue(name, out StringValues value) &&
                !StringValues.IsNullOrEmpty(value))
            {
                return CommonHelper.To<T>(value.FirstOrDefault());
            }

            return default(T);
        }

        /// <summary>
        /// Restart application domain
        /// </summary>
        public virtual void RestartAppDomain()
        {
            // In ASP.NET Core, there's no direct equivalent to AppDomain.Unload.
            // The recommended approach is to use IHostApplicationLifetime to stop the application,
            // then rely on the hosting environment (IIS, systemd, Docker, etc.) to restart it.
            var hostApplicationLifetime = EngineContext.Current.Resolve<IHostApplicationLifetime>();
            hostApplicationLifetime?.StopApplication();
        }

        /// <summary>
        /// Gets a value that indicates whether the client is being redirected to a new location
        /// </summary>
        public virtual bool IsRequestBeingRedirected
        {
            get
            {
                var response = _httpContextAccessor?.HttpContext?.Response;
                if (response == null)
                    return false;

                // 301, 302, 303, 307, 308
                return response.StatusCode == StatusCodes.Status301MovedPermanently ||
                       response.StatusCode == StatusCodes.Status302Found ||
                       response.StatusCode == StatusCodes.Status303SeeOther ||
                       response.StatusCode == StatusCodes.Status307TemporaryRedirect ||
                       response.StatusCode == StatusCodes.Status308PermanentRedirect;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the client is being redirected to a new location using POST
        /// </summary>
        public virtual bool IsPostBeingDone
        {
            get
            {
                if (_httpContextAccessor?.HttpContext?.Items == null)
                    return false;

                if (_httpContextAccessor.HttpContext.Items.TryGetValue("nop.IsPOSTBeingDone", out var value))
                    return Convert.ToBoolean(value);

                return false;
            }
            set
            {
                if (_httpContextAccessor?.HttpContext?.Items != null)
                    _httpContextAccessor.HttpContext.Items["nop.IsPOSTBeingDone"] = value;
            }
        }

        #endregion
    }
}
