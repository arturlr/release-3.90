using System;
using Nop.Web.Controllers;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    [TestFixture]
    public class RoutesTests : RoutesTestsBase
    {
        //TODO: All route tests need to be rewritten for ASP.NET Core endpoint routing.
        //The ShouldMapTo extension method relied on System.Web.Routing which is not available.
        //These tests should use Microsoft.AspNetCore.TestHost WebApplicationFactory-based testing.

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Default_route()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
            //"~/".ShouldMapTo<HomeController>(c => c.Index());
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Blog_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Boards_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Catalog_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Customer_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Profile_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Cart_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Checkout_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Order_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void ReturnRequest_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Common_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void Newsletter_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void PrivateMessages_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }

        [Test]
        [Ignore("Route tests need migration to ASP.NET Core endpoint routing")]
        public void News_routes()
        {
            //TODO: Rewrite using ASP.NET Core TestServer
        }
    }
}
