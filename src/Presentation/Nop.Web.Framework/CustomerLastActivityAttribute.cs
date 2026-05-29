using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    public class CustomerLastActivityAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                return;

            //only GET requests
            if (!string.Equals(context.HttpContext.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
                return;

            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var customer = workContext.CurrentCustomer;

            //update last activity date
            if (customer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
            {
                var customerService = EngineContext.Current.Resolve<ICustomerService>();
                customer.LastActivityDateUtc = DateTime.UtcNow;
                customerService.UpdateCustomer(customer);
            }
        }
    }
}
