using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Nop.Web.Framework
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Gets the raw URL (path + query) of the request (compatibility for Request.RawUrl)
        /// </summary>
        public static string GetRawUrl(this HttpRequest request)
        {
            return request.Path + request.QueryString;
        }
    }
}
