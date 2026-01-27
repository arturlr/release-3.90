using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;

namespace Nop.Web.Framework.Filters
{
    /// <summary>
    /// Customer last activity filter - updates last activity date
    /// </summary>
    public class CustomerLastActivityAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request == null)
                return;

            var workContext = context.HttpContext.RequestServices.GetService(typeof(IWorkContext)) as IWorkContext;

            if (workContext?.CurrentCustomer != null)
            {
                var customer = workContext.CurrentCustomer;
                if (customer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
                {
                    customer.LastActivityDateUtc = DateTime.UtcNow;
                    // Customer service update would go here
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
