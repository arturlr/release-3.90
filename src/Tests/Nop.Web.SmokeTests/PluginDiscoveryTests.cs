using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Nop.Core;
using Nop.Core.Plugins;

namespace Nop.Web.SmokeTests
{
    /// <summary>
    /// Task 8.8 — runtime deferral 8.2-1: <c>PluginManager.PerformFileDeploy</c> loaded each
    /// shadow-copied plugin assembly BY NAME, which cannot work on .NET.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this fixture exists.</b> The 8.8 gate unblocks all 20 plugin projects (groups
    /// 10–15), and deferral 8.2-1 was recorded HIGH and still open: task 8.2 measured
    /// <c>Assembly.Load(AssemblyName.GetAssemblyName(path))</c> throwing
    /// <c>FileNotFoundException</c> for an assembly present on disk but absent from the host's
    /// <c>deps.json</c>, because .NET's default <see cref="System.Runtime.Loader.AssemblyLoadContext"/>
    /// binds from the <c>deps.json</c>-derived trusted-platform-assemblies list and does not probe
    /// directories. <c>~/Plugins/bin</c> is in no <c>deps.json</c>, so <b>the entire plugin
    /// subsystem would have failed at startup on the first migrated plugin</b>. It went unnoticed
    /// because no plugin exists yet — task 7.7 measured <c>ReferencedPlugins</c> non-null with
    /// <b>0</b> plugins.
    /// </para>
    /// <para>
    /// <b>This verifies the fix with a REAL plugin, not a probe.</b> A minimal assembly deriving
    /// from <c>Nop.Core.Plugins.BasePlugin</c> plus a real <c>Description.txt</c> is planted under
    /// the content root's <c>Plugins/</c> directory before the host starts, so the real
    /// <c>PluginManager.Initialize()</c> — invoked from <c>Program.Main</c> via
    /// <c>UseNopHostingEnvironment</c> — discovers it, shadow-copies it to <c>~/Plugins/bin</c> and
    /// loads it. That exercises the changed line and nothing else stands in for it.
    /// </para>
    /// <para>
    /// It also asserts the property that makes the DEFAULT load context mandatory: the loaded type
    /// must still be assignable to <c>IPlugin</c>. An assembly loaded into a separate context would
    /// get its own copy of every <c>Nop.Core</c> type, so
    /// <c>typeof(IPlugin).IsAssignableFrom(t)</c> in <c>PluginManager.Initialize</c> would be false
    /// and the plugin would be <b>silently invisible</b> — discovered, loaded, and then ignored.
    /// </para>
    /// <para>
    /// <c>src/Presentation/Nop.Web/Plugins/*</c> is already in <c>.gitignore</c>, so the planted
    /// files cannot be committed by accident; they are removed in teardown regardless.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class PluginDiscoveryTests
    {
        private const string SystemName = "SmokeProbe.Test";
        private const string AssemblyFileName = "Nop.Plugin.SmokeProbe.dll";

        private static readonly string ProbeBuildOutput = Path.Combine(
            "src", "Tests", "Nop.Plugin.SmokeProbe", "bin", "Debug", "net10.0", AssemblyFileName);

        private string _pluginDir;
        private NopWebApplicationFactory _factory;
        private bool _planted;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var contentRoot = NopWebApplicationFactory.ResolveNopWebContentRoot();
            //the repository root is two levels above src/Presentation
            var repoRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", "..", ".."));
            var source = Path.Combine(repoRoot, ProbeBuildOutput);
            if (!File.Exists(source))
                return;

            _pluginDir = Path.Combine(contentRoot, "Plugins", "Nop.Plugin.SmokeProbe");
            Directory.CreateDirectory(_pluginDir);
            File.Copy(source, Path.Combine(_pluginDir, AssemblyFileName), true);
            File.WriteAllText(Path.Combine(_pluginDir, "Description.txt"), string.Join("\n", new[]
            {
                "Group: Task 8.8 verification",
                "FriendlyName: Task 8.8 plugin-load probe",
                "SystemName: " + SystemName,
                "Version: 1.0",
                "SupportedVersions: " + NopVersion.CurrentVersion,
                "Author: task 8.8",
                "DisplayOrder: 1",
                "FileName: " + AssemblyFileName,
                "Description: Verifies runtime deferral 8.2-1. Safe to delete."
            }));
            _planted = true;

            //The host must be created AFTER planting: PluginManager.Initialize() runs once, from
            //Program.Main, before the container exists.
            _factory = new NopWebApplicationFactory();
            _factory.CreateClient().Dispose();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (_factory != null)
                _factory.Dispose();

            try
            {
                if (_pluginDir != null && Directory.Exists(_pluginDir))
                    Directory.Delete(_pluginDir, true);

                //the shadow copy the plugin manager made
                var shadow = Path.Combine(
                    NopWebApplicationFactory.ResolveNopWebContentRoot(),
                    "Plugins", "bin", AssemblyFileName);
                if (File.Exists(shadow))
                    File.Delete(shadow);
            }
            catch (IOException)
            {
                //the assembly is loaded into this process, so the shadow copy may be locked on
                //some platforms. src/Presentation/Nop.Web/Plugins/* is gitignored, so a leftover
                //cannot be committed - and it is named SmokeProbe, so it is obvious.
            }
        }

