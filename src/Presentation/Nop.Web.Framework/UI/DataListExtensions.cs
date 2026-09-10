using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework.UI
{
    /// <summary>
    /// Renders a sequence of items into a fixed-column HTML table.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3: <c>System.Web.IHtmlString</c> → <see cref="IHtmlContent"/>,
    /// <c>System.Web.Mvc.HtmlHelper</c> → <see cref="IHtmlHelper"/>, and
    /// <c>System.Web.WebPages.HelperResult</c> → <see cref="HelperResult"/>
    /// (<c>Microsoft.AspNetCore.Mvc.Razor</c>) — the type Razor templated delegates
    /// (<c>@&lt;text&gt;…&lt;/text&gt;</c>) produce in ASP.NET Core.
    /// </remarks>
    public static class DataListExtensions
    {
        public static IHtmlContent DataList<T>(this IHtmlHelper helper, IEnumerable<T> items, int columns,
            Func<T, HelperResult> template) 
            where T : class
        {
            if (items == null)
                return new HtmlString("");

            var sb = new StringBuilder();
            sb.Append("<table>");

            int cellIndex = 0;

            foreach (T item in items)
            {
                if (cellIndex == 0)
                    sb.Append("<tr>");

                sb.Append("<td");
                sb.Append(">");

                sb.Append(template(item).ToHtmlString());
                sb.Append("</td>");

                cellIndex++;

                if (cellIndex == columns)
                {
                    cellIndex = 0;
                    sb.Append("</tr>");
                }
            }

            if (cellIndex != 0)
            {
                for (; cellIndex < columns; cellIndex++)
                {
                    sb.Append("<td>&nbsp;</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</table>");

            return new HtmlString(sb.ToString());
        }
    }
}
