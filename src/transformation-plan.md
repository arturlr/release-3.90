# Transformation Plan: NopCommerce

- **Solution Path**: /tmp/dotnet_migration_orchestrator/workdir/54f6bba9-f62f-4670-97f3-477b030d8f5b_extracted/sourceCode/NopCommerce.sln
- **Created**: 2026-05-08T19:59:22Z
- **Default Target Framework**: net10.0

## Projects

### 1. Nop.Core

- **Path**: Libraries/Nop.Core/Nop.Core.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Core from v4.5.1. 3 incompatible NuGet package(s) require replacement. 10 NuGet reference(s) to review.
- **Estimated Changes**: 19
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- None

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| Autofac.Mvc5 | 4.0.1 | N/A | yes |
| AutoMapper | 5.2.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| RedLock.net.StrongName | 1.7.4 | N/A | yes |
| StackExchange.Redis.StrongName | 1.2.1 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 2. Nop.Data

- **Path**: Libraries/Nop.Data/Nop.Data.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: medium
- **Summary**: Migration of Nop.Data from v4.5.1. 3 NuGet reference(s) to review. 1 project dependency/dependencies.
- **Estimated Changes**: 4
- **Blocking Issues**:
  - None
- **Notes**: Complexity raised from low to medium (framework floor for v4.5.1)

#### Dependencies

- Nop.Core

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| EntityFramework | 6.1.3 | N/A | yes |
| EntityFramework.SqlServerCompact | 6.1.3 | N/A | yes |
| Microsoft.SqlServer.Compact | 4.0.8876.1 | N/A | yes |

#### Migration Risks

- None

### 3. Nop.Services

- **Path**: Libraries/Nop.Services/Nop.Services.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Services from v4.5.1. 3 incompatible NuGet package(s) require replacement. 28 NuGet reference(s) to review. 2 project dependency/dependencies.
- **Estimated Changes**: 39
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: High NuGet dependency count (28)

#### Dependencies

- Nop.Core
- Nop.Data

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| EPPlus | 4.1.0 | N/A | yes |
| ImageResizer | 4.0.5 | N/A | yes |
| ImageResizer.Plugins.PrettyGifs | 4.0.5 | N/A | yes |
| iTextSharp | 5.5.10 | N/A | yes |
| MaxMind.Db | 2.1.3 | N/A | yes |
| MaxMind.GeoIP2 | 2.7.2 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Azure.KeyVault.Core | 2.0.4 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Data.Edm | 5.8.2 | N/A | yes |
| Microsoft.Data.OData | 5.8.2 | N/A | yes |
| Microsoft.Data.Services.Client | 5.8.2 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| System.ComponentModel.EventBasedAsync | 4.3.0 | N/A | yes |
| System.Dynamic.Runtime | 4.3.0 | N/A | yes |
| System.Linq.Dynamic | 1.0.7 | N/A | yes |
| System.Linq.Queryable | 4.3.0 | N/A | yes |
| System.Net.Http | 4.3.1 | N/A | yes |
| System.Net.Requests | 4.3.0 | N/A | yes |
| System.Spatial | 5.8.2 | N/A | yes |
| WindowsAzure.Storage | 8.1.1 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 4. Nop.Web.Framework

- **Path**: Presentation/Nop.Web.Framework/Nop.Web.Framework.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Web.Framework from v4.5.1. 4 incompatible NuGet package(s) require replacement. 18 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 33
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (18)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Antlr | 3.5.0.2 | N/A | yes |
| Autofac | 4.4.0 | N/A | yes |
| Autofac.Mvc5 | 4.0.1 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| FluentValidation | 6.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| System.Linq.Dynamic | 1.0.7 | N/A | yes |
| WebActivator | 1.5.2 | N/A | yes |
| WebGrease | 1.6.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 5. Nop.Web

- **Path**: Presentation/Nop.Web/Nop.Web.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Web from v4.5.1. 11 incompatible NuGet package(s) require replacement. 43 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 80
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
  - Incompatible NuGet package: Microsoft.AspNet.WebApi
  - Incompatible NuGet package: Microsoft.AspNet.WebApi.Client
  - Incompatible NuGet package: Microsoft.AspNet.WebApi.Core
  - Incompatible NuGet package: Microsoft.AspNet.WebApi.Owin
  - Incompatible NuGet package: Microsoft.AspNet.WebApi.WebHost
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
  - Incompatible NuGet package: Microsoft.Owin
  - Incompatible NuGet package: Microsoft.Owin.Host.SystemWeb
