using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    /// <summary>
    /// Renders the Google reCAPTCHA markup.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3. Two changes:
    /// <list type="bullet">
    /// <item>
    /// <b><c>RenderControl(HtmlTextWriter)</c> → <c>RenderControl(TextWriter)</c>.</b>
    /// <c>System.Web.UI.HtmlTextWriter</c> is a Web Forms type with no ASP.NET Core equivalent.
    /// Nothing here used any of its HTML-aware API (<c>RenderBeginTag</c>, indentation, attribute
    /// buffering) — every call was a plain <c>Write(string)</c> — so a bare
    /// <see cref="TextWriter"/> is a complete replacement.
    /// </item>
    /// <item>
    /// <c>TagBuilder.ToString(TagRenderMode)</c> and the settable <c>InnerHtml</c> string are gone
    /// in ASP.NET Core; the render mode is a property and <c>InnerHtml</c> is an
    /// <c>IHtmlContentBuilder</c>. The script bodies must go through <c>AppendHtml</c>, not
    /// <c>Append</c>, or the JavaScript would be HTML-encoded.
    /// </item>
    /// </list>
    /// Also note <c>Attributes.Add("async", null)</c> became <c>Attributes.Add("async", "")</c>:
    /// ASP.NET Core's <c>AttributeDictionary</c> is <c>IDictionary&lt;string, string&gt;</c> and
    /// both render as <c>async=""</c>, which is what MVC 5 emitted for a null value.
    /// </remarks>
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

        public void RenderControl(TextWriter writer)
        {
            SetTheme();

            if (_version == ReCaptchaVersion.Version1)
            {
                var scriptCaptchaOptionsTag = new TagBuilder("script");
                scriptCaptchaOptionsTag.Attributes.Add("type", MimeTypes.TextJavascript);
                scriptCaptchaOptionsTag.InnerHtml.AppendHtml(
                    string.Format("var RecaptchaOptions = {{ theme: '{0}', tabindex: 0 }}; ", Theme));
                writer.Write(scriptCaptchaOptionsTag.ToHtmlString(TagRenderMode.Normal));

                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                var scriptLoadApiTag = new TagBuilder("script");
                var scriptSrc = webHelper.IsCurrentConnectionSecured() ? 
                    string.Format(RECAPTCHA_API_URL_HTTPS_VERSION1, PublicKey) :
                    string.Format(RECAPTCHA_API_URL_HTTP_VERSION1, PublicKey);
                scriptLoadApiTag.Attributes.Add("src", scriptSrc);
                writer.Write(scriptLoadApiTag.ToHtmlString(TagRenderMode.Normal));
            }
            else if (_version == ReCaptchaVersion.Version2)
            {
                var scriptCallbackTag = new TagBuilder("script");
                scriptCallbackTag.Attributes.Add("type", MimeTypes.TextJavascript);
                scriptCallbackTag.InnerHtml.AppendHtml(string.Format("var onloadCallback = function() {{grecaptcha.render('{0}', {{'sitekey' : '{1}', 'theme' : '{2}' }});}};", Id, PublicKey, Theme));
                writer.Write(scriptCallbackTag.ToHtmlString(TagRenderMode.Normal));

                var captchaTag = new TagBuilder("div");
                captchaTag.Attributes.Add("id", Id);
                writer.Write(captchaTag.ToHtmlString(TagRenderMode.Normal));

                var scriptLoadApiTag = new TagBuilder("script");
                scriptLoadApiTag.Attributes.Add("src", RECAPTCHA_API_URL_VERSION2 + (string.IsNullOrEmpty(Language) ? "" : string.Format("&hl={0}", Language)));
                scriptLoadApiTag.Attributes.Add("async", "");
                scriptLoadApiTag.Attributes.Add("defer", "");
                writer.Write(scriptLoadApiTag.ToHtmlString(TagRenderMode.Normal));
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
