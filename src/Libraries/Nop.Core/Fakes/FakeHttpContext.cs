using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Nop.Core.Fakes
{
    /// <summary>
    /// Provides static helper methods to create configured ASP.NET Core HttpContext instances for testing.
    /// </summary>
    public static class FakeHttpContext
    {
        /// <summary>
        /// Creates a default HttpContext configured for the root path.
        /// </summary>
        /// <returns>A configured DefaultHttpContext instance.</returns>
        public static HttpContext Root()
        {
            return Create("/");
        }

        /// <summary>
        /// Creates an HttpContext with the specified path and optional method.
        /// </summary>
        /// <param name="path">The request path (e.g. "/home/index").</param>
        /// <param name="method">The HTTP method (defaults to GET).</param>
        /// <returns>A configured DefaultHttpContext instance.</returns>
        public static HttpContext Create(string path, string method = "GET")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            return context;
        }

        /// <summary>
        /// Creates an HttpContext with full request configuration.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="method">The HTTP method.</param>
        /// <param name="scheme">The URL scheme (http or https).</param>
        /// <param name="host">The host string.</param>
        /// <param name="queryString">The query string (e.g. "?key=value").</param>
        /// <param name="headers">Optional headers to include in the request.</param>
        /// <param name="remoteIpAddress">Optional remote IP address.</param>
        /// <param name="user">Optional ClaimsPrincipal for the request.</param>
        /// <returns>A configured DefaultHttpContext instance.</returns>
        public static HttpContext Create(
            string path,
            string method,
            string scheme,
            string host,
            string queryString = null,
            IDictionary<string, StringValues> headers = null,
            IPAddress remoteIpAddress = null,
            ClaimsPrincipal user = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = method;
            context.Request.Scheme = scheme;
            context.Request.Host = new HostString(host);

            if (!string.IsNullOrEmpty(queryString))
            {
                context.Request.QueryString = new QueryString(queryString);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    context.Request.Headers[header.Key] = header.Value;
                }
            }

            if (remoteIpAddress != null)
            {
                context.Connection.RemoteIpAddress = remoteIpAddress;
            }

            if (user != null)
            {
                context.User = user;
            }

            return context;
        }

        /// <summary>
        /// Creates an HttpContext configured for SSL (HTTPS).
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="host">The host string (defaults to "localhost").</param>
        /// <returns>A configured DefaultHttpContext instance with HTTPS scheme.</returns>
        public static HttpContext CreateHttps(string path, string host = "localhost")
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = "GET";
            context.Request.Scheme = "https";
            context.Request.Host = new HostString(host);
            context.Request.IsHttps = true;
            return context;
        }

        /// <summary>
        /// Creates an HttpContext with session support.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="sessionItems">Optional session items to pre-populate.</param>
        /// <returns>A configured DefaultHttpContext instance with a session.</returns>
        public static HttpContext CreateWithSession(string path, IDictionary<string, byte[]> sessionItems = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Request.Method = "GET";
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");

            var session = new FakeSession();
            if (sessionItems != null)
            {
                foreach (var item in sessionItems)
                {
                    session.Set(item.Key, item.Value);
                }
            }
            context.Features.Set<ISessionFeature>(new FakeSessionFeature(session));

            return context;
        }

        /// <summary>
        /// Creates an HttpContext with an authenticated user.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="userName">The user name for the claims identity.</param>
        /// <param name="roles">Optional roles for the user.</param>
        /// <returns>A configured DefaultHttpContext instance with user principal.</returns>
        public static HttpContext CreateAuthenticated(string path, string userName, string[] roles = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName)
            };

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var principal = new ClaimsPrincipal(identity);

            var context = Create(path);
            context.User = principal;
            return context;
        }
    }

    /// <summary>
    /// A simple in-memory session implementation for testing.
    /// </summary>
    internal class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new Dictionary<string, byte[]>();

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public System.Threading.Tasks.Task LoadAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return System.Threading.Tasks.Task.CompletedTask;
        }

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }

    /// <summary>
    /// Session feature for the fake HTTP context.
    /// </summary>
    internal class FakeSessionFeature : ISessionFeature
    {
        public FakeSessionFeature(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; set; }
    }
}