- **Notes**: High NuGet dependency count (43)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Antlr | 3.5.0.2 | N/A | yes |
| Autofac | 4.4.0 | N/A | yes |
| Autofac.Mvc5 | 4.0.1 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| EntityFramework.SqlServerCompact | 6.1.3 | N/A | yes |
| FluentValidation | 6.4.0 | N/A | yes |
| FluentValidation.MVC5 | 6.4.0 | N/A | yes |
| MaxMind.Db | 2.1.3 | N/A | yes |
| MaxMind.GeoIP2 | 2.7.2 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | N/A | yes |
| Microsoft.AspNet.WebApi | 5.2.3 | N/A | yes |
| Microsoft.AspNet.WebApi.Client | 5.2.3 | N/A | yes |
| Microsoft.AspNet.WebApi.Core | 5.2.3 | N/A | yes |
| Microsoft.AspNet.WebApi.Owin | 5.2.3 | N/A | yes |
| Microsoft.AspNet.WebApi.WebHost | 5.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Data.Edm | 5.8.2 | N/A | yes |
| Microsoft.Data.OData | 5.8.2 | N/A | yes |
| Microsoft.Data.Services.Client | 5.8.2 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Owin | 3.0.1 | N/A | yes |
| Microsoft.Owin.Host.SystemWeb | 3.0.1 | N/A | yes |
| Microsoft.SqlServer.Compact | 4.0.8876.1 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Microsoft.Web.RedisSessionStateProvider | 2.2.3 | N/A | yes |
| MiniProfiler | 3.2.0.157 | N/A | yes |
| MiniProfiler.MVC4 | 3.0.11 | N/A | yes |
| Mvc2Futures | 2.0.50217.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| Owin | 1.0 | N/A | yes |
| StackExchange.Redis.StrongName | 1.2.1 | N/A | yes |
| System.ComponentModel.EventBasedAsync | 4.3.0 | N/A | yes |
| System.Dynamic.Runtime | 4.3.0 | N/A | yes |
| System.Linq.Queryable | 4.3.0 | N/A | yes |
| System.Net.Http | 4.3.1 | N/A | yes |
| System.Net.Requests | 4.3.0 | N/A | yes |
| System.Spatial | 5.8.2 | N/A | yes |
| WebGrease | 1.6.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
- Incompatible NuGet package: Microsoft.AspNet.WebApi
- Incompatible NuGet package: Microsoft.AspNet.WebApi.Client
- Incompatible NuGet package: Microsoft.AspNet.WebApi.Core
- Incompatible NuGet package: Microsoft.AspNet.WebApi.Owin
- Incompatible NuGet package: Microsoft.AspNet.WebApi.WebHost
- Incompatible NuGet package: Microsoft.AspNet.WebPages
- Incompatible NuGet package: Microsoft.Owin
- Incompatible NuGet package: Microsoft.Owin.Host.SystemWeb

### 6. Nop.Admin

- **Path**: Presentation/Nop.Web/Administration/Nop.Admin.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Admin from v4.5.1. 4 incompatible NuGet package(s) require replacement. 15 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 31
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (15)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Antlr | 3.5.0.2 | N/A | yes |
| Autofac | 4.4.0 | N/A | yes |
| AutoMapper | 5.2.0 | N/A | yes |
| FluentValidation | 6.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| WebGrease | 1.6.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.Web.Optimization
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 7. Nop.Tests

- **Path**: Tests/Nop.Tests/Nop.Tests.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: medium
- **Summary**: Migration of Nop.Tests from v4.5.1. 2 NuGet reference(s) to review. 1 project dependency/dependencies.
- **Estimated Changes**: 3
- **Blocking Issues**:
  - None
- **Notes**: Complexity raised from low to medium (framework floor for v4.5.1)

#### Dependencies

- Nop.Core

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| NUnit | 3.6.1 | N/A | yes |
| RhinoMocks | 3.6.1 | N/A | yes |

#### Migration Risks

- None

### 8. Nop.Data.Tests

- **Path**: Tests/Nop.Data.Tests/Nop.Data.Tests.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: medium
- **Summary**: Migration of Nop.Data.Tests from v4.5.1. 4 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 7
- **Blocking Issues**:
  - None
- **Notes**: Complexity raised from low to medium (framework floor for v4.5.1)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Tests

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| EntityFramework | 6.1.3 | N/A | yes |
| EntityFramework.SqlServerCompact | 6.1.3 | N/A | yes |
| Microsoft.SqlServer.Compact | 4.0.8876.1 | N/A | yes |
| NUnit | 3.6.1 | N/A | yes |

#### Migration Risks

- None

### 9. Nop.Services.Tests

- **Path**: Tests/Nop.Services.Tests/Nop.Services.Tests.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Services.Tests from v4.5.1. 3 incompatible NuGet package(s) require replacement. 13 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 26
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (13)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Tests

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| Autofac.Mvc5 | 4.0.1 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| NUnit | 3.6.1 | N/A | yes |
| RhinoMocks | 3.6.1 | N/A | yes |
| WebActivator | 1.5.2 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 10. Nop.Core.Tests

