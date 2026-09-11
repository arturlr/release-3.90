using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Contributes nopCommerce assemblies that are not compile-time references of the host as
    /// MVC <see cref="ApplicationPart"/>s, so their controllers, view components, tag helpers
    /// and <b>compiled Razor views</b> are discoverable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists — task 8.2, the load-bearing unknown.</b> ASP.NET Core discovers
    /// controllers and compiled views from the <see cref="ApplicationPartManager"/>, which
    /// <c>AddControllersWithViews()</c> seeds from the <b>entry assembly's</b>
    /// <c>DependencyContext</c> — i.e. from <c>Nop.Web.deps.json</c>. Only compile-time
    /// references appear there. nopCommerce has two kinds of assembly that are deliberately
    /// <b>not</b> compile-time references of <c>Nop.Web</c>:
    /// </para>
    /// <list type="number">
    /// <item><b>plugins</b> — shadow-copied at runtime by <see cref="PluginManager"/> and not
    /// known at build time at all (runtime deferral 1.2);</item>
    /// <item><b><c>Nop.Admin</c></b> — a deliberate <i>sibling</i> of <c>Nop.Web</c>: design §6
    /// records that neither project references the other, and that in 3.90 the relationship was
    /// a build/deploy one (<c>Nop.Admin</c>'s <c>OutputPath</c> was <c>..\bin\</c>, so
    /// <c>Nop.Admin.dll</c> landed in <c>Nop.Web\bin</c> and <c>System.Web</c>'s
    /// <c>BuildManager</c> loaded every assembly in <c>bin</c>). <b>.NET has no equivalent of
    /// that implicit <c>bin</c> load and no equivalent of <c>AreaRegistration.RegisterAllAreas()</c>,
    /// so an explicit host-side statement is unavoidable.</b> This class is that statement.</item>
    /// </list>
    /// <para>
    /// <b>There is exactly ONE assembly-loading mechanism in the process, and it is not new.</b>
    /// <c>Nop.Core</c>'s <see cref="WebAppTypeFinder"/> already loads every assembly in
    /// <c>AppDomain.CurrentDomain.BaseDirectory</c> whose name passes its
    /// <c>AssemblySkipLoadingPattern</c>/<c>AssemblyRestrictToLoadingPattern</c> filter
    /// (<c>WebAppTypeFinder.GetAssemblies</c> → <c>LoadMatchingAssemblies</c>), and
    /// <c>NopHostedEngine</c> already constructs one and calls <c>GetAssemblies()</c> during
    /// container registration — which is how a sibling's <c>IDependencyRegistrar</c>,
    /// <c>IRouteProvider</c>, <c>IStartupTask</c> and AutoMapper profile are found today.
    /// <see cref="AddNopDiscoveredApplicationParts"/> reuses <i>that</i> assembly set rather
    /// than inventing a second discovery rule, so "what nopCommerce considers its own" is
    /// defined in one place.
    /// </para>
    /// <para>
    /// <b>The remaining half is build/deploy, not code.</b> The assembly must physically be in
    /// the host's base directory. <c>Nop.Admin.csproj</c> reproduces 3.90's drop with a
    /// post-build copy into <c>Nop.Web</c>'s output directory — directional
    /// (<c>Nop.Admin</c> → <c>Nop.Web</c>, exactly as 3.90's <c>OutputPath</c> was), so it adds
    /// no compile-time reference, no MSBuild build-order coupling, and cannot make
    /// <c>Nop.Web</c> fail to build. A <c>ProjectReference</c> was considered and rejected: it
    /// would convert design §6's build/deploy relationship into a compile-time one, couple the
    /// 7.6 gate to the 8.8 gate, and stop <c>Nop.Web</c> building on its own.
    /// </para>
    /// <para>
    /// <b>Compiled Razor views come along, and that is why <see cref="ApplicationPartFactory"/>
    /// is used rather than <c>new AssemblyPart(assembly)</c>.</b> A project built with the Razor
    /// SDK carries <c>[ProvideApplicationPartFactory]</c> naming
    /// <c>ConsolidatedAssemblyApplicationPartFactory</c>, which yields both the
    /// <see cref="AssemblyPart"/> (controllers) and the Razor part (compiled views). Adding a
    /// bare <see cref="AssemblyPart"/> would register the controllers and silently leave every
    /// view unresolvable — the failure mode runtime deferral 8.1-4 is about.
    /// </para>
    /// </remarks>
    public static class NopApplicationPartExtensions
    {
        /// <summary>
        /// Adds an application part for each assembly, honouring any
        /// <c>[ProvideApplicationPartFactory]</c> the assembly declares, and skipping
        /// assemblies that are already represented.
        /// </summary>
        /// <param name="partManager">Application part manager</param>
        /// <param name="assemblies">Assemblies to contribute; nulls are ignored</param>
        public static void AddNopApplicationParts(ApplicationPartManager partManager,
            IEnumerable<Assembly> assemblies)
        {
            if (partManager == null || assemblies == null)
                return;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in partManager.ApplicationParts)
                seen.Add(part.Name);

            foreach (var assembly in assemblies)
            {
                if (assembly == null || assembly.IsDynamic)
                    continue;

                var name = assembly.GetName().Name;
                if (string.IsNullOrEmpty(name) || !seen.Add(name))
                    continue;

                //ApplicationPartFactory, not `new AssemblyPart(...)` - see the remarks above.
                //A malformed or non-MVC assembly must not take the host down at startup, and a
                //part that cannot be produced simply means "nothing to contribute".
                IEnumerable<ApplicationPart> parts;
                try
                {
                    parts = ApplicationPartFactory.GetApplicationPartFactory(assembly)
                        .GetApplicationParts(assembly);
                }
                catch
                {
                    continue;
                }

                foreach (var part in parts)
                {
                    if (part == null)
                        continue;

                    partManager.ApplicationParts.Add(part);
                }
            }
        }

        /// <summary>
        /// Contributes every assembly nopCommerce already considers its own — the set
        /// <see cref="WebAppTypeFinder"/> loads and scans — as an application part.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is what makes <c>Nop.Admin</c>'s controllers routable and its compiled views
        /// resolvable without a <c>ProjectReference</c> from <c>Nop.Web</c> (task 8.2).
        /// </para>
        /// <para>
        /// The type finder's assembly set is used deliberately, so there is one definition of
        /// "a nopCommerce assembly" rather than two. The cost is that the three libraries
        /// (<c>Nop.Core</c>, <c>Nop.Data</c>, <c>Nop.Services</c>) also become parts and are
        /// scanned once for controllers/view components/tag helpers, of which they contain
        /// none. That is a one-off startup reflection cost over types MVC would otherwise not
        /// look at, and it is the same cost the plugin path already pays.
        /// </para>
        /// <para>
        /// It does <b>not</b> add a load that was not already happening:
        /// <c>NopHostedEngine.RegisterInto</c> constructs a <see cref="WebAppTypeFinder"/> and
        /// calls <c>GetAssemblies()</c> during container registration regardless, so the
        /// base-directory probe runs at startup either way.
        /// </para>
        /// </remarks>
        /// <param name="partManager">Application part manager</param>
        public static void AddNopDiscoveredApplicationParts(ApplicationPartManager partManager)
        {
            if (partManager == null)
                return;

            IList<Assembly> assemblies;
            try
            {
                assemblies = new WebAppTypeFinder().GetAssemblies();
            }
            catch
            {
                //a base directory containing an unloadable assembly must not stop the host from
                //starting; the consequence is only that its parts are not contributed
                return;
            }

            AddNopApplicationParts(partManager, assemblies);
        }

        /// <summary>
        /// Contributes every loaded plugin assembly as an application part (runtime deferral
        /// 1.2).
        /// </summary>
        /// <remarks>
        /// Silent no-op while <see cref="PluginManager.ReferencedPlugins"/> is null, i.e. until
        /// <c>PluginManager.Initialize()</c> has run (runtime deferral 1.1). Call order in
        /// <c>Program.cs</c> must be <c>UseNopHostingEnvironment()</c> →
        /// <c>AddNopFramework()</c>.
        /// </remarks>
        /// <param name="partManager">Application part manager</param>
        public static void AddPluginApplicationParts(ApplicationPartManager partManager)
        {
            if (partManager == null)
                return;

            var referencedPlugins = PluginManager.ReferencedPlugins;
            if (referencedPlugins == null)
                return;

            var assemblies = new List<Assembly>();
            foreach (var plugin in referencedPlugins)
            {
                if (plugin == null || plugin.ReferencedAssembly == null)
                    continue;

                assemblies.Add(plugin.ReferencedAssembly);
            }

            AddNopApplicationParts(partManager, assemblies);
        }
    }
}
