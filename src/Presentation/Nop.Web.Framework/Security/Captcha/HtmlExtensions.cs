using System.IO;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    /// <summary>
    /// Captcha HTML helper extensions.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3. 3.90 wrapped a <see cref="StringWriter"/> in a
    /// <c>System.Web.UI.HtmlTextWriter</c> and then read the markup back out through
    /// <c>HtmlTextWriter.InnerWriter.ToString()</c> — the <c>HtmlTextWriter</c> was pure ceremony.
    /// <c>HtmlTextWriter</c> has no ASP.NET Core equivalent, so
    /// <see cref="GRecaptchaControl.RenderControl"/> now takes a plain
    /// <see cref="TextWriter"/> and the <see cref="StringWriter"/> is used directly.
    /// The returned markup is byte-identical.
    /// </remarks>
    public static class HtmlExtensions
    {
        public static string GenerateCaptcha(this IHtmlHelper helper)
        {
            var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();

            using (var writer = new StringWriter())
            {
                var captchaControl = new GRecaptchaControl(captchaSettings.ReCaptchaVersion)
                {
                    Theme = captchaSettings.ReCaptchaTheme,
                    Id = "recaptcha",
                    PublicKey = captchaSettings.ReCaptchaPublicKey,
                    Language = captchaSettings.ReCaptchaLanguage
                };
                captchaControl.RenderControl(writer);

                return writer.ToString();
            }
        }
    }
}
