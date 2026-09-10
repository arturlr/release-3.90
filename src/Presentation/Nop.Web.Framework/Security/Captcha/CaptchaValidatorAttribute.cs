using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC filters.
    /// <list type="bullet">
    /// <item><c>System.Web.Mvc.ActionFilterAttribute</c> -&gt;
    /// <c>Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute</c>.</item>
    /// <item><c>filterContext.ActionParameters</c> -&gt;
    /// <see cref="ActionExecutingContext.ActionArguments"/> - this is how the
    /// <c>captchaValid</c> action parameter is injected, and it is the reason every captcha-guarded
    /// action keeps its <c>bool captchaValid</c> parameter unchanged.</item>
    /// <item><c>Request.UserHostAddress</c> -&gt;
    /// <c>HttpContext.Connection.RemoteIpAddress</c> (there is no <c>UserHostAddress</c> in
    /// ASP.NET Core).</item>
    /// <item><c>Request.Form[key]</c> guarded by <c>HasFormContentType</c> - ASP.NET Core throws
    /// on a non-form request where System.Web returned an empty collection. With no form the
    /// outcome is <c>captchaValid = false</c>, exactly as before.</item>
    /// </list>
    /// SECURITY NOTE: the fail-closed default is preserved - <c>valid</c> starts false and is only
    /// set from a successful reCAPTCHA verification, so a missing/blank response still yields
    /// <c>captchaValid = false</c>.
    /// </summary>
    public class CaptchaValidatorAttribute : ActionFilterAttribute
    {
        private const string CHALLENGE_FIELD_KEY = "recaptcha_challenge_field";
        private const string RESPONSE_FIELD_KEY = "recaptcha_response_field";
        private const string G_RESPONSE_FIELD_KEY = "g-recaptcha-response";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool valid = false;
            var request = filterContext.HttpContext.Request;
            var hasForm = request.HasFormContentType;
            var captchaChallengeValue = hasForm ? (string)request.Form[CHALLENGE_FIELD_KEY] : null;
            var captchaResponseValue = hasForm ? (string)request.Form[RESPONSE_FIELD_KEY] : null;
            var gCaptchaResponseValue = hasForm ? (string)request.Form[G_RESPONSE_FIELD_KEY] : null;
            if ((!string.IsNullOrEmpty(captchaChallengeValue) && !string.IsNullOrEmpty(captchaResponseValue)) || !string.IsNullOrEmpty(gCaptchaResponseValue))
            {
                var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();
                if (captchaSettings.Enabled)
                {
                    var remoteIpAddress = filterContext.HttpContext.Connection != null &&
                                          filterContext.HttpContext.Connection.RemoteIpAddress != null
                        ? filterContext.HttpContext.Connection.RemoteIpAddress.ToString()
                        : null;

                    var captchaValidtor = new GReCaptchaValidator(captchaSettings.ReCaptchaVersion)
                    {
                        SecretKey = captchaSettings.ReCaptchaPrivateKey,
                        RemoteIp = remoteIpAddress,
                        Response = captchaResponseValue ?? gCaptchaResponseValue,
                        Challenge = captchaChallengeValue
                    };

                    var recaptchaResponse = captchaValidtor.Validate();
                    valid = recaptchaResponse.IsValid;
                }
            }

            //this will push the result value into a parameter in our Action  
            filterContext.ActionArguments["captchaValid"] = valid;

            base.OnActionExecuting(filterContext);
        }
    }
}
