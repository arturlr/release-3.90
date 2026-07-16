using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nop.Services.Security;
using Nop.Web.Framework.Controllers;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Framework.Controllers
{
    [TestFixture]
    public class AdminAuthorizeAttributeTests
    {
        private AuthorizationFilterContext CreateAuthorizationContext(bool hasAccess)
        {
            var services = new ServiceCollection();
            var mockPermissionService = new Mock<IPermissionService>();
            mockPermissionService.Setup(x => x.Authorize(It.IsAny<Core.Domain.Security.PermissionRecord>()))
                .Returns(hasAccess);
            services.AddSingleton(mockPermissionService.Object);
            var serviceProvider = services.BuildServiceProvider();

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;

            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            var filterContext = new AuthorizationFilterContext(
                actionContext,
                new List<IFilterMetadata>());

            return filterContext;
        }

        [Test]
        public void Normal_request_should_not_be_affected()
        {
            // A controller without the attribute - the attribute won't be applied
            // So just verify that when dontValidate is true, no result is set
            var attribute = new AdminAuthorizeAttribute(true);
            var filterContext = CreateAuthorizationContext(false);
            attribute.OnAuthorization(filterContext);
            Assert.That(filterContext.Result, Is.Null);
        }

        [Test]
        public void Should_set_unauthorized_when_no_access()
        {
            var attribute = new AdminAuthorizeAttribute();
            var filterContext = CreateAuthorizationContext(false);
            attribute.OnAuthorization(filterContext);
            Assert.That(filterContext.Result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public void Should_not_set_result_when_has_access()
        {
            var attribute = new AdminAuthorizeAttribute();
            var filterContext = CreateAuthorizationContext(true);
            attribute.OnAuthorization(filterContext);
            Assert.That(filterContext.Result, Is.Null);
        }

        [Test]
        public void DontValidate_should_skip_authorization()
        {
            var attribute = new AdminAuthorizeAttribute(true);
            var filterContext = CreateAuthorizationContext(false);
            attribute.OnAuthorization(filterContext);
            Assert.That(filterContext.Result, Is.Null);
        }
    }
}
