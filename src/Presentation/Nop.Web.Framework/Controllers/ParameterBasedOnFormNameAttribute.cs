using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// If form name exists, then specified "actionParameterName" will be set to "true"
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ParameterBasedOnFormNameAttribute : ActionFilterAttribute
    {
        private readonly string _name;
        private readonly string _actionParameterName;

        public ParameterBasedOnFormNameAttribute(string name, string actionParameterName)
        {
            this._name = name;
            this._actionParameterName = actionParameterName;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var form = filterContext.HttpContext.Request.HasFormContentType
                ? filterContext.HttpContext.Request.Form
                : null;

            if (form != null)
            {
                filterContext.ActionArguments[_actionParameterName] = form.Keys.Any(x => x.Equals(_name));
            }
            else
            {
                filterContext.ActionArguments[_actionParameterName] = false;
            }
        }
    }
}
