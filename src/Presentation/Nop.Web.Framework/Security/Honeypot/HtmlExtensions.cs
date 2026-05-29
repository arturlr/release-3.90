using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework.Security.Honeypot
{
    public static class HoneypotHtmlExtensions
    {
        public static IHtmlContent GenerateHoneypotInput(this IHtmlHelper helper)
        {
            var html = "<div style=\"display:none;\"><input type=\"text\" name=\"HP_FieldName\" value=\"\" /></div>";
            return new HtmlString(html);
        }
    }
}
