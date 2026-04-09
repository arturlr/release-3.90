# ASP.NET MVC Implementation

## Overview
nopCommerce uses ASP.NET MVC 5.2.3 as its web framework, following MVC architectural pattern with extensive use of conventions and extensibility points.

## MVC Structure

### Controllers
**Location**: `Nop.Web/Controllers` (public), `Nop.Admin/Controllers` (admin)

**Base Controllers**:
- `BasePublicController` - Public website base
- `BaseAdminController` - Admin area base
- Provides common functionality (localization, work context, etc.)

**Controller Responsibilities**:
- Route handling
- Input validation
- Service orchestration
- View model preparation
- Result generation (View, JSON, Redirect)

### Views
**View Engine**: Razor (.cshtml)

**View Locations**:
- `~/Views/{Controller}/{Action}.cshtml`
- `~/Views/Shared/` - Shared views and layouts
- `~/Administration/Views/` - Admin views

**Layout Structure**:
- `_Root.cshtml` - Master layout
- `_ColumnsTwo.cshtml`, `_ColumnsOne.cshtml` - Column layouts
- Nested layouts for flexibility

### Models
**View Models**: Separate from domain models

**Pattern**:
- Domain Model → AutoMapper → View Model
- View Models in `Nop.Web/Models` namespace
- Validation attributes on view models
- Localized display names

## Routing

### Route Configuration
**Location**: `RouteConfig.cs`, `RouteProvider.cs`

**Default Routes**:
```csharp
routes.MapRoute(
    name: "Default",
    url: "{controller}/{action}/{id}",
    defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
);
```

### Custom Routes
- SEO-friendly URLs for products, categories
- Language prefix routes (`/en/`, `/es/`)
- Admin area routes (`/Admin/...`)
- Plugin routes registered dynamically

### URL Generation
- `Url.Action()` for controller actions
- `Url.RouteUrl()` for named routes
- SEO service generates friendly URLs

## Filters and Attributes

### Custom Action Filters
**Location**: `Nop.Web.Framework/Mvc/Filters`

**Key Filters**:
- `CheckAccessPublicStoreAttribute` - Store access validation
- `HttpsRequirementAttribute` - HTTPS enforcement
- `ValidateIpAddressAttribute` - IP filtering
- `StoreIpAddressAttribute` - IP logging
- `CustomerLastActivityAttribute` - Activity tracking

### Authorization
- `[Authorize]` - Requires authentication
- `[AdminAuthorize]` - Admin-specific authorization
- `[PublicAntiForgery]` - CSRF protection
- `[ValidateInput(false)]` - Disable input validation (admin only)

## Model Binding

### Custom Model Binders
- Complex object binding
- File upload handling
- Custom type conversion

### Validation
**Server-Side**:
- FluentValidation library
- Validators in `Nop.Web/Validators` namespace
- Automatic validation via MVC pipeline

**Client-Side**:
- jQuery Validation
- Unobtrusive validation
- Custom validation rules

## Action Results

### Common Results
- `View()` - Render Razor view
- `PartialView()` - Render partial view
- `Json()` - JSON response
- `Redirect()` / `RedirectToAction()` - Redirects
- `File()` - File downloads
- `Content()` - Raw content

### Custom Results
- `NullJsonResult` - Prevents JSON null serialization issues
- `RobotsTextResult` - Dynamic robots.txt generation

## Areas

### Admin Area
**Route Prefix**: `/Admin`
- Separate controllers and views
- Admin-specific layout
- Enhanced security (admin authorization required)
- Rich JavaScript components (grids, editors)

### Plugin Areas
- Plugins can define own areas
- Isolated routes and views
- Plugin-specific resources

## Bundling and Minification

### Bundle Configuration
**Location**: `BundleConfig.cs`

**Script Bundles**:
- jQuery and dependencies
- Admin scripts
- Plugin scripts (dynamic)

**Style Bundles**:
- Theme CSS
- Admin CSS
- RTL support

### Resource Management
- Scripts/CSS registered via page builder
- Deferred loading support
- CDN support configurable

## Dependency Injection in MVC

### Controller Injection
- Constructor injection via Autofac
- Services resolved automatically
- Scoped lifetime per request

### View Injection
- `@inject` directive in Razor views
- Direct service access from views
- Use sparingly (prefer view models)

## Ajax Support

### Ajax Patterns
- `JsonResult` for data endpoints
- jQuery AJAX calls
- Form submission via AJAX
- Partial view updates

### Error Handling
- Global AJAX error handler
- Formatted error responses
- Friendly error messages

## Localization in Views

### Resource Strings
```razor
@T("Products.Name")
@T("Checkout.BillingAddress")
```

### `IWorkContext` Usage
- Current customer
- Working language
- Working currency
- Current store

## Theming

### Theme Structure
- Themes in `/Themes/` directory
- Views override default views
- Theme-specific CSS and JavaScript
- Mobile-responsive design

### Theme Selection
- Per-store theme configuration
- Device-specific themes supported
- Runtime theme switching

## Performance Optimization

### Output Caching
- Partial view caching
- Action result caching
- Cache vary by parameters

### View Compilation
- Precompiled views option
- Runtime compilation in development
- Faster rendering

## Security Features

### CSRF Protection
- Anti-forgery tokens on forms
- `@Html.AntiForgeryToken()` in views
- `[ValidateAntiForgeryToken]` on actions

### XSS Prevention
- Razor auto-encoding
- `Html.Raw()` used minimally
- Input sanitization

### Input Validation
- Max length validation
- Format validation
- Business rule validation

## Related Documentation
- [Program Structure](../reference/program-structure.md)
- [Workflows](../behavior/workflows.md)

**Version**: 1.0
