using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.Security.Captcha
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CaptchaValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();
            if (captchaSettings.Enabled)
            {
                var request = context.HttpContext.Request;
                var gRecaptchaResponse = request.Form["g-recaptcha-response"];

                if (string.IsNullOrEmpty(gRecaptchaResponse))
                {
                    context.ActionArguments["captchaValid"] = false;
                }
                else
                {
                    var validator = new GReCaptchaValidator
                    {
                        SecretKey = captchaSettings.ReCaptchaPrivateKey,
                        Response = gRecaptchaResponse
                    };
                    var result = validator.Validate();
                    context.ActionArguments["captchaValid"] = result;
                }
            }
            else
            {
                context.ActionArguments["captchaValid"] = false;
            }

            base.OnActionExecuting(context);
        }
    }
}
