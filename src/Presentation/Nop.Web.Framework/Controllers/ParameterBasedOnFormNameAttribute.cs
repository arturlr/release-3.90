using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// If form name exists, then specified "actionParameterName" will be set to "true"
    /// </summary>
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core MVC filters.
    /// <list type="bullet">
    /// <item><c>FilterAttribute, IActionFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IActionFilter</c>.</item>
    /// <item><c>filterContext.ActionParameters</c> -&gt;
    /// <see cref="ActionExecutingContext.ActionArguments"/>.</item>
    /// <item><c>filterContext.RequestContext.HttpContext</c> -&gt;
    /// <c>filterContext.HttpContext</c> (there is no <c>RequestContext</c> in ASP.NET Core).</item>
    /// <item><c>Request.Form.AllKeys</c> -&gt; <c>Request.Form.Keys</c>, plus a
    /// <c>HasFormContentType</c> guard because reading <c>Form</c> on a non-form request throws
    /// in ASP.NET Core where System.Web returned an empty collection.</item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)] 
    public class ParameterBasedOnFormNameAttribute : Attribute, IActionFilter
    {
        private readonly string _name;
        private readonly string _actionParameterName;

        public ParameterBasedOnFormNameAttribute(string name, string actionParameterName)
        {
            this._name = name;
            this._actionParameterName = actionParameterName;
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }

        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //we check "name" only. uncomment the code below if you want to check whether "value" attribute is specified
            //var formValue = filterContext.HttpContext.Request.Form[_name];
            //filterContext.ActionArguments[_actionParameterName] = !string.IsNullOrEmpty(formValue);
            var request = filterContext.HttpContext.Request;
            filterContext.ActionArguments[_actionParameterName] = request.HasFormContentType &&
                request.Form.Keys.Any(x => x.Equals(_name));
        }
    }
}
