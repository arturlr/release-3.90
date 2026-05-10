using Microsoft.AspNetCore.Html;

namespace Nop.Web.Framework.Mvc
{
    /// <summary>
    /// Compatibility shim for MvcHtmlString which doesn't exist in ASP.NET Core.
    /// </summary>
    public class MvcHtmlString : HtmlString
    {
        public MvcHtmlString(string value) : base(value) { }

        public static bool IsNullOrEmpty(object value)
        {
            if (value == null) return true;
            if (value is HtmlString hs) return string.IsNullOrEmpty(hs.Value);
            if (value is IHtmlContent hc)
            {
                using var writer = new System.IO.StringWriter();
                hc.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return string.IsNullOrEmpty(writer.ToString());
            }
            return string.IsNullOrEmpty(value.ToString());
        }

        public static readonly MvcHtmlString Empty = new MvcHtmlString(string.Empty);
    }
}
