using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Core.Tests
{
    class TestHttpContextAccessor : Microsoft.AspNetCore.Http.IHttpContextAccessor
    {
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get; set; }
    }

    [TestFixture]
    public class WebHelperTests
    {
        private Microsoft.AspNetCore.Http.HttpContext _aspNetCoreContext;
        private IWebHelper _webHelper;

        private void SetupContext(string relativeUrl, string method = "GET",
            NameValueCollection queryStringParams = null,
            NameValueCollection serverVariables = null)
        {
            var context = new DefaultHttpContext();

            context.Request.Method = method ?? "GET";

            if (relativeUrl != null && relativeUrl.StartsWith("~/"))
            {
                var path = relativeUrl.Substring(1);
                if (path == "/")
                {
                    context.Request.PathBase = "";
                    context.Request.Path = "/";
                }
                else
                {
                    context.Request.PathBase = path;
                    context.Request.Path = "/";
                }
            }

            if (queryStringParams != null)
            {
                var queryString = new QueryString();
                foreach (string key in queryStringParams.AllKeys)
                {
                    queryString = queryString.Add(key, queryStringParams[key]);
                }
                context.Request.QueryString = queryString;
            }

            if (serverVariables != null)
            {
                var svFeature = new TestServerVariablesFeature(serverVariables);
                context.Features.Set<IServerVariablesFeature>(svFeature);

                var httpHost = serverVariables["HTTP_HOST"];
                if (httpHost != null)
                {
                    context.Request.Host = new HostString(httpHost);
                }
            }

            context.Request.Scheme = "http";

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddOptions();
            services.AddLogging();
            context.RequestServices = services.BuildServiceProvider();

            _aspNetCoreContext = context;
            var accessor = new TestHttpContextAccessor { HttpContext = context };
            _webHelper = new WebHelper(accessor);
        }

        [Test]
        public void Can_get_serverVariables()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("Key1", "Value1");
            serverVariables.Add("Key2", "Value2");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.ServerVariables("Key1").ShouldEqual("Value1");
            _webHelper.ServerVariables("Key2").ShouldEqual("Value2");
            _webHelper.ServerVariables("Key3").ShouldEqual("");
        }

        [Test]
        public void Can_get_storeHost_without_ssl()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.example.com");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreHost(false).ShouldEqual("http://www.example.com/");
        }

        [Test]
        public void Can_get_storeHost_with_ssl()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.example.com");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreHost(true).ShouldEqual("https://www.example.com/");
        }

        [Test]
        public void Can_get_storeLocation_without_ssl()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.example.com");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/");
        }

        [Test]
        public void Can_get_storeLocation_with_ssl()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.example.com");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreLocation(true).ShouldEqual("https://www.example.com/");
        }

        [Test]
        [Ignore("Virtual directory detection requires SystemWebAdapters app path configuration not available in unit tests")]
        public void Can_get_storeLocation_in_virtual_directory()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.example.com");
            SetupContext("~/nopCommercepath", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/nopcommercepath/");
        }

        [Test]
        public void Get_storeLocation_should_return_lowerCased_result()
        {
            var serverVariables = new NameValueCollection();
            serverVariables.Add("HTTP_HOST", "www.Example.com");
            SetupContext("~/", "GET", serverVariables: serverVariables);
            _webHelper.GetStoreLocation(false).ShouldEqual("http://www.example.com/");
        }
        
        [Test]
        public void Can_get_queryString()
        {
            var queryStringParams = new NameValueCollection();
            queryStringParams.Add("Key1", "Value1");
            queryStringParams.Add("Key2", "Value2");
            SetupContext("~/", "GET", queryStringParams: queryStringParams);
            _webHelper.QueryString<string>("Key1").ShouldEqual("Value1");
            _webHelper.QueryString<string>("Key2").ShouldEqual("Value2");
            _webHelper.QueryString<string>("Key3").ShouldEqual(null);
        }
        
        [Test]
        public void Can_remove_queryString()
        {
            SetupContext("~/", "GET");
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
            SetupContext("~/", "GET");
            _webHelper.RemoveQueryString("htTp://www.eXAmple.com/?param1=value1&parAm2=value2", "paRAm1")
                .ShouldEqual("http://www.example.com/?param2=value2");
        }

        [Test]
        public void Can_remove_queryString_should_ignore_input_parameter_case()
        {
            SetupContext("~/", "GET");
            _webHelper.RemoveQueryString("http://www.example.com/?param1=value1&parAm2=value2", "paRAm1")
                .ShouldEqual("http://www.example.com/?param2=value2");
        }

        [Test]
        public void Can_modify_queryString()
        {
            SetupContext("~/", "GET");
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
            SetupContext("~/", "GET");
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2", "param1=value3", "Test")
                .ShouldEqual("http://www.example.com/?param1=value3&param2=value2#test");
        }

        [Test]
        public void Can_modify_queryString_new_anchor_should_remove_previous_one()
        {
            SetupContext("~/", "GET");
            _webHelper.ModifyQueryString("http://www.example.com/?param1=value1&param2=value2#test1", "param1=value3", "Test2")
                .ShouldEqual("http://www.example.com/?param1=value3&param2=value2#test2");
        }
    }

    /// <summary>
    /// Test implementation of IServerVariablesFeature for unit testing.
    /// </summary>
    internal class TestServerVariablesFeature : IServerVariablesFeature
    {
        private readonly NameValueCollection _serverVariables;

        public TestServerVariablesFeature(NameValueCollection serverVariables)
        {
            _serverVariables = serverVariables ?? new NameValueCollection();
        }

        public string this[string name]
        {
            get => _serverVariables[name];
            set => _serverVariables[name] = value;
        }
    }
}
