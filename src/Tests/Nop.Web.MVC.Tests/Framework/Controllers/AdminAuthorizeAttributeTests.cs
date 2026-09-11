using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Controllers;
using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Framework.Controllers
{
    /// <summary>
    /// Task 17.1 — ported from MVC 5 to ASP.NET Core.
    ///
    /// WHAT THE 3.90 TEST ASSERTED, AND WHAT STILL HOLDS
    /// -------------------------------------------------
    /// The intent is unchanged: when an action (or its controller) carries
    /// [AdminAuthorize] and the admin-access check fails, the request must be refused;
    /// when the check passes, or when neither the action nor its controller carries the
    /// attribute, the request proceeds untouched. All four original test cases are
    /// preserved (Normal_request_should_not_be_affected, and the controller-level,
    /// action-level and inherited-attribute permission cases).
    ///
    /// WHAT CHANGED, AND WHY (faithfulness notes)
    /// ------------------------------------------
    /// The MVC-5 shape does not exist on net10.0, so the mechanics are re-expressed
    /// against the ACTUAL ported AdminAuthorizeAttribute (Nop.Web.Framework, task 6.2),
    /// which is an ASP.NET Core Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter:
    ///
    ///   * AuthorizationContext            -> AuthorizationFilterContext
    ///   * ReflectedControllerDescriptor   -> ControllerActionDescriptor with MethodInfo /
    ///     + ControllerContext             -> ControllerTypeInfo populated (that is exactly
    ///     + FakeHttpContext                 what the ported IsAdminPageRequested reflects
    ///                                        over) wrapped in an ActionContext built from a
    ///                                        DefaultHttpContext.
    ///   * HttpUnauthorizedResult          -> ChallengeResult. The port replaced
    ///                                        HttpUnauthorizedResult with ChallengeResult
    ///                                        (runtime-deferrals: AdminAuthorizeAttribute
    ///                                        6.2 rationale). Asserting InstanceOf&lt;ChallengeResult&gt;
    ///                                        here is asserting the CURRENT contract, not a
    ///                                        type that no longer exists.
    ///   * MockRepository.GeneratePartialMock -> Substitute.ForPartsOf (partial substitute).
    ///     + attribute.Expect(x => x.HasAdminAccess()).Return(result)
    ///       -> attribute.Configure().HasAdminAccess().Returns(result)
    ///
    /// The FakeHttpContext / RouteData("~/") plumbing 3.90 used only existed to satisfy the
    /// MVC-5 ControllerContext + FindAction path. The ported filter never touches the
    /// HttpContext or the route data — it decides purely from the ActionDescriptor's
    /// attributes and HasAdminAccess() — so a DefaultHttpContext with empty RouteData is a
    /// faithful, minimal stand-in.
    ///
    /// NSUBSTITUTE IDIOM (matches Rhino intent exactly)
    /// ------------------------------------------------
    /// HasAdminAccess() is virtual, so ForPartsOf can override it. Configure() is used before
    /// .Returns(result) so the REAL HasAdminAccess() — which resolves IPermissionService from
    /// EngineContext.Current and would throw in a unit test with no engine — never runs during
    /// arrangement. OnAuthorization() then calls the overridden HasAdminAccess(), so the
    /// substitute genuinely drives the code path (proven able to fail: flipping the expected
    /// result turns the assertion red — see the migration write-up).
    /// </summary>
    [TestFixture]
    public class AdminAuthorizeAttributeTests
    {
        private static AuthorizationFilterContext GetAuthorizationContext<TController>() where TController : ControllerBase, new()
        {
            var actionDescriptor = new ControllerActionDescriptor
            {
                ControllerTypeInfo = typeof(TController).GetTypeInfo(),
                MethodInfo = typeof(TController).GetMethod("Index"),
                ActionName = "Index",
                ControllerName = typeof(TController).Name.Replace("Controller", "")
            };

            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
            return new AuthorizationFilterContext(actionContext, new IFilterMetadata[0]);
        }

        private static AdminAuthorizeAttribute GetAdminAuthorizeAttribute(bool result)
        {
            //Partial substitute: real behaviour except HasAdminAccess(), which is overridden.
            //Configure() before Returns() keeps the real (engine-resolving) HasAdminAccess()
            //from executing while we arrange it.
            var attribute = Substitute.ForPartsOf<AdminAuthorizeAttribute>();
            attribute.Configure().HasAdminAccess().Returns(result);
            return attribute;
        }

        private static void TestActionThatShouldRequirePermission<TController>() where TController : ControllerBase, new()
        {
            var authorizationContext = GetAuthorizationContext<TController>();
            var attribute = GetAdminAuthorizeAttribute(false);
            attribute.OnAuthorization(authorizationContext);
            Assert.That(authorizationContext.Result, Is.InstanceOf<ChallengeResult>());

            var authorizationContext2 = GetAuthorizationContext<TController>();
            var attribute2 = GetAdminAuthorizeAttribute(true);
            attribute2.OnAuthorization(authorizationContext2);
            Assert.That(authorizationContext2.Result, Is.Null);
        }

        [Test]
        public void Normal_request_should_not_be_affected()
        {
            var authorizationContext = GetAuthorizationContext<NormalController>();

            var attribute = GetAdminAuthorizeAttribute(false);
            attribute.OnAuthorization(authorizationContext);

            Assert.That(authorizationContext.Result, Is.Null);
        }

        
        [Test]
        public void Normal_with_attribute_request_should_require_permission()
        {
            TestActionThatShouldRequirePermission<NormalWithAttribController>();
        }

        [Test]
        public void Normal_with_action_attribute_request_should_require_permission()
        {
            TestActionThatShouldRequirePermission<NormalWithActionAttribController>();
        }

        [Test]
        public void Inherited_attribute_request_should_require_permission()
        {
            TestActionThatShouldRequirePermission<InheritedAttribController>();
        }
    }

    public class NormalController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
    
    [AdminAuthorize]
    public class NormalWithAttribController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }

    public class NormalWithActionAttribController : Controller
    {
        [AdminAuthorize]
        public ActionResult Index()
        {
            return View();
        }
    }

    [AdminAuthorize]
    public class BaseWithAttribController : Controller
    {
        public ActionResult Something()
        {
            return View();
        }
    }

    public class InheritedAttribController : BaseWithAttribController
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
