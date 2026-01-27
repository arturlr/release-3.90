namespace Nop.Core.Http
{
    /// <summary>
    /// Provides access to the current HTTP context
    /// </summary>
    public interface IHttpContextAccessor
    {
        /// <summary>
        /// Gets the current HTTP context
        /// </summary>
        IHttpContext HttpContext { get; }
    }

    /// <summary>
    /// HTTP context abstraction
    /// </summary>
    public interface IHttpContext
    {
        /// <summary>
        /// Gets the HTTP request
        /// </summary>
        IHttpRequest Request { get; }

        /// <summary>
        /// Gets the HTTP response
        /// </summary>
        IHttpResponse Response { get; }

        /// <summary>
        /// Gets or sets an item in the context
        /// </summary>
        object this[string key] { get; set; }
    }

    /// <summary>
    /// HTTP request abstraction
    /// </summary>
    public interface IHttpRequest
    {
        /// <summary>
        /// Gets the URL
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Gets the URL referrer
        /// </summary>
        string UrlReferrer { get; }

        /// <summary>
        /// Gets the user agent
        /// </summary>
        string UserAgent { get; }

        /// <summary>
        /// Gets the user host address
        /// </summary>
        string UserHostAddress { get; }

        /// <summary>
        /// Gets whether the request is secure (HTTPS)
        /// </summary>
        bool IsSecureConnection { get; }

        /// <summary>
        /// Gets whether the request is local
        /// </summary>
        bool IsLocal { get; }
    }

    /// <summary>
    /// HTTP response abstraction
    /// </summary>
    public interface IHttpResponse
    {
        /// <summary>
        /// Redirects to the specified URL
        /// </summary>
        void Redirect(string url);
    }
}
