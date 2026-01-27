using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Base controller for ASP.NET Core
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Called before the action method is invoked
        /// </summary>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Called after the action method is invoked
        /// </summary>
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
        }
    }
}
