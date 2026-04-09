# Plugin System

## Overview
nopCommerce features a powerful plugin architecture enabling extensibility without modifying core code. Plugins can add new features, payment methods, shipping providers, widgets, and more.

## Plugin Architecture

### Plugin Base
**Interface**: `IPlugin` (Nop.Core.Plugins)

**Key Methods**:
- `Install()` - Called when plugin installed
- `Uninstall()` - Called when plugin uninstalled
- `PluginDescriptor` - Metadata about plugin

### Plugin Categories
1. **Payment Plugins** (`IPaymentMethod`)
2. **Shipping Plugins** (`IShippingRateComputationMethod`)
3. **Tax Providers** (`ITaxProvider`)
4. **Widget Plugins** (`IWidgetPlugin`)
5. **Discount Rules** (`IDiscountRequirementRule`)
6. **External Authentication** (`IExternalAuthenticationMethod`)
7. **Exchange Rate Providers** (`IExchangeRateProvider`)
8. **Misc Plugins** (custom functionality)

## Plugin Structure

### Directory Layout
```
Plugins/
└── Nop.Plugin.{Category}.{Name}/
    ├── plugin.json               (Plugin descriptor)
    ├── Description.txt           (Plugin description)
    ├── {PluginName}Plugin.cs    (Main plugin class)
    ├── Controllers/             (MVC controllers)
    ├── Models/                  (View models)
    ├── Views/                   (Razor views)
    ├── Content/                 (CSS, images)
    ├── Scripts/                 (JavaScript)
    ├── RouteProvider.cs         (Custom routes)
    └── DependencyRegistrar.cs   (IoC configuration)
```

### Plugin Descriptor (plugin.json)
```json
{
    "Group": "Payment methods",
    "FriendlyName": "PayPal Standard",
    "SystemName": "Payments.PayPalStandard",
    "Version": "1.50",
    "SupportedVersions": [ "4.00" ],
    "Author": "nopCommerce team",
    "DisplayOrder": 1,
    "FileName": "Nop.Plugin.Payments.PayPalStandard.dll",
    "Description": "PayPal Standard payment processor"
}
```

## Plugin Lifecycle

### Discovery and Loading
**Process**:
1. Application startup scans `/Plugins/` directory
2. Plugin descriptors (plugin.json) read
3. Plugin assemblies loaded into memory
4. Plugins registered with IoC container
5. Plugin instances created when needed

**Shadow Copying**:
- Plugins copied to `/Plugins/bin/` for hot reload
- Allows plugin updates without restart
- Original DLLs remain locked

### Installation
**Steps**:
1. User uploads plugin or selects from list
2. System extracts plugin to `/Plugins/` directory
3. Plugin descriptor validated
4. Plugin assembly loaded
5. `Install()` method called:
   - Register settings
   - Create database tables/records
   - Schedule tasks if needed
   - Set installed flag

### Uninstallation
**Steps**:
1. User selects "Uninstall" for plugin
2. `Uninstall()` method called:
   - Remove settings
   - Remove database records
   - Cancel scheduled tasks
3. Installed flag removed
4. Plugin remains on disk (can reinstall)

### Updates
**Process**:
1. Replace plugin files in `/Plugins/` directory
2. Application restart (or shadow copy refresh)
3. Version detection (compare with previous)
4. Optional: Call `Update()` method if provided
5. Re-register services with IoC

## Plugin Development

### Creating a Payment Plugin
**Example**:
```csharp
public class PayPalStandardPaymentProcessor : BasePlugin, IPaymentMethod
{
    private readonly ISettingService _settingService;
    
    public PayPalStandardPaymentProcessor(ISettingService settingService)
    {
        _settingService = settingService;
    }
    
    public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest request)
    {
        // Process payment logic
        var result = new ProcessPaymentResult();
        result.NewPaymentStatus = PaymentStatus.Pending;
        return result;
    }
    
    public void PostProcessPayment(PostProcessPaymentRequest request)
    {
        // Redirect to PayPal
    }
    
    public override void Install()
    {
        // Save default settings
        _settingService.SaveSetting(new PayPalStandardSettings());
        base.Install();
    }
    
    public override void Uninstall()
    {
        // Remove settings
        _settingService.DeleteSetting<PayPalStandardSettings>();
        base.Uninstall();
    }
}
```

