# Implementation Plan: .NET 10 Modernization of nopCommerce release-3.90

## Overview

This plan ports every in-scope project of nopCommerce release-3.90 from .NET Framework 4.5.1 (classic MSBuild, `packages.config`, ASP.NET MVC 5 / System.Web) to **.NET 10** (`net10.0`, SDK-style projects, `PackageReference`, ASP.NET Core MVC).

The plan applies the **per-project migration recipe** from the design (SDK-style `.csproj` at `net10.0` → `packages.config` to `PackageReference` → config migration → remove obsolete files → rewrite unavailable APIs → fix project references → **clean-compile gate**) in **strict leaf-first dependency order**:

```
Nop.Core → Nop.Data → Nop.Services → Nop.Web.Framework → {Nop.Web, Nop.Admin} → Plugins (×20) → Tests (×5)
```

`{Nop.Web, Nop.Admin}` denotes a **parallelizable sibling pair**: `Nop.Admin` (`src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`) does not reference `Nop.Web`, and `Nop.Web` does not reference `Nop.Admin`; both depend only on `Nop.Web.Framework` plus the three libraries (design §6). Once the `Nop.Web.Framework` gate passes, the two may proceed in either order or concurrently — but **both** must clean-compile before any plugin is migrated.

Each project's task group ends with a **clean-compile gate** (zero compiler errors) that MUST pass before any dependent group begins. `Clean_Compile` — not test execution — is the completion criterion for every stage (Req 3.3). Coverage is exhaustive across all **31 projects**: 3 libraries (`Nop.Core`, `Nop.Data`, `Nop.Services`), 3 presentation projects (`Nop.Web.Framework`, `Nop.Web`, `Nop.Admin`), all 20 plugins, and all 5 test projects.

## Tasks

- [x] 1. Solution-wide migration scaffolding
  - [x] 1.1 Establish central package management and shared build props
    - Create `src/Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and net10.0-compatible `<PackageVersion>` entries for the full dependency surface (Autofac + Autofac.Extensions.DependencyInjection, AutoMapper, Newtonsoft.Json/System.Text.Json, Microsoft.Extensions.Caching.Memory, Microsoft.Extensions.Configuration, EF Core + EF Core SqlServer, StackExchange.Redis, a maintained RedLock package, SixLabors.ImageSharp)
    - Create `src/Directory.Build.props` setting shared properties (`TargetFramework=net10.0`, `Nullable=disable`, `ImplicitUsings=disable`, `GenerateAssemblyInfo=false`) to keep per-project `.csproj` files minimal
    - _Requirements: 1.1, 1.2, 1.3, 5.2_

- [x] 2. Migrate Nop.Core (Leaf_Project)
  - [x] 2.1 Convert Nop.Core.csproj to SDK-style net10.0
    - Replace the classic `.csproj` with `<Project Sdk="Microsoft.NET.Sdk">`, `<TargetFramework>net10.0</TargetFramework>`
    - Delete explicit `<Compile Include=.../>` list (rely on implicit globbing); drop `ProjectGuid`, `TargetFrameworkVersion`, Configuration/Platform blocks, `Import *.targets`, and BCL `<Reference Include="System.*"/>` entries
    - _Requirements: 1.1, 1.2, 5.1_
  - [x] 2.2 Convert Nop.Core packages.config to PackageReference
    - Translate direct dependencies to `PackageReference` (via central versions); trim transitive entries (`Microsoft.Web.Infrastructure`, individual `System.Web.*`); advance net45-era pins to net10.0-compatible versions; delete `packages.config`
    - _Requirements: 1.3, 1.5_
  - [x] 2.3 Migrate Nop.Core configuration and remove obsolete files
    - Remove `app.config` (BCL/binding-redirect noise handled by SDK); remove `Properties/AssemblyInfo.cs` if superseded by generated assembly info; surface any genuine settings through `IConfiguration`/options
    - _Requirements: 1.4, 1.5_
  - [x] 2.4 Rewrite unavailable-API usages in Nop.Core
    - Re-base caching implementations behind existing `ICacheManager`: `MemoryCacheManager` from `System.Runtime.Caching` → `Microsoft.Extensions.Caching.Memory.IMemoryCache` (map expirations to `MemoryCacheEntryOptions`, clear-all via `CancellationChangeToken`); `PerRequestCacheManager` from `HttpContext.Current.Items` → `IHttpContextAccessor.HttpContext.Items`; keep `NopNullCache`; keep `ICacheManager` signatures stable
    - Reimplement `IWebHelper`/`WebHelper` against `Microsoft.AspNetCore.Http` `HttpContext` (via `IHttpContextAccessor`) instead of `System.Web`
    - Replace `Fakes/Fake*` System.Web seams with `DefaultHttpContext`-based abstractions or remove where superseded
    - _Requirements: 4.2, 5.1, 5.3_
  - [x] 2.5 Clean-compile gate — Nop.Core
    - Build `src/Libraries/Nop.Core/Nop.Core.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve all errors to zero before unlocking Nop.Data; verify no `System.Web*` references remain
    - _Requirements: 3.1, 3.2, 3.3_
  - [ ]* 2.6 Migrate Nop.Core.Tests to SDK-style net10.0
    - Convert `src/Tests/Nop.Core.Tests` to SDK-style net10.0, packages.config → PackageReference (test SDK + runner), `App.config` removed, `ProjectReference` → migrated Nop.Core; require clean compile
    - _Requirements: 5.6, 1.1, 1.2, 1.3, 3.1_

