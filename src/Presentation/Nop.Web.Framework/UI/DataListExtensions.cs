using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework.UI
{
    public static class DataListExtensions
    {
        public static IHtmlContent DataList<T>(this IHtmlHelper html,
            IEnumerable<T> data, int columns, Func<T, IHtmlContent> template)
        {
            if (data == null)
                return HtmlString.Empty;

            var sb = new StringBuilder();
            sb.Append("<table>");
            int cellIndex = 0;
            foreach (var item in data)
            {
                if (cellIndex == 0) sb.Append("<tr>");
                sb.Append("<td>");
                using (var writer = new System.IO.StringWriter())
                {
                    template(item).WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                    sb.Append(writer.ToString());
                }
                sb.Append("</td>");
                cellIndex++;
                if (cellIndex >= columns) { sb.Append("</tr>"); cellIndex = 0; }
            }
            if (cellIndex > 0)
            {
                for (int i = cellIndex; i < columns; i++) sb.Append("<td></td>");
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return new HtmlString(sb.ToString());
        }
    }
}
