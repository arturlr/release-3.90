using System;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Domain.Security;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Honeypot
{
    /// <summary>
    /// Honeypot HTML helper extensions.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3: <c>HtmlHelper</c> → <see cref="IHtmlHelper"/>,
    /// <c>MvcHtmlString</c> → <see cref="IHtmlContent"/>. <c>helper.TextBox(name)</c> now returns
    /// an <see cref="IHtmlContent"/>, so it must be rendered rather than <c>ToString()</c>-ed.
    /// </remarks>
    public static class HtmlExtensions
    {
        public static IHtmlContent GenerateHoneypotInput(this IHtmlHelper helper)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("<div style=\"display:none;\">");
            sb.Append(Environment.NewLine);

            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            var hpInput = helper.TextBox(securitySettings.HoneypotInputName);
            sb.Append(hpInput.ToHtmlString());

            sb.Append(Environment.NewLine);
            sb.Append("</div>");

            return new HtmlString(sb.ToString());

            //var hpInput = helper.TextBox(securitySettings.HoneypotInputName, "", new { @class = "hp" });
            //var hpInput = helper.Hidden(securitySettings.HoneypotInputName);
            //return hpInput;
        }
    }
}
