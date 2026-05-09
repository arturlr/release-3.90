using Nop.Web.Framework.Controllers;
using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Collections.Generic;

namespace Nop.Web.MVC.Tests.Framework.Controllers
{
    [TestFixture]
    public class AdminAuthorizeAttributeTests
    {
        private AuthorizationFilterContext GetAuthorizationContext<TController>() where TController : Controller, new()
        {
            var httpContext = new DefaultHttpContext();
            var routeData = new RouteData();
            var actionDescriptor = new ActionDescriptor();
            var actionContext = new ActionContext(httpContext, routeData, actionDescriptor, new ModelStateDictionary());
            var filters = new List<IFilterMetadata>();
            return new AuthorizationFilterContext(actionContext, filters);
        }

        private Mock<AdminAuthorizeAttribute> GetAdminAuthorizeAttributeMock(bool result)
        {
            var attributeMock = new Mock<AdminAuthorizeAttribute>() { CallBase = true };
            attributeMock.Setup(x => x.HasAdminAccess()).Returns(result);
            return attributeMock;
        }

        private void TestActionThatShouldRequirePermission<TController>() where TController : Controller, new()
        {
            var authorizationContext = GetAuthorizationContext<TController>();
            var attributeMock = GetAdminAuthorizeAttributeMock(false);
            attributeMock.Object.OnAuthorization(authorizationContext);
            Assert.That(authorizationContext.Result, Is.InstanceOf<UnauthorizedResult>());

            var authorizationContext2 = GetAuthorizationContext<TController>();
            var attributeMock2 = GetAdminAuthorizeAttributeMock(true);
            attributeMock2.Object.OnAuthorization(authorizationContext2);
            Assert.That(authorizationContext2.Result, Is.Null);
        }

        [Test]
        public void Normal_request_should_not_be_affected()
        {
            var authorizationContext = GetAuthorizationContext<NormalController>();

            var attributeMock = GetAdminAuthorizeAttributeMock(false);
            attributeMock.Object.OnAuthorization(authorizationContext);

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
        public IActionResult Index()
        {
            return View();
        }
    }
    
    [AdminAuthorize]
    public class NormalWithAttribController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }

    public class NormalWithActionAttribController : Controller
    {
        [AdminAuthorize]
        public IActionResult Index()
        {
            return View();
        }
    }

    [AdminAuthorize]
    public class BaseWithAttribController : Controller
    {
        public IActionResult Something()
        {
            return View();
        }
    }

    public class InheritedAttribController : BaseWithAttribController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