        private void RequirePlanted()
        {
            if (!_planted)
                Assert.Ignore("NOT EXERCISED: " + ProbeBuildOutput + " was not built. Run " +
                              "`dotnet build src/Tests/Nop.Plugin.SmokeProbe/Nop.Plugin.SmokeProbe.csproj` " +
                              "first - Nop.Web.SmokeTests has a build-order ProjectReference to " +
                              "it, so this should not happen. See deferral 8.2-1.");
        }

        [Test]
        public void Deferral_8_2_1_a_shadow_copied_plugin_assembly_LOADS()
        {
            //THE assertion. Before the fix this could not happen: the shadow-copied assembly lives
            //in ~/Plugins/bin, which is in no deps.json, so Assembly.Load by name threw
            //FileNotFoundException - and PluginManager.Initialize wraps every failure and RETHROWS,
            //so it would have taken the host down before anything could report the cause.
            RequirePlanted();

            var plugins = PluginManager.ReferencedPlugins;
            Assert.IsNotNull(plugins, "PluginManager.Initialize() did not run (deferral 1.1).");
            TestContext.WriteLine("discovered plugins: " +
                string.Join(", ", plugins.Select(p => p.SystemName)));

            var probe = plugins.FirstOrDefault(p => p.SystemName == SystemName);
            Assert.IsNotNull(probe,
                "The planted plugin was not discovered. Descriptors found: " +
                string.Join(", ", plugins.Select(p => p.SystemName)) +
                ". Check that Plugins/Nop.Plugin.SmokeProbe/Description.txt was written before " +
                "the host started.");

            Assert.IsNotNull(probe.ReferencedAssembly,
                "The descriptor was parsed but the assembly was never loaded - deferral 8.2-1.");
            Assert.AreEqual("Nop.Plugin.SmokeProbe", probe.ReferencedAssembly.GetName().Name);
        }

