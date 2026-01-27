# Task 4: Design Modern Plugin Architecture - Design Document

## Overview

Design a modern plugin system for .NET 8 that replaces the legacy shadow copying mechanism with `AssemblyLoadContext`.

## Current Architecture (EF6/.NET Framework)

### Problems:
1. **PreApplicationStartMethod** - Not available in .NET Core
2. **BuildManager** - Not available in .NET Core
3. **Shadow Copying** - File locking issues, complex deployment
4. **Static Initialization** - Not compatible with DI/hosting model
5. **~/Plugins folder** - Web-specific path assumptions

### Current Flow:
```
PreApplicationStartMethod (PluginManager.Initialize)
  ↓
Scan ~/Plugins folder
  ↓
Shadow copy assemblies to ~/Plugins/bin
  ↓
BuildManager.AddReferencedAssembly()
  ↓
Plugins available globally
```

## New Architecture (.NET 8)

### Core Concepts:

1. **AssemblyLoadContext** - Isolated plugin loading
2. **IHostedService** - Lifecycle management
3. **Dependency Injection** - Service-based discovery
4. **plugin.json** - Modern manifest format
5. **No Shadow Copying** - Direct assembly loading

### New Flow:
```
Application Startup
  ↓
PluginLoader (IHostedService)
  ↓
Scan Plugins folder for plugin.json
  ↓
Create PluginLoadContext per plugin
  ↓
Load assemblies in isolated context
  ↓
Register plugin services in DI
  ↓
Plugins available via DI
```

## Design Components

### 1. Plugin Manifest (plugin.json)

```json
{
  "systemName": "Payments.PayPal",
  "friendlyName": "PayPal Standard",
  "version": "1.0.0",
  "author": "nopCommerce team",
  "description": "PayPal Standard payment processor",
  "assemblyName": "Nop.Plugin.Payments.PayPal.dll",
  "supportedVersions": ["4.70"],
  "dependencies": [],
  "category": "Payment",
  "displayOrder": 1
}
```

### 2. PluginLoadContext

```csharp
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginPath;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _pluginPath = pluginPath;
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        // Try to load from plugin directory first
        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
            return LoadFromAssemblyPath(assemblyPath);

        // Fall back to default context (shared dependencies)
        return null;
    }
}
```

### 3. Plugin Descriptor

```csharp
public class PluginDescriptor
{
    public string SystemName { get; set; }
    public string FriendlyName { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    public string AssemblyName { get; set; }
    public List<string> SupportedVersions { get; set; }
    public List<string> Dependencies { get; set; }
    public string Category { get; set; }
    public int DisplayOrder { get; set; }
    
    // Runtime properties
    public Assembly PluginAssembly { get; set; }
    public Type PluginType { get; set; }
    public PluginLoadContext LoadContext { get; set; }
    public bool IsInstalled { get; set; }
}
```

### 4. IPluginLoader Service

```csharp
public interface IPluginLoader
{
    /// <summary>
    /// Discover all plugins
    /// </summary>
    IEnumerable<PluginDescriptor> DiscoverPlugins();
    
    /// <summary>
    /// Load a specific plugin
    /// </summary>
    PluginDescriptor LoadPlugin(string systemName);
    
    /// <summary>
    /// Unload a plugin
    /// </summary>
    void UnloadPlugin(string systemName);
    
    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    IEnumerable<PluginDescriptor> GetLoadedPlugins();
    
    /// <summary>
    /// Install a plugin
    /// </summary>
    void InstallPlugin(string systemName);
    
    /// <summary>
    /// Uninstall a plugin
    /// </summary>
    void UninstallPlugin(string systemName);
}
```

### 5. Plugin Base Interface

```csharp
public interface IPlugin
{
    /// <summary>
    /// Gets plugin descriptor
    /// </summary>
    PluginDescriptor PluginDescriptor { get; set; }
    
    /// <summary>
    /// Install plugin
    /// </summary>
    void Install();
    
    /// <summary>
    /// Uninstall plugin
    /// </summary>
    void Uninstall();
    
    /// <summary>
    /// Gets configuration page URL
    /// </summary>
    string GetConfigurationPageUrl();
}
```

### 6. Plugin Loader Implementation

```csharp
public class PluginLoader : IPluginLoader, IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginLoader> _logger;
    private readonly Dictionary<string, PluginDescriptor> _loadedPlugins;
    private readonly string _pluginsPath;

    public PluginLoader(
        IServiceProvider serviceProvider,
        ILogger<PluginLoader> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _loadedPlugins = new Dictionary<string, PluginDescriptor>();
        _pluginsPath = configuration["PluginsPath"] ?? "Plugins";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Discover and load plugins on startup
        var plugins = DiscoverPlugins();
        foreach (var plugin in plugins.Where(p => p.IsInstalled))
        {
            LoadPlugin(plugin.SystemName);
        }
    }

    public IEnumerable<PluginDescriptor> DiscoverPlugins()
    {
        // Scan for plugin.json files
        // Parse manifests
        // Return descriptors
    }

    public PluginDescriptor LoadPlugin(string systemName)
    {
        // Create PluginLoadContext
        // Load assembly
        // Instantiate plugin
        // Register services
    }
}
```

## Plugin Categories

### Payment Plugins
```csharp
public interface IPaymentMethod : IPlugin
{
    ProcessPaymentResult ProcessPayment(ProcessPaymentRequest request);
    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request);
}
```

