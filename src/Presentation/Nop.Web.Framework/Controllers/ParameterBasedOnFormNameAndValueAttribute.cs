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

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var form = filterContext.HttpContext.Request.HasFormContentType
                ? filterContext.HttpContext.Request.Form
                : null;

            if (form != null)
            {
                var formValue = form[_name].ToString();
                filterContext.ActionArguments[_actionParameterName] = !string.IsNullOrEmpty(formValue) &&
                                                                       formValue.ToLower().Equals(_value.ToLower());
            }
            else
            {
                filterContext.ActionArguments[_actionParameterName] = false;
            }
        }
    }
}
