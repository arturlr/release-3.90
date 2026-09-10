# Design Document

## Overview

This design describes the exhaustive port of **nopCommerce release-3.90** from .NET Framework 4.5.1 (classic ASP.NET MVC 5, non-SDK MSBuild projects using `packages.config`/`app.config`/`web.config`) to **.NET 10** (`net10.0`, SDK-style projects, `PackageReference`, ASP.NET Core MVC).

The migration is a **declarative/procedural transformation**, not the construction of a runtime feature. Its correctness bar is defined by the requirements as a **clean compile** of each project as an SDK-style `net10.0` project — test execution is explicitly *not* the completion criterion for a stage. Accordingly, the design centers on:

- A **per-project migration recipe** applied identically to every in-scope project.
- **Strict dependency-order enforcement** (leaf → outward), so each project is migrated only after its dependencies clean-compile.
- A concrete **System.Web → ASP.NET Core** mapping for the presentation layer.
- Concrete replacement strategies for **configuration**, **NuGet packaging**, **caching**, and the **data layer**.
- A **clean-compile gate** that is checked and recorded at each stage.

The in-scope solution graph (from the workspace) is:

```
Nop.Core        (Leaf_Project — no Source_Project references)
  └─ Nop.Data           → references Nop.Core
       └─ Nop.Services  → references Nop.Core, Nop.Data
            └─ Nop.Web.Framework → references Nop.Core, Nop.Data, Nop.Services
                 ├─ Nop.Web    → references Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework
                 └─ Nop.Admin  → references Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework
                      └─ Plugins  → reference Nop.Core/Data/Services/Web.Framework
                           └─ Tests → reference the projects under test
```

