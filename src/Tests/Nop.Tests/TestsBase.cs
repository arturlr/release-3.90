using System.Security.Principal;
using NUnit.Framework;
using Moq;

namespace Nop.Tests
{
    public abstract class TestsBase
    {
        [SetUp]
        public virtual void SetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
        }

        protected static IPrincipal CreatePrincipal(string name, params string[] roles)
        {
            return new GenericPrincipal(new GenericIdentity(name, "TestIdentity"), roles);
        }
    }
}
