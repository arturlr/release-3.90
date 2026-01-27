using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Plugin loader service for .NET 8
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// Discover all plugins in the plugins directory
        /// </summary>
        IEnumerable<PluginDescriptor> DiscoverPlugins();
        
        /// <summary>
        /// Load a specific plugin by system name
        /// </summary>
        PluginDescriptor LoadPlugin(string systemName);
        
        /// <summary>
        /// Unload a plugin
        /// </summary>
        void UnloadPlugin(string systemName);
        
        /// <summary>
        /// Get all currently loaded plugins
        /// </summary>
        IEnumerable<PluginDescriptor> GetLoadedPlugins();
        
        /// <summary>
        /// Install a plugin
        /// </summary>
        Task InstallPluginAsync(string systemName);
        
        /// <summary>
        /// Uninstall a plugin
        /// </summary>
        Task UninstallPluginAsync(string systemName);
        
        /// <summary>
        /// Check if a plugin is installed
        /// </summary>
        bool IsPluginInstalled(string systemName);
    }
}
