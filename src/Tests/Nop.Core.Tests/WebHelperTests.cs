using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Nop.Core.Configuration;
using Nop.Tests;
using NUnit.Framework;
using NSubstitute;

namespace Nop.Core.Tests
{
    [TestFixture]
    public class WebHelperTests
    {
        private IWebHelper _webHelper;

        [Test]
        public void Can_create_webhelper()
        {
            var httpContext = new DefaultHttpContext();
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var hostEnv = Substitute.For<IHostEnvironment>();
            var nopConfig = new NopConfig();
            _webHelper = new WebHelper(accessor, hostEnv, nopConfig);
            _webHelper.ShouldNotBeNull();
        }

        [Test]
        public void WebHelper_can_be_instantiated()
        {
            var httpContext = new DefaultHttpContext();
            var accessor = new HttpContextAccessor { HttpContext = httpContext };
            var hostEnv = Substitute.For<IHostEnvironment>();
            var nopConfig = new NopConfig();
            _webHelper = new WebHelper(accessor, hostEnv, nopConfig);
            _webHelper.ShouldNotBeNull();
        }
    }
}