- **Path**: Tests/Nop.Core.Tests/Nop.Core.Tests.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Core.Tests from v4.5.1. 3 incompatible NuGet package(s) require replacement. 7 NuGet reference(s) to review. 2 project dependency/dependencies.
- **Estimated Changes**: 18
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Tests

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| NUnit | 3.6.1 | N/A | yes |
| RhinoMocks | 3.6.1 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 11. Nop.Web.MVC.Tests

- **Path**: Tests/Nop.Web.MVC.Tests/Nop.Web.MVC.Tests.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Web.MVC.Tests from v4.5.1. 3 incompatible NuGet package(s) require replacement. 13 NuGet reference(s) to review. 7 project dependency/dependencies.
- **Estimated Changes**: 29
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (13); Moderate project dependency fan-out (7)

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework
- Nop.Admin
- Nop.Web
- Nop.Tests

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| AutoMapper | 5.2.0 | N/A | yes |
| FluentValidation | 6.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| NUnit | 3.6.1 | N/A | yes |
| RhinoMocks | 3.6.1 | N/A | yes |
| WebActivator | 1.5.2 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 12. Nop.Plugin.Shipping.AustraliaPost

- **Path**: Plugins/Nop.Plugin.Shipping.AustraliaPost/Nop.Plugin.Shipping.AustraliaPost.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.AustraliaPost from v4.5.1. 3 incompatible NuGet package(s) require replacement. 9 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 21
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 13. Nop.Plugin.DiscountRules.CustomerRoles

- **Path**: Plugins/Nop.Plugin.DiscountRules.CustomerRoles/Nop.Plugin.DiscountRules.CustomerRoles.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.DiscountRules.CustomerRoles from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 14. Nop.Plugin.Payments.Manual

- **Path**: Plugins/Nop.Plugin.Payments.Manual/Nop.Plugin.Payments.Manual.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Payments.Manual from v4.5.1. 3 incompatible NuGet package(s) require replacement. 9 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 21
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| FluentValidation | 6.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 15. Nop.Plugin.DiscountRules.HasOneProduct

- **Path**: Plugins/Nop.Plugin.DiscountRules.HasOneProduct/Nop.Plugin.DiscountRules.HasOneProduct.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.DiscountRules.HasOneProduct from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 16. Nop.Plugin.Shipping.CanadaPost

- **Path**: Plugins/Nop.Plugin.Shipping.CanadaPost/Nop.Plugin.Shipping.CanadaPost.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.CanadaPost from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 17. Nop.Plugin.Shipping.USPS

- **Path**: Plugins/Nop.Plugin.Shipping.USPS/Nop.Plugin.Shipping.USPS.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.USPS from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 18. Nop.Plugin.Shipping.UPS

- **Path**: Plugins/Nop.Plugin.Shipping.UPS/Nop.Plugin.Shipping.UPS.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.UPS from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 19. Nop.Plugin.Shipping.Fedex

- **Path**: Plugins/Nop.Plugin.Shipping.Fedex/Nop.Plugin.Shipping.Fedex.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.Fedex from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 20. Nop.Plugin.Payments.CheckMoneyOrder

- **Path**: Plugins/Nop.Plugin.Payments.CheckMoneyOrder/Nop.Plugin.Payments.CheckMoneyOrder.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Payments.CheckMoneyOrder from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 21. Nop.Plugin.Payments.PurchaseOrder

- **Path**: Plugins/Nop.Plugin.Payments.PurchaseOrder/Nop.Plugin.Payments.PurchaseOrder.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Payments.PurchaseOrder from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 22. Nop.Plugin.Payments.PayPalStandard

- **Path**: Plugins/Nop.Plugin.Payments.PayPalStandard/Nop.Plugin.Payments.PayPalStandard.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Payments.PayPalStandard from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 23. Nop.Plugin.Payments.PayPalDirect

- **Path**: Plugins/Nop.Plugin.Payments.PayPalDirect/Nop.Plugin.Payments.PayPalDirect.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Payments.PayPalDirect from v4.5.1. 3 incompatible NuGet package(s) require replacement. 11 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 23
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (11)

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| FluentValidation | 6.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |
| PayPal | 1.8.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 24. Nop.Plugin.Widgets.GoogleAnalytics

- **Path**: Plugins/Nop.Plugin.Widgets.GoogleAnalytics/Nop.Plugin.Widgets.GoogleAnalytics.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Widgets.GoogleAnalytics from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 20
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 25. Nop.Plugin.ExternalAuth.Facebook