`Nop.Web` and `Nop.Admin` (`src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`) are **siblings**, not parent/child: `Nop.Admin` does not reference `Nop.Web`, and `Nop.Web` does not reference `Nop.Admin`. Both depend only on `Nop.Web.Framework` plus the three libraries. Consequently, once `Nop.Web.Framework` clean-compiles, the two may be migrated **in either order, or in parallel** — neither gates the other. (In nopCommerce 3.x, `Nop.Admin` builds as the MVC `Admin` area and drops its output into `Nop.Web`'s `bin` directory; that packaging relationship is a build/deploy detail, not a compile-time project reference.) The solution contains **31 projects** in total, and both presentation projects must reach the clean-compile gate before plugins begin.

### Detected Legacy Surface (from workspace inspection)

`Nop.Core.csproj` and its `packages.config` reveal the concrete legacy dependency surface that must be replaced:

| Legacy dependency (net451) | Role | net10.0 replacement |
|---|---|---|
| `Microsoft.AspNet.Mvc` 5.2.3 (`System.Web.Mvc`) | MVC framework | ASP.NET Core MVC (`Microsoft.AspNetCore.Mvc`, in shared framework) |
| `Microsoft.AspNet.Razor` 3.2.3 (`System.Web.Razor`) | Razor engine | ASP.NET Core Razor (`Microsoft.AspNetCore.Mvc.Razor`) |
| `Microsoft.AspNet.WebPages` 3.2.3 (`System.Web.WebPages`, `System.Web.Helpers`) | Web Pages/Helpers | ASP.NET Core view features / `IHtmlHelper` |
| `Autofac` 4.4.0 | DI container | `Autofac` (net10.0) + `Autofac.Extensions.DependencyInjection` |
| `Autofac.Mvc5` 4.0.1 (`Autofac.Integration.Mvc`) | MVC5 DI integration | `Autofac.Extensions.DependencyInjection` (ASP.NET Core integration) |
| `AutoMapper` 5.2.0 | Mapping | `AutoMapper` (current, net-compatible) |
| `System.Runtime.Caching` (BCL ref) | In-memory cache | `Microsoft.Extensions.Caching.Memory` (`IMemoryCache`) |
| `System.Web` (BCL ref), `Microsoft.Web.Infrastructure` | HTTP context, modules/handlers | `Microsoft.AspNetCore.Http` (`HttpContext`), middleware |
| `System.configuration` (BCL ref) | `app.config` config | `Microsoft.Extensions.Configuration` + `appsettings.json` |
| `Newtonsoft.Json` 9.0.1 | JSON | `System.Text.Json` (preferred) or current `Newtonsoft.Json` |
| `EntityFramework` 6.1.3, `EntityFramework.SqlServerCompact`, `Microsoft.SqlServer.Compact` (Nop.Data) | ORM + SQL CE | EF Core (`Microsoft.EntityFrameworkCore`, `...SqlServer`); drop SQL CE |
| `RedLock.net.StrongName`, `StackExchange.Redis.StrongName` | Redis lock/cache | `StackExchange.Redis` + a maintained distributed-lock package |
| `ImageResizer` 4.0.5, `ImageResizer.Plugins.PrettyGifs` 4.0.5 (Nop.Services) | Image resize/thumbnail pipeline | **`SixLabors.ImageSharp`** — no .NET 10 version of ImageResizer exists (see §7) |
| `System.Drawing` (BCL ref, Nop.Services / Nop.Web / Nop.Admin) | Bitmap/Graphics imaging | **`SixLabors.ImageSharp`** — `System.Drawing.Common` is Windows-only since .NET 6 (see §7) |
| `System.Web.Fakes` (`Fakes\*` HTTP fakes) | Test seams over `HttpContext` | Abstractions over `Microsoft.AspNetCore.Http` |

## Architecture

### Migration pipeline (per project)

Every in-scope project passes through the same recipe. The recipe is applied strictly in `Migration_Order`; a project does not begin until **all** of its Source_Project dependencies have reached the clean-compile gate.

```
                 ┌─────────────────────────────────────────────┐
   next project  │ 1. Convert .csproj → SDK-style, TFM=net10.0  │
   in order ───▶ │ 2. packages.config → PackageReference        │
                 │ 3. app/web.config → appsettings.json / IConfig│
                 │ 4. Remove obsolete files (AssemblyInfo, etc.) │
                 │ 5. Rewrite unavailable-API usages             │
                 │ 6. Fix ProjectReferences to migrated deps     │
                 └───────────────────────┬─────────────────────┘
                                         ▼
                          ┌──────────────────────────┐
                          │  CLEAN-COMPILE GATE       │
                          │  dotnet build -c Debug     │
                          │  errors == 0 ?             │
                          └───────┬───────────┬────────┘
                              no  │           │ yes
                          ┌───────▼──┐   ┌────▼──────────────┐
                          │ fix errors│   │ mark stage DONE,  │
                          │ (loop 5)  │   │ unlock dependents │
                          └───────────┘   └───────────────────┘
```

### Build environment for the clean-compile gate

The clean-compile gate cannot be executed with the host `dotnet` CLI: this host is **Amazon Linux 2 (glibc 2.26)** and .NET 10 requires **glibc 2.27+**, so the locally installed .NET 10 SDK fails to load `libcoreclr.so`. All `net10.0` builds — every gate in the migration, from `Nop.Core` through the final solution build — therefore run through the **containerized `mcr.microsoft.com/dotnet/sdk:10.0` command documented in `build-environment.md` in this spec folder**, with the repository mounted at `/workspace` and project paths given in `/workspace`-relative form. The gate criterion itself is unchanged (zero compiler errors; warnings non-blocking).

### Dependency-order enforcement

- A single ordered worklist drives the migration: `[Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework, {Nop.Web, Nop.Admin}, <plugins…>, <tests…>]` (Req 2.2). The braces denote a **parallelizable pair**: `Nop.Web` and `Nop.Admin` are siblings over `Nop.Web.Framework` with no reference between them, so either order satisfies the ordering constraint and both may proceed concurrently once `Nop.Web.Framework` is `CleanCompiled`.
- Each project records a **state**: `Pending → InProgress → CleanCompiled`.
- The gate function `CanStart(project)` returns true only when every entry in `SourceProjectReferences(project)` is `CleanCompiled` (Req 2.1, 2.3). Otherwise the project is deferred.
- When a project is migrated, its `ProjectReference` items point only at already-migrated SDK-style projects (Req 2.4); no project references a not-yet-migrated dependency.
- Plugins are migrated after **both** `Nop.Web` and `Nop.Admin` because they depend on the migrated libraries and web framework and are hosted by the web/admin surface (Req 5.5); tests migrate last as they reference the projects under test (Req 5.6).

## Components and Interfaces

### 1. Project-file conversion (Req 1.1, 1.2, 2.4)

Each `Classic_Project` `.csproj` is replaced with an `SDK_Style_Project`:

```xml
<Project Sdk="Microsoft.NET.Sdk">          <!-- libraries/tests -->
<!-- or Microsoft.NET.Sdk.Web for Nop.Web -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>disable</Nullable>           <!-- keep initial diff minimal; opt-in later -->
    <ImplicitUsings>disable</ImplicitUsings>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo> <!-- only if a hand-kept AssemblyInfo remains -->
  </PropertyGroup>

  <ItemGroup>
    <!-- ProjectReferences point ONLY at already-migrated SDK-style projects -->
    <ProjectReference Include="..\Nop.Core\Nop.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- PackageReferences replace packages.config entries -->
    <PackageReference Include="Newtonsoft.Json" Version="13.*" />
  </ItemGroup>
</Project>
```

Key behaviors:
- SDK-style projects use **implicit file globbing** — the long explicit `<Compile Include=... />` list in the legacy `Nop.Core.csproj` is deleted; all `.cs` files under the project are compiled by default.
- `Web` SDK (`Microsoft.NET.Sdk.Web`) is used for `Nop.Web` and `Nop.Admin` (and web-hosting plugins if any); libraries and tests use `Microsoft.NET.Sdk`.
- `<TargetFrameworkVersion>v4.5.1</TargetFrameworkVersion>`, `ProjectGuid`, `Configuration/Platform` blocks, `Import Microsoft.CSharp.targets`, and BCL `<Reference Include="System.*" />` entries are all dropped (subsumed by the SDK).

### 2. NuGet migration: packages.config → PackageReference (Req 1.3)

Approach:
- For each `packages.config`, translate every `<package>` into a `<PackageReference>` in the new `.csproj`, then delete `packages.config` (Req 1.5).
- **Trim transitive entries**: `packages.config` lists the full transitive closure; `PackageReference` is transitive-by-default, so only *direct* dependencies are declared. (E.g., `Microsoft.Web.Infrastructure` and the individual `System.Web.*` DLL refs are dropped, not restated.)
- **Version floor to net10.0-compatible**: packages pinned to net45-era versions (AutoMapper 5.2.0, Newtonsoft 9.0.1, Autofac 4.4.0) are advanced to versions that support `net10.0`.
- **Framework-provided assemblies** (`System`, `System.Core`, `System.Xml`, `System.Data`, `Microsoft.CSharp`, `System.IO.Compression`, `System.ComponentModel.Composition`) become part of the shared framework or are added only if actually used (e.g., `System.ComponentModel.Composition` via the `System.ComponentModel.Composition` NuGet package if MEF is still used).
- **Centralization (optional but recommended):** introduce `Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` so all projects share one version per package, reducing drift across the many plugins.

### 3. Configuration migration: app.config/web.config → IConfiguration (Req 1.4, 5.3)

- **Libraries** (`Nop.Core`, `Nop.Data`, `Nop.Services`): their `app.config` files are almost entirely BCL runtime/binding-redirect noise, which the SDK handles automatically — these `app.config` files are removed. Any genuine settings (e.g., EF connection semantics, `DataSettings`) are surfaced through `IConfiguration`/options objects rather than `ConfigurationManager`.
- **`Nop.Web`**: `web.config` is split:
  - App settings, connection strings, and custom sections → **`appsettings.json`** bound via `Microsoft.Extensions.Configuration` and typed options (`IOptions<T>`).
  - `system.web`/`system.webServer` handler & module registrations → **ASP.NET Core middleware** in `Program.cs`/`Startup.cs` (see §5).
  - A minimal `web.config` may remain **only** as the IIS/ANCM hosting shim emitted for the ASP.NET Core module — it no longer carries application configuration.
- `ConfigurationManager.AppSettings[...]` / `ConfigurationManager.ConnectionStrings[...]` call-sites are rewritten to read from injected `IConfiguration` or bound options. nopCommerce's `DataSettings` (persisted settings file) continues to work but is loaded through the new configuration/startup path.

### 4. Caching abstraction: System.Runtime.Caching → Microsoft.Extensions.Caching (Req 4.2, 5.3)

nopCommerce already abstracts caching behind `ICacheManager` (`Caching\ICacheManager.cs`) with implementations `MemoryCacheManager`, `PerRequestCacheManager`, `NopNullCache`, and Redis (`RedisCacheManager`). The abstraction is preserved; the implementations are re-based:

- `MemoryCacheManager`: replace `System.Runtime.Caching.MemoryCache` / `ObjectCache` with `Microsoft.Extensions.Caching.Memory.IMemoryCache`. Cache entry options (absolute/sliding expiration, change tokens for bulk clear) map to `MemoryCacheEntryOptions`; the "clear all" behavior is implemented with a `CancellationChangeToken` linked to entries.
- `PerRequestCacheManager`: replace `HttpContext.Current.Items` with `IHttpContextAccessor.HttpContext.Items` (per-request store in ASP.NET Core).
- `RedisCacheManager` / `RedisConnectionWrapper`: move to current `StackExchange.Redis`; replace `RedLock.net.StrongName` with a maintained RedLock package (or `IDistributedLock`), keeping the same wrapper interface.
- Public `ICacheManager` signatures are held stable so `Nop.Services` and above compile unchanged against the abstraction.

### 5. System.Web → ASP.NET Core MVC port (Req 4.1–4.6)

This is the largest behavioral surface and applies to `Nop.Web.Framework`, `Nop.Web`, and `Nop.Admin` (admin-specific concerns are detailed in §6).

**Hosting model (Req 4.6):** `Global.asax`/`Global.asax.cs` and `System.Web` HTTP application events are replaced by the ASP.NET Core host — `Program.cs` (generic host + `WebApplication`) with a `Startup`-style `ConfigureServices`/`Configure` split. nopCommerce's `IEngine`/`NopEngine`/`EngineContext` startup is invoked from here instead of from `Application_Start`.

**HTTP context and helpers:**
- `HttpContext.Current`, `HttpRequest`, `HttpResponse`, `HttpSessionState` → `Microsoft.AspNetCore.Http.HttpContext` accessed via `IHttpContextAccessor`. `IWebHelper`/`WebHelper` is reimplemented against `HttpContext`/`HttpRequest` (ASP.NET Core).
- The `Fakes\Fake*` test seams (which fake `System.Web` types) are replaced with `DefaultHttpContext`-based fakes or removed where the code moves to injectable abstractions.

**Controllers (Req 4.3):** `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`.
- `ActionResult`/`ViewResult`/`JsonResult`/`RedirectResult` map to their `Microsoft.AspNetCore.Mvc` counterparts (note: ASP.NET Core `JsonResult` semantics differ; explicit serializer settings where needed).
- `[ChildActionOnly]` + `Html.Action(...)` → **View Components** or partial rendering.
- Model binding attributes, `TryUpdateModel`, `ModelState` map to ASP.NET Core equivalents.
- Filters (`ActionFilterAttribute`, `AuthorizeAttribute`) → ASP.NET Core filters/`IAsyncActionFilter`, `AuthorizeAttribute` (policy-based).

**Routing:** `RouteCollection`/`RouteConfig` and area registration → ASP.NET Core endpoint routing (`MapControllerRoute`, area conventions). nopCommerce's `IRouteProvider` pattern is reimplemented over `IEndpointRouteBuilder`.

**Views / Razor engine (Req 4.4):**
- `.cshtml` views move to the ASP.NET Core Razor engine. `WebViewPage`/`@model` base → `RazorPage<TModel>`.
- `System.Web.Mvc.HtmlHelper` (`@Html`) → `IHtmlHelper`; `UrlHelper` → `IUrlHelper`; `@Html.Action` → View Components.
- `_ViewStart.cshtml`, `_Layout.cshtml`, and `Web.config` files inside `Views/` are replaced by `_ViewImports.cshtml` (`@addTagHelper`, `@using`) and `_ViewStart.cshtml`; the `Views/web.config` is deleted.
- Custom `WebViewPage` extensions and display/editor templates are ported to the Core view base and Tag Helpers where appropriate.

**Modules & handlers (Req 4.6):** `IHttpModule`/`IHttpHandler` registrations in `web.config` → **middleware** registered in the request pipeline (`app.UseMiddleware<T>()`), preserving ordering. Examples: URL rewriting/SEO, culture/localization, authentication, install-mode redirection.

**Dependency injection (Req 4.5):**
- The existing Autofac-based container (`ContainerManager`, `IDependencyRegistrar`, `NopEngine`) is retained but integrated via **`Autofac.Extensions.DependencyInjection`**: `UseServiceProviderFactory(new AutofacServiceProviderFactory())` and `ConfigureContainer`.
- `Autofac.Integration.Mvc` (`Autofac.Mvc5`) DI plumbing (per-request lifetime via `RequestLifetimeScopeProvider`, `AutofacDependencyResolver`) is removed; ASP.NET Core's request scope + Autofac integration provide per-request lifetimes.
- Alternatively, registrations may target the built-in `IServiceCollection`; the design keeps Autofac to minimize churn in the many `IDependencyRegistrar` implementations across services and plugins.

### 6. Nop.Admin: administration UI port to ASP.NET Core MVC (Req 4.1–4.6)

`Nop.Admin` (`src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`) is the entire administration UI and the **largest single UI surface in the application**: roughly **277 `.cs` files** (controllers, models, validators, view-model factories) and **~325 `.cshtml` Razor views**. It applies the same recipe as `Nop.Web`, at substantially greater volume.

- **Project conversion:** classic `.csproj` → SDK-style `Microsoft.NET.Sdk.Web` with `<TargetFramework>net10.0</TargetFramework>`; `packages.config` → `PackageReference`; the explicit `<Compile>`/`<Content>` lists are dropped in favor of implicit globbing (Req 1.1–1.3, 1.5). `ProjectReference`s resolve to the already-migrated `Nop.Core`, `Nop.Data`, `Nop.Services`, and `Nop.Web.Framework` (Req 2.4).
- **Area registration:** the legacy `AreaRegistration`-derived registration for the **`Admin`** area (and its `RegisterArea`/`AreaRegistrationContext.MapRoute` calls) has no ASP.NET Core equivalent. It is replaced by ASP.NET Core **area conventions**: `[Area("Admin")]` on controllers plus `MapAreaControllerRoute("areaAdmin", "Admin", "Admin/{controller=Home}/{action=Index}/{id?}")` registered through nopCommerce's `IRouteProvider` reimplemented over `IEndpointRouteBuilder` (Req 4.6). Because the admin area is hosted in the `Nop.Web` pipeline, the route registration is contributed by `Nop.Admin` and discovered by the host rather than declared in `Global.asax`.
- **Controllers:** `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller` across all admin controllers, with the result types, filters, model binding, and `JsonResult` mappings described in §5. Admin-heavy patterns get particular attention: grid/AJAX actions returning `JsonResult` (serializer-setting differences), `[ChildActionOnly]` + `Html.Action` blocks → View Components, and permission filters → policy-based `AuthorizeAttribute`/ASP.NET Core filters.
- **Views:** the ~325 `.cshtml` files move to the ASP.NET Core Razor engine (`WebViewPage` → `RazorPage<TModel>`, `@Html` → `IHtmlHelper`, `UrlHelper` → `IUrlHelper`). `Areas/Admin/Views/web.config` (and any nested `Views/web.config`) is **deleted** and replaced by **`_ViewImports.cshtml`** carrying `@using`, `@inject`, and `@addTagHelper` directives; `_ViewStart.cshtml`/`_Layout.cshtml` are retained but re-based on the Core view engine (Req 4.4).
- **Static admin assets** (`Content/`, `Scripts/`) move under `wwwroot` (or are served via the static-file middleware configured by the host) instead of being served by `System.Web`'s handler pipeline.
- **Gate placement:** `Nop.Admin` must reach the **clean-compile gate before any plugin is migrated** — several plugins contribute admin-side configuration UI and are compiled against the migrated admin/web-framework surface (Req 5.5). It is a sibling of `Nop.Web`, so it neither waits on nor blocks it.
- **Effort note:** given ~277 classes and ~325 views over a single `System.Web` → ASP.NET Core boundary, this is expected to be the **highest-iteration stage** of the migration — the compile-fix loop (step 5 of the recipe) will cycle many times here. Plan the stage as a repeated `build → fix → build` loop rather than a single pass.

### 7. Image processing: ImageResizer / System.Drawing → SixLabors.ImageSharp (Req 5.3)

**Decision: replace both `ImageResizer` and `System.Drawing` with [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp), and keep the target framework as cross-platform `net10.0` — explicitly *not* `net10.0-windows`.**

Drivers:

- **`ImageResizer` 4.0.5 has no .NET 10 version.** It is a `net45`/`System.Web`-era library (referenced from `packages/ImageResizer.4.0.5/lib/net45/`) whose pipeline is built on `System.Web` handlers and `System.Drawing`. It **must** be replaced; there is no upgrade path.
- **`System.Drawing` / `System.Drawing.Common` is Windows-only since .NET 6** and throws `PlatformNotSupportedException` on non-Windows. Keeping it would force `net10.0-windows` on every project in the chain that touches imaging.

Rationale for the cross-platform choice: the `ImageResizer` code paths must be rewritten regardless of which OS target is chosen, so performing that rewrite against a cross-platform library costs little additional effort while preserving Linux and container hosting options. The application will be **tested on Windows first**, which cross-platform `net10.0` fully supports — the choice forfeits nothing in the near term.

Affected call sites (verified in the workspace):

| File | Legacy usage |
|---|---|
| `src/Libraries/Nop.Services/Media/PictureService.cs` | `using System.Drawing;` **and** `using ImageResizer;` — thumbnail generation and resize pipeline |
| `src/Libraries/Nop.Services/ExportImport/ExportManager.cs` | `using System.Drawing;` — image handling during export |
| `src/Presentation/Nop.Web/Administration/Controllers/RoxyFilemanController.cs` | `System.Drawing` + `System.Drawing.Imaging` (`Bitmap`, `Graphics`, `Image.GetThumbnailImageAbort`, `PixelFormat`, `InterpolationMode`) — file-manager crop/resize; lives in **`Nop.Admin`** |

Mapping notes:

- `ImageResizer.ImageBuilder`/`ResizeSettings` → `Image.Load` + `image.Mutate(x => x.Resize(new ResizeOptions { … }))` + `image.Save(...)` with an ImageSharp encoder.
- `Bitmap`/`Graphics`/`InterpolationMode`/`PixelFormat` → `Image<TPixel>` + `Mutate`/`Resize` with an ImageSharp `IResampler`; `Image.GetThumbnailImageAbort` has no counterpart and is dropped (ImageSharp resize needs no abort callback).
- `System.Drawing.ColorTranslator.FromHtml(...)`, used only for colour-string validation in `Nop.Admin`'s `ProductController` and `CheckoutAttributeController` (and equivalents), maps to `SixLabors.ImageSharp.Color.TryParse(...)` — no imaging pipeline required.
- **`ImageResizer.Plugins.PrettyGifs`** is referenced by `Nop.Services` (`packages/ImageResizer.Plugins.PrettyGifs.4.0.5`) for GIF quantization; its role is covered by ImageSharp's built-in GIF encoder/quantizers, and the reference is removed.
- The **`resizer` configuration section** declared in `src/Presentation/Nop.Web/Web.config` (`<section name="resizer" type="ImageResizer.ResizerSection,ImageResizer" />`) and its element are **removed** during the configuration migration (§3) — ImageSharp is configured in code/options, not via a `configSections` entry.

The legacy→net10.0 mapping table in the Overview is updated accordingly: the `ImageResizer` / `System.Drawing` rows resolve to `SixLabors.ImageSharp`.

### 8. Data layer: EF6 → EF Core considerations (Req 5.1, 5.3)

`Nop.Data` (EF6 `DbContext` `NopObjectContext`, fluent `Mapping/*`, `EfRepository`, `IDbContext`) targets net10.0 via **EF Core**:

- `System.Data.Entity.DbContext` → `Microsoft.EntityFrameworkCore.DbContext`; `DbSet<T>` and `IRepository<T>`/`EfRepository<T>` keep their public shape so `Nop.Services` compiles against the same abstractions.
- Fluent mappings (`EntityTypeConfiguration<T>` in `Mapping/*`) → EF Core `IEntityTypeConfiguration<T>` applied via `ModelBuilder.ApplyConfigurationsFromAssembly`.
- `DbContextExtensions`/`DataReaderExtensions` raw-SQL and stored-proc helpers (`ExecuteStoredProcedureList`, `SqlQuery`) → EF Core `Database.SqlQuery`/`FromSqlRaw`/`ExecuteSqlRaw`.
- Provider change: `SqlServerDataProvider` → `Microsoft.EntityFrameworkCore.SqlServer`. **SQL Server Compact** (`EntityFramework.SqlServerCompact`, `Microsoft.SqlServer.Compact`, `SqlCeDataProvider`) has no EF Core provider and is **removed**; the SQL CE data provider is dropped from scope (SQL Server remains).
- EF6 initializers (`Initializers/*`, `CreateTablesIfNotExist`) → EF Core model creation via migrations or `EnsureCreated` in the installation path.
- Note: The requirement bar is **clean compile** (Req 3.3). The design ports the API surface so dependents compile; behavioral parity of queries/migrations is validated later by integration/smoke tests, not required for stage completion.

### 9. Solution file update (Req 5.4)

`NopCommerce.sln` is updated so every project entry references the migrated SDK-style `.csproj`. Project GUIDs that the SDK no longer requires internally are kept in the `.sln` (the solution still tracks projects by GUID); removed/renamed projects (e.g., a dropped SQL CE provider) are pruned from the solution. After the last stage, a full `dotnet build NopCommerce.sln` resolves all migrated projects.

## Data Models

This migration introduces no new domain data models; the nopCommerce domain entities under `Nop.Core/Domain/**` are preserved. The design does introduce/repurpose these **process-level** models to drive and verify the migration:

```
MigrationUnit
  ProjectName        : string        // e.g. "Nop.Services"
  ProjectPath        : path          // path to .csproj
  Kind               : enum { Library, WebFramework, Web, Admin, Plugin, Test }
  SourceRefs         : string[]      // referenced Source_Projects
  State              : enum { Pending, InProgress, CleanCompiled }

MigrationOrder       : ordered list of MigrationUnit
                       [Nop.Core, Nop.Data, Nop.Services,
                        Nop.Web.Framework, {Nop.Web, Nop.Admin},
                        <plugins…>, <tests…>]
                       // {…} = parallelizable siblings, no mutual reference

CleanCompileResult
  ProjectName        : string
  ErrorCount         : int           // gate passes iff ErrorCount == 0
  WarningCount       : int           // recorded, non-blocking
  Diagnostics        : string[]      // compiler messages when ErrorCount > 0
```

## Error Handling

- **Compile-error handling (Req 3.1, 3.2):** When the clean-compile gate reports `ErrorCount > 0`, the project stays `InProgress`; the migration loops on step 5 (rewrite unavailable-API usages / fix references) until `ErrorCount == 0`. No dependent project is started while an upstream dependency has errors.
- **Unavailable API with no direct counterpart (Req 5.3):** the usage is replaced with the supported net10.0 API documented in the mapping table (e.g., `MemoryCache` → `IMemoryCache`, `HttpContext.Current` → `IHttpContextAccessor`). If a subsystem has no equivalent at all (SQL CE), it is removed from scope with the dependency pruned rather than stubbed.
- **Transitive/version conflicts (Req 1.3):** resolved by advancing package versions to net10.0-compatible releases and (optionally) centralizing versions in `Directory.Packages.props`; restore failures block the gate exactly like compile errors.
- **Reference-order violations (Req 2.3, 2.4):** prevented structurally — the gate refuses to start a project whose Source_Project references are not yet `CleanCompiled`, so a dependent can never be built against an unmigrated dependency.

## Testing Strategy

Per the requirements, **Clean_Compile is the completion criterion for each stage (Req 3.3)** — passing tests is not the bar. The verification approach is therefore centered on build verification, with structural checks and a small number of smoke/integration checks for the behavioral ports:

- **Clean-compile gate (primary, Req 3.1–3.3, 5.1, 5.2):** at each stage run the project build (`dotnet build <project> -c Debug`) and require zero compiler errors before unlocking dependents. The final solution build (`dotnet build NopCommerce.sln`) must compile every in-scope project (Req 5.2, 5.4). All of these builds execute inside the .NET 10 SDK container described in `build-environment.md` — the host (Amazon Linux 2, glibc 2.26) cannot run .NET 10 directly.
- **Structural assertions (Req 1.1–1.5, 2.4, 4.2, 5.4–5.6):** verify each in-scope `.csproj` is SDK-style with `<TargetFramework>net10.0</TargetFramework>`; verify `packages.config` files are removed and dependencies expressed as `PackageReference`; verify no `System.Web*` references remain; verify `ProjectReference`s and the `.sln` point at migrated projects.
- **Smoke / integration checks (Req 4.1, 4.3–4.6, 1.4):** after `Nop.Web` compiles, verify the ASP.NET Core host **starts**, the Autofac container **builds** and core services resolve, a representative controller route responds, a representative Razor view renders, the middleware pipeline (former modules/handlers) executes, and configuration binds from `appsettings.json`. After `Nop.Admin` compiles, additionally verify one **`Admin` area route** resolves via `MapAreaControllerRoute` and one admin Razor view renders. These are representative (1–3 cases each), not exhaustive, and are not gating for stage completion.
- **Existing unit/integration tests (Req 5.6):** test projects are migrated to SDK-style net10.0 and must **compile cleanly**; running them and achieving green is a follow-on activity beyond the defined stage bar.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — a formal, machine-verifiable statement about what the system should do.*

**No property-based correctness properties are defined for this feature.**

Rationale (from the acceptance-criteria prework): every acceptance criterion in this spec is a **structural transformation** of project/configuration files verified by inspection plus clean compile (Requirements 1.1–1.5, 2.4, 4.2, 5.1–5.2, 5.4–5.6), a **process/ordering gate** verified by audit and the build gate (Requirements 2.1–2.3, 3.1–3.3), or a **behavioral port** verified by clean compile plus a few representative startup/routing/render/pipeline smoke tests (Requirements 4.1, 4.3–4.6, 1.4). None of them can be expressed as "for all inputs X in a large domain, property P(X) holds" over shipped runtime logic — the deliverable is a migration whose correctness bar is the clean compile, not an input-driven algorithm with universal invariants. This matches the guidance that migration/IaC-style declarative work is not amenable to property-based testing and should instead be validated by build success, structural/snapshot checks, and integration/smoke tests (as detailed in the Testing Strategy above).
