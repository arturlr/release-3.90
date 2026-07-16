using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Nop.Tests;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Nop.Core.Tests
{
    [TestFixture]
    public class WebHelperTests
    {
        private IWebHelper _webHelper;
        private Mock<IHttpContextAccessor> _httpContextAccessor;
        private Mock<IHostApplicationLifetime> _hostApplicationLifetime;
        private Mock<IConfiguration> _configuration;

        private void SetupWebHelper(HttpContext httpContext)
        {
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
            _hostApplicationLifetime = new Mock<IHostApplicationLifetime>();
            _configuration = new Mock<IConfiguration>();
            _webHelper = new WebHelper(_httpContextAccessor.Object, _hostApplicationLifetime.Object, _configuration.Object);
        }

        [Test]
        public void Can_get_serverVariables()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Key1"] = "Value1";
            context.Request.Headers["Key2"] = "Value2";
            SetupWebHelper(context);
            _webHelper.ServerVariables("HTTP_KEY1").ShouldEqual("Value1");
            _webHelper.ServerVariables("HTTP_KEY2").ShouldEqual("Value2");
            _webHelper.ServerVariables("HTTP_KEY3").ShouldEqual("");
        }

        [Test]
        public void Can_get_storeHost_without_ssl()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.example.com");
            context.Request.Scheme = "http";
            SetupWebHelper(context);
            _webHelper.GetStoreHost(false).ShouldEqual("http://www.example.com/");
        }

        [Test]
        public void Can_get_storeHost_with_ssl()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.example.com");
            context.Request.Scheme = "http";
            SetupWebHelper(context);
            _webHelper.GetStoreHost(true).ShouldEqual("https://www.example.com/");
        }

        [Test]
        public void Can_get_storeLocation_without_ssl()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.example.com");
            context.Request.Scheme = "http";
            context.Request.PathBase = "";
            SetupWebHelper(context);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/");
        }

        [Test]
        public void Can_get_storeLocation_with_ssl()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.example.com");
            context.Request.Scheme = "http";
            context.Request.PathBase = "";
            SetupWebHelper(context);
            _webHelper.GetStoreLocation(true).ShouldEqual("https://www.example.com/");
        }

        [Test]
        public void Can_get_storeLocation_in_virtual_directory()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.example.com");
            context.Request.Scheme = "http";
            context.Request.PathBase = "/nopCommercepath";
            SetupWebHelper(context);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/nopcommercepath/");
        }

        [Test]
        public void Get_storeLocation_should_return_lowerCased_result()
        {
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("www.Example.com");
            context.Request.Scheme = "http";
            context.Request.PathBase = "";
            SetupWebHelper(context);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/");
        }
        
        [Test]
        public void Can_get_queryString()
        {
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?Key1=Value1&Key2=Value2");
            SetupWebHelper(context);
            _webHelper.QueryString<string>("Key1").ShouldEqual("Value1");
            _webHelper.QueryString<string>("Key2").ShouldEqual("Value2");
            _webHelper.QueryString<string>("Key3").ShouldEqual(null);
        }
        
        [Test]
        public void Can_remove_queryString()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            //first param (?)
            _webHelper.RemoveQueryString("http://www.example.com/?param1=value1&param2=value2", "param1")
                .ShouldEqual("http://www.example.com/?param2=value2");
            //second param (&)
            _webHelper.RemoveQueryString("http://www.example.com/?param1=value1&param2=value2", "param2")
                .ShouldEqual("http://www.example.com/?param1=value1");
            //non-existing param
            _webHelper.RemoveQueryString("http://www.example.com/?param1=value1&param2=value2", "param3")
                .ShouldEqual("http://www.example.com/?param1=value1&param2=value2");
        }

        [Test]
        public void Can_remove_queryString_should_return_lowerCased_result()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            _webHelper.RemoveQueryString("htTp://www.eXAmple.com/?param1=value1&parAm2=value2", "paRAm1")
                .ShouldEqual("http://www.example.com/?param2=value2");
        }

        [Test]
        public void Can_remove_queryString_should_ignore_input_parameter_case()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            _webHelper.RemoveQueryString("http://www.example.com/?param1=value1&parAm2=value2", "paRAm1")
                .ShouldEqual("http://www.example.com/?param2=value2");
        }

        [Test]
        public void Can_modify_queryString()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            //first param (?)
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2", "param1=value3", null)
                .ShouldEqual("http://www.example.com/?param1=value3&param2=value2");
            //second param (&)
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2", "param2=value3", null)
                .ShouldEqual("http://www.example.com/?param1=value1&param2=value3");
            //non-existing param
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2", "param3=value3", null)
                .ShouldEqual("http://www.example.com/?param1=value1&param2=value2&param3=value3");
        }

        [Test]
        public void Can_modify_queryString_with_anchor()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2", "param1=value3", "Test")
                .ShouldEqual("http://www.example.com/?param1=value3&param2=value2#test");
        }

        [Test]
        public void Can_modify_queryString_new_anchor_should_remove_previous_one()
        {
            var context = new DefaultHttpContext();
            SetupWebHelper(context);
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2#test1", "param1=value3", "Test2")
                .ShouldEqual("http://www.example.com/?param1=value3&param2=value2#test2");
        }
    }
}
