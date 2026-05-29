using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;
using NSubstitute;

namespace Nop.Web.MVC.Tests.Framework.Controllers
{
    [TestFixture]
    public class AdminAuthorizeAttributeTests
    {
        [Test]
        public void Admin_authorize_attribute_exists()
        {
            // Basic test verifying the attribute type exists
            var attrType = typeof(Nop.Web.Framework.Controllers.AdminAuthorizeAttribute);
            Assert.IsNotNull(attrType);
        }

        [Test]
        public void Admin_authorize_attribute_is_an_attribute()
        {
            Assert.IsTrue(typeof(System.Attribute).IsAssignableFrom(
                typeof(Nop.Web.Framework.Controllers.AdminAuthorizeAttribute)));
        }
    }
}
