using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain;
using Nop.Services.Security;
using Nop.Services.Topics;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Store closed attribute
    /// </summary>
    public class StoreClosedAttribute : ActionFilterAttribute
    {
        private readonly bool _ignore;

        /// <summary>
        /// Ctor 
        /// </summary>
        /// <param name="ignore">Pass false in order to ignore this functionality for a certain action method</param>
        public StoreClosedAttribute(bool ignore = false)
        {
            this._ignore = ignore;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null || filterContext.HttpContext == null)
                return;

            //search the solution by "[StoreClosed(true)]" keyword 
            //in order to find method available even when a store is closed
            if (_ignore)
                return;

            var request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            try
            {
                var storeInformationSettings = filterContext.HttpContext.RequestServices.GetService<StoreInformationSettings>();
                if (storeInformationSettings == null || !storeInformationSettings.StoreClosed)
                    return;
            }
            catch
            {
                // If settings can't be resolved, allow navigation
                return;
            }

            //get controller and action names
            string controllerName = string.Empty;
            string actionName = string.Empty;
            if (filterContext.RouteData.Values.ContainsKey("controller"))
                controllerName = filterContext.RouteData.Values["controller"].ToString();
            if (filterContext.RouteData.Values.ContainsKey("action"))
                actionName = filterContext.RouteData.Values["action"].ToString();

            //topics accessible when a store is closed
            if (controllerName.Equals("Topic", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.Equals("TopicDetails", StringComparison.InvariantCultureIgnoreCase))
            {
                var topicService = filterContext.HttpContext.RequestServices.GetService<ITopicService>();
                var storeContext = filterContext.HttpContext.RequestServices.GetService<IStoreContext>();
                var allowedTopicIds = topicService.GetAllTopics(storeContext.CurrentStore.Id)
                    .Where(t => t.AccessibleWhenStoreClosed)
                    .Select(t => t.Id)
                    .ToList();
                var requestedTopicId = filterContext.RouteData.Values["topicId"] as int?;
                if (requestedTopicId.HasValue && allowedTopicIds.Contains(requestedTopicId.Value))
                    return;
            }

            //access to a closed store?
            var permissionService = filterContext.HttpContext.RequestServices.GetService<IPermissionService>();
            if (permissionService.Authorize(StandardPermissionProvider.AccessClosedStore))
                return;

            filterContext.Result = new RedirectToRouteResult("StoreClosed", new RouteValueDictionary());
        }
    }
}
