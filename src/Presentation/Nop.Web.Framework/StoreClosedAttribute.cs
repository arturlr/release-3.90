using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain;
using Nop.Core.Infrastructure;
using Nop.Services.Security;
using Nop.Services.Topics;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Store closed attribute
    /// </summary>
    /// <remarks>
    /// Task 6.2: ported to ASP.NET Core MVC filters - see ValidatePasswordAttribute for the
    /// ActionDescriptor / IUrlHelperFactory mappings. One extra difference here:
    /// <c>RouteData.Values["topicId"] as int?</c> worked in System.Web because the value was
    /// already boxed as an int by the route; ASP.NET Core route values are always
    /// <see cref="string"/>, so the cast would silently produce null and every topic would be
    /// blocked when the store is closed. It is therefore parsed explicitly.
    /// </remarks>
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

            HttpRequest request = filterContext.HttpContext.Request;
            if (request == null)
                return;

            var controllerActionDescriptor = filterContext.ActionDescriptor as ControllerActionDescriptor;
            string actionName = controllerActionDescriptor != null ? controllerActionDescriptor.ActionName : null;
            if (String.IsNullOrEmpty(actionName))
                return;

            string controllerName = filterContext.Controller != null ? filterContext.Controller.ToString() : null;
            if (String.IsNullOrEmpty(controllerName))
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            var storeInformationSettings = EngineContext.Current.Resolve<StoreInformationSettings>();
            if (!storeInformationSettings.StoreClosed)
                return;

            //topics accessible when a store is closed
            if (controllerName.Equals("Nop.Web.Controllers.TopicController", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.Equals("TopicDetails", StringComparison.InvariantCultureIgnoreCase))
            {
                var topicService = EngineContext.Current.Resolve<ITopicService>();
                var storeContext = EngineContext.Current.Resolve<IStoreContext>();
                var allowedTopicIds = topicService.GetAllTopics(storeContext.CurrentStore.Id)
                    .Where(t => t.AccessibleWhenStoreClosed)
                    .Select(t => t.Id)
                    .ToList();
                int requestedTopicId;
                var routeTopicId = filterContext.RouteData != null ? filterContext.RouteData.Values["topicId"] : null;
                if (routeTopicId != null &&
                    int.TryParse(Convert.ToString(routeTopicId), out requestedTopicId) &&
                    allowedTopicIds.Contains(requestedTopicId))
                    return;
            }

            //access to a closed store?
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            if (permissionService.Authorize(StandardPermissionProvider.AccessClosedStore))
                return;

            var urlHelper = filterContext.HttpContext.RequestServices
                .GetRequiredService<IUrlHelperFactory>()
                .GetUrlHelper(filterContext);
            var storeClosedUrl = urlHelper.RouteUrl("StoreClosed");
            filterContext.Result = new RedirectResult(storeClosedUrl);
        }
    }
}
