using System;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    public static class CaptchaHtmlExtensions
    {
        public static IHtmlContent GenerateCaptcha(this IHtmlHelper helper)
        {
            var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();
            var scriptTag = $"<script src=\"https://www.google.com/recaptcha/api.js\" async defer></script>";
            var divTag = $"<div class=\"g-recaptcha\" data-sitekey=\"{captchaSettings.ReCaptchaPublicKey}\"></div>";
            return new HtmlString(scriptTag + Environment.NewLine + divTag);
        }
    }
}