### Shipping Plugins
```csharp
public interface IShippingRateComputationMethod : IPlugin
{
    GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest request);
}
```

### Tax Plugins
```csharp
public interface ITaxProvider : IPlugin
{
    CalculateTaxResult CalculateTax(CalculateTaxRequest request);
}
```

### Widget Plugins
```csharp
public interface IWidgetPlugin : IPlugin
{
    IList<string> GetWidgetZones();
    string GetWidgetViewComponentName(string widgetZone);
}
```

## Dependency Injection Integration

### Plugin Service Registration

```csharp
public static class PluginServiceCollectionExtensions
{
    public static IServiceCollection AddPlugins(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register plugin loader
        services.AddSingleton<IPluginLoader, PluginLoader>();
        services.AddHostedService<PluginLoader>();
        
        // Register plugin finder
        services.AddScoped<IPluginFinder, PluginFinder>();
        
        return services;
    }
}
```

### Plugin Discovery at Runtime

```csharp
public class PluginFinder : IPluginFinder
{
    private readonly IPluginLoader _pluginLoader;
    
    public IEnumerable<T> GetPlugins<T>() where T : class, IPlugin
    {
        var plugins = _pluginLoader.GetLoadedPlugins();
        foreach (var descriptor in plugins)
        {
            if (typeof(T).IsAssignableFrom(descriptor.PluginType))
            {
                var instance = Activator.CreateInstance(descriptor.PluginType);
                yield return instance as T;
            }
        }
    }
}
```

## File Structure

```
Plugins/
├── Payments.PayPal/
│   ├── plugin.json
│   ├── Nop.Plugin.Payments.PayPal.dll
│   ├── Views/
│   ├── Content/
│   └── logo.png
├── Shipping.UPS/
│   ├── plugin.json
│   ├── Nop.Plugin.Shipping.UPS.dll
│   └── ...
└── Tax.FixedRate/
    ├── plugin.json
    ├── Nop.Plugin.Tax.FixedRate.dll
    └── ...
```

## Plugin Lifecycle

### 1. Discovery
- Scan Plugins folder
- Parse plugin.json files
- Validate compatibility

### 2. Loading
- Create AssemblyLoadContext
- Load plugin assembly
- Resolve dependencies
- Instantiate plugin class

### 3. Installation
- Call plugin.Install()
- Run database migrations
- Copy static files
- Mark as installed

### 4. Activation
- Register services in DI
- Register routes/endpoints
- Register view components

### 5. Uninstallation
- Call plugin.Uninstall()
- Remove database changes
- Remove static files
- Mark as uninstalled

### 6. Unloading
- Dispose plugin instance
- Unload AssemblyLoadContext
- Free resources

## Hot Reload Support

```csharp
public class PluginWatcher : IHostedService
{
    private FileSystemWatcher _watcher;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher = new FileSystemWatcher(_pluginsPath);
        _watcher.Changed += OnPluginChanged;
        _watcher.EnableRaisingEvents = true;
        return Task.CompletedTask;
    }
    
    private void OnPluginChanged(object sender, FileSystemEventArgs e)
    {
        // Unload old version
        // Load new version
        // Notify application
    }
}
```

## Migration from Old System

### Compatibility Layer

```csharp
public abstract class BasePlugin : IPlugin
{
    // Maintain same interface as old system
    public virtual void Install() { }
    public virtual void Uninstall() { }
    public virtual string GetConfigurationPageUrl() => null;
    
    // New properties
    public PluginDescriptor PluginDescriptor { get; set; }
}
```

### Migration Steps

1. Convert Description.txt → plugin.json
2. Update plugin base class
3. Remove shadow copy dependencies
4. Update service registration
5. Test plugin loading

## Benefits

✅ **No Shadow Copying** - Simpler deployment
✅ **Isolated Loading** - Better dependency management
✅ **Hot Reload** - Development productivity
✅ **Collectible** - Memory management
✅ **Modern** - .NET 8 best practices
✅ **Testable** - DI-friendly design

## Challenges

⚠️ **Shared Dependencies** - Version conflicts
⚠️ **View Discovery** - Razor views in plugins
⚠️ **Static Files** - wwwroot in plugins
⚠️ **Database Migrations** - Plugin-specific migrations
⚠️ **Backward Compatibility** - Existing plugins

## Implementation Plan

### Phase 1: Core Infrastructure (Task 4)
- Design interfaces
- Create plugin manifest schema
- Document architecture

### Phase 2: Implementation (Task 5)
- Implement PluginLoadContext
- Implement PluginLoader
- Implement PluginFinder
- Add DI integration

### Phase 3: Migration (Task 10)
- Convert 20 plugins
- Test each plugin
- Update documentation

## Testing Strategy

1. **Unit Tests** - Plugin loading/unloading
2. **Integration Tests** - Full plugin lifecycle
3. **Performance Tests** - Load time, memory usage
4. **Compatibility Tests** - All 20 plugins

## Estimated Effort

- **Design**: 4-6 hours ✅
- **Implementation**: 12-16 hours
- **Testing**: 6-8 hours
- **Documentation**: 4-6 hours

**Total**: 26-36 hours

## Status: DESIGN COMPLETE ✅

This design provides a modern, maintainable plugin architecture for .NET 8 that eliminates the legacy shadow copying mechanism while maintaining compatibility with existing plugin concepts.
