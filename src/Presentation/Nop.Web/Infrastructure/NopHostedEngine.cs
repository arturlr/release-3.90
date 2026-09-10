using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// The <see cref="IEngine"/> the ASP.NET Core host drives (task 7.2, Requirement 4.5).
    /// It is a <see cref="NopEngine"/> that registers into the <b>host's</b> Autofac
    /// <see cref="ContainerBuilder"/> instead of creating and building one of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this type has to exist.</b> <c>runtime-deferrals.md</c> §17.5 records the intended
    /// integration precisely:
    /// <i>"<c>AutofacServiceProviderFactory.CreateBuilder(services)</c> runs
    /// <c>Populate(services)</c> first, then the <c>ConfigureContainer</c> callbacks (which is
    /// where <c>NopEngine</c> runs the <c>IDependencyRegistrar</c>s), then <c>Build()</c> — which
    /// fires the callback."</i> But <see cref="NopEngine.Initialize"/> as written constructs its
    /// own <see cref="ContainerBuilder"/> and calls <c>Build()</c> on it, and §17.4b forbids
    /// changing <c>IEngine</c>/<c>NopEngine</c>/<c>ContainerManager</c> — all three are in
    /// <c>Nop.Core</c>, which passed its clean-compile gate (2.5) and is committed.
    /// </para>
    /// <para>
    /// Calling <c>EngineContext.Initialize(false)</c> from the host would therefore produce
    /// <b>two containers</b>: nopCommerce's, and the host's Autofac container built by
    /// <c>AutofacServiceProviderFactory</c>. That is not a tidiness problem, it is a correctness
    /// one, and it breaks two things silently:
    /// <list type="number">
    /// <item>Every <c>SingleInstance()</c> registration would exist twice — two
    /// <c>MemoryCacheManager</c>s, two settings caches, two <c>IWorkContext</c> graphs.</item>
    /// <item><b>Runtime deferral 1.3 would regress into something worse than it was.</b>
    /// <c>DependencyRegistrar</c> sets
    /// <c>ContainerManager.CurrentScopeProvider</c> to
    /// <c>() =&gt; HttpContext.RequestServices.GetService&lt;ILifetimeScope&gt;()</c>, i.e. a scope
    /// of the <b>host's</b> container, while <c>ContainerManager.Container</c> would be
    /// nopCommerce's. Every <c>EngineContext.Current.Resolve&lt;T&gt;()</c> during a request would
    /// then resolve out of a container that has no nopCommerce registrations in it.</item>
    /// </list>
    /// Subclassing here — in <c>Nop.Web</c>, where the host lives — keeps <c>Nop.Core</c>
    /// untouched and yields exactly one container.
    /// </para>
    /// <para>
    /// <b>Two-phase, because container construction is split across the host's lifecycle.</b>
    /// <list type="bullet">
    /// <item><see cref="RegisterInto"/> — called from
    /// <c>builder.Host.ConfigureContainer&lt;ContainerBuilder&gt;(...)</c>. Registers everything
    /// and does NOT build.</item>
    /// <item><see cref="RunStartupTasks(NopConfig)"/> — called from <c>Program.cs</c> after
    /// <c>builder.Build()</c>, once the container exists. Startup tasks resolve services
    /// (<c>EfStartUpTask</c> resolves <c>DataSettings</c> and <c>IDataProvider</c>), so they
    /// cannot run before the container is built.</item>
    /// </list>
    /// <see cref="ContainerManager"/> is populated in between by a build callback registered in
    /// <see cref="RegisterInto"/>.
    /// </para>
    /// <para>
    /// <b>3.90 parity note.</b> <c>Global.asax.cs</c>'s <c>Application_Start</c> called
    /// <c>EngineContext.Initialize(false)</c>, which ran registration, AutoMapper configuration
    /// and startup tasks in one statement. The same three things happen here in the same order,
    /// just split across the host's build boundary. <c>Program.cs</c> installs this instance with
    /// <c>EngineContext.Replace(...)</c> before anything can touch <c>EngineContext.Current</c>,
    /// so the base <c>EngineContext.Initialize</c> path — which would build a second container —
    /// is never reached.
    /// </para>
    /// </remarks>
    public class NopHostedEngine : NopEngine
    {
        #region Fields

        private ContainerManager _containerManager;
        private ContainerBuilder _hostContainerBuilder;

        #endregion

        #region Properties

        /// <summary>
        /// Container manager over the <b>host's</b> container. Null until the host has built the
        /// service provider — see the class remarks.
        /// </summary>
        public override ContainerManager ContainerManager
        {
            get { return _containerManager; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Phase 1. Contributes every nopCommerce registration to the host's container builder,
        /// exactly as <c>NopEngine.RegisterDependencies</c> + <c>RegisterMapperConfiguration</c>
        /// would, but without building the container.
        /// </summary>
        /// <param name="builder">
        /// The host's builder, supplied by
        /// <c>builder.Host.ConfigureContainer&lt;ContainerBuilder&gt;(...)</c>. It has already had
        /// <c>Populate(services)</c> applied, so everything <c>AddNopFramework()</c> registered —
        /// <c>IHttpContextAccessor</c>, <c>IMemoryCache</c>, <c>IFileVersionProvider</c>,
        /// <c>IAntiforgery</c>, <c>IHostApplicationLifetime</c>, the MVC service graph — is
        /// already visible to the <c>IDependencyRegistrar</c>s and to the build callbacks they
        /// register.
        /// </param>
        /// <param name="config">Bound <see cref="NopConfig"/></param>
        public virtual void RegisterInto(ContainerBuilder builder, NopConfig config)
        {
            if (builder == null)
                throw new ArgumentNullException("builder");
            if (config == null)
                throw new ArgumentNullException("config");

            _hostContainerBuilder = builder;

            //capture the container the host builds. Autofac hands the root ILifetimeScope -
            //which IS the IContainer - to build callbacks.
            builder.RegisterBuildCallback(scope =>
            {
                var container = scope as IContainer;
                if (container == null)
                    throw new NopException(
                        "The Autofac build callback did not supply an IContainer, so ContainerManager " +
                        "cannot be created and EngineContext.Current.Resolve<T>() would fail.");

                _containerManager = new ContainerManager(container);
            });

            RegisterDependencies(config);
            RegisterMapperConfiguration(config);
        }

        /// <summary>
        /// Phase 2. Runs <see cref="IStartupTask"/>s, honouring
        /// <see cref="NopConfig.IgnoreStartupTasks"/> exactly as <c>NopEngine.Initialize</c> does.
        /// Call after the host has built its service provider.
        /// </summary>
        /// <param name="config">Bound <see cref="NopConfig"/></param>
        public virtual void RunStartupTasks(NopConfig config)
        {
            if (_containerManager == null)
                throw new NopException(
                    "The container has not been built yet. Call RunStartupTasks(NopConfig) after " +
                    "WebApplicationBuilder.Build().");

            if (config != null && config.IgnoreStartupTasks)
                return;

            RunStartupTasks();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Same body as <c>NopEngine.RegisterDependencies</c> minus the two statements that create
        /// and build a private container.
        /// </summary>
        protected override void RegisterDependencies(NopConfig config)
        {
            var builder = _hostContainerBuilder;

            //dependencies
            var typeFinder = new WebAppTypeFinder();
            builder.RegisterInstance(config).As<NopConfig>().SingleInstance();
            builder.RegisterInstance(this).As<IEngine>().SingleInstance();
            builder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();

            //register dependencies provided by other assemblies - this is the scan that finds
            //Nop.Web.Framework's DependencyRegistrar (Order 0), Nop.Web's own
            //Infrastructure/DependencyRegistrar.cs (Order 2) and every installed plugin's.
            //Plugin assemblies are only visible here because PluginManager.Initialize() has
            //already run (runtime deferral 1.1).
            var drTypes = typeFinder.FindClassesOfType<IDependencyRegistrar>();
            var drInstances = new List<IDependencyRegistrar>();
            foreach (var drType in drTypes)
                drInstances.Add((IDependencyRegistrar)Activator.CreateInstance(drType));
            //sort
            drInstances = drInstances.AsQueryable().OrderBy(t => t.Order).ToList();
            foreach (var dependencyRegistrar in drInstances)
                dependencyRegistrar.Register(builder, typeFinder, config);

            //NO builder.Build() - the host owns that, via AutofacServiceProviderFactory.
        }

        /// <summary>
        /// Same body as <c>NopEngine.RunStartupTasks</c>, re-implemented because the base version
        /// reads <c>NopEngine</c>'s private container-manager field, which this subclass
        /// deliberately leaves null.
        /// </summary>
        protected override void RunStartupTasks()
        {
            var typeFinder = _containerManager.Resolve<ITypeFinder>();
            var startUpTaskTypes = typeFinder.FindClassesOfType<IStartupTask>();
            var startUpTasks = new List<IStartupTask>();
            foreach (var startUpTaskType in startUpTaskTypes)
                startUpTasks.Add((IStartupTask)Activator.CreateInstance(startUpTaskType));
            //sort
            startUpTasks = startUpTasks.AsQueryable().OrderBy(st => st.Order).ToList();
            foreach (var startUpTask in startUpTasks)
                startUpTask.Execute();
        }

        #endregion
    }
}
