using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Nop.Core.Infrastructure;
using Nop.Services.Security;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Task 6.2: ported to ASP.NET Core MVC authorization filters.
    /// </summary>
    /// <remarks>
    /// SECURITY-SENSITIVE FILE. The authorization decision is byte-for-byte the same as 3.90:
    /// if the action (or its controller) carries <see cref="AdminAuthorizeAttribute"/> and
    /// <see cref="IPermissionService.Authorize(Nop.Core.Domain.Security.PermissionRecord)"/> for
    /// <see cref="StandardPermissionProvider.AccessAdminPanel"/> returns false, the request is
    /// refused. Nothing was loosened to make this compile.
    ///
    /// WHY THIS IS *NOT* CONVERTED TO POLICY-BASED <c>[Authorize]</c>:
    /// design section 5 lists "AuthorizeAttribute -&gt; policy-based AuthorizeAttribute" as the
    /// generic mapping, but this type never derived from <c>System.Web.Mvc.AuthorizeAttribute</c>
    /// and performs no principal/role test. It queries nopCommerce's own database-backed
    /// <see cref="IPermissionService"/>, keyed off <c>IWorkContext.CurrentCustomer</c>, which is
    /// resolved from the nopCommerce engine and not from <c>HttpContext.User</c>. Expressing that
    /// as an <c>IAuthorizationRequirement</c> + handler would require the host
    /// (<c>Program.cs</c>, task 7.2) to register a named policy, which a class library cannot do,
    /// and would fail *open* if the host forgot - a policy name that is not registered throws,
    /// but a `[Authorize(Policy="…")]` that is never reached because the filter was dropped does
    /// not. Keeping an <see cref="IAuthorizationFilter"/> keeps the check self-contained and
    /// unconditional. A policy wrapper can be layered on later without weakening this.
    ///
    /// MAPPINGS:
    /// <list type="bullet">
    /// <item><c>FilterAttribute, IAuthorizationFilter</c> -&gt; <c>Attribute,
    /// Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter</c> (there is no
    /// <c>FilterAttribute</c> base class in ASP.NET Core).</item>
    /// <item><c>AuthorizationContext</c> -&gt; <see cref="AuthorizationFilterContext"/>.</item>
    /// <item><c>ActionDescriptor.GetCustomAttributes</c> /
    /// <c>ActionDescriptor.ControllerDescriptor.GetCustomAttributes</c> -&gt; reflection over
    /// <see cref="ControllerActionDescriptor.MethodInfo"/> and
    /// <see cref="ControllerActionDescriptor.ControllerTypeInfo"/>. ASP.NET Core's
    /// <see cref="ActionDescriptor"/> carries no attribute accessors and has no controller
    /// descriptor.</item>
    /// <item><c>HttpUnauthorizedResult</c> -&gt; <see cref="ChallengeResult"/> - see the
    /// extended rationale on <c>PublicStoreAllowNavigationAttribute</c>. Deny either way;
    /// <see cref="ChallengeResult"/> is what reproduces 3.90's redirect-to-login once the
    /// cookie handler is registered (deferral 7.13).</item>
    /// <item><c>OutputCacheAttribute.IsChildActionCacheActive(filterContext)</c> and the
    /// <c>InvalidOperationException</c> it guarded were REMOVED: ASP.NET Core has neither
    /// <c>OutputCacheAttribute</c> in the MVC5 sense nor child actions, so a child-action
    /// output cache cannot be active. This removes a throw, not a check - the permission test
    /// below still runs in every case where it ran before.</item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
    public class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly bool _dontValidate;


        public AdminAuthorizeAttribute()
            : this(false)
        {
        }

        public AdminAuthorizeAttribute(bool dontValidate)
        {
            this._dontValidate = dontValidate;
        }

        private void HandleUnauthorizedRequest(AuthorizationFilterContext filterContext)
        {
            filterContext.Result = new ChallengeResult();
        }

        private IEnumerable<AdminAuthorizeAttribute> GetAdminAuthorizeAttributes(ActionDescriptor descriptor)
        {
            var controllerActionDescriptor = descriptor as ControllerActionDescriptor;
            if (controllerActionDescriptor == null)
                return Enumerable.Empty<AdminAuthorizeAttribute>();

            return controllerActionDescriptor.MethodInfo
                .GetCustomAttributes(typeof(AdminAuthorizeAttribute), true)
                .Concat(controllerActionDescriptor.ControllerTypeInfo
                    .GetCustomAttributes(typeof(AdminAuthorizeAttribute), true))
                .OfType<AdminAuthorizeAttribute>();
        }

        private bool IsAdminPageRequested(AuthorizationFilterContext filterContext)
        {
            var adminAttributes = GetAdminAuthorizeAttributes(filterContext.ActionDescriptor);
            if (adminAttributes != null && adminAttributes.Any())
                return true;
            return false;
        }

        public void OnAuthorization(AuthorizationFilterContext filterContext)
        {
            if (_dontValidate)
                return;

            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            if (IsAdminPageRequested(filterContext))
            {
                if (!this.HasAdminAccess())
                    this.HandleUnauthorizedRequest(filterContext);
            }
        }

        public virtual bool HasAdminAccess()
        {
            var permissionService = EngineContext.Current.Resolve<IPermissionService>();
            bool result = permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel);
            return result;
        }
    }
}
