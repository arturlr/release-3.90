# Task 0: Create .NET Standard 2.0 Foundation Projects (Nop.Core) - Progress Report

## Status: IN PROGRESS

## Completed Steps:

### 1. Created New Project File
- Created `Nop.Core.NetStandard.csproj` targeting .NET Standard 2.0
- Updated NuGet packages to PackageReference format
- Selected compatible package versions:
  - Autofac 6.5.0
  - AutoMapper 10.1.1 (compatible with netstandard2.0)
  - Newtonsoft.Json 13.0.3
  - StackExchange.Redis 2.6.122
  - Microsoft.Extensions.Caching.Memory 8.0.1

### 2. Created HTTP Abstraction Layer
- Created `Http/IHttpContextAccessor.cs` with interfaces:
  - `IHttpContextAccessor` - replaces direct System.Web.HttpContext access
  - `IHttpContext` - HTTP context abstraction
  - `IHttpRequest` - HTTP request abstraction
  - `IHttpResponse` - HTTP response abstraction

## Remaining Work:

### 3. Files Requiring Migration/Exclusion

**Files to Exclude (System.Web dependent):**
- `Fakes/**` - All fake HTTP context classes
- `WebHelper.cs` - Uses System.Web.HttpContext extensively
- `Infrastructure/WebAppTypeFinder.cs` - Uses System.Web.Hosting.HostingEnvironment
- `Infrastructure/NopStarter.cs` - Uses PreApplicationStartMethod
- `Plugins/PluginManager.cs` - Uses BuildManager and shadow copying

**Files Requiring Modification:**
- `CommonHelper.cs` - Remove AspNetHostingPermissionLevel usage
- `Caching/MemoryCacheManager.cs` - Migrate from System.Runtime.Caching to Microsoft.Extensions.Caching.Memory
- `Caching/PerRequestCacheManager.cs` - Replace HttpContextBase with IHttpContext
- `Caching/RedisConnectionWrapper.cs` - Add RedLock.net package
- `Infrastructure/NopEngine.cs` - Remove System.Web.Mvc dependencies
- `Infrastructure/DependencyManagement/ContainerManager.cs` - Update Autofac.Integration references
- `Configuration/NopConfig.cs` - Remove IConfigurationSectionHandler, use IConfiguration
- `IWebHelper.cs` - Replace HttpRequest parameter with abstraction

### 4. Additional Packages Needed
```xml
<PackageReference Include="RedLock.net" Version="2.3.2" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
```

### 5. Assembly Info Conflict
- Need to disable auto-generated assembly attributes:
```xml
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
```

## Next Steps:

1. Update project file with all exclusions and additional packages
2. Create .NET Standard compatible versions of:
   - `MemoryCacheManager.cs` using Microsoft.Extensions.Caching.Memory
   - `PerRequestCacheManager.cs` using IHttpContextAccessor
   - `CommonHelper.cs` without AspNetHostingPermissionLevel
3. Create stub implementations for excluded infrastructure files
4. Verify compilation
5. Create unit tests to validate domain entities work correctly

## Estimated Completion Time:
- 4-6 hours for full migration of Nop.Core to .NET Standard 2.0
- This is foundational work that enables all subsequent tasks

## Demo Criteria:
✅ Project file created
✅ HTTP abstractions created
⏳ Project compiles successfully
⏳ Domain entities accessible
⏳ Can be referenced by both .NET Framework and .NET Core projects
