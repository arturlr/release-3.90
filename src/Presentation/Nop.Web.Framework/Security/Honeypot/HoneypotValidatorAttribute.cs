using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Security.Honeypot
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class HoneypotValidatorAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var honeypotValue = request.Form["HP_FieldName"];
                if (!string.IsNullOrEmpty(honeypotValue))
                {
                    // Bot detected
                    context.Result = new BadRequestResult();
                    return;
                }
            }
            base.OnActionExecuting(context);
        }
    }
}
