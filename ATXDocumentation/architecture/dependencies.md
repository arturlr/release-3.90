# nopCommerce Dependencies Analysis

## Table of Contents
- [Overview](#overview)
- [External Dependencies](#external-dependencies)
- [Internal Dependencies](#internal-dependencies)
- [Dependency Graph](#dependency-graph)
- [Layer Dependencies](#layer-dependencies)
- [Plugin Dependencies](#plugin-dependencies)

## Overview

nopCommerce follows a layered architecture with clear dependency rules:
- **Core Layer** (Nop.Core) - No dependencies on other layers, only external libraries
- **Data Layer** (Nop.Data) - Depends on Core + Entity Framework
- **Service Layer** (Nop.Services) - Depends on Core, Data + business logic libraries
- **Presentation Layer** (Nop.Web, Nop.Web.Framework, Nop.Admin) - Depends on all lower layers
- **Plugins** - Depend on Core and Services

**Dependency Direction**: Always flows downward, preventing circular dependencies.

## External Dependencies

### Core Framework Dependencies

#### .NET Framework 4.5.1
**Target Framework**: All projects target .NET Framework 4.5.1
**Visual Studio**: Requires Visual Studio 2015 or later (ToolsVersion 12.0)

**Framework Assemblies Used**:
- `System` - Core types
- `System.Core` - LINQ, extension methods
- `System.Data` - ADO.NET
- `System.Web` - Web infrastructure
- `System.Xml` - XML processing
- `System.Configuration` - Configuration management
- `System.ComponentModel.Composition` - MEF support
- `System.Runtime.Caching` - Memory caching
- `System.IO.Compression` - Compression utilities
- `Microsoft.CSharp` - Dynamic language support

### NuGet Package Dependencies

#### Dependency Injection and IoC

##### Autofac 4.4.0
**Used In**: Nop.Core, Nop.Web, Nop.Web.Framework
**Purpose**: Inversion of Control container
**Key Features**:
- Constructor injection
- Property injection
- Module-based registration
- Lifetime scope management

**Package Details**:
```xml
<package id="Autofac" version="4.4.0" targetFramework="net451" />
```

**Latest Version**: 8.x (Current version significantly outdated)

##### Autofac.Mvc5 4.0.1
**Used In**: Nop.Core, Nop.Web, Nop.Web.Framework
**Purpose**: Autofac integration for ASP.NET MVC 5
**Provides**:
- Controller activation
- Filter injection
- Action method injection

**Package Details**:
```xml
<package id="Autofac.Mvc5" version="4.0.1" targetFramework="net451" />
```

#### Object Mapping

##### AutoMapper 5.2.0
**Used In**: Nop.Core, Nop.Services, Nop.Web
**Purpose**: Object-to-object mapping
**Use Cases**:
- Entity to ViewModel mapping
- DTO transformations
- Reducing boilerplate mapping code

**Package Details**:
```xml
<package id="AutoMapper" version="5.2.0" targetFramework="net451" />
```

**Latest Version**: 13.x (Current version significantly outdated)

#### Data Access

##### EntityFramework 6.1.3
**Used In**: Nop.Data, Tests
**Purpose**: Object-Relational Mapper (ORM)
**Features Used**:
- Code First approach
- Fluent API configuration
- Lazy loading
- Change tracking
- LINQ queries
- Migration support

**Package Details**:
```xml
<package id="EntityFramework" version="6.1.3" targetFramework="net451" />
```

**Latest Version**: 6.5.x (Minor updates available)

##### EntityFramework.SqlServerCompact 6.1.3
**Used In**: Nop.Data
**Purpose**: SQL Server Compact Edition provider for Entity Framework

**Package Details**:
```xml
<package id="EntityFramework.SqlServerCompact" version="6.1.3" targetFramework="net451" />
```

##### Microsoft.SqlServer.Compact 4.0.8876.1
**Used In**: Nop.Data
**Purpose**: SQL Server Compact Edition runtime

**Package Details**:
```xml
<package id="Microsoft.SqlServer.Compact" version="4.0.8876.1" targetFramework="net451" />
```

**Note**: SQL Server Compact is deprecated; migration to SQL Server or other databases recommended.

#### Caching

##### StackExchange.Redis.StrongName 1.2.1
**Used In**: Nop.Core
**Purpose**: Redis client for distributed caching
**Features**:
- High-performance async Redis client
- Connection multiplexing
- Pipeline support
- Pub/sub messaging
- Strong-named assembly for GAC

**Package Details**:
```xml
<package id="StackExchange.Redis.StrongName" version="1.2.1" targetFramework="net451" />
```

**Latest Version**: 2.x (Major version update available)

##### RedLock.net.StrongName 1.7.4
**Used In**: Nop.Core
**Purpose**: Distributed locking using Redis
**Features**:
- RedLock algorithm implementation
- Distributed lock acquisition
- Automatic lock renewal
- Deadlock prevention

**Package Details**:
```xml
<package id="RedLock.net.StrongName" version="1.7.4" targetFramework="net451" />
```

#### Serialization

##### Newtonsoft.Json 9.0.1
**Used In**: Nop.Core, Nop.Services, Nop.Web, Plugins
**Purpose**: JSON serialization/deserialization
**Use Cases**:
- API responses
- Configuration storage
- Ajax communication
- Data exchange

**Package Details**:
```xml
<package id="Newtonsoft.Json" version="9.0.1" targetFramework="net451" />
```

**Latest Version**: 13.x (Current version significantly outdated)
**Security Concerns**: Known vulnerabilities in 9.x, upgrade strongly recommended

#### ASP.NET MVC Framework

##### Microsoft.AspNet.Mvc 5.2.3
**Used In**: Nop.Core, Nop.Web, Nop.Web.Framework
**Purpose**: ASP.NET MVC 5 framework
**Features**:
- Model-View-Controller pattern
- Routing
- Model binding
- Validation
- Filters and attributes

**Package Details**:
```xml
<package id="Microsoft.AspNet.Mvc" version="5.2.3" targetFramework="net451" />
```

**Latest Version**: 5.3.x (Minor updates available)

##### Microsoft.AspNet.Razor 3.2.3
**Used In**: Nop.Core, Nop.Web, Nop.Web.Framework
**Purpose**: Razor view engine

**Package Details**:
```xml
<package id="Microsoft.AspNet.Razor" version="3.2.3" targetFramework="net451" />
```

##### Microsoft.AspNet.WebPages 3.2.3
**Used In**: Nop.Core, Nop.Web, Nop.Web.Framework
**Purpose**: ASP.NET Web Pages infrastructure

**Package Details**:
```xml
<package id="Microsoft.AspNet.WebPages" version="3.2.3" targetFramework="net451" />
```

##### Microsoft.Web.Infrastructure 1.0.0.0
**Used In**: Nop.Core, Nop.Web
**Purpose**: Core web infrastructure types

**Package Details**:
```xml
<package id="Microsoft.Web.Infrastructure" version="1.0.0.0" targetFramework="net451" />
```

#### Additional Web Dependencies

##### Microsoft.AspNet.WebApi (Various Projects)
**Used In**: Nop.Web, Nop.Web.Framework
**Packages**:
- Microsoft.AspNet.WebApi.Core
- Microsoft.AspNet.WebApi.Client
- Microsoft.AspNet.WebApi.WebHost
**Purpose**: Web API functionality for REST endpoints

##### Antlr 3.x
**Used In**: Nop.Web
**Purpose**: CSS and JavaScript minification

##### WebGrease
**Used In**: Nop.Web
**Purpose**: Asset optimization and bundling

#### Validation

##### FluentValidation 6.x
**Used In**: Nop.Services, Nop.Web.Framework
**Purpose**: Fluent validation rules for models
**Features**:
- Declarative validation rules
- Custom validators
- Conditional validation
- Localization support

#### Testing (Test Projects Only)

##### NUnit
**Version**: Various
**Purpose**: Unit testing framework

##### Moq
**Purpose**: Mocking framework for unit tests

### Plugin-Specific Dependencies

#### Payment Plugins

**PayPal Plugins**:
- RestSharp - REST API client
- PayPal SDK packages

#### Shipping Plugins

**UPS/FedEx/USPS**:
- SOAP web service references
- Carrier-specific SDK packages

#### External Authentication

**Facebook Plugin**:
- OAuth libraries
- Facebook SDK

#### Exchange Rate Plugins

**ECB Exchange**:
- HTTP clients for API access

### Version Summary Table

| Package | Current Version | Latest Available | Status |
|---------|----------------|------------------|---------|
| .NET Framework | 4.5.1 | 4.8 | Outdated |
| Autofac | 4.4.0 | 8.x | Major updates |
| AutoMapper | 5.2.0 | 13.x | Major updates |
| EntityFramework | 6.1.3 | 6.5.x | Minor updates |
| Newtonsoft.Json | 9.0.1 | 13.x | Security risk |
| StackExchange.Redis | 1.2.1 | 2.x | Major updates |
| ASP.NET MVC | 5.2.3 | 5.3.x | Minor updates |

## Internal Dependencies

### Project Dependency Hierarchy

```
Nop.Core (No internal dependencies)
    ↑
    ├── Nop.Data
    │       ↑
    │       └── Nop.Services
    │               ↑
    │               ├── Nop.Web.Framework
    │               │       ↑
    │               │       ├── Nop.Web
    │               │       │       ↑
    │               │       │       └── Nop.Admin
    │               │       └── Plugins
    │               └── Plugins
    └── Plugins (some)
```

### Nop.Core (Foundation Layer)

**Dependencies**: None (only external packages)
**Referenced By**: All other projects

**Provides**:
- Domain entities
- Core interfaces (IRepository, IEngine, IWorkContext)
- Infrastructure (DI, caching, events)
- Plugin system
- Common utilities

**Key Exports**:
- `BaseEntity` - Entity base class
- `IRepository<T>` - Repository pattern
- `IWorkContext` - Current context
- `IStoreContext` - Store context
- `IPlugin` - Plugin interface
- `ICacheManager` - Caching interface
- `IEventPublisher` - Event system

### Nop.Data (Data Access Layer)

**Dependencies**:
- Nop.Core
- EntityFramework 6.1.3

**Referenced By**:
- Nop.Services
- Test projects

**Provides**:
- Entity Framework DbContext
- Repository implementations
- Entity mappings (Fluent API)
- Database initialization
- Data providers (SQL Server, SQL CE)

**Key Exports**:
- `NopObjectContext` - DbContext implementation
- `EfRepository<T>` - Generic repository
- Entity mappings for all domain entities
- `IDbContext` implementation

### Nop.Services (Business Logic Layer)

**Dependencies**:
- Nop.Core
- Nop.Data

**Referenced By**:
- Nop.Web.Framework
- Nop.Web
- Nop.Admin
- Plugins
- Test projects

**Provides**:
- All business services
- Business logic implementation
- Workflow coordination
- External service integration

**Service Categories**:
- Catalog services (products, categories, manufacturers)
- Customer services (registration, authentication)
- Order services (cart, checkout, order processing)
- Payment services
- Shipping services
- Content services (blogs, news, forums)
- Security services (permissions, ACL)
- Message services (email, notifications)
- Localization services
- Configuration services

**Key Exports**:
- 100+ service interfaces and implementations
- Business logic workflows
- Domain event handlers

### Nop.Web.Framework (Web Infrastructure)

**Dependencies**:
- Nop.Core
- Nop.Services
- ASP.NET MVC 5

**Referenced By**:
- Nop.Web
- Nop.Admin
- Plugin web components

**Provides**:
- Custom MVC infrastructure
- Base controllers
- HTML helpers
- View engines
- Routing extensions
- Security filters
- Localization support
- Themes infrastructure
- UI components

**Key Exports**:
- `BasePublicController` - Public controller base
- Custom MVC filters
- HTML helpers for UI components
- Theme support
- Security attributes
- Localization helpers

### Nop.Web (Public Storefront)

**Dependencies**:
- Nop.Core
- Nop.Services
- Nop.Web.Framework

**Referenced By**:
- Nop.Admin (for shared resources)

**Provides**:
- Customer-facing web application
- Public controllers
- Public views
- Frontend assets (CSS, JavaScript, images)
- Public routing

**Key Components**:
- Controllers (Catalog, Product, Customer, Checkout, Order, etc.)
- View models
- Razor views
- Themes
- Static content

### Nop.Admin (Administration Area)

**Dependencies**:
- Nop.Core
- Nop.Services
- Nop.Web.Framework
- Nop.Web (shared resources)

**Provides**:
- Administration interface
- Admin controllers
- Admin views
- Management tools

**Key Components**:
- Admin controllers
- Admin view models
- Admin Razor views
- Management interfaces

### Plugins

**Dependencies**:
- Nop.Core (always)
- Nop.Services (usually)
- Nop.Web.Framework (for web components)
- Plugin-specific packages

**Referenced By**: None (dynamically loaded)

**Categories**:
- Payment plugins
- Shipping plugins
- Tax plugins
- Widget plugins
- Discount rules plugins
- External authentication plugins
- Exchange rate providers
- Product feeds

**Dependency Pattern**:
```
Plugin.{Category}.{Name}
    → Nop.Core (domain entities, plugin interface)
    → Nop.Services (business services)
    → Nop.Web.Framework (if has web components)
    → Plugin-specific packages
```

### Test Projects

**Test Project Dependencies**:

**Nop.Core.Tests**:
- Nop.Core
- NUnit, Moq

**Nop.Data.Tests**:
- Nop.Core
- Nop.Data
- NUnit, Moq

**Nop.Services.Tests**:
- Nop.Core
- Nop.Data
- Nop.Services
- NUnit, Moq

**Nop.Web.MVC.Tests**:
- Nop.Core
- Nop.Services
- Nop.Web.Framework
- NUnit, Moq

**Nop.Tests** (Integration):
- All main projects

## Dependency Graph

### Layer Dependency Visualization

```
┌─────────────────────────────────────────────┐
│              Presentation Layer              │
│  ┌─────────┐  ┌───────────────┐  ┌────────┐ │
│  │Nop.Admin│  │Nop.Web.Framework│  │Nop.Web│ │
│  └────┬────┘  └───────┬─────────┘  └───┬───┘ │
│       └───────────────┼────────────────┘     │
└────────────────────────┼──────────────────────┘
                         │
┌────────────────────────┼──────────────────────┐
│              Service Layer                    │
│               ┌────┴─────┐                    │
│               │Nop.Services│                  │
│               └─────┬────┘                    │
└─────────────────────┼─────────────────────────┘
                      │
┌─────────────────────┼─────────────────────────┐
│              Data Layer                       │
│                ┌────┴────┐                    │
│                │Nop.Data │                    │
│                └────┬────┘                    │
└─────────────────────┼─────────────────────────┘
                      │
┌─────────────────────┼─────────────────────────┐
│              Domain Layer                     │
│                ┌────┴────┐                    │
│                │Nop.Core │                    │
│                └─────────┘                    │
└───────────────────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        │  External Dependencies    │
        │  - .NET Framework 4.5.1   │
        │  - Autofac                │
        │  - AutoMapper             │
        │  - Entity Framework       │
        │  - ASP.NET MVC 5          │
        │  - Redis                  │
        │  - Newtonsoft.Json        │
        └───────────────────────────┘
```

### Plugin Dependency Pattern

```
┌──────────────────────────────┐
│   Plugin.Payment.PayPal      │
│                              │
│  Implements: IPaymentMethod  │
└──────────┬───────────────────┘
           │
           ├─→ Nop.Core (IPlugin, domain entities)
           ├─→ Nop.Services (IPaymentService, etc.)
           ├─→ Nop.Web.Framework (controllers, views)
           └─→ PayPal SDK (plugin-specific)
```

## Layer Dependencies

### Dependency Rules

**Rule 1: Unidirectional Dependencies**
- Dependencies flow downward only
- Lower layers never depend on higher layers
- Prevents circular dependencies

**Rule 2: Core Independence**
- Nop.Core has no internal dependencies
- Only depends on external packages
- Provides foundation for all layers

**Rule 3: Data Layer Isolation**
- Nop.Data only depends on Nop.Core
- Encapsulates all data access
- Entity Framework isolated to this layer

**Rule 4: Service Layer Business Logic**
- Nop.Services contains all business logic
- Depends on Core and Data only
- No UI dependencies

**Rule 5: Presentation Depends on All**
- Web projects depend on all lower layers
- Can use services directly
- Should not duplicate business logic

**Rule 6: Plugin Dependencies**
- Plugins depend on Core (always)
- Plugins depend on Services (usually)
- Plugins depend on Web.Framework (for UI)
- Plugins never depend on other plugins

### Cross-Cutting Concerns

**Caching**:
- Interface: Nop.Core (`ICacheManager`)
- Implementation: Nop.Core (`MemoryCacheManager`, `RedisCacheManager`)
- Usage: All layers via dependency injection

**Events**:
- Interface: Nop.Core (`IEventPublisher`, `IConsumer<T>`)
- Implementation: Nop.Services
- Publishers: Services layer
- Consumers: Services layer, plugins

**Localization**:
- Interface: Nop.Services (`ILocalizationService`)
- Usage: All presentation layer components

**Logging**:
- Interface: Nop.Services (`ILogger`)
- Usage: All layers

**Permissions**:
- Interface: Nop.Services (`IPermissionService`)
- Usage: Controllers, services

## Plugin Dependencies

### Plugin Types and Dependencies

#### Payment Plugins
**Base Dependencies**:
- Nop.Core (IPlugin, domain entities)
- Nop.Services.Payments (IPaymentMethod)
- Nop.Services.Orders (for order processing)

**Additional Dependencies** (plugin-specific):
- REST clients (RestSharp)
- Payment gateway SDKs
- Cryptography libraries

**Example: Nop.Plugin.Payments.PayPalStandard**:
```
Nop.Core
Nop.Services
Nop.Web.Framework
+ No additional packages (uses HTTP posts)
```

#### Shipping Plugins
**Base Dependencies**:
- Nop.Core
- Nop.Services.Shipping (IShippingRateComputationMethod)

**Additional Dependencies**:
- Carrier API clients
- SOAP service references

**Example: Nop.Plugin.Shipping.UPS**:
```
Nop.Core
Nop.Services
Nop.Web.Framework
+ UPS API SOAP reference
```

#### Widget Plugins
**Base Dependencies**:
- Nop.Core
- Nop.Services.Cms (IWidgetPlugin)
- Nop.Web.Framework (for views)

**Example: Nop.Plugin.Widgets.GoogleAnalytics**:
```
Nop.Core
Nop.Services
Nop.Web.Framework
+ No additional packages (JavaScript injection)
```

#### Tax Plugins
**Base Dependencies**:
- Nop.Core
- Nop.Services.Tax (ITaxProvider)

#### External Authentication Plugins
**Base Dependencies**:
- Nop.Core
- Nop.Services.Authentication.External (IExternalAuthenticationMethod)
- Nop.Services.Customers

**Additional Dependencies**:
- OAuth libraries
- Provider SDKs

### Plugin Discovery and Loading

**Discovery Process**:
1. Application scans `~/Plugins/` directory
2. Loads plugin assemblies dynamically
3. Reads `plugin.json` metadata
4. Registers with IoC container
5. Calls `Install()` method on first activation

**Dependency Resolution**:
- Plugins use same IoC container as main application
- Can inject any registered service
- Services automatically available

## Summary

### Dependency Strengths

1. **Clear Layering**: Well-defined layer boundaries
2. **Separation of Concerns**: Each layer has specific responsibility
3. **Loose Coupling**: Interface-based dependencies
4. **Testability**: Dependencies can be mocked
5. **Plugin Architecture**: Extensible without core modification
6. **Dependency Injection**: Autofac manages all dependencies

### Dependency Concerns

1. **Outdated Dependencies**: Many NuGet packages significantly outdated
2. **Security Vulnerabilities**: Newtonsoft.Json 9.0.1 has known vulnerabilities
3. **Framework Version**: .NET Framework 4.5.1 is outdated (4.8 is latest)
4. **SQL CE Deprecated**: SQL Server Compact Edition is no longer supported
5. **Major Version Gaps**: Autofac, AutoMapper, Redis client have major updates

### Recommended Dependency Updates

**Critical** (Security/Support):
- Newtonsoft.Json 9.0.1 → 13.x (security fixes)
- .NET Framework 4.5.1 → 4.8 (or migrate to .NET 6/8)

**High Priority** (Compatibility/Features):
- Autofac 4.4.0 → 8.x
- AutoMapper 5.2.0 → 13.x
- StackExchange.Redis 1.2.1 → 2.x

**Medium Priority** (Minor Updates):
- EntityFramework 6.1.3 → 6.5.x
- ASP.NET MVC 5.2.3 → 5.3.x

**Migration Consideration**:
- .NET Framework → .NET 6/8 (modern runtime)
- SQL CE → SQL Server Express/LocalDB

---

**Related Documentation:**
- [Program Structure](../reference/program-structure.md)
- [Architecture Patterns](patterns.md)
- [Technical Debt Report](../technical-debt-report.md)