- [x] 3. Migrate Nop.Data (EF6 → EF Core)
  - [x] 3.1 Convert Nop.Data.csproj to SDK-style net10.0 and migrate packages
    - SDK-style conversion (`Microsoft.NET.Sdk`, `net10.0`); drop legacy globbing/GUID/BCL refs; packages.config → PackageReference; replace `EntityFramework` 6 with `Microsoft.EntityFrameworkCore` + `Microsoft.EntityFrameworkCore.SqlServer`; delete `packages.config`
    - Remove SQL CE dependencies (`EntityFramework.SqlServerCompact`, `Microsoft.SqlServer.Compact`) and delete `SqlCeDataProvider.cs` (no EF Core provider; pruned from scope)
    - Remove `app.config`; fix `ProjectReference` to migrated Nop.Core
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 5.3_
  - [x] 3.2 Port DbContext, mappings, and repository to EF Core
    - `NopObjectContext`: `System.Data.Entity.DbContext` → `Microsoft.EntityFrameworkCore.DbContext`; keep `IDbContext`, `IRepository<T>`, `EfRepository<T>` public shape so dependents compile unchanged
    - Convert fluent `Mapping/*` `EntityTypeConfiguration<T>` → `IEntityTypeConfiguration<T>` applied via `ModelBuilder.ApplyConfigurationsFromAssembly`
    - Port `DbContextExtensions`/`DataReaderExtensions` raw-SQL/stored-proc helpers to `FromSqlRaw`/`ExecuteSqlRaw`/`SqlQuery`; port `Initializers/*` to EF Core model creation (`EnsureCreated`/migrations)
    - Re-base `SqlServerDataProvider` on EF Core SqlServer
    - _Requirements: 4.2, 5.1, 5.3_
  - [x] 3.3 Clean-compile gate — Nop.Data
    - Build `src/Libraries/Nop.Data/Nop.Data.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve to zero errors before unlocking Nop.Services
    - _Requirements: 3.1, 3.2, 3.3_
  - [ ]* 3.4 Migrate Nop.Data.Tests to SDK-style net10.0
    - Convert `src/Tests/Nop.Data.Tests`; packages.config → PackageReference; remove `App.config`; `ProjectReference` → migrated Nop.Data/Nop.Core; require clean compile
    - _Requirements: 5.6, 1.1, 1.2, 1.3, 3.1_

- [x] 4. Migrate Nop.Services
  - [x] 4.1 Convert Nop.Services.csproj to SDK-style net10.0 and migrate packages
    - SDK-style conversion at `net10.0`; packages.config → PackageReference (AutoMapper advanced, Autofac advanced); delete `packages.config`; remove `app.config`; fix `ProjectReference`s to migrated Nop.Core and Nop.Data
    - Add the `SixLabors.ImageSharp` `PackageReference` and **drop the `ImageResizer` and `ImageResizer.Plugins.PrettyGifs` references** (no net10.0 release exists); drop the `System.Drawing` BCL `<Reference>` — GIF quantization is covered by ImageSharp's built-in encoder (design §7)
    - Keep the target framework as cross-platform `net10.0` (not `net10.0-windows`) now that imaging no longer depends on `System.Drawing`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 5.8, 5.10_
  - [x] 4.2 Rewrite unavailable-API usages across Nop.Services
    - Update every source file using APIs unavailable in net10.0: `ConfigurationManager` → injected `IConfiguration`/options; `HttpContext.Current` → `IHttpContextAccessor`; `System.Web`-based helpers → ASP.NET Core abstractions; AutoMapper API updates; caching against stable `ICacheManager`
    - Replace `ImageResizer` and `System.Drawing` in `Media/PictureService.cs` with SixLabors.ImageSharp: `ImageBuilder`/`ResizeSettings` → `Image.Load` + `image.Mutate(x => x.Resize(new ResizeOptions { … }))` + `image.Save(...)` with an ImageSharp encoder; `Bitmap`/`Graphics`/`InterpolationMode`/`PixelFormat` → `Image<TPixel>` + `Mutate`/`Resize` with an `IResampler` (design §7)
    - Replace `System.Drawing` usage in `ExportImport/ExportManager.cs` with SixLabors.ImageSharp equivalents
    - Preserve `IDependencyRegistrar` implementations (registration churn deferred to DI integration in Web.Framework)
    - _Requirements: 4.2, 5.1, 5.3, 5.8, 5.9, 5.11_
  - [x] 4.3 Clean-compile gate — Nop.Services
    - Build `src/Libraries/Nop.Services/Nop.Services.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve to zero errors before unlocking Nop.Web.Framework; verify no `ImageResizer` or `System.Drawing` references remain
    - _Requirements: 3.1, 3.2, 3.3_
  - [ ]* 4.4 Migrate Nop.Services.Tests to SDK-style net10.0
    - Convert `src/Tests/Nop.Services.Tests`; packages.config → PackageReference; remove `App.config`; `ProjectReference`s → migrated Nop.Services/Nop.Data/Nop.Core; require clean compile
    - _Requirements: 5.6, 1.1, 1.2, 1.3, 3.1_
  - [x] 4.5 Migrate shared Nop.Tests helper project to SDK-style net10.0
    - Convert `src/Tests/Nop.Tests` (shared test infrastructure) to SDK-style net10.0; packages.config → PackageReference; replace System.Web fakes with `DefaultHttpContext`-based fakes; fix `ProjectReference`s; require clean compile
    - _Requirements: 5.6, 5.3, 1.1, 1.2, 1.3, 3.1_