### Plugin Configuration
**Configuration Controller**:
```csharp
[AdminAuthorize]
public class PaymentPayPalStandardController : BasePaymentController
{
    public IActionResult Configure()
    {
        var model = new ConfigurationModel();
        // Load settings into model
        return View("~/Plugins/Payments.PayPalStandard/Views/Configure.cshtml", model);
    }
    
    [HttpPost]
    public IActionResult Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return Configure();
        
        // Save settings
        return Configure();
    }
}
```

## Dependency Injection in Plugins

### DependencyRegistrar
**Pattern**: Autofac module registration

```csharp
public class DependencyRegistrar : IDependencyRegistrar
{
    public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
    {
        builder.RegisterType<PayPalService>().As<IPayPalService>().InstancePerLifetimeScope();
    }
    
    public int Order => 1;
}
```

### Service Registration
- Plugins can register own services
- Services available to plugin and core
- Scoped, transient, or singleton lifetimes

## Plugin Resources

### Localization
**Resource Files**: `Localization/resources.{LanguageCode}.xml`

**Example**:
```xml
<Language Name="English">
    <LocaleResource Name="Plugins.Payments.PayPalStandard.Fields.UseSandbox">
        <Value>Use Sandbox</Value>
    </LocaleResource>
</Language>
```

**Usage in Code**:
```csharp
_localizationService.GetResource("Plugins.Payments.PayPalStandard.Fields.UseSandbox")
```

### Routes
**RouteProvider**:
```csharp
public class RouteProvider : IRouteProvider
{
    public void RegisterRoutes(RouteBuilder routeBuilder)
    {
        routeBuilder.MapRoute(
            "Plugin.Payments.PayPalStandard.PDTHandler",
            "Plugins/PaymentPayPalStandard/PDTHandler",
            new { controller = "PaymentPayPalStandard", action = "PDTHandler" }
        );
    }
    
    public int Priority => 0;
}
```

### Views
**Location**: `/Plugins/{PluginName}/Views/`

**Referencing**:
```csharp
return View("~/Plugins/Payments.PayPalStandard/Views/PaymentInfo.cshtml", model);
```

## Widget Plugins

### Widget Zones
**Common Zones**:
- `header` - Header area
- `footer` - Footer area
- `home_page_top` - Homepage top
- `productdetails_add_info` - Product page
- Custom zones defined by theme

**Implementation**:
```csharp
public class NivoSliderPlugin : BasePlugin, IWidgetPlugin
{
    public IList<string> GetWidgetZones()
    {
        return new List<string> { "home_page_top" };
    }
    
    public void GetDisplayWidgetRoute(string widgetZone, 
        out string actionName, out string controllerName, 
        out RouteValueDictionary routeValues)
    {
        actionName = "PublicInfo";
        controllerName = "WidgetsNivoSlider";
        routeValues = new RouteValueDictionary();
    }
}
```

## Plugin Settings

### Settings Pattern
**Settings Class**:
```csharp
public class PayPalStandardSettings : ISettings
{
    public bool UseSandbox { get; set; }
    public string BusinessEmail { get; set; }
    public string PdtToken { get; set; }
    public bool PassProductNamesAndTotals { get; set; }
}
```

**Saving Settings**:
```csharp
_settingService.SaveSetting(settings);
```

**Loading Settings**:
```csharp
var settings = _settingService.LoadSetting<PayPalStandardSettings>();
```

## Plugin Security

### Admin Authorization
- Controllers require `[AdminAuthorize]`
- Configuration pages admin-only
- Settings require admin permission

### Input Validation
- Validate all user input
- Sanitize external data
- Use anti-forgery tokens

### API Keys
- Store API keys encrypted
- Never expose keys in client code
- Use HTTPS for API calls

## Plugin Best Practices

1. **Follow naming conventions**: `Nop.Plugin.{Category}.{Name}`
2. **Use dependency injection**: Never use static services
3. **Implement IDisposable** if needed
4. **Handle errors gracefully**: Don't crash the application
5. **Provide clear configuration**: User-friendly settings
6. **Document thoroughly**: Instructions and prerequisites
7. **Test extensively**: All payment/shipping scenarios
8. **Version compatibility**: Specify supported versions
9. **Localize resources**: Support multiple languages
10. **Update regularly**: Keep compatible with core updates

## Related Documentation
- [Program Structure](../reference/program-structure.md)
- [Interfaces](../reference/interfaces.md)

**Version**: 1.0
