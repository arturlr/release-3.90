using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    [TestFixture]
    public abstract class RoutesTestsBase
    {
        [SetUp]
        public void Setup()
        {
            // ASP.NET Core routing setup would go here
            // The MVC 5 RouteTable approach is not applicable
        }

        [TearDown]
        public void TearDown()
        {
        }
    }
}