- [x] 5. Checkpoint — libraries migrated
  - Ensure Nop.Core, Nop.Data, Nop.Services all clean-compile and dependent references point only at migrated SDK-style projects. Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.3_

- [x] 6. Port Nop.Web.Framework to ASP.NET Core
  - [x] 6.1 Convert Nop.Web.Framework.csproj to SDK-style net10.0 and migrate packages
    - SDK-style conversion (`Microsoft.NET.Sdk`, with Razor/ASP.NET Core MVC framework reference as needed) at `net10.0`; packages.config → PackageReference; replace `Microsoft.AspNet.Mvc/Razor/WebPages`, `Autofac.Mvc5` with ASP.NET Core MVC + `Autofac.Extensions.DependencyInjection`; delete `packages.config`; fix `ProjectReference`s to migrated libraries
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 4.1, 4.2_
  - [x] 6.2 Port HTTP context, WebHelper, and controllers/filters
    - Replace `System.Web` `HttpContext.Current`/`HttpRequest`/`HttpResponse`/session usages with `Microsoft.AspNetCore.Http` via `IHttpContextAccessor`
    - `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`; map `ActionResult`/`JsonResult`/`RedirectResult`; port filters (`ActionFilterAttribute`, `AuthorizeAttribute`) to ASP.NET Core filters/policy-based auth
    - _Requirements: 4.1, 4.2, 4.3_
  - [x] 6.3 Port Razor infrastructure and HTML/URL helpers
    - `WebViewPage`/`@model` base → `RazorPage<TModel>`; `System.Web.Mvc.HtmlHelper` → `IHtmlHelper`, `UrlHelper` → `IUrlHelper`; `@Html.Action`/`[ChildActionOnly]` → View Components; port custom view-page extensions and templates to Tag Helpers where appropriate
    - _Requirements: 4.1, 4.4, 4.2_
  - [x] 6.4 Port routing, modules/handlers, and DI integration
    - `IRouteProvider`/`RouteCollection`/area registration → ASP.NET Core endpoint routing (`MapControllerRoute`, area conventions) over `IEndpointRouteBuilder`
    - `IHttpModule`/`IHttpHandler` (URL rewrite/SEO, culture/localization, auth, install-mode redirect) → middleware components preserving ordering
    - Integrate Autofac via `Autofac.Extensions.DependencyInjection`; remove `Autofac.Integration.Mvc` per-request plumbing (`AutofacDependencyResolver`, `RequestLifetimeScopeProvider`); keep `IEngine`/`NopEngine`/`ContainerManager` shape
    - _Requirements: 4.2, 4.5, 4.6_
  - [x] 6.5 Migrate Nop.Web.Framework configuration and remove obsolete files
    - Move settings to `IConfiguration`/typed options; remove obsolete `AssemblyInfo`/`app.config`/View `web.config` fragments owned by the framework project
    - _Requirements: 1.4, 1.5, 5.3_
  - [x] 6.6 Clean-compile gate — Nop.Web.Framework
    - Build `src/Presentation/Nop.Web.Framework/Nop.Web.Framework.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve to zero errors before unlocking **both** Nop.Web and Nop.Admin; verify no `System.Web*` references remain
    - _Requirements: 3.1, 3.2, 3.3, 4.2_

- [ ] 7. Port Nop.Web to ASP.NET Core
  - [ ] 7.1 Convert Nop.Web.csproj to SDK-style Web project at net10.0
    - Convert to `<Project Sdk="Microsoft.NET.Sdk.Web">`, `net10.0`; packages.config → PackageReference; delete `packages.config`; fix `ProjectReference`s to all migrated upstream projects
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 4.1_
  - [ ] 7.2 Replace hosting model (Global.asax → Program/Startup)
    - Replace `Global.asax`/`Global.asax.cs` and `System.Web` application events with `Program.cs` (`WebApplication` generic host) and `ConfigureServices`/`Configure`; invoke `IEngine`/`NopEngine`/`EngineContext` startup from host; wire `UseServiceProviderFactory(new AutofacServiceProviderFactory())` + `ConfigureContainer`
    - _Requirements: 4.1, 4.5, 4.6_
  - [ ] 7.3 Port Nop.Web controllers, views, routing, and pipeline
    - `System.Web.Mvc.Controller` → ASP.NET Core controllers across all Nop.Web controllers (Req 4.3)
    - Port all `.cshtml` views to ASP.NET Core Razor; replace `Views/web.config` + `_ViewStart` with `_ViewImports.cshtml`/`_ViewStart.cshtml`; port display/editor templates (Req 4.4)
    - Register routing over endpoint routing and register former modules/handlers as middleware in correct order (Req 4.6)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6_
  - [ ] 7.4 Migrate web.config to appsettings.json / IConfiguration
    - Split `web.config`: appSettings/connectionStrings/custom sections → `appsettings.json` bound via `IConfiguration`/`IOptions<T>`; `system.web`/`system.webServer` module & handler registrations → middleware in `Program.cs`; rewrite `ConfigurationManager.*` call-sites; keep only the ANCM hosting shim `web.config` if needed; load `DataSettings` via new config path
    - **Remove the `resizer` `configSections` declaration and its element** (`<section name="resizer" type="ImageResizer.ResizerSection,ImageResizer" />`) from `src/Presentation/Nop.Web/Web.config` — ImageSharp is configured in code/options, not via a config section (design §7)
    - _Requirements: 1.4, 4.6, 5.3, 5.8_
  - [ ] 7.5 Remove obsolete Nop.Web files
    - Remove `AssemblyInfo`, `Global.asax`, `RouteConfig`/`*Config` App_Start files, and `Views/web.config` files rendered obsolete by the SDK/ASP.NET Core model
    - _Requirements: 1.5_
  - [ ] 7.6 Clean-compile gate — Nop.Web
    - Build `src/Presentation/Nop.Web/Nop.Web.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve to zero errors; verify no `System.Web*` references remain. Plugins remain blocked until the Nop.Admin gate (8.8) also passes
    - _Requirements: 3.1, 3.2, 3.3, 4.2_
  - [ ] 7.7 Smoke-check the ASP.NET Core host
    - Add a minimal integration/smoke check verifying the host starts, the Autofac container builds and core services resolve, one controller route responds, one Razor view renders, one former-module middleware executes, and `appsettings.json` binds (representative, non-gating)
    - _Requirements: 4.1, 4.3, 4.4, 4.5, 4.6, 1.4_

