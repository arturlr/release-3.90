using System.IO;
using System.Text;
using Nop.Core.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Web.Framework.Security.Captcha
{
    public static class HtmlExtensions
    {
        public static IHtmlContent GenerateCaptcha(this IHtmlHelper helper)
        {
            var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();

            var captchaControl = new GRecaptchaControl(captchaSettings.ReCaptchaVersion)
            {
                Theme = captchaSettings.ReCaptchaTheme,
                Id = "recaptcha",
                PublicKey = captchaSettings.ReCaptchaPublicKey,
                Language = captchaSettings.ReCaptchaLanguage
            };

            var sb = new StringBuilder();
            captchaControl.RenderControl(sb);

            return new HtmlString(sb.ToString());
        }
    }
}
