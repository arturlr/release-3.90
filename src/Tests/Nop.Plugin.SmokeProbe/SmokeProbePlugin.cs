using Nop.Core.Plugins;

namespace Nop.Plugin.SmokeProbe
{
    /// <summary>
    /// The smallest thing nopCommerce recognises as a plugin. Created by task 8.8 to verify
    /// runtime deferral 8.2-1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deriving from <see cref="BasePlugin"/> is the whole point rather than an incidental
    /// convenience. <c>PluginManager.Initialize()</c> finds a plugin's type by scanning the loaded
    /// assembly for <c>typeof(IPlugin).IsAssignableFrom(t)</c>. If the assembly were loaded into a
    /// non-default <see cref="System.Runtime.Loader.AssemblyLoadContext"/> it would get its own
    /// copy of every <c>Nop.Core</c> type, that comparison would be <b>false</b>, and the plugin
    /// would be discovered, loaded and then <b>silently ignored</b> — a worse failure than not
    /// loading, because nothing reports it. So this type's assignability is itself an assertion.
    /// </para>
    /// <para>
    /// Everything else is deliberately absent: no controller, no view, no setting, no
    /// <c>IDependencyRegistrar</c>. See the remarks in the project file.
    /// </para>
    /// </remarks>
    public class SmokeProbePlugin : BasePlugin
    {
    }
}
