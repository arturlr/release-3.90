using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Plugin loader service implementation
    /// </summary>
    public class PluginLoader : IPluginLoader
    {
        private readonly Dictionary<string, PluginDescriptor> _loadedPlugins = new();
        private readonly string _pluginsPath;
        private readonly string _installedPluginsFile;

        public PluginLoader(string pluginsPath = "Plugins")
        {
            _pluginsPath = pluginsPath;
            _installedPluginsFile = Path.Combine("App_Data", "InstalledPlugins.txt");
        }

        public IEnumerable<PluginDescriptor> DiscoverPlugins()
        {
            if (!Directory.Exists(_pluginsPath))
                return Enumerable.Empty<PluginDescriptor>();

            var installedPlugins = LoadInstalledPluginsList();
            var plugins = new List<PluginDescriptor>();

            foreach (var dir in Directory.GetDirectories(_pluginsPath))
            {
                var manifestPath = Path.Combine(dir, "plugin.json");
                if (!File.Exists(manifestPath))
                    continue;

                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var descriptor = JsonSerializer.Deserialize<PluginDescriptor>(json);
                    if (descriptor != null)
                    {
                        descriptor.IsInstalled = installedPlugins.Contains(descriptor.SystemName);
                        plugins.Add(descriptor);
                    }
                }
                catch
                {
                    // Skip invalid manifests
                }
            }

            return plugins;
        }

        public PluginDescriptor LoadPlugin(string systemName)
        {
            if (_loadedPlugins.ContainsKey(systemName))
                return _loadedPlugins[systemName];

            var plugins = DiscoverPlugins();
            var descriptor = plugins.FirstOrDefault(p => p.SystemName == systemName);
            if (descriptor == null)
                return null;

            var pluginDir = Path.Combine(_pluginsPath, systemName);
            var assemblyPath = Path.Combine(pluginDir, descriptor.AssemblyName);

            if (!File.Exists(assemblyPath))
                return null;

            var loadContext = new PluginLoadContext(assemblyPath);
            var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);

            descriptor.PluginAssembly = assembly;
            descriptor.LoadContext = loadContext;
            descriptor.PluginType = FindPluginType(assembly);

            _loadedPlugins[systemName] = descriptor;
            return descriptor;
        }

        public void UnloadPlugin(string systemName)
        {
            if (!_loadedPlugins.TryGetValue(systemName, out var descriptor))
                return;

            if (descriptor.LoadContext is PluginLoadContext context)
            {
                context.Unload();
            }

            _loadedPlugins.Remove(systemName);
        }

        public IEnumerable<PluginDescriptor> GetLoadedPlugins()
        {
            return _loadedPlugins.Values;
        }

        public Task InstallPluginAsync(string systemName)
        {
            var installedPlugins = LoadInstalledPluginsList();
            if (!installedPlugins.Contains(systemName))
            {
                installedPlugins.Add(systemName);
                SaveInstalledPluginsList(installedPlugins);
            }

            var descriptor = LoadPlugin(systemName);
            if (descriptor != null)
                descriptor.IsInstalled = true;

            return Task.CompletedTask;
        }

        public Task UninstallPluginAsync(string systemName)
        {
            var installedPlugins = LoadInstalledPluginsList();
            installedPlugins.Remove(systemName);
            SaveInstalledPluginsList(installedPlugins);

            if (_loadedPlugins.TryGetValue(systemName, out var descriptor))
                descriptor.IsInstalled = false;

            return Task.CompletedTask;
        }

        public bool IsPluginInstalled(string systemName)
        {
            var installedPlugins = LoadInstalledPluginsList();
            return installedPlugins.Contains(systemName);
        }

        private Type FindPluginType(Assembly assembly)
        {
            return assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        }

        private HashSet<string> LoadInstalledPluginsList()
        {
            if (!File.Exists(_installedPluginsFile))
                return new HashSet<string>();

            var lines = File.ReadAllLines(_installedPluginsFile);
            return new HashSet<string>(lines.Where(l => !string.IsNullOrWhiteSpace(l)));
        }

        private void SaveInstalledPluginsList(HashSet<string> plugins)
        {
            var dir = Path.GetDirectoryName(_installedPluginsFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllLines(_installedPluginsFile, plugins);
        }
    }
}
