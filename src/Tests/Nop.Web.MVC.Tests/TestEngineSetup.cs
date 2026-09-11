using System;
using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Domain.Localization;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Services.Localization;
using NSubstitute;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests
{
    /// <summary>
    /// Task 17.1 — assembly-level test harness.
    ///
    /// WHY THIS EXISTS
    /// ===============
    /// Almost every validator in Nop.Web / Nop.Admin decorates its model properties with
    /// [NopResourceDisplayName]. FluentValidation resolves that display name during validation
    /// (both for ShouldNotHaveValidationErrorFor and for the message of a failing rule), and
    /// Nop.Web.Framework.NopResourceDisplayName.DisplayName does:
    ///
    ///     EngineContext.Current.Resolve&lt;IWorkContext&gt;().WorkingLanguage.Id
    ///     EngineContext.Current.Resolve&lt;ILocalizationService&gt;().GetResource(key, langId, true, key)
    ///
    /// i.e. it reaches through the STATIC EngineContext, not through the ILocalizationService the
    /// validator base classes inject. Under MVC 5 / 3.90 these fixtures ran inside a process where
    /// EngineContext.Current was a live NopEngine, so DisplayName resolved. In a bare net10.0 unit
    /// test run EngineContext.Current lazily initialises a REAL NopEngine
    /// (EngineContext.Initialize(false)) whose container has no registration usable without a host,
    /// so the first DisplayName access throws Autofac's DependencyResolutionException — which is
    /// exactly what a first (un-harnessed) run produced: 114 validator/mapper failures, all the same
    /// exception, none of them a defect in the code under test or in the RhinoMocks→NSubstitute port.
    ///
    /// This is a harness gap, not a stale test expectation: the validators' assertions are correct
    /// and unchanged; what was missing is the ambient EngineContext the display-name attribute has
    /// always assumed. This [SetUpFixture] restores it ONCE for the whole assembly by installing a
    /// minimal fake IEngine (via EngineContext.Replace) that resolves only the two services
    /// NopResourceDisplayName needs, both as NSubstitute doubles:
    ///   * IWorkContext.WorkingLanguage -> a Language with Id 1 (any positive id; GetResource below
    ///     ignores it).
    ///   * ILocalizationService.GetResource(...) -> returns the resource key unchanged, which is the
    ///     documented "not found" fallback (NopResourceDisplayName passes ResourceKey as defaultValue),
    ///     so display names are stable, non-null strings and never hit a database.
    ///
    /// It deliberately does NOT stand up a real container: the tests under this assembly assert
    /// validation behaviour, not localisation, and a real engine is both unavailable without a host
    /// and unnecessary. EventsTests builds its own local NopEngine and reads EngineContext not at
    /// all, so it is unaffected by this replacement.
    /// </summary>
    [SetUpFixture]
    public class TestEngineSetup
    {
        private IEngine _previous;

        [OneTimeSetUp]
        public void InstallFakeEngine()
        {
            _previous = EngineContext.Current;

            var workContext = Substitute.For<IWorkContext>();
            workContext.WorkingLanguage.Returns(new Language { Id = 1 });

            var localizationService = Substitute.For<ILocalizationService>();
            //Return the requested key unchanged — the same value NopResourceDisplayName passes as
            //its own default — so display-name resolution yields a stable non-null string.
            localizationService
                .GetResource(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<string>())
                .Returns(ci => ci.ArgAt<string>(0));
            localizationService.GetResource(Arg.Any<string>()).Returns(ci => ci.ArgAt<string>(0));

            EngineContext.Replace(new TestEngine(workContext, localizationService));
        }

        [OneTimeTearDown]
        public void RestoreEngine()
        {
            EngineContext.Replace(_previous);
        }

        /// <summary>
        /// Minimal IEngine that satisfies what NopResourceDisplayName resolves (IWorkContext,
        /// ILocalizationService) and returns null for everything else.
        ///
        /// Returning null — rather than throwing — for an unregistered service is deliberate and is
        /// what keeps EventsTests green. EventsTests builds its OWN real NopEngine and calls
        /// Initialize(NopConfig), whose RunStartupTasks runs Nop.Data.EfStartUpTask; that task reads
        /// the STATIC EngineContext.Current (this fake), not the local engine, and resolves
        /// DataSettings. The real lazily-initialised engine returned a null/invalid DataSettings, so
        /// EfStartUpTask early-returned and did nothing; a benign null here reproduces exactly that,
        /// so EventsTests behaves as it did before this harness existed. An earlier throw-on-unknown
        /// version broke EventsTests for this precise reason (DataSettings was demanded by the
        /// startup task), which is why the fallback is null.
        /// </summary>
        private sealed class TestEngine : IEngine
        {
            private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

            public TestEngine(IWorkContext workContext, ILocalizationService localizationService)
            {
                _services[typeof(IWorkContext)] = workContext;
                _services[typeof(ILocalizationService)] = localizationService;
            }

            public ContainerManager ContainerManager
            {
                get { throw new NotSupportedException("TestEngine exposes no ContainerManager."); }
            }

            public void Initialize(NopConfig config)
            {
                //no-op — the fake engine is pre-populated in the constructor
            }

            public T Resolve<T>() where T : class
            {
                return (T)Resolve(typeof(T));
            }

            public object Resolve(Type type)
            {
                if (_services.TryGetValue(type, out var service))
                    return service;
                //null for the unregistered rest — see the class remarks (EfStartUpTask / EventsTests).
                return null;
            }

            public T[] ResolveAll<T>()
            {
                return new T[0];
            }
        }
    }
}
