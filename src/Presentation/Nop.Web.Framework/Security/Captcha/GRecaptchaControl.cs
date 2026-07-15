using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    public class GRecaptchaControl
    {
        private const string RECAPTCHA_API_URL_VERSION2 = "https://www.google.com/recaptcha/api.js?onload=onloadCallback&render=explicit";

        public string Id { get; set; }
        public string Theme { get; set; }
        public string PublicKey { get; set; }
        public string Language { get; set; }

        private readonly ReCaptchaVersion _version;

        public GRecaptchaControl(ReCaptchaVersion version = ReCaptchaVersion.Version1)
        {
            _version = version;
        }

        public string RenderControlToString()
        {
            SetTheme();
            var sb = new StringBuilder();

            if (_version == ReCaptchaVersion.Version2)
            {
                sb.AppendFormat("<script type=\"{0}\">var onloadCallback = function() {{grecaptcha.render('{1}', {{'sitekey' : '{2}', 'theme' : '{3}' }});}};;</script>",
                    MimeTypes.TextJavascript, Id, PublicKey, Theme);
                sb.AppendFormat("<div id=\"{0}\"></div>", Id);
                sb.AppendFormat("<script src=\"{0}\" async defer></script>",
                    RECAPTCHA_API_URL_VERSION2 + (string.IsNullOrEmpty(Language) ? "" : string.Format("&hl={0}", Language)));
            }
            else
            {
                // Version 1 is deprecated, render v2 instead
                sb.AppendFormat("<script type=\"{0}\">var onloadCallback = function() {{grecaptcha.render('{1}', {{'sitekey' : '{2}', 'theme' : '{3}' }});}};;</script>",
                    MimeTypes.TextJavascript, Id, PublicKey, Theme);
                sb.AppendFormat("<div id=\"{0}\"></div>", Id);
                sb.AppendFormat("<script src=\"{0}\" async defer></script>", RECAPTCHA_API_URL_VERSION2);
            }

            return sb.ToString();
        }

        public void RenderControl(TextWriter writer)
        {
            writer.Write(RenderControlToString());
        }

        private void SetTheme()
        {
            var themes = new[] { "white", "blackglass", "red", "clean", "light", "dark" };

            if (_version == ReCaptchaVersion.Version2)
            {
                switch ((Theme ?? "").ToLower())
                {
                    case "clean":
                    case "red":
                    case "white":
                        Theme = "light";
                        break;
                    case "blackglass":
                        Theme = "dark";
                        break;
                    default:
                        if (!themes.Contains((Theme ?? "").ToLower()))
                            Theme = "light";
                        break;
                }
            }
            else
            {
                Theme = Theme ?? "white";
            }
        }
    }
}
