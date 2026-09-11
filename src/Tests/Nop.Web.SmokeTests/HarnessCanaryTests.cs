using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Deliberately-failing tests whose only purpose is to prove this harness can fail.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A green smoke run is worthless if nothing was ever shown to make it red. This migration has
    /// already been bitten twice by exactly that: the Six Labors licence task that failed on every
    /// compile while MSBuild reported only warnings (<c>ContinueOnError=true</c>), and Roslyn's
    /// refusal to bind method bodies while declaration errors existed, which hid a real
    /// <c>CS1929</c> in <c>FilePermissionHelper</c> across three completed tasks. So each of the
    /// three mechanisms this suite depends on has a canary that travels the same code path and
    /// asserts something known to be false:
    /// </para>
    /// <list type="number">
    /// <item>a real HTTP request through TestServer</item>
    /// <item>a resolve through the real Autofac container</item>
    /// <item>the <c>/__smoke/*</c> probe middleware's text output</item>
    /// </list>
    /// <para>
    /// The fixture is <see cref="ExplicitAttribute"/>, so an ordinary <c>dotnet test</c> reports it
    /// as skipped and CI stays green. Run it on demand to re-confirm the harness:
    /// </para>
    /// <code>dotnet test src/Tests/Nop.Web.SmokeTests --filter "FullyQualifiedName~HarnessCanaryTests"</code>
    /// <para>
    /// <b>All three MUST report Failed.</b> If any of them passes, or is silently skipped when
    /// selected explicitly, the corresponding group of real assertions cannot be trusted.
    /// Task 7.7 ran this and observed 3 failed / 0 passed.
    /// </para>
    /// </remarks>
    [TestFixture]
    [Explicit("Deliberately failing. Run explicitly to prove the harness reports failures.")]
    [Category("Canary")]
    public class HarnessCanaryTests
    {
        private NopWebApplicationFactory _factory;
        private HttpClient _client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _factory = new NopWebApplicationFactory();
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_client != null)
                _client.Dispose();
            if (_factory != null)
                _factory.Dispose();
        }

        [Test]
        public void CANARY_http_assertions_can_fail()
        {
            //The install probe endpoint answers 200. Asserting 418 must be reported as a failure.
            var response = _client.GetAsync(SmokeProbeMiddleware.Prefix + "ok").Result;
            Assert.AreEqual((HttpStatusCode)418, response.StatusCode,
                "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_container_assertions_can_fail()
        {
            //IWebHelper resolves. Asserting it does not must be reported as a failure.
            var webHelper = EngineContext.Current.Resolve<IWebHelper>();
            Assert.IsNull(webHelper, "CANARY: this assertion is meant to fail.");
        }

        [Test]
        public void CANARY_probe_middleware_assertions_can_fail()
        {
            //The scope probe never emits this token. Asserting it does must be reported as a failure.
            var body = _client.GetStringAsync(SmokeProbeMiddleware.Prefix + "scope").Result;
            StringAssert.Contains("thisTokenIsNeverEmitted=True", body,
                "CANARY: this assertion is meant to fail.");
        }
    }
}
