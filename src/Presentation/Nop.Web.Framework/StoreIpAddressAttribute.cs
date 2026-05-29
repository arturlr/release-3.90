using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    public class StoreIpAddressAttribute : ActionFilterAttribute
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

            var webHelper = EngineContext.Current.Resolve<IWebHelper>();

            //update IP address
            string currentIpAddress = webHelper.GetCurrentIpAddress();
            if (!String.IsNullOrEmpty(currentIpAddress))
            {
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                var customer = workContext.CurrentCustomer;
                if (!currentIpAddress.Equals(customer.LastIpAddress, StringComparison.InvariantCultureIgnoreCase))
                {
                    var customerService = EngineContext.Current.Resolve<ICustomerService>();
                    customer.LastIpAddress = currentIpAddress;
                    customerService.UpdateCustomer(customer);
                }
            }
        }
    }
}
