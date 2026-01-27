using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Core.Plugins
{
    /// <summary>
    /// Extension methods for plugin service registration
    /// </summary>
    public static class PluginServiceExtensions
    {
        public static IServiceCollection AddPluginSupport(this IServiceCollection services, string pluginsPath = "Plugins")
        {
            services.AddSingleton<IPluginLoader>(sp => new PluginLoader(pluginsPath));
            services.AddScoped<IPluginFinder, PluginFinder>();
            services.AddHostedService<PluginHostedService>();
            
            return services;
        }
    }

    /// <summary>
    /// Hosted service for plugin lifecycle management
    /// </summary>
    internal class PluginHostedService : IHostedService
    {
        private readonly IPluginLoader _pluginLoader;

        public PluginHostedService(IPluginLoader pluginLoader)
        {
            _pluginLoader = pluginLoader;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // Load all installed plugins on startup
            var plugins = _pluginLoader.DiscoverPlugins();
            foreach (var plugin in plugins)
            {
                if (plugin.IsInstalled)
                {
                    _pluginLoader.LoadPlugin(plugin.SystemName);
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // Unload all plugins on shutdown
            var loadedPlugins = _pluginLoader.GetLoadedPlugins().ToList();
            foreach (var plugin in loadedPlugins)
            {
                _pluginLoader.UnloadPlugin(plugin.SystemName);
            }

            return Task.CompletedTask;
        }
    }
}
