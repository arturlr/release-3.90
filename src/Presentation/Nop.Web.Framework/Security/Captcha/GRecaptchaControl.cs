using System.Linq;
using System.Text;
using Nop.Core;
using Nop.Core.Infrastructure;


namespace Nop.Web.Framework.Security.Captcha
{
    public class GRecaptchaControl
    {
        private const string RECAPTCHA_API_URL_HTTP_VERSION1 = "http://www.google.com/recaptcha/api/challenge?k={0}";
        private const string RECAPTCHA_API_URL_HTTPS_VERSION1 = "https://www.google.com/recaptcha/api/challenge?k={0}";
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

        public void RenderControl(StringBuilder writer)
        {
            SetTheme();

            if (_version == ReCaptchaVersion.Version1)
            {
                writer.AppendFormat("<script type=\"{0}\">", MimeTypes.TextJavascript);
                writer.AppendFormat("var RecaptchaOptions = {{ theme: '{0}', tabindex: 0 }}; ", Theme);
                writer.Append("</script>");

                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var scriptSrc = webHelper.IsCurrentConnectionSecured() ? 
                    string.Format(RECAPTCHA_API_URL_HTTPS_VERSION1, PublicKey) :
                    string.Format(RECAPTCHA_API_URL_HTTP_VERSION1, PublicKey);
                writer.AppendFormat("<script src=\"{0}\"></script>", scriptSrc);
            }
            else if (_version == ReCaptchaVersion.Version2)
            {
                writer.AppendFormat("<script type=\"{0}\">", MimeTypes.TextJavascript);
                writer.AppendFormat("var onloadCallback = function() {{grecaptcha.render('{0}', {{'sitekey' : '{1}', 'theme' : '{2}' }});}};", Id, PublicKey, Theme);
                writer.Append("</script>");

                writer.AppendFormat("<div id=\"{0}\"></div>", Id);

                var src = RECAPTCHA_API_URL_VERSION2 + (string.IsNullOrEmpty(Language) ? "" : string.Format("&hl={0}", Language));
                writer.AppendFormat("<script src=\"{0}\" async defer></script>", src);
            }
        }

        private void SetTheme()
        {
            var themes = new[] {"white", "blackglass", "red", "clean", "light", "dark"};

            if (_version == ReCaptchaVersion.Version1)
            {
                switch (Theme.ToLower())
                {
                    case "light":
                        Theme = "white";
                        break;
                    case "dark":
                        Theme = "blackglass";
                        break;
                    default:
                        if (!themes.Contains(Theme.ToLower()))
                        {
                            Theme = "white";
                        }
                        break;
                }
            }
            else if (_version == ReCaptchaVersion.Version2)
            {
                switch (Theme.ToLower())
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
                        if (!themes.Contains(Theme.ToLower()))
                        {
                            Theme = "light";
                        }
                        break;
                }
            }
        }
    }
}
