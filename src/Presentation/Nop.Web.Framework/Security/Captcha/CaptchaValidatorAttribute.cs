using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Web.Framework.Security.Captcha
{
    public class CaptchaValidatorAttribute : ActionFilterAttribute
    {
        private const string G_RESPONSE_FIELD_KEY = "g-recaptcha-response";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            bool valid = false;
            var form = filterContext.HttpContext.Request.HasFormContentType
                ? filterContext.HttpContext.Request.Form : null;

            var gCaptchaResponseValue = form != null ? form[G_RESPONSE_FIELD_KEY].ToString() : null;
            if (!string.IsNullOrEmpty(gCaptchaResponseValue))
            {
                var captchaSettings = filterContext.HttpContext.RequestServices.GetService<CaptchaSettings>();
                if (captchaSettings.Enabled)
                {
                    var captchaValidator = new GReCaptchaValidator(captchaSettings.ReCaptchaVersion)
                    {
                        SecretKey = captchaSettings.ReCaptchaPrivateKey,
                        RemoteIp = filterContext.HttpContext.Connection.RemoteIpAddress?.ToString(),
                        Response = gCaptchaResponseValue,
                        Challenge = string.Empty
                    };

                    var recaptchaResponse = captchaValidator.Validate();
                    valid = recaptchaResponse.IsValid;
                }
            }

            filterContext.ActionArguments["captchaValid"] = valid;
            base.OnActionExecuting(filterContext);
        }
    }
}
