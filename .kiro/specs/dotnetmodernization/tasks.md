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
  - [x] 7.1 Convert Nop.Web.csproj to SDK-style Web project at net10.0
    - Convert to `<Project Sdk="Microsoft.NET.Sdk.Web">`, `net10.0`; packages.config → PackageReference; delete `packages.config`; fix `ProjectReference`s to all migrated upstream projects
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 4.1_
  - [x] 7.2 Replace hosting model (Global.asax → Program/Startup)
    - Replace `Global.asax`/`Global.asax.cs` and `System.Web` application events with `Program.cs` (`WebApplication` generic host) and `ConfigureServices`/`Configure`; invoke `IEngine`/`NopEngine`/`EngineContext` startup from host; wire `UseServiceProviderFactory(new AutofacServiceProviderFactory())` + `ConfigureContainer`
    - _Requirements: 4.1, 4.5, 4.6_
  - [x] 7.3 Port Nop.Web controllers, views, routing, and pipeline
    - `System.Web.Mvc.Controller` → ASP.NET Core controllers across all Nop.Web controllers (Req 4.3)
    - Port all `.cshtml` views to ASP.NET Core Razor; replace `Views/web.config` + `_ViewStart` with `_ViewImports.cshtml`/`_ViewStart.cshtml`; port display/editor templates (Req 4.4)
    - Register routing over endpoint routing and register former modules/handlers as middleware in correct order (Req 4.6)
    - **RESULT: Nop.Web reached 0 errors** (from the 1110 baseline) — the 7.6 gate criterion is met. 15 warnings remain, all pre-existing (10 upstream `SYSLIB*` in Nop.Core/Nop.Services, 5 `CS0618` on FluentValidation 7.x's obsolete `Custom(...)` in unmodified `Validators/` files). See runtime-deferrals.md §27–§30.
    - **SCOPE ADDITIONS forced by the port:** a `Html.Action` child-action bridge (`Extensions/ChildActionExtensions.cs`) — unavoidable because five call sites name the controller/action from plugin data at runtime; `Components/WidgetViewComponent.cs` (closes deferral 32); `Views/_ViewImports.cshtml`; `Extensions/SessionExtensions.cs`; `Extensions/ViewCompatibilityExtensions.cs`. `Views/Widget/WidgetsByZone.cshtml` moved to `Views/Shared/Components/Widget/Default.cshtml`.
    - **CORRECTION to an upstream instruction:** the recommended `.WithOrder(1000)` on `GenericUrlRouteProvider`'s seven name-only `{SeName}` routes is *backwards* — a live probe showed it **creates** the `AmbiguousMatchException` it was meant to prevent. `SuppressMatchingMetadata` is used instead. See runtime-deferrals.md §28.1.
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6_
  - [x] 7.4 Migrate web.config to appsettings.json / IConfiguration
    - Split `web.config`: appSettings/connectionStrings/custom sections → `appsettings.json` bound via `IConfiguration`/`IOptions<T>`; `system.web`/`system.webServer` module & handler registrations → middleware in `Program.cs`; rewrite `ConfigurationManager.*` call-sites; keep only the ANCM hosting shim `web.config` if needed; load `DataSettings` via new config path
    - **Remove the `resizer` `configSections` declaration and its element** (`<section name="resizer" type="ImageResizer.ResizerSection,ImageResizer" />`) from `src/Presentation/Nop.Web/Web.config` — ImageSharp is configured in code/options, not via a config section (design §7)
    - **RESULT: still 0 errors / 15 warnings** — the 7.3 baseline is preserved byte-for-byte (the 5 `CS0618` FluentValidation warnings are in `Validators/` files this task did not touch; the pin is design §9). **Six deferrals closed** (4/7.2-4 `appsettings.json`; 16/7.16 EU VAT endpoint; 33/14.33 cache busting; 39/7.1-4 publish shaping; 40/7.1-5 static assets; the second half of 7.20), **one closed by decision** (36/7.1-1 Redis session), **two opened** (7.4-1, 7.4-2). See runtime-deferrals.md §31–§36.
    - **NEW FILES:** `appsettings.json` (every value traced to the legacy element it replaces); `Infrastructure/NopStaticFileProvider.cs` + `Infrastructure/NopStaticFilesExtensions.cs`. `Web.config` reduced to the IIS/ANCM shim and **renamed to lowercase `web.config`** — see the correction below. `Web.Debug.config` / `Web.Release.config` deleted (XDT boilerplate the Web SDK does not run). `Content/Images/Thumbs/placeholder.txt` restored.
    - **DECISION on deferral 40 (static assets), which 7.3 left open:** the *provider* moved, not the files — an allow-listed `IFileProvider` over the content root, installed onto `IWebHostEnvironment.WebRootFileProvider` so `IFileVersionProvider` covers the same file set (closing 33 with the same line). Relocation under `wwwroot/` was rejected because `ThemeProvider`, `CommonModelFactory`'s favicon *probe* and `PictureService`'s thumbnail *write* all require the files to stay under the content root. The allow-list is also what keeps `App_Data/Settings.txt` (the connection string) unreachable — `System.Web` blocked `App_Data` implicitly and ASP.NET Core does not.
    - **TWO DEFECTS FOUND AND FIXED.** (1) `Web.config` with a capital W silently loses the ANCM handler on any case-sensitive filesystem: the SDK's `TransformWebConfig` looks for the literal name `web.config`, so a Linux-built publish emitted a `Web.config` with **no `<aspNetCore>` element at all** and would have failed to start under IIS. (2) `.gitignore` ignored the whole `Content/Images/Thumbs/` directory, which had swallowed `placeholder.txt` — without it the directory is absent from a fresh clone and `PictureService.DeletePictureThumbs` throws `DirectoryNotFoundException`.
    - **CORRECTION to the deferral-39 framing:** it was written as "re-express the exclusions", but the larger half turned out to be **inclusions** — the SDK classifies `.css`/`.js`/`.png`/`.xml`/`.sql`/`.ttf`/`.mmdb` as `None` items, which default to `CopyToPublishDirectory=Never`, so a publish of the pre-7.4 project produced an app with **no static assets, no installation scripts, no browscap database and no PDF font**.
    - **VERIFIED, not just compiled:** two throwaway probes (both deleted, `git status` clean) asserted **74 facts, all PASS**, with a proven canary — 14 `NopConfig` properties bind, 6 `Authentication` values bind, the legacy `appSettings` keys behave exactly as in 3.90, the deferral-40 symptom reproduces before the fix, 8/8 asset paths serve and 19/19 non-asset paths refuse, a real `?v=<sha256>` is emitted, and 10 live HTTP requests confirm 404 for `Settings.txt`/`appsettings.json`/`web.config`/`*.cshtml`/`*.dll`. `dotnet publish` was audited against **planted secret files**.
    - _Requirements: 1.4, 4.6, 5.3, 5.8_
  - [x] 7.5 Remove obsolete Nop.Web files
    - Remove `AssemblyInfo`, `Global.asax`, `RouteConfig`/`*Config` App_Start files, and `Views/web.config` files rendered obsolete by the SDK/ASP.NET Core model
    - **RESULT: still 0 errors / 15 warnings** — the 7.3/7.4 baseline is preserved byte-for-byte (same 10 upstream `SYSLIB*` + 5 `CS0618`; no warning added). **Deleted:** `Views/Web.config`, `Themes/DefaultClean/Views/Web.config`, and the three dead IE8 assets (`Themes/DefaultClean/Content/css/ie8.css`, `Scripts/selectivizr.min.js`, `Scripts/respond.min.js`). **Deferral 18.5 CLOSED.** One deferral opened (7.5-1, per-theme `_ViewImports`). See runtime-deferrals.md §37–§39.
    - **`Properties/AssemblyInfo.cs` is KEPT, and the task text asking to remove it is WRONG for this solution — measured both ways.** `Directory.Build.props` sets `GenerateAssemblyInfo=false` for the whole solution (task 2.3's decision, so hand-kept files stay authoritative), which means the SDK emits **no replacement attributes**. Building with the file removed produced `AssemblyVersion 0.0.0.0` instead of `3.9.0.0`, and dropped `AssemblyTitle`/`AssemblyFileVersion`/`ComVisible`/`Guid` outright. It is also what all four migrated upstream projects do (task 6.5 §18.2 recorded the same decision for `Nop.Web.Framework`). Deleting it would require flipping a solution-wide property for one project and would make `Nop.Web` the only inconsistent one, with 26 projects still to migrate.
    - **NEW FILE — `Themes/DefaultClean/Views/_ViewImports.cshtml`, and it is NOT optional.** 7.3's note above says both `Views/Web.config` files were superseded by `Views/_ViewImports.cshtml`; **that is true of the first and false of the second.** Razor resolves `_ViewImports.cshtml` by walking up from the view's **own** directory, so the theme tree never sees the one under `Views/`. **Measured, not assumed:** a probe view at `Themes/DefaultClean/Views/Shared/` containing `@T("Account.Login")` failed with `CS0103: The name 'T' does not exist`; after adding the theme `_ViewImports.cshtml` the identical probe compiled at 0 errors. Both probes deleted, `git status` verified clean. Without it the deletion would have been a silent capability loss: `ThemeableViewLocationExpander` searches the theme tree **first** for every storefront view, so the next theme override to use `T()` — i.e. nearly any — would fail confusingly.
    - **Confirmed absent rather than assumed:** no `App_Start/` directory and **no `*Config.cs` file of any kind** exists under `Nop.Web` (3.90 put route/bundle registration in `Global.asax.cs`, not in App_Start), so that clause of the task text had nothing to act on. `Global.asax*` and `Web.Debug/Release.config` verified already gone.
    - **`FilePermissionHelper.GetFilesWrite()` fixed (deferral 18.5)** in the gated `Nop.Web.Framework`: the `~/Global.asax` entry is removed (task 7.2 deleted that file; the entry was harmless only because `CheckPermissions` swallows every failure and so reported a non-existent path as "permission OK"). `web.config` is **kept** — 7.4 retained it as the ANCM shim. Noted in passing: this list always said lowercase `web.config` while 3.90's file was `Web.config`, so the check silently missed the real file on Linux until 7.4's rename made it correct. **`Nop.Web.Framework` re-gated: 0 errors / 10 warnings**, its recorded 6.6 baseline, unchanged.
    - **Dead assets proven dead before deletion:** a repository-wide search (all file types, `.git` and `obj`/`bin` excluded) found the three IE8 files referenced **only** by their own copyright banners and 7.3's explanatory comment — no `AppendCssFileParts`/`AddScriptParts` call anywhere names them. Their sole loader was removed at 7.3 because `System.Web.HttpBrowserCapabilities` has no ASP.NET Core replacement, so they are obsolete by the platform, not by preference. `ie_warning.jpg` was checked and **kept** — still live via `Views/Shared/OldInternetExplorerWarning.cshtml`, which `_Root.cshtml` renders.
    - **`Nop.Web.csproj` cleanup:** the two interim `<Content Update="…Views\Web.config" CopyToPublishDirectory="Never" />` entries 7.4 added are **removed**, not left as no-ops, now that the files are gone; verified neither path appears in publish output.
    - **SCOPE REDUCED BY TASK 7.2:** `Global.asax` and `Global.asax.cs` are already deleted — they carried the `System.Web.Mvc` / `FluentValidation.Mvc` / `StackExchange.Profiling` references that blocked 7.2's own compile. See runtime-deferrals.md §20.
    - **SCOPE REDUCED BY TASK 7.4:** `Web.Debug.config` and `Web.Release.config` are already deleted, and `Web.config` was reduced to the IIS/ANCM hosting shim and renamed to lowercase `web.config` (do **not** delete it — it carries the two live IIS settings and is the file the SDK merges the ANCM handler into). See runtime-deferrals.md §33.6.
    - **ADDITIONS FROM TASK 7.3:** `Views/Web.config` and `Themes/DefaultClean/Views/Web.config` are now fully superseded by `Views/_ViewImports.cshtml` (both the `pageBaseType` and the `<namespaces>` list were translated) and can be deleted. Three static assets became unreferenced when the IE8 browser-capability branches were removed from `Themes/DefaultClean/Views/Shared/Head.cshtml`: `Themes/DefaultClean/Content/css/ie8.css`, `Scripts/selectivizr.min.js`, `Scripts/respond.min.js`. `FilePermissionHelper.GetFilesWrite()` still asks for write access to `~/Global.asax`, which no longer exists — see runtime-deferrals.md §18.5.
    - **NOTE FROM TASK 7.4 on those two `Views/Web.config` files:** they are already excluded from publish output (`CopyToPublishDirectory="Never"` in `Nop.Web.csproj`), because both declare a `System.Web.WebPages.Razor` `<configSections>` group and IIS parses every file literally named `web.config` in the served tree — publishing them would produce an **HTTP 500.19**. Once 7.5 deletes them, those two csproj entries become harmless no-ops and can be removed. `Themes/DefaultClean/theme.config` must **stay** — `ThemeProvider` reads it.
    - _Requirements: 1.5_
  - [x] 7.6 Clean-compile gate — Nop.Web
    - Build `src/Presentation/Nop.Web/Nop.Web.csproj -c Debug` using the containerized .NET 10 SDK command in `build-environment.md`; resolve to zero errors; verify no `System.Web*` references remain. Plugins remain blocked until the Nop.Admin gate (8.8) also passes
    - **GATE PASSED: 0 errors / 15 warnings** (`--no-incremental`, `obj`/`bin` removed first, containerized `mcr.microsoft.com/dotnet/sdk:10.0`). Warnings are the unchanged 10 upstream `SYSLIB0014/0021/0023/0045/0051` in `Nop.Core`/`Nop.Services` plus 5 `CS0618` on FluentValidation 7.x's obsolete `Custom(...)` in three `Validators/` files this task did not touch (design §9 pin). **No warning was added by 7.5.**
    - **No `System.Web*` in source: 0 real hits across 435 `.cs`/`.cshtml` files.** Scanned with a comment-blanking scanner (`@* *@`, `/* */`, `//`, string/verbatim-string aware) so explanatory prose cannot produce a false positive, and **the scanner was proven able to fail** by planting a `using System.Web.Mvc;` canary, which it caught. Tokens searched also included `HttpContext.Current`, `HttpContextBase`, `HttpPostedFileBase`, `MvcHtmlString`, `System.Configuration`, `System.Runtime.Caching`.
    - **No `System.Web*` in the emitted assembly: 0 banned entries in `Nop.Web.dll`'s 59-entry AssemblyRef table** (read via `System.Reflection.Metadata`, not text search). Also 0 for `ImageResizer`, `WebGrease`, `System.Web.Optimization`, `Autofac.Integration.*`, `MiniProfiler`, `StackExchange.Profiling`, `EntityFramework` (EF6), `Antlr`, `Microsoft.Web.*`. Assembly identity confirms `Nop.Web v3.9.0.0`, i.e. `AssemblyInfo.cs` is doing its job.
    - **One honest exception to report: `System.Drawing.Common` 4.7.2 IS in the output directory and `deps.json`** — pre-existing and deliberate, not a 7.5 regression. It is a **direct `PackageReference` of no project**; `project.assets.json` shows the single edge `EPPlus/4.5.3.3 → System.Drawing.Common`, and task 4.2 §10 pinned it to 4.7.2 to clear `NU1904`/CVE-2021-24112 (RCE via crafted metafile) which 4.7.0 carries. Nothing in the repository binds a type from it — `Nop.Services` only names `System.Drawing.Color`, which lives in the cross-platform `System.Drawing.Primitives`. `Nop.Web.dll` has **no** reference to it.
    - **No swallowed diagnostics** on a `-v:normal` log (900 lines): `"converted to a warning"` 0, `ContinueOnError` 0, `NU1901`–`NU1904` 0, `error MSB*` 0, and 0 Six Labors licence lines.
    - **All four upstream projects re-verified at 0 errors** after `obj`/`bin` removal: `Nop.Core` 0/3, `Nop.Data` 0/3, `Nop.Services` 0/10, `Nop.Web.Framework` 0/10 — each matching its recorded gate baseline.
    - **`dotnet publish` audited:** nothing deleted was load-bearing and 7.4's exclusions still hold. Present: `appsettings.json`, the transformed `web.config` (ANCM `AspNetCoreModuleV2` + `hostingModel="inprocess"` still merged, alongside 7.4's preserved `urlCompression`/`X-Powered-By`), `App_Data/browscap.xml`, `App_Data/Install`, `FreeSerif.ttf`, `Content/Images/Thumbs/placeholder.txt`, `Content/files/ExportImport/Index.htm`, `Scripts`, `theme.config`, `preview.jpg`, `styles.css`, `favicon.ico`, `ErrorPage.htm`, `FileNotFound.htm`. Absent: `App_Data/Settings.txt`, `InstalledPlugins.txt`, `browscap.crawlersonly.xml`, both `Views/Web.config`, `launchSettings.json`, the three IE8 assets, and **0** `.cs`/`.cshtml`/`.csproj` files. The only `web.config` in the publish tree is the root shim. The 195 compiled Razor view types include `Themes_DefaultClean_Views__ViewImports`, confirming the new theme file is compiled and applied.
    - _Requirements: 3.1, 3.2, 3.3, 4.2_
  - [x] 7.7 Smoke-check the ASP.NET Core host
    - Add a minimal integration/smoke check verifying the host starts, the Autofac container builds and core services resolve, one controller route responds, one Razor view renders, one former-module middleware executes, and `appsettings.json` binds (representative, non-gating)
    - **RESULT: the host runs, and the check found FIVE REAL DEFECTS — THREE of them release blockers that made nopCommerce IMPOSSIBLE TO INSTALL.** Two deferrals recorded as RESOLVED were **not**. Full analysis in runtime-deferrals.md §41–§44.
    - **NEW PROJECT: `src/Tests/Nop.Web.SmokeTests`** — `WebApplicationFactory<Nop.Web.Program>` + `Microsoft.AspNetCore.Mvc.Testing` 10.0.12 (the one new central pin) + NUnit 3.14.0, following task 4.5's pattern. **49 tests. 36 pass / 0 fail / 13 skip with a database installed; 32 pass / 0 fail / 17 skip without one.** It starts the REAL host: the real `Program.Main` through to `app.Run()`, the real Autofac container, the real middleware order, the real compiled Razor views — only `IServer` is swapped for `TestServer`. **No production source file was changed to enable testing** (`Program` was already a `public class` with a conventional `Main`, so the allowed `partial` accommodation was not needed).
    - **BLOCKER 1 — deferral 4.8 was marked RESOLVED and 7.2's fix was INERT.** `Program.InitializeDatabaseSchema()` opens with `if (!DataSettingsHelper.DatabaseIsInstalled()) return;` and runs once at startup, but the case 4.8 is about is *installing onto an empty database*, where at startup there is no `Settings.txt` — so it early-returned; on later starts the store is installed and the initializer short-circuits. Dead in both directions. Measured by POSTing the real installer form: **`Setup failed: Entity: Store State: Added … Invalid object name 'Store'.`** — 4.8's predicted symptom verbatim. Fixed in `Nop.Data/SqlServerDataProvider.InitDatabase()`, which is the installer's own call and where EF6's `Database.SetInitializer` hook fired.
    - **BLOCKER 2 — deferral 4.12's severity was wrong.** It said "not required for the compile gate", which is true and misleading: `App_Data/Install/SqlServer.StoredProcedures.sql` joins `Product_Id`/`ProductTag_Id`/`Customer_Id`/`CustomerRole_Id` **by name**, so stored-procedure creation failed with **`Invalid column name 'ProductTag_Id'`** and installation aborted. All **8** join tables now pin their 3.90 column names via `HasColumnName`, exactly as 4.12 recommended. Only the column name is pinned — no query or navigation code changed.
    - **BLOCKER 3 — NEW: EF Core's FK index naming collides with 15 of nopCommerce's own indexes.** `ForeignKeyIndexConvention` names them `IX_<Table>_<Column>`; EF6 used `IX_<Column>`. Measured: **`Setup failed: … index … 'IX_StateProvince_CountryId' already exists`**, and because the custom script aborts at the first failure **none of the 57 later indexes was created either**. Fixed by prefixing every `CREATE INDEX` in `SqlServer.Indexes.sql` with a guarded `DROP INDEX` — **drop-and-create, not `IF NOT EXISTS`**, because nopCommerce's definitions are richer (e.g. `IX_StateProvince_CountryId` carries `INCLUDE ([DisplayOrder])`) and skipping would have silently kept EF Core's narrower index.
    - **DEFECT 4 — NEW: eight paths break on any case-sensitive filesystem, i.e. on every Linux/container deployment.** Same class as task 7.4's `Web.config` → `web.config` rename, which was treated as a one-off; it was not. `PictureService` ×3 (`~/content/images[/thumbs]` vs `Content/Images[/Thumbs]`) — **the entire filesystem picture store was dead**; `CodeFirstInstallationService` ×4 (`~/content/samples/`) — **sample-data install failed**; `PdfService` ×1; and `Views/Install/Index.cshtml` ×3 — **the install page rendered with no stylesheet** (measured: `/Content/Install/style.css` → 302, `/Content/install/style.css` → 200). All corrected, then **two systematic audits written and run to close the class**: all 22 `MapPath("~/…")` literals and all 30 `~/Content|Scripts|Themes/…` view/stylesheet references now resolve case-exactly (0 mismatches). A permanent test walks every asset the install page emits and requires 200 from each.
    - **DEFECT 5 — deferral 7.2-1 resolved, and it had a second cause nobody had identified.** An unreachable database killed the host at startup (`NopException: No database instance`, exit 134). Making `InitializeDatabaseSchema()` non-fatal was not enough — it then died at `StartScheduledTasks → TaskManager.Initialize → ScheduleTaskService.GetAllTasks`, which reads the `ScheduleTask` table. Both are now guarded. **This is a reversal of 7.2's "deliberately not swallowed", and it is not 3.90 parity in 7.2's favour**: in System.Web a throw from `Application_Start` failed only the triggering request and was retried on the next one, so the worker survived and recovered by itself; an exception out of `Program.Main` terminates the process and produces a restart loop. Verified after the change: the host starts and stays up with the database down.
    - **VERIFIED BY EXECUTION — the six things the task names, plus tasks 7.3/7.4's prioritised list.** Host starts; **ONE** Autofac container (`EngineContext…Container` reference-equal to `app.Services.GetAutofacRoot()`); per-request scope genuinely shared (two `IWebHelper` resolves in one request are the same instance, and the same one `RequestServices` yields — deferral 1.3); plugin discovery **ran** (`ReferencedPlugins` non-null, count 0, which is correct — no plugin project is migrated yet); `appsettings.json` binds **as values off the Autofac singleton and off the live `CookieAuthenticationOptions`**, including `ExpireTimeSpan == 30 days`, the exact 30-minute silent fallback deferral 7.13 warned about; the install controller responds and its compiled Razor view renders; `InstallUrlMiddleware` redirects `/` and `/cart` and is proven to sit before routing. With a database: the **home page and its ~15 `@Html.Action` child actions all render** (deferral 7.3-1 — task 7.3 called this the single most important thing for 7.7 to do, and it had only ever been probed against a synthetic app); a real product slug resolves through `SlugRouteTransformer`; all seven `SuppressMatchingMetadata` routes still generate URLs against the **real** endpoint set; `/cart` is not swallowed by `{generic_se_name}`; empty widget zones render nothing and do not throw; **9 asset URLs carry a real `?v=<sha256>` and all return 200**; `IsSearchEngine()` is live (Googlebot true, Chrome false; cold 1171 ms, warm 19 ms, crawler file written); **`GET /App_Data/Settings.txt` returns 404 with a real connection string in it**; and **FluentValidation demonstrably REJECTS** invalid input on both the install and register forms (deferral 11.20, security-relevant — previously only inferred from a registration in `MvcOptions`).
    - **THE HARNESS WAS PROVEN ABLE TO FAIL** before its green run was believed: `HarnessCanaryTests` is an `[Explicit]` fixture with one deliberately-false assertion per mechanism the suite depends on (real HTTP through `TestServer`, real container resolve, probe-middleware output). Observed **3 failed / 0 passed**. This migration has twice been bitten by silently-swallowed failure, so a green run that had never been shown to go red would not have been reported as evidence.
    - **NO REGRESSION.** All gates re-verified with `--no-incremental` after `obj`/`bin` removal: `Nop.Core` **0**/3 · `Nop.Data` **0**/3 · `Nop.Services` **0**/10 · `Nop.Web.Framework` **0**/10 · `Nop.Web` **0**/15 — each exactly its recorded baseline, **no warning added**; `"converted to a warning"` 0 on all seven builds. `Nop.Tests` still 4 passed / 0 failed.
    - **FIVE NEW DEFERRALS: 7.7-1** a refused XSRF POST answers **404 instead of 400** (task 7.2's `UseStatusCodePagesWithReExecute` rewrites any bodiless 4xx; 7.2 judged this "uncommon", but it hits every `[PublicAntiForgery]` action — measured `POST /login` 200 vs `POST /register` and `POST /contactus` 404 — and will hit the whole admin surface at 8.3; **not** a security hole, the action does not run); **7.7-2** the smoke project is not in `NopCommerce.sln` (task 18.1; it must NOT become a gate); **7.7-3** EF Core adds ~97 unrequested FK indexes (97 of 100 on a fresh database) — deliberately NOT decided here, since EF6 also created FK indexes under different names, so removing the convention would drop indexes rather than restore parity; **7.7-4** both case-sensitivity audits still need running over `Administration/` (tasks 8.4/8.5 — on this evidence they will find instances); **7.7-5** a scope note listing what is still unexercised — the entire admin UI, all 20 plugins, sign-in, checkout/payment/email, deferral 4.11's Fast-installer path, and Windows.
    - _Requirements: 4.1, 4.3, 4.4, 4.5, 4.6, 1.4_

- [ ] 8. Port Nop.Admin to ASP.NET Core
  - Largest single UI surface in the application: `src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`, **277 `.cs` files** and **325 `.cshtml` Razor views**. References Nop.Core, Nop.Data, Nop.Services, Nop.Web.Framework — and neither references nor is referenced by Nop.Web, so this group is a **parallelizable sibling of task 7** once the 6.6 gate passes (design §6).
  - **Expect the highest iteration count of any stage in this plan.** Per design §6, run this group as a repeated `build → fix → build` loop against the containerized SDK rather than attempting a single pass; step 8.8 will be re-entered many times.
  - [ ] 8.1 Convert Nop.Admin.csproj to SDK-style Web project at net10.0
    - Convert to `<Project Sdk="Microsoft.NET.Sdk.Web">` with `<TargetFramework>net10.0</TargetFramework>` (cross-platform, not `net10.0-windows`)
    - `packages.config` → `PackageReference`; delete `packages.config`; drop `Microsoft.AspNet.Mvc/Razor/WebPages` and `Autofac.Mvc5` in favour of ASP.NET Core MVC + `Autofac.Extensions.DependencyInjection`; add the `SixLabors.ImageSharp` `PackageReference` needed by 8.6
    - Drop the explicit `<Compile Include=.../>` and `<Content Include=.../>` globs (implicit globbing), plus `ProjectGuid`, `TargetFrameworkVersion`, Configuration/Platform blocks, `Import *.targets`, and BCL `<Reference Include="System.*"/>` entries including `System.Drawing`
    - Point `ProjectReference`s at the already-migrated `Nop.Core`, `Nop.Data`, `Nop.Services`, and `Nop.Web.Framework`
    - **FROM TASK 7.4 (new deferral 7.4-2, SECURITY-RELEVANT):** re-express the 3.90 publish exclusion for **`Administration/db_backups/*.bak`** here. `Nop.Web.csproj` structurally cannot do it — `Administration\**` is in its `DefaultItemExcludes`, so nothing under it is an item of that project — yet in 3.90 Nop.Admin dropped its output into `Nop.Web\bin`. Database backups must not reach a published, web-served directory. Use `<None Update="db_backups\**" CopyToPublishDirectory="Never" />`. Also note that 7.4 deliberately did **not** reproduce 3.90's `<mimeMap fileExtension=".bak">`, so backups are not downloadable as static files either. See runtime-deferrals.md §34.
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
    - **FROM TASK 7.4 (deferral 7.3-4, re-targeted here):** 7.3 removed `[ChildActionOnly]` from 48 `Nop.Web` actions, which are now reachable by URL and return the bare partial's HTML. 7.4 declined to fix it in isolation because the identical decision has to be made for the ~325-view admin surface, where the exposure is worse. Implement once, in `Nop.Web.Framework` so both projects share it: a marker attribute on the former child actions plus an `IActionModelConvention` that adds `Microsoft.AspNetCore.Routing.SuppressMatchingMetadata`, registered in `AddNopFramework`. That is the same mechanism 7.3 proved works for `GenericUrlRouteProvider`'s seven name-only routes. See runtime-deferrals.md §32.2.
    - **ALSO FROM 7.3 (deferral 7.3-1):** `Nop.Web/Extensions/ChildActionExtensions.cs` (the `Html.Action` bridge) is needed by the admin views too. Recommend **promoting it to `Nop.Web.Framework`** rather than duplicating it; the plugins then get it for free. **Task 7.7 verified the bridge works against nopCommerce's real controllers** (the home page's ~15 child actions all render), so promotion is a move, not a rewrite.
    - **FROM TASK 7.7 (new deferral 7.7-1):** a refused XSRF POST currently answers **404, not 400**. `PublicAntiForgeryAttribute`/`AdminAntiForgeryAttribute` correctly return `BadRequestResult`, but that 400 has no body and `Program.cs`'s `UseStatusCodePagesWithReExecute("/page-not-found")` fires for *any* bodiless 4xx and re-executes `CommonController.PageNotFound`, which sets 404. Measured: `POST /login` (no `[PublicAntiForgery]`) → 200, `POST /register` and `POST /contactus` → 404. Task 7.2 recorded the imprecision but judged only "the uncommon bare 403/400" affected; it hits every `[PublicAntiForgery]` action, and `[AdminAntiForgery]` is applied across the whole admin surface. **Not a security hole** — the action does not run, which task 7.7 asserts. Fix: restrict the re-execute to 404 (a predicate, or a small middleware). Two tests in `Nop.Web.SmokeTests` pin the current behaviour and will flip. See runtime-deferrals.md §44/7.7-1.
    - _Requirements: 4.8, 4.7, 4.2, 4.3, 4.5_
  - [ ] 8.4 Port all Nop.Admin Razor views to the ASP.NET Core Razor engine
    - Port all ~325 `.cshtml` files: `WebViewPage`/`@model` base → `RazorPage<TModel>`; `@Html` `HtmlHelper` → `IHtmlHelper`; `UrlHelper` → `IUrlHelper`; `@Html.Action` → View Components
    - **Delete `Areas/Admin/Views/web.config`** and any nested `Views/web.config`; add `_ViewImports.cshtml` carrying `@using`, `@inject`, and `@addTagHelper` directives
    - Re-base `_ViewStart.cshtml` and `_Layout.cshtml` on the Core view engine; port admin display/editor templates
    - **FROM TASK 7.7 (new deferral 7.7-4) — RUN THE CASE-SENSITIVITY AUDIT OVER `Administration/`.** 7.7 found **8** asset/path references in Nop.Web and Nop.Services spelled differently from the directories on disk; they resolved on Windows and 404'd/threw on Linux, and one of them killed the entire filesystem picture store. `Administration/` was excluded by 7.7's scope boundary and has ~325 views plus its own asset trees, so on this evidence it contains instances. Audit **every** `~/Content|Scripts|Themes/…` reference in admin views and stylesheets, and every `MapPath("~/…")` literal in admin controllers (`RoxyFilemanController` does its own path arithmetic), checked **case-exactly** against the filesystem — not by eye, with a script. See runtime-deferrals.md §42.4 and §44/7.7-4.
    - _Requirements: 4.9, 4.4, 4.7_
  - [ ] 8.5 Move static admin assets to wwwroot / static-file middleware
    - Relocate the admin `Content/` and `Scripts/` trees under `wwwroot` (or expose them through the host's static-file middleware) instead of relying on the `System.Web` handler pipeline; update view asset references accordingly
    - **FROM TASK 7.7:** the admin 404 is now confirmed by execution, not inference — the installation page links `/Administration/Content/bootstrap/css/bootstrap.min.css` and `/Administration/Content/adminLTE/AdminLTE-2.3.0.min.css`, neither of which serves, so **the installer renders unstyled today**. `Nop.Web.SmokeTests` asserts that as a known gap and will fail when this task fixes it, which is the signal to update the test. Also note deferral 7.7-4: audit the admin asset references for case-sensitivity at the same time, or a widened allow-list will still 404.
    - **FROM TASK 7.4 (new deferral 7.4-2):** the storefront half of this problem is solved and the mechanism is reusable. `Nop.Web/Infrastructure/NopStaticFileProvider.cs` is an **allow-listed** `IFileProvider` over the content root, and it deliberately excludes `Administration/`, so the admin assets currently **do not serve** — the same symptom deferral 40 described for the storefront. The cheapest correct fix is to widen that allow-list with `Administration/Content/**` and `Administration/Scripts/**` rather than relocating the trees; that also gives admin assets `IFileVersionProvider` cache busting for free, because the deferral-33 property holds for anything the provider serves. It must **not** be done by widening the root, or `Administration/db_backups` and the admin `.cshtml` tree come with it. See runtime-deferrals.md §34.
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
    - **FROM TASK 7.7 (new deferral 7.7-2): add `src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj`.** It is a new project, so no legacy entry exists to update. It must **NOT** be added to a clean-compile gate: task 7.7 is non-gating by design and 13 of its 49 tests require a reachable SQL Server (they `Assert.Ignore` with an explicit reason when there is none, so `dotnet test` stays green without a database).
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
