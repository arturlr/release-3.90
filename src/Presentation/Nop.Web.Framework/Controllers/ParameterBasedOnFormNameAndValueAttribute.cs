using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC filters - see
    /// <see cref="ParameterBasedOnFormNameAttribute"/> for the mapping notes
    /// (<c>ActionParameters</c> -&gt; <c>ActionArguments</c>, <c>RequestContext</c> dropped,
    /// <c>HasFormContentType</c> guard added).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] 
    public class ParameterBasedOnFormNameAndValueAttribute : Attribute, IActionFilter
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

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var formValue = request.HasFormContentType ? (string)request.Form[_name] : null;
            filterContext.ActionArguments[_actionParameterName] = !string.IsNullOrEmpty(formValue) &&
                                                                 formValue.ToLower().Equals(_value.ToLower());
        }
    }
}
