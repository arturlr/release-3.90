using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    [TestFixture]
    public abstract class RoutesTestsBase
    {
        [SetUp]
        public void Setup()
        {
            //TODO: Routing tests need to be rewritten for ASP.NET Core endpoint routing.
            //The old System.Web.Routing.RouteTable is not available in ASP.NET Core.
            //These tests should use Microsoft.AspNetCore.TestHost or 
            //Microsoft.AspNetCore.Routing testing utilities.
        }

        [TearDown]
        public void TearDown()
        {
        }
    }
}
