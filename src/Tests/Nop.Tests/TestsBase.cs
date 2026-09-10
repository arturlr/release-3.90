using System.Security.Principal;
using NUnit.Framework;

namespace Nop.Tests
{
    public abstract class TestsBase
    {
        //Rhino Mocks (RhinoMocks 3.6.1) has no net10.0-compatible release and is
        //unmaintained, so it was dropped by migration task 4.5 rather than swapped for a
        //different mocking library.
        //
        //This class previously held:
        //    protected MockRepository mocks;
        //assigned "mocks = new MockRepository()" in SetUp and calling
        //"mocks.ReplayAll(); mocks.VerifyAll();" in TearDown.
        //
        //That was dead code. No test in the repository ever created a mock through that
        //instance: the fixtures that do mock (in Nop.Services.Tests) use the static
        //factory MockRepository.GenerateMock<T>(), which does not register with any
        //MockRepository instance. ReplayAll()/VerifyAll() therefore always ran against an
        //empty repository, so removing them changes no test outcome.
        //
        //SetUp/TearDown are kept as virtual no-op hooks because derived fixtures override
        //them and call base (for example Nop.Core.Tests.TypeFindingBase.SetUp).

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
