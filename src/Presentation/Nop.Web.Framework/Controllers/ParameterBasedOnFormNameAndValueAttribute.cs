using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Controllers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ParameterBasedOnFormNameAndValueAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly string _value;
        private readonly string _actionParameterName;

        public ParameterBasedOnFormNameAndValueAttribute(string name, string value, string actionParameterName)
        {
            this._name = name;
            this._value = value;
            this._actionParameterName = actionParameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;
            if (request.HasFormContentType)
            {
                var formValue = request.Form[_name];
                context.ActionArguments[_actionParameterName] = !string.IsNullOrEmpty(formValue) &&
                                                                formValue.ToString().Equals(_value, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                context.ActionArguments[_actionParameterName] = false;
            }
        }
    }
}
