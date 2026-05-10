using System.Net;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Compatibility shim for System.Web.HttpUtility
    /// </summary>
    public static class HttpUtility
    {
        public static string UrlEncode(string value) => WebUtility.UrlEncode(value);
        public static string UrlDecode(string value) => WebUtility.UrlDecode(value);
        public static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
        public static string HtmlDecode(string value) => WebUtility.HtmlDecode(value);
        public static string JavaScriptStringEncode(string value) => 
            System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(value ?? string.Empty);
    }
}