        [Test]
        public void Deferral_8_2_1_the_plugin_was_loaded_from_the_SHADOW_COPY_directory()
        {
            //Confirms which file was loaded, i.e. that the deps.json-absent path really is the one
            //under test rather than some other copy that happens to be resolvable.
            RequirePlanted();

            var probe = PluginManager.ReferencedPlugins.FirstOrDefault(p => p.SystemName == SystemName);
            Assert.IsNotNull(probe, "plugin not discovered - see the other test");

            var location = probe.ReferencedAssembly.Location;
            TestContext.WriteLine("loaded from: " + location);
            Assert.IsNotEmpty(location, "the assembly has no Location, so it was not loaded by path");

            var shadowDir = Path.GetFullPath(Path.Combine(
                NopWebApplicationFactory.ResolveNopWebContentRoot(), "Plugins", "bin"));
            Assert.AreEqual(shadowDir,
                Path.GetFullPath(Path.GetDirectoryName(location)),
                "the plugin was not loaded from ~/Plugins/bin, so the shadow-copy path is not what " +
                "this test is measuring.");

            //and that directory is in no deps.json - the property that made Assembly.Load fail
            var deps = Path.Combine(AppContext.BaseDirectory, "Nop.Web.SmokeTests.deps.json");
            Assert.IsTrue(File.Exists(deps), "PREMISE BROKEN: " + deps + " not found");
            StringAssert.DoesNotContain("Nop.Plugin.SmokeProbe", File.ReadAllText(deps),
                "the probe plugin is in deps.json, so it could have been resolved from the " +
                "trusted-platform-assemblies list and this test proves nothing.");

            //...nor is it in the test output directory, where WebAppTypeFinder would have loaded it
            //as an ordinary base-directory assembly and bypassed the plugin path entirely. This is
            //what the ReferenceOutputAssembly="false" / ExcludeAssets="all" reference in
            //Nop.Web.SmokeTests.csproj is for.
            Assert.IsFalse(File.Exists(Path.Combine(AppContext.BaseDirectory, AssemblyFileName)),
                AssemblyFileName + " is in the test output directory, so WebAppTypeFinder loaded " +
                "it directly and PluginManager's shadow-copy path was never exercised.");
        }

        [Test]
        public void Deferral_8_2_1_the_plugin_type_is_still_assignable_to_IPlugin()
        {
            //WHY THE DEFAULT LOAD CONTEXT IS MANDATORY. An assembly in a separate context gets its
            //own copy of every Nop.Core type, so this comparison would be FALSE and
            //PluginManager.Initialize's `typeof(IPlugin).IsAssignableFrom(t)` scan would skip the
            //plugin entirely - discovered, loaded, and then silently ignored. That is a worse
            //failure than not loading at all, because nothing reports it.
            RequirePlanted();

            var probe = PluginManager.ReferencedPlugins.FirstOrDefault(p => p.SystemName == SystemName);
            Assert.IsNotNull(probe, "plugin not discovered - see the other test");

            Assert.IsNotNull(probe.PluginType,
                "PluginManager found no IPlugin implementation in the assembly. The assembly was " +
                "loaded into a NON-DEFAULT AssemblyLoadContext, so its BasePlugin is a different " +
                "type from the host's and the IsAssignableFrom scan skipped it.");
            Assert.IsTrue(typeof(IPlugin).IsAssignableFrom(probe.PluginType),
                probe.PluginType.FullName + " is not assignable to Nop.Core.Plugins.IPlugin.");

            var context = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(
                probe.ReferencedAssembly);
            Assert.AreSame(System.Runtime.Loader.AssemblyLoadContext.Default, context,
                "the plugin was loaded into '" + (context == null ? "<null>" : context.Name) +
                "' rather than the default context.");
        }

        [Test]
        public void Deferral_1_2_the_plugin_became_an_MVC_application_part()
        {
            //The other half of plugin loading, and the first time it has been exercised with a real
            //plugin: AddNopFramework calls
            //NopApplicationPartExtensions.AddPluginApplicationParts, which is a silent no-op while
            //ReferencedPlugins is null. Task 8.2 additionally changed it from `new
            //AssemblyPart(assembly)` to ApplicationPartFactory so a plugin's COMPILED RAZOR VIEWS
            //are contributed too (deferral 1.2). This probe plugin has no views, so only the
            //AssemblyPart can be asserted - which is still the part that makes a plugin's
            //controllers routable.
            RequirePlanted();

            var partManager = _factory.Services
                .GetRequiredService<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager>();
            var names = partManager.ApplicationParts.Select(p => p.Name).ToList();
            TestContext.WriteLine("application parts: " + string.Join(", ", names));

            Assert.Contains("Nop.Plugin.SmokeProbe", names,
                "the loaded plugin did not become an application part, so a real plugin's " +
                "controllers would not be routable and its views would not resolve (deferral 1.2).");
        }
    }
}
