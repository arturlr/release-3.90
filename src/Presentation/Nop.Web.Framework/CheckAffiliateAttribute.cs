using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Infrastructure;
using Nop.Services.Affiliates;
using Nop.Services.Customers;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Task 6.2: ported from System.Web.Mvc to ASP.NET Core MVC filters.
    /// <list type="bullet">
    /// <item><c>System.Web.Mvc.ActionFilterAttribute</c> -&gt; <c>Microsoft.AspNetCore.Mvc.Filters.ActionFilterAttribute</c></item>
    /// <item><c>HttpRequestBase.QueryString</c> (NameValueCollection) -&gt; <c>HttpRequest.Query</c> (IQueryCollection)</item>
    /// <item><c>filterContext.IsChildAction</c> has NO ASP.NET Core equivalent and was removed -
    /// child actions were replaced by View Components, which do not run the action filter
    /// pipeline at all, so the guard is unnecessary rather than merely unavailable.</item>
    /// </list>
    /// </summary>
    public class CheckAffiliateAttribute : ActionFilterAttribute
    {
        private const string AFFILIATE_ID_QUERY_PARAMETER_NAME = "affiliateid";
        private const string AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME = "affiliate";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            HttpRequest request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            Affiliate affiliate = null;

            var query = request.Query;
            if (query != null)
            {
                //try to find by ID ("affiliateId" parameter)
                if (query.ContainsKey(AFFILIATE_ID_QUERY_PARAMETER_NAME))
                {
                    var affiliateId = Convert.ToInt32(query[AFFILIATE_ID_QUERY_PARAMETER_NAME].ToString());
                    if (affiliateId > 0)
                    {
                        var affiliateService = EngineContext.Current.Resolve<IAffiliateService>();
                        affiliate = affiliateService.GetAffiliateById(affiliateId);
                    }
                }
                //try to find by friendly name ("affiliate" parameter)
                else if (query.ContainsKey(AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME))
                {
                    var friendlyUrlName = query[AFFILIATE_FRIENDLYURLNAME_QUERY_PARAMETER_NAME].ToString();
                    if (!String.IsNullOrEmpty(friendlyUrlName))
                    {
                        var affiliateService = EngineContext.Current.Resolve<IAffiliateService>();
                        affiliate = affiliateService.GetAffiliateByFriendlyUrlName(friendlyUrlName);
                    }
                }
            }


            if (affiliate != null && !affiliate.Deleted && affiliate.Active)
            {
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                if (workContext.CurrentCustomer.AffiliateId != affiliate.Id)
                {
                    workContext.CurrentCustomer.AffiliateId = affiliate.Id;
                    var customerService = EngineContext.Current.Resolve<ICustomerService>();
                    customerService.UpdateCustomer(workContext.CurrentCustomer);
                }
            }
        }
    }
}