- [ ] 8. Port Nop.Admin to ASP.NET Core
  - Largest single UI surface in the application: `src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`, **277 `.cs` files** and **325 `.cshtml` Razor views**. References Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework — and neither references nor is referenced by Nop.Web, so this group is a **parallelizable sibling of task 7** once the 6.6 gate passes (design §6).
  - **Expect the highest iteration count of any stage in this plan.** Per design §6, run this group as a repeated `build → fix → build` loop against the containerized SDK rather than attempting a single pass; step 8.8 will be re-entered many times.
  - [ ] 8.1 Convert Nop.Admin.csproj to SDK-style Web project at net10.0
    - Convert to `<Project Sdk="Microsoft.NET.Sdk.Web">` with `<TargetFramework>net10.0</TargetFramework>` (cross-platform, not `net10.0-windows`)
    - `packages.config` → `PackageReference`; delete `packages.config`; drop `Microsoft.AspNet.Mvc/Razor/WebPages` and `Autofac.Mvc5` in favour of ASP.NET Core MVC + `Autofac.Extensions.DependencyInjection`; add the `SixLabors.ImageSharp` `PackageReference` needed by 8.6
    - Drop the explicit `<Compile Include=.../>` and `<Content Include=.../>` globs (implicit globbing), plus `ProjectGuid`, `TargetFrameworkVersion`, Configuration/Platform blocks, `Import *.targets`, and BCL `<Reference Include="System.*"/>` entries including `System.Drawing`
    - Point `ProjectReference`s at the already-migrated `Nop.Core`, `Nop.Data`, `Nop.Services`, and `Nop.Web.Framework`
    - _Requirements: 5.7, 1.1, 1.2, 1.3, 1.5, 2.4, 4.11, 5.10_
  - [ ] 8.2 Replace the Admin AreaRegistration with ASP.NET Core area routing
    - Delete the legacy `AreaRegistration`-derived registration for the `Admin` area (`RegisterArea`/`AreaRegistrationContext.MapRoute`) — it has no ASP.NET Core equivalent
    - Apply `[Area("Admin")]` to admin controllers and register `MapAreaControllerRoute("areaAdmin", "Admin", "Admin/{controller=Home}/{action=Index}/{id?}")` through nopCommerce's `IRouteProvider` reimplemented over `IEndpointRouteBuilder`, preserving the existing `Admin` route prefix
    - Contribute the route registration from Nop.Admin for discovery by the Nop.Web host (no `Global.asax` declaration)
    - _Requirements: 4.10, 4.6, 4.7_
  - [ ] 8.3 Convert all Nop.Admin controllers to ASP.NET Core MVC
    - `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller` across every admin controller; map `ActionResult`/`ViewResult`/`RedirectResult`; port model binding, `TryUpdateModel`, and `ModelState` usages
    - Port grid/AJAX actions returning `JsonResult` to the ASP.NET Core `JsonResult`, supplying explicit serializer settings where the differing semantics change payload shape
    - Convert `[ChildActionOnly]` actions plus their `Html.Action(...)` call sites to **View Components**
    - Port admin permission filters and `AuthorizeAttribute` usages to ASP.NET Core filters with **policy-based authorization**
    - _Requirements: 4.8, 4.7, 4.2, 4.3, 4.5_
  - [ ] 8.4 Port all Nop.Admin Razor views to the ASP.NET Core Razor engine
    - Port all ~325 `.cshtml` files: `WebViewPage`/`@model` base → `RazorPage<TModel>`; `@Html` `HtmlHelper` → `IHtmlHelper`; `UrlHelper` → `IUrlHelper`; `@Html.Action` → View Components
    - **Delete `Areas/Admin/Views/web.config`** and any nested `Views/web.config`; add `_ViewImports.cshtml` carrying `@using`, `@inject`, and `@addTagHelper` directives
    - Re-base `_ViewStart.cshtml` and `_Layout.cshtml` on the Core view engine; port admin display/editor templates
    - _Requirements: 4.9, 4.4, 4.7_
  - [ ] 8.5 Move static admin assets to wwwroot / static-file middleware
    - Relocate the admin `Content/` and `Scripts/` trees under `wwwroot` (or expose them through the host's static-file middleware) instead of relying on the `System.Web` handler pipeline; update view asset references accordingly
    - _Requirements: 4.7, 4.6_
  - [ ] 8.6 Replace System.Drawing image processing in Nop.Admin with SixLabors.ImageSharp
    - Rewrite `Controllers/RoxyFilemanController.cs`: `System.Drawing`/`System.Drawing.Imaging` (`Bitmap`, `Graphics`, `PixelFormat`, `InterpolationMode`) → `Image<TPixel>` + `image.Mutate(x => x.Resize(new ResizeOptions { … }))` with an ImageSharp `IResampler`; `Image.GetThumbnailImageAbort` has no counterpart and is dropped
    - Replace `System.Drawing.ColorTranslator.FromHtml(...)` colour-string validation in `Controllers/ProductController.cs` and `Controllers/CheckoutAttributeController.cs` (and equivalents) with `SixLabors.ImageSharp.Color.TryParse(...)` — no imaging pipeline required
    - _Requirements: 5.8, 5.9, 5.11, 5.3, 5.10_
  - [ ] 8.7 Migrate Nop.Admin configuration and remove obsolete files
    - Move any admin-owned settings to `IConfiguration`/typed options; rewrite `ConfigurationManager.*` call-sites
    - Remove obsolete files: `Properties/AssemblyInfo.cs`, `app.config`/`web.config` fragments owned by the admin project, and any App_Start-style registration files superseded by the ASP.NET Core model
    - _Requirements: 1.4, 1.5, 5.3_
  - [ ] 8.8 Clean-compile gate — Nop.Admin
    - Build `src/Presentation/Nop.Web/Administration/Nop.Admin.csproj -c Debug` using the **containerized .NET 10 SDK command in `build-environment.md`** (the host runs Amazon Linux 2 / glibc 2.26 and cannot execute .NET 10 directly); substitute the `/workspace`-relative project path
    - Gate: **zero compiler errors** (warnings non-blocking); verify no `System.Web*`, `ImageResizer`, or `System.Drawing` references remain
    - Plugins stay blocked until both this gate and 7.6 report zero errors
    - _Requirements: 3.1, 3.2, 3.3, 4.2, 4.7_

- [ ] 9. Checkpoint — presentation ported
  - Ensure **all three** presentation projects — Nop.Web.Framework, Nop.Web, **and Nop.Admin** — clean-compile on ASP.NET Core with all System_Web_Dependencies replaced, the `Admin` area route preserved via `MapAreaControllerRoute`, and no `ImageResizer`/`System.Drawing` usage remaining. Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 3.1, 3.3, 4.1, 4.2, 4.7, 4.10, 5.8_

- [ ] 10. Migrate DiscountRules and ExchangeRate plugins
  - [ ] 10.1 Migrate Nop.Plugin.DiscountRules.CustomerRoles
    - SDK-style net10.0; packages.config → PackageReference; config migration; remove obsolete files; rewrite unavailable APIs (controllers/views/DI as applicable); fix `ProjectReference`s to migrated projects; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 10.2 Migrate Nop.Plugin.DiscountRules.HasOneProduct
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 10.3 Migrate Nop.Plugin.ExchangeRate.EcbExchange
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_

- [ ] 11. Migrate ExternalAuth and Feed plugins
  - [ ] 11.1 Migrate Nop.Plugin.ExternalAuth.Facebook
    - Apply per-project recipe (port OAuth/controllers/views to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 5.1, 5.3, 3.1_
  - [ ] 11.2 Migrate Nop.Plugin.Feed.GoogleShopping
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_

- [ ] 12. Migrate Payments plugins
  - [ ] 12.1 Migrate Nop.Plugin.Payments.CheckMoneyOrder
    - Apply per-project recipe (controllers/views/DI to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 12.2 Migrate Nop.Plugin.Payments.Manual
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 12.3 Migrate Nop.Plugin.Payments.PayPalDirect
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 12.4 Migrate Nop.Plugin.Payments.PayPalStandard
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 12.5 Migrate Nop.Plugin.Payments.PurchaseOrder
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_

- [ ] 13. Migrate Pickup plugin
  - [ ] 13.1 Migrate Nop.Plugin.Pickup.PickupInStore
    - Apply per-project recipe (controllers/views/DI to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_

- [ ] 14. Migrate Shipping plugins
  - [ ] 14.1 Migrate Nop.Plugin.Shipping.AustraliaPost
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 14.2 Migrate Nop.Plugin.Shipping.CanadaPost
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 14.3 Migrate Nop.Plugin.Shipping.Fedex
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 14.4 Migrate Nop.Plugin.Shipping.FixedOrByWeight
    - Apply per-project recipe (controllers/views to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 14.5 Migrate Nop.Plugin.Shipping.UPS
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_
  - [ ] 14.6 Migrate Nop.Plugin.Shipping.USPS
    - Apply per-project recipe; fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 5.1, 5.3, 3.1_

- [ ] 15. Migrate Tax and Widgets plugins
  - [ ] 15.1 Migrate Nop.Plugin.Tax.FixedOrByCountryStateZip
    - Apply per-project recipe (controllers/views to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 15.2 Migrate Nop.Plugin.Widgets.GoogleAnalytics
    - Apply per-project recipe (widget view components/views to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_
  - [ ] 15.3 Migrate Nop.Plugin.Widgets.NivoSlider
    - Apply per-project recipe (widget view components/views to ASP.NET Core); fix `ProjectReference`s; clean-compile
    - _Requirements: 5.5, 1.1, 1.2, 1.3, 1.4, 1.5, 2.4, 4.2, 4.3, 4.4, 5.1, 5.3, 3.1_

- [ ] 16. Checkpoint — all plugins migrated
  - Ensure all 20 plugin projects clean-compile as SDK-style net10.0 with references to migrated projects only. Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 5.2, 5.5, 3.1, 3.3, 2.4_

- [ ] 17. Migrate remaining Test projects
  - [ ] 17.1 Migrate Nop.Web.MVC.Tests to SDK-style net10.0
    - Convert `src/Tests/Nop.Web.MVC.Tests`; packages.config → PackageReference (test SDK + runner); replace System.Web-based test seams with `DefaultHttpContext`/ASP.NET Core test host; fix `ProjectReference`s to migrated Nop.Web/Nop.Web.Framework and shared Nop.Tests; require clean compile
    - _Requirements: 5.6, 1.1, 1.2, 1.3, 4.2, 5.3, 3.1_

- [ ] 18. Update solution file and verify full-solution build
  - [ ] 18.1 Update NopCommerce.sln to reference migrated projects
    - Update every project entry to the migrated SDK-style `.csproj` (including `Nop.Admin`); prune the removed SQL CE data provider / any dropped project from the solution
    - _Requirements: 5.4_
  - [ ] 18.2 Full-solution clean-compile verification
    - Build `src/NopCommerce.sln -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; require zero errors across all 31 in-scope projects (3 libraries, 3 presentation projects including Nop.Admin, all 20 plugins, all 5 test projects); verify no residual `System.Web*`, `ImageResizer`, or `System.Drawing` references and no remaining `packages.config`
    - _Requirements: 5.2, 5.4, 3.1, 3.3, 1.3, 5.8_

- [ ] 19. Final checkpoint — migration complete
  - Ensure the full solution clean-compiles on net10.0 in strict dependency order with complete coverage and no shortcuts. Ensure all tests pass, ask the user if questions arise.
  - _Requirements: 2.1, 2.2, 3.1, 3.3, 5.2, 5.4_

## Notes

- Tasks marked with `*` are optional test-migration/smoke sub-tasks and can be skipped for a faster path to a compiling application; core migration tasks are never optional. **Amendment (user decision):** tasks **4.5** (shared `Nop.Tests` helper project) and **7.7** (ASP.NET Core host smoke-check) were **promoted from optional to required** and no longer carry the `*` marker. Rationale: skipping every test-related task would mean no ported code is ever *executed* before the end of the migration, so the plan retained no executable validation at all. 4.5 provides a runnable test project (test SDK + runner) that later stages build on, and 7.7 provides the one end-to-end check that the ported host actually starts. Tasks **2.6**, **3.4** and **4.4** remain `*` optional and are **skipped**.
- The design defines **no Correctness Properties** (migration is a structural/declarative transformation), so no property-based test tasks are included; verification is by clean-compile gate, structural checks, and a few representative smoke checks.
- Every project group ends with an explicit **clean-compile gate**; a dependent group MUST NOT begin until its upstream gate reports zero errors (Req 2.1–2.3, 3.1–3.3).
- **All clean-compile gates run inside the `mcr.microsoft.com/dotnet/sdk:10.0` container documented in `build-environment.md`.** The host is Amazon Linux 2 (glibc 2.26) and .NET 10 requires glibc 2.27+, so the host `dotnet` CLI cannot build `net10.0`. Use `/workspace`-relative project paths in the containerized command.
- Tasks 7 (Nop.Web) and 8 (Nop.Admin) are **sibling groups** with no reference between them; both unlock after the 6.6 gate and may run in parallel, but **plugins require both the 7.6 and 8.8 gates to pass** (design §6).
- Task group 8 (Nop.Admin: 277 `.cs` files, 325 `.cshtml` views) is the **highest-iteration stage** — plan for a repeated `build → fix → build` loop, not a single pass.
- Each task references the specific requirement acceptance-criteria numbers it satisfies for traceability.
- SQL Server Compact has no EF Core provider and is intentionally removed from scope (design §8); SQL Server support is retained.
- `ImageResizer` and `System.Drawing` are replaced by `SixLabors.ImageSharp` across `Nop.Services` (4.1/4.2) and `Nop.Admin` (8.6), which keeps every project on cross-platform `net10.0` rather than `net10.0-windows` (design §7).

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1"] },
    { "id": 2, "tasks": ["2.2", "2.3"] },
    { "id": 3, "tasks": ["2.4"] },
    { "id": 4, "tasks": ["2.5"] },
    { "id": 5, "tasks": ["2.6", "3.1"] },
    { "id": 6, "tasks": ["3.2"] },
    { "id": 7, "tasks": ["3.3"] },
    { "id": 8, "tasks": ["3.4", "4.1"] },
    { "id": 9, "tasks": ["4.2"] },
    { "id": 10, "tasks": ["4.3"] },
    { "id": 11, "tasks": ["4.4", "4.5", "6.1"] },
    { "id": 12, "tasks": ["6.2", "6.3", "6.5"] },
    { "id": 13, "tasks": ["6.4"] },
    { "id": 14, "tasks": ["6.6"] },
    { "id": 15, "tasks": ["7.1", "8.1"] },
    { "id": 16, "tasks": ["7.2", "8.2"] },
    { "id": 17, "tasks": ["7.3", "7.4", "7.5", "8.3"] },
    { "id": 18, "tasks": ["7.6", "8.4"] },
    { "id": 19, "tasks": ["8.5", "8.6", "8.7"] },
    { "id": 20, "tasks": ["8.8"] },
    { "id": 21, "tasks": ["7.7", "10.1", "10.2", "10.3", "11.1", "11.2", "12.1", "12.2", "12.3", "12.4", "12.5", "13.1", "14.1", "14.2", "14.3", "14.4", "14.5", "14.6", "15.1", "15.2", "15.3"] },
    { "id": 22, "tasks": ["17.1"] },
    { "id": 23, "tasks": ["18.1"] },
    { "id": 24, "tasks": ["18.2"] }
  ]
}
```
