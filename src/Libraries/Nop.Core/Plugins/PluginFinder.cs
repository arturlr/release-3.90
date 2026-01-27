using System.Collections.Generic;
using System.Linq;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Plugin finder service for runtime plugin discovery
    /// </summary>
    public class PluginFinder : IPluginFinder
    {
        private readonly IPluginLoader _pluginLoader;

        public PluginFinder(IPluginLoader pluginLoader)
        {
            _pluginLoader = pluginLoader;
        }

        public IEnumerable<T> GetPlugins<T>(bool installedOnly = true) where T : class, IPlugin
        {
            var plugins = _pluginLoader.GetLoadedPlugins();
            
            if (installedOnly)
                plugins = plugins.Where(p => p.IsInstalled);

            foreach (var descriptor in plugins)
            {
                if (descriptor.PluginType != null && typeof(T).IsAssignableFrom(descriptor.PluginType))
                {
                    var instance = System.Activator.CreateInstance(descriptor.PluginType) as T;
                    if (instance != null)
                    {
                        instance.PluginDescriptor = descriptor;
                        yield return instance;
                    }
                }
            }
        }

        public IEnumerable<PluginDescriptor> GetPluginDescriptors(bool installedOnly = true)
        {
            var plugins = _pluginLoader.DiscoverPlugins();
            return installedOnly ? plugins.Where(p => p.IsInstalled) : plugins;
        }

        public PluginDescriptor GetPluginDescriptorBySystemName(string systemName)
        {
            return _pluginLoader.DiscoverPlugins()
                .FirstOrDefault(p => p.SystemName == systemName);
        }
    }
}
