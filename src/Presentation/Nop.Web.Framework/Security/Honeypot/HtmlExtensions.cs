using System;
using System.Text;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;


namespace Nop.Web.Framework.Security.Honeypot
{
    public static class HtmlExtensions
    {
        public static IHtmlContent GenerateHoneypotInput(this IHtmlHelper helper)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("<div style=\"display:none;\">");
            sb.Append(Environment.NewLine);

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            var hpInput = helper.TextBox(securitySettings.HoneypotInputName);
            using (var writer = new System.IO.StringWriter())
            {
                hpInput.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                sb.Append(writer.ToString());
            }

            sb.Append(Environment.NewLine);
            sb.Append("</div>");

            return new HtmlString(sb.ToString());
        }
    }
}