- **Path**: Plugins/Nop.Plugin.ExternalAuth.Facebook/Nop.Plugin.ExternalAuth.Facebook.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.ExternalAuth.Facebook from v4.5.1. 3 incompatible NuGet package(s) require replacement. 16 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 28
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: Moderate NuGet dependency count (16)

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| DotNetOpenAuth.AspNet | 4.3.4.13329 | N/A | yes |
| DotNetOpenAuth.Core | 4.3.4.13329 | N/A | yes |
| DotNetOpenAuth.OAuth.Consumer | 4.3.4.13329 | N/A | yes |
| DotNetOpenAuth.OAuth.Core | 4.3.4.13329 | N/A | yes |
| DotNetOpenAuth.OpenId.Core | 4.3.4.13329 | N/A | yes |
| DotNetOpenAuth.OpenId.RelyingParty | 4.3.4.13329 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |
| Newtonsoft.Json | 9.0.1 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 26. Nop.Plugin.Widgets.NivoSlider

- **Path**: Plugins/Nop.Plugin.Widgets.NivoSlider/Nop.Plugin.Widgets.NivoSlider.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Widgets.NivoSlider from v4.5.1. 3 incompatible NuGet package(s) require replacement. 9 NuGet reference(s) to review. 3 project dependency/dependencies.
- **Estimated Changes**: 21
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 27. Nop.Plugin.ExchangeRate.EcbExchange

- **Path**: Plugins/Nop.Plugin.ExchangeRate.EcbExchange/Nop.Plugin.ExchangeRate.EcbExchange.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.ExchangeRate.EcbExchange from v4.5.1. 3 incompatible NuGet package(s) require replacement. 8 NuGet reference(s) to review. 2 project dependency/dependencies.
- **Estimated Changes**: 19
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Services

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 28. Nop.Plugin.Pickup.PickupInStore

- **Path**: Plugins/Nop.Plugin.Pickup.PickupInStore/Nop.Plugin.Pickup.PickupInStore.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Pickup.PickupInStore from v4.5.1. 3 incompatible NuGet package(s) require replacement. 10 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 23
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 29. Nop.Plugin.Shipping.FixedOrByWeight

- **Path**: Plugins/Nop.Plugin.Shipping.FixedOrByWeight/Nop.Plugin.Shipping.FixedOrByWeight.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Shipping.FixedOrByWeight from v4.5.1. 3 incompatible NuGet package(s) require replacement. 10 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 23
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 30. Nop.Plugin.Feed.GoogleShopping

- **Path**: Plugins/Nop.Plugin.Feed.GoogleShopping/Nop.Plugin.Feed.GoogleShopping.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Feed.GoogleShopping from v4.5.1. 3 incompatible NuGet package(s) require replacement. 10 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 23
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages

### 31. Nop.Plugin.Tax.FixedOrByCountryStateZip

- **Path**: Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/Nop.Plugin.Tax.FixedOrByCountryStateZip.csproj
- **Current Framework**: v4.5.1
- **Target Framework**: net10.0

#### Migration Overview

- **Complexity**: critical
- **Summary**: Migration of Nop.Plugin.Tax.FixedOrByCountryStateZip from v4.5.1. 3 incompatible NuGet package(s) require replacement. 10 NuGet reference(s) to review. 4 project dependency/dependencies.
- **Estimated Changes**: 23
- **Blocking Issues**:
  - Incompatible NuGet package: Microsoft.AspNet.Mvc
  - Incompatible NuGet package: Microsoft.AspNet.Razor
  - Incompatible NuGet package: Microsoft.AspNet.WebPages
- **Notes**: No special concerns identified.

#### Dependencies

- Nop.Core
- Nop.Data
- Nop.Services
- Nop.Web.Framework

#### NuGet Packages

| Package | Current Version | Recommended Version | Compatible |
|---------|----------------|--------------------:|:----------:|
| Autofac | 4.4.0 | N/A | yes |
| EntityFramework | 6.1.3 | N/A | yes |
| Microsoft.AspNet.Mvc | 5.2.3 | N/A | yes |
| Microsoft.AspNet.Razor | 3.2.3 | N/A | yes |
| Microsoft.AspNet.WebPages | 3.2.3 | N/A | yes |
| Microsoft.Bcl | 1.1.10 | N/A | yes |
| Microsoft.Bcl.Async | 1.0.168 | N/A | yes |
| Microsoft.Bcl.Build | 1.0.21 | N/A | yes |
| Microsoft.Net.Http | 2.2.29 | N/A | yes |
| Microsoft.Web.Infrastructure | 1.0.0.0 | N/A | yes |

#### Migration Risks

- Incompatible NuGet package: Microsoft.AspNet.Mvc
- Incompatible NuGet package: Microsoft.AspNet.Razor
- Incompatible NuGet package: Microsoft.AspNet.WebPages
