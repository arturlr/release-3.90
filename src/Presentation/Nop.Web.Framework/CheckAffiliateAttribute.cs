using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Services.Affiliates;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    public class CheckAffiliateAttribute : ActionFilterAttribute
    {
        private const string AFFILIATE_ID_QUERY_PARAMETER_NAME = "affiliateid";
        private const string AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME = "affiliate";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            var request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            Affiliate affiliate = null;

            if (request.Query != null)
            {
                //try to find by ID ("affiliateId" parameter)
                if (request.Query.ContainsKey(AFFILIATE_ID_QUERY_PARAMETER_NAME))
                {
                    var affiliateIdStr = request.Query[AFFILIATE_ID_QUERY_PARAMETER_NAME].ToString();
                    int affiliateId;
                    if (int.TryParse(affiliateIdStr, out affiliateId) && affiliateId > 0)
                    {
                        var affiliateService = filterContext.HttpContext.RequestServices.GetService<IAffiliateService>();
                        affiliate = affiliateService.GetAffiliateById(affiliateId);
                    }
                }
                //try to find by friendly name ("affiliate" parameter)
                else if (request.Query.ContainsKey(AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME))
                {
                    var friendlyUrlName = request.Query[AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME].ToString();
                    if (!String.IsNullOrEmpty(friendlyUrlName))
                    {
                        var affiliateService = filterContext.HttpContext.RequestServices.GetService<IAffiliateService>();
                        affiliate = affiliateService.GetAffiliateByFriendlyUrlName(friendlyUrlName);
                    }
                }
            }


            if (affiliate != null && !affiliate.Deleted && affiliate.Active)
            {
                var workContext = filterContext.HttpContext.RequestServices.GetService<IWorkContext>();
                if (workContext.CurrentCustomer.AffiliateId != affiliate.Id)
                {
                    workContext.CurrentCustomer.AffiliateId = affiliate.Id;
                    var customerService = filterContext.HttpContext.RequestServices.GetService<ICustomerService>();
                    customerService.UpdateCustomer(workContext.CurrentCustomer);
                }
            }
        }
    }
}
