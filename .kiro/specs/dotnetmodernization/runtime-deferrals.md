# Runtime Deferrals Register

## Why this document exists

The migration plan's completion criterion is a clean compile (Requirement 3.3). A clean
compile cannot detect code that **compiles correctly but is wrong at runtime until a later
task wires it up**. Task 2.4 (Nop.Core) produced several such items: static seams that are
null until the host assigns them, hosting hooks that no longer fire, and helpers that now
resolve against the wrong directory.

Every gate in the plan will pass regardless of whether these are fixed. This file is the
running register so they are not lost. Later task subagents should read this before
starting tasks 6.x, 7.x and 8.x.

Three sections:

1. **Open deferrals** — must be fixed by a named future task. Runtime is currently wrong.
2. **Final behavioral changes** — intentional, complete, no future fix needed, but downstream
   tasks and testers must know.
3. **Breaking constructor/signature changes** — compile-breaking for callers, owned by task 6.x.

---

## 1. Open deferrals

All items below originate from **Nop.Core task 2.4**.

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 1 | Plugin discovery never runs | 7.2 | **Highest** |
| 2 | Plugin assemblies invisible to the Razor compiler | 6.4, 7.2 | High |
| 3 | Per-request DI scope not shared within a request | 6.4 | High |
| 4 | Configuration source unset — all `NopConfig` settings at defaults | 7.2, 7.4 | High |
| 5 | `CommonHelper.MapPath` resolves relative to `bin/` | 6.5, 7.2 | High |
| 6 | `WebHelper.RestartAppDomain` throws instead of restarting | 6.4, 7.2 | Medium |

### 1.1 Plugin discovery never runs — HIGHEST IMPACT

- **What changed:** `[assembly: PreApplicationStartMethod(typeof(PluginManager), "Initialize")]`
  was removed from `src/Libraries/Nop.Core/Plugins/PluginManager.cs`.
- **Why:** `System.Web.PreApplicationStartMethodAttribute` is a System.Web hosting hook. There
  is no ASP.NET Core equivalent.
- **Current runtime behavior:** `PluginManager.Initialize()` is no longer invoked automatically.
  `PluginManager.ReferencedPlugins` stays **null** and **no plugin is discovered**.
- **Fix (task 7.2, Program.cs / host startup):** call `PluginManager.Initialize()` explicitly
  during startup, **before engine initialization**.
- **Impact if unfixed:** the plugin subsystem is entirely dead. No plugin loads, and anything
  reading `ReferencedPlugins` sees null.

### 1.2 Plugin assemblies invisible to the Razor compiler

- **What changed:** `System.Web.Compilation.BuildManager.AddReferencedAssembly` was removed from
  `PluginManager.PerformFileDeploy`.
- **Why:** it existed so the System.Web build manager could compile views against dynamically
  loaded plugin assemblies. No equivalent exists.
- **Current runtime behavior:** plugin assemblies are loaded but never registered with the view
  compiler.
- **Fix (tasks 6.4 and 7.2):** contribute plugin assemblies as MVC **application parts /
  MetadataReferences**.
- **Impact if unfixed:** plugin Razor views will not compile.

### 1.3 Per-request DI scope is not shared within a request

- **What changed:** `ContainerManager.Scope()` previously used
  `AutofacDependencyResolver.Current.RequestLifetimeScope` from `Autofac.Integration.Mvc`
  (Autofac.Mvc5), which was dropped. A new **additive** seam
  `ContainerManager.CurrentScopeProvider` (a `static Func<ILifetimeScope>`) was added in its place.
- **Current runtime behavior:** until `CurrentScopeProvider` is assigned, `Scope()` begins a
  **fresh** lifetime scope on every call, so per-request-scoped services are **not shared within
  a request**.
- **Fix (task 6.4):**
  ```csharp
  ContainerManager.CurrentScopeProvider =
      () => httpContextAccessor.HttpContext?.RequestServices.GetService<ILifetimeScope>();
  ```
- **Impact if unfixed:** duplicated per-request state, extra allocations, and subtle correctness
  bugs anywhere state is assumed shared across a single request (work context, caching, unit of work).

### 1.4 Configuration source is unset — all `NopConfig` settings are at defaults

- **What changed:** a new `Nop.Core.Configuration.NopConfigurationManager` static seam replaced
  the `System.Configuration.ConfigurationManager` call sites. Those call sites are static and run
  before the container exists, so they cannot use an injected `IConfiguration`.
- **Current runtime behavior:** `NopConfigurationManager.Configuration` is **null** until the host
  assigns it.
  - Legacy `appSettings` lookups return null. This is **benign** — `ForwardedHTTPheader`,
    `Use_HTTP_CLUSTER_HTTPS`, `Use_HTTP_X_FORWARDED_PROTO` and
    `ClearPluginsShadowDirectoryOnStartup` all fall back to documented defaults.
  - **But** `NopConfig` binds to all defaults, so `RedisCachingEnabled`, `UserAgentStringsPath`
    and the **Azure blob settings** are unset.
- **Fix (tasks 7.2 and 7.4):** add the `NopConfig` and `appSettings` sections to
  `appsettings.json` and assign `NopConfigurationManager.Configuration`.
- **Impact if unfixed:** Redis caching silently off, user-agent strings path unresolved, Azure
  blob storage unconfigured — all failing as "feature not enabled" rather than as an error.

### 1.5 `CommonHelper.MapPath` resolves relative to `bin/`, not the web content root

- **What changed:** a new settable `CommonHelper.BaseDirectory` defaults to
  `AppDomain.CurrentDomain.BaseDirectory`.
- **Current runtime behavior:** until the host sets it to the content root,
  `MapPath("~/App_Data/...")` resolves **under `bin/`**.
- **Scope:** roughly **30 downstream call sites** across Nop.Data, Nop.Services and the
  presentation projects.
- **Fix (tasks 6.5 and 7.2):** set `CommonHelper.BaseDirectory` to the content root during startup.
- **Impact if unfixed:** widespread wrong paths — install files, `App_Data`, plugin directories and
  file uploads all point into the output folder.

### 1.6 `WebHelper.RestartAppDomain` throws instead of restarting

- **What changed:** .NET has no unloadable AppDomain, so `HttpRuntime.UnloadAppDomain()` and both
  medium-trust fallbacks (touching `web.config` / `global.asax`) are gone. The method now resolves
  `IHostApplicationLifetime` and calls `StopApplication()`, relying on the process supervisor
  (ANCM / systemd / container orchestrator) to restart the process.
- **Current runtime behavior:** if `IHostApplicationLifetime` is not registered it throws
  `NopException` with an explanatory message — deliberately **not** a silent no-op.
- **Fix (tasks 6.4 / 7.2):** register `IHostApplicationLifetime` so it is resolvable.
- **Impact if unfixed:** every restart path (plugin install/uninstall, settings that require a
  restart) throws instead of restarting.

---

## 2. Final behavioral changes — intentional, no future fix needed

These are complete. They need no follow-up task, but downstream implementers and testers should
know the observable behavior differs from 3.90.

- **`IWebHelper.ServerVariables`** — no server-variable collection exists in ASP.NET Core.
  `HTTP_*` names map back to the originating header (`HTTP_X_FORWARDED_PROTO` →
  `X-FORWARDED-PROTO`); other names are looked up verbatim. Non-header variables such as
  `SERVER_SOFTWARE` and `LOCAL_ADDR` are **no longer resolvable and return `""`**. All three
  in-tree uses (`HTTP_HOST`, `HTTP_CLUSTER_HTTPS`, `HTTP_X_FORWARDED_PROTO`) still work.

- **`IWebHelper.IsRequestBeingRedirected`** — now tests for a **3xx status code**
  (301/302/303/307/308) instead of `HttpResponse.IsRequestBeingRedirected`.

- **`MemoryCacheManager.Set` with `cacheTime <= 0`** — now **skips caching**. `IMemoryCache`
  throws on a non-positive relative expiration; `ObjectCache` silently discarded an
  already-expired item. Same observable result.

- **`System.Web.HttpUtility` → `System.Net.WebUtility`** in `Html/HtmlHelper.cs`,
  `Html/CodeFormatter/CodeFormatHelper.cs`, `Plugins/OfficialFeedManager.cs`.
  `HtmlEncode`/`HtmlDecode` are identical. `WebUtility.UrlEncode` encodes a space as `%20` where
  `HttpUtility` used `+` — both decode to a space server-side.

- **`CommonHelper.GetTrustLevel()` and `AspNetHostingPermissionLevel` were REMOVED** — there is no
  Code Access Security or medium trust on .NET.
  **One downstream call site must be fixed in task 8.x:**
  `src/Presentation/Nop.Web/Administration/Controllers/CommonController.cs` **lines 205 and 217**
  (SystemInfo) should report `"Full"` unconditionally.

- **`PluginManager.PerformFileDeploy`** always shadow-copies to `~/Plugins/bin`. The removed
  full-trust branch used `AppDomain.CurrentDomain.DynamicDirectory`, which returns null on .NET and
  would throw.

- **AutoMapper** is initialized with `NullLoggerFactory.Instance` (AutoMapper 16 requires an
  `ILoggerFactory`), so **mapper diagnostics are off** until a real factory is supplied.

---

## 3. Breaking constructor / signature changes — owned by task 6.x

All of these must be fixed in `src/Presentation/Nop.Web.Framework/DependencyRegistrar.cs`.

| Before | After | Note |
|--------|-------|------|
| `WebHelper(HttpContextBase)` | `WebHelper(IHttpContextAccessor)` | |
| `PerRequestCacheManager(HttpContextBase)` | `PerRequestCacheManager(IHttpContextAccessor)` | |
| `MemoryCacheManager()` (parameterless) | `MemoryCacheManager(IMemoryCache)` | **New dependency** — requires `services.AddMemoryCache()` |
| `IWebHelper.IsStaticResource(HttpRequest)` | same name, now `Microsoft.AspNetCore.Http.HttpRequest` | |

**Unchanged — do not touch:**

- `ICacheManager` and `IRedisConnectionWrapper` are completely unchanged.
- `IEngine`, `NopEngine`, `ContainerManager` and `IDependencyRegistrar` had **no** signature
  changes. The only difference is the **additive** `ContainerManager.CurrentScopeProvider`
  (see deferral 1.3).

### Note on `FakeHttpContext`

`src/Presentation/Nop.Web.Framework/WebWorkContext.cs` line 186 does
`_httpContext is FakeHttpContext`, and `DependencyRegistrar.cs` lines 74/77 construct
`new FakeHttpContext("~/")`. `FakeHttpContext` was **re-implemented, not deleted** — built over a
**composed** `DefaultHttpContext` specifically to keep these call sites working, because
`DefaultHttpContext` is sealed in .NET 10.


---

# Nop.Data — EF6 → EF Core (task 3.2)

`Nop.Data` reached its clean-compile gate with **0 errors / 0 warnings**, but EF6 and EF Core
differ in ways a compiler cannot see: EF6 configured things globally and lazily that EF Core
requires to be stated explicitly, and several query/model behaviors only fail when a real
database is attached. Everything below was introduced knowingly.

## 4. Open deferrals — Nop.Data

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7 | Lazy loading is off — all `virtual` navigations return null | 4.2 + decision at 7.2 | **Highest** |
| 8 | Schema initializer is never invoked — a fresh install creates no tables | 4.2, 7.2 | **Highest** |
| 9 | `NopObjectContext` now needs a real connection string, not a database name | 3.4, 6.4 | Medium |
| 10 | `CreateDatabaseScript()` output is `GO`-batched — 4 plugin contexts will fail | 11.2, 13.1, 14.4, 15.1 | Medium |
| 11 | `ExecuteSqlCommand(doNotEnsureTransaction: false)` now opens a real transaction | 4.2 | Medium |
| 12 | Many-to-many join **column** names follow EF Core conventions, not 3.90's | 4.2 / schema review | Medium |

### 4.7 Lazy loading is off — all `virtual` navigation properties return null — HIGHEST IMPACT

- **What changed:** EF6 created dynamic proxies by default, so every `public virtual` navigation
  on `Nop.Core.Domain.**` lazy-loaded on first access. EF Core has **no proxy layer in the core
  package**; lazy loading is opt-in and requires either
  `Microsoft.EntityFrameworkCore.Proxies` + `optionsBuilder.UseLazyLoadingProxies()`, or an
  `ILazyLoader` injection, or explicit `Include(...)` at every query site.
- **`IDbContext.ProxyCreationEnabled` no longer means what it meant.** The property is preserved
  on the interface (so `Nop.Services` compiles unchanged) but is now backed by a private field
  that also drives `ChangeTracker.LazyLoadingEnabled`. Setting it to `false` — which
  `PictureService.GetPictureHashes` does deliberately as a perf hack — has no proxy effect
  because there were no proxies to suppress.
- **Impact if unfixed:** the single largest behavioral regression of the whole data-layer move.
  Every service that walks a navigation without `Include` (`order.Customer`,
  `product.ProductCategories`, `customer.CustomerRoles`, …) sees `null` or an empty collection
  instead of loaded data. This is silent — no exception.
- **Fix — pick one, at task 7.2:**
  1. Add `Microsoft.EntityFrameworkCore.Proxies`, call `UseLazyLoadingProxies()` in
     `NopObjectContext.OnConfiguring`, and mark all navigations `virtual` (they already are).
     Lowest-churn, closest to 3.90 behavior. **Recommended.**
  2. Add explicit `Include`/`ThenInclude` at every query site in `Nop.Services`. Faithful to
     modern EF Core practice, but touches hundreds of methods.
  Note that option 1 makes `Extensions.GetUnproxiedEntityType` meaningful again — it was
  re-implemented to detect Castle DynamicProxy subclasses precisely so it keeps working if
  proxies are switched on.

### 4.8 Schema initializer is never invoked — a fresh install creates no tables

- **What changed:** EF6's `Database.SetInitializer(initializer)` registered a **global, lazily
  fired** hook: the first time any `NopObjectContext` was used, EF6 ran
  `CreateTablesIfNotExist.InitializeDatabase(context)`. EF Core deleted the entire initializer
  concept — schema creation is an explicit application action.
- **Current runtime behavior:** `SqlServerDataProvider.SetDatabaseInitializer()` now only
  *publishes* the configured initializer on the new static
  `SqlServerDataProvider.DatabaseInitializer`. **Nothing calls it.** `EfStartUpTask` still calls
  `SetDatabaseInitializer()`, so the object is built — and then never used.
- **Fix (tasks 4.2 / 7.2):** in the installation path, after the context exists, call
  ```csharp
  SqlServerDataProvider.DatabaseInitializer?.InitializeDatabase(nopObjectContext);
  ```
  or replace it outright with EF Core migrations (`Database.Migrate()`). The initializer already
  does the right thing internally: `CanConnect()` → probe `INFORMATION_SCHEMA.TABLES` → execute
  `Database.GenerateCreateScript()` in `GO`-separated batches → run the custom index/stored-proc
  commands.
- **Impact if unfixed:** installing onto an empty database silently creates no schema; the first
  real query fails with "Invalid object name".
- **Static-state note:** `DatabaseInitializer` is a `static` property. It mirrors EF6's global
  `Database.SetInitializer` and carries the same caveat as the Nop.Core static seams in §1 — it
  is process-wide and unset until `SetDatabaseInitializer()` runs.

### 4.9 `NopObjectContext` now requires a connection string, not a database name

- **What changed:** `InitConnectionFactory()` is now a **no-op**. EF6 installed a process-wide
  `Database.DefaultConnectionFactory` (`SqlConnectionFactory`) which let
  `new NopObjectContext("Test")` resolve a bare database name or an `app.config` connection-string
  *name* against a default server. EF Core has no ambient factory: the provider and connection
  string are configured per context, which `NopObjectContext.OnConfiguring` now does with
  `optionsBuilder.UseSqlServer(_nameOrConnectionString)`.
- **Production is fine:** `Nop.Web.Framework/DependencyRegistrar.cs` lines 117 and 121 already
  pass `DataConnectionString`.
- **Tests are not:** `src/Tests/Nop.Data.Tests/PersistenceTest.cs` passes `GetTestDbName()` and
  `SchemaTests.cs` passes the literal `"Test"`. Both will fail to open a connection.
  `PersistenceTest` additionally uses `Database.Delete()`/`Database.Create()`
  (→ `EnsureDeleted()`/`EnsureCreated()`) and a SQL CE connection factory that no longer exists.
- **Fix (task 3.4):** rework those tests onto `UseSqlServer(<real connection string>)` or an
  EF Core in-memory/SQLite provider via the **new additive constructor**
  `NopObjectContext(DbContextOptions<NopObjectContext> options)`, which exists for exactly this.

### 4.10 `CreateDatabaseScript()` output is `GO`-batched — four plugin contexts will fail

- **What changed:** `NopObjectContext.CreateDatabaseScript()` now returns
  `Database.GenerateCreateScript()` instead of EF6's
  `ObjectContext.CreateDatabaseScript()`. EF Core's script separates statements with a `GO`
  batch terminator, which is a **client directive, not T-SQL** — sending the whole script as one
  command throws a syntax error.
- **Handled inside Nop.Data:** `CreateTablesIfNotExist` splits on `GO` before executing.
- **NOT handled outside Nop.Data.** These four plugin contexts do
  `Database.ExecuteSqlCommand(CreateDatabaseScript())` in one shot and must split batches when
  they are migrated:
  - `Nop.Plugin.Feed.GoogleShopping/Data/GoogleProductObjectContext.cs` (task 11.2)
  - `Nop.Plugin.Pickup.PickupInStore/Data/StorePickupPointObjectContext.cs` (task 13.1)
  - `Nop.Plugin.Shipping.FixedOrByWeight/Data/ShippingByWeightObjectContext.cs` (task 14.4)
  - `Nop.Plugin.Tax.FixedOrByCountryStateZip/Data/CountryStateZipObjectContext.cs` (task 15.1)
- Independently: the emitted DDL follows **EF Core's** naming/ordering conventions, so the
  generated schema is not byte-identical to 3.90's.

### 4.11 `ExecuteSqlCommand(doNotEnsureTransaction: false)` now opens a real transaction

- **What changed:** EF6 had `TransactionalBehavior.EnsureTransaction` /
  `DoNotEnsureTransaction`. EF Core's `ExecuteSqlRaw` never opens its own transaction — that is
  EF6's `DoNotEnsureTransaction` semantics. To honor the `false` default, `ExecuteSqlCommand` now
  wraps the call in an explicit `Database.BeginTransaction()`/`Commit()` when no ambient
  transaction exists.
- **Consequence:** call sites that pass `false` (the default) now execute inside a transaction
  where previously EF6's behavior differed by provider. `SqlFileInstallationService.ExecuteSqlFile`
  and the three `TRUNCATE TABLE` call sites (`DefaultLogger`, `CustomerActivityService`,
  `QueuedEmailService`) plus `MaintenanceService`'s `DBCC CHECKIDENT` are affected. **Some DDL
  cannot run inside a transaction** (notably full-text index creation) — the FullText call sites
  already pass `doNotEnsureTransaction: true`, but installation scripts should be reviewed.
- **Output parameters:** `CustomerService.DeleteGuests` passes `@TotalRecordsDeleted OUTPUT` and
  reads `pTotalRecordsDeleted.Value` back after the call. EF Core supports this, but a
  `DbParameter` instance may not be attached to two commands — verify on first run.
- **Timeout:** EF6 set `ObjectContext.CommandTimeout`; this now uses
  `Database.SetCommandTimeout`/`GetCommandTimeout`, which is equivalent.

### 4.12 Many-to-many join *column* names follow EF Core conventions

- **What changed:** EF6's `HasMany(x).WithMany(y).Map(m => m.ToTable("T"))` became EF Core's skip
  navigation `HasMany(x).WithMany(y).UsingEntity(j => j.ToTable("T"))`. The **table** name is
  pinned; the **join FK column** names are left to convention, and EF Core's convention
  (`ProductsId`, `ProductTagsId`) differs from EF6's (`Product_Id`, `ProductTag_Id`).
- **The eight affected join tables:**
  | Map | Join table |
  |---|---|
  | `Catalog/ProductMap` | `Product_ProductTag_Mapping` |
  | `Customers/CustomerMap` | `Customer_CustomerRole_Mapping` |
  | `Customers/CustomerMap` | `CustomerAddresses` |
  | `Discounts/DiscountMap` | `Discount_AppliedToCategories` |
  | `Discounts/DiscountMap` | `Discount_AppliedToManufacturers` |
  | `Discounts/DiscountMap` | `Discount_AppliedToProducts` |
  | `Security/PermissionRecordMap` | `PermissionRecord_Role_Mapping` |
  | `Shipping/ShippingMethodMap` | `ShippingMethodRestrictions` |
- Two of these (`Customer_CustomerRole_Mapping`, `CustomerAddresses`) are **unidirectional** —
  `CustomerRole` and `Address` have no inverse collection back to `Customer`, so they use
  `WithMany()` with no navigation argument.
- **Fix if schema parity against an existing 3.90 database is required:** add explicit
  `.UsingEntity(j => { j.ToTable("…"); j.Property<int>("…").HasColumnName("…"); })` or a named
  join entity. Not required for the compile gate.

## 5. Final behavioral changes — Nop.Data, intentional, no future fix needed

- **`IDbContext.SqlQuery<TElement>` is no longer an EF query.** EF6's
  `Database.SqlQuery<T>` materialized *any* type — scalars and arbitrary non-entity classes — by
  column/property name matching. EF Core has no equivalent: `Database.SqlQueryRaw<T>` handles only
  scalars (and demands the column be aliased `Value`), and `FromSqlRaw` only handles types in the
  model. To keep the interface method and its two non-entity call sites
  (`PictureService.HashItem`, `ProductTagService.ProductTagWithCount`) working, it now drops to
  ADO.NET on the context's own `DbConnection` and reuses the reflection row-mapper in
  `DataReaderExtensions`. Differences: results are **fully materialized eagerly** (EF6's
  `DbRawSqlQuery<T>` was lazy/streaming); EF value converters and type mappings are **not**
  applied — scalars go through `Convert.ChangeType` with `InvariantCulture`, enums through
  `Enum.ToObject`. It enlists in `Database.CurrentTransaction` when one exists and opens/closes
  the connection only if it was closed.

- **`IDbContext.Detach(entity)`** is now `Entry(entity).State = EntityState.Detached`. EF6's
  `ObjectContext.Detach` threw `InvalidOperationException` for an untracked entity; EF Core's
  `Entry()` begins tracking it as `Detached`, so the call is a harmless no-op instead.

- **Cascade-delete semantics.** `WillCascadeOnDelete(false)` → `DeleteBehavior.Restrict`,
  `WillCascadeOnDelete(true)` → `DeleteBehavior.Cascade` (32 call sites). `Restrict` in EF Core
  additionally suppresses EF's *client-side* fixup — EF Core will not null out loaded dependents
  where EF6's `WillCascadeOnDelete(false)` only affected the generated FK constraint. EF Core's
  default for **optional** relationships is `ClientSetNull`, not EF6's DB-level `SET NULL`.

- **Requiredness is now inferred from FK nullability, and that is verified equivalent.** EF6's
  `HasRequired`/`HasOptional` became `HasOne` with no explicit `.IsRequired()`. All **77**
  `HasRequired` chains were checked against the CLR types in `Nop.Core.Domain`: every one has a
  **non-nullable** FK property, so EF Core's convention reproduces `HasRequired` exactly. Of the
  **10** `HasOptional` chains, 7 have nullable FKs (convention reproduces `HasOptional`) and the
  3 with no CLR FK property were configured explicitly — see the next bullet.

- **Three relationships now use explicitly named shadow foreign keys.** EF6 generated a shadow FK
  column when a navigation had no CLR FK property; EF Core requires both ends to be stated. The
  shadow property names were pinned to EF6's generated column names, but this has **not been
  verified against a real 3.90 database**:
  | Map | Relationship | Shadow FK |
  |---|---|---|
  | `Customers/CustomerMap` | `Customer.BillingAddress` (`HasOptional` with no `With*`) | `BillingAddress_Id` |
  | `Customers/CustomerMap` | `Customer.ShippingAddress` (`HasOptional` with no `With*`) | `ShippingAddress_Id` |
  | `Customers/RewardPointsHistoryMap` | `RewardPointsHistory.UsedWithOrder` ↔ `Order.RedeemedRewardPointsEntry` (`WithOptionalDependent`, one-to-one) | `UsedWithOrder_Id` |

- **`Property(...).IsMaxLength()` was dropped** from `Media/PictureMap` (`Picture.PictureBinary`)
  and `Media/DownloadMap` (`Download.DownloadBinary`). EF Core has no `IsMaxLength()`; it maps
  unconstrained `byte[]` to `varbinary(max)` by default, so the resulting column is the same.

- **`DbContextExtensions` metadata helpers re-based on the EF Core model.** EF6 walked the
  `ObjectContext.MetadataWorkspace` (`StoreItemCollection`, `EntityContainer`,
  `TypeUsage.Facets`, `EntityConnection`); these now read `DbContext.Model`:
  `GetTableName<T>` → `Model.FindEntityType(typeof(T)).GetTableName()`;
  `GetColumnsMaxLength` → `IProperty.GetMaxLength()`;
  `GetDecimalMaxValue` → `IProperty.GetPrecision()/GetScale()`;
  `DbName` → `Database.GetDbConnection().Database`;
  `LoadOriginalCopy`/`LoadDatabaseCopy` → `EntityEntry<T>.OriginalValues`/`GetDatabaseValues()`.
  Public signatures are unchanged. `GetColumnsMaxLength`/`GetDecimalMaxValue` now return an entry
  **only when the model states the facet** — a decimal without an explicit `HasPrecision` is
  omitted where EF6 reported the provider default. All 55 `HasPrecision(18, n)` properties are
  covered. `GetTableName<T>` now throws `InvalidOperationException` if the type is not in the
  model (EF6 threw from `First()`).

- **`Extensions.GetUnproxiedEntityType`** no longer calls `ObjectContext.GetObjectType`. It walks
  up past dynamically emitted / `Castle.Proxies` subclasses instead. With EF Core's default
  (no proxies) it returns the type unchanged; if the Proxies package is added (see 4.7) it
  behaves as before.

- **`SqlServerDataProvider.GetParameter()` returns `Microsoft.Data.SqlClient.SqlParameter`**
  instead of `System.Data.SqlClient.SqlParameter`. `IDataProvider.GetParameter()` declares
  `DbParameter` and both types derive from it, so **no interface change was needed and Nop.Core
  was not touched.** Call sites that set `ParameterName`/`Value`/`DbType`/`Direction` (e.g.
  `CustomerService`, `CategoryService`, `ProductService`, `ProductTagService`) are unaffected.

- **SQL Server Compact removed.** `Initializers/SqlCeInitializer.cs`,
  `CreateCeDatabaseIfNotExists.cs`, `DropCreateCeDatabaseAlways.cs` and
  `DropCreateCeDatabaseIfModelChanges.cs` were **deleted** (EF6 `IDatabaseInitializer<T>` over
  `System.Data.SqlServerCe`; no EF Core provider, no `System.Data.SqlServerCe` for net10.0).
  `EfDataProviderManager` already throws `NopException` for a `"sqlce"` provider name, so a
  `DataSettings` file naming SQL CE now fails loudly at startup.

## 6. Breaking signature changes — Nop.Data, owned by task 4.2 (and plugin tasks)

| Before (EF6) | After (EF Core) | Consumer impact |
|---|---|---|
| `IDbContext.Set<TEntity>()` → `IDbSet<TEntity>` | → `DbSet<TEntity>` | **Nop.Services is unaffected** — no file under `Nop.Services` names `IDbSet` or `System.Data.Entity`. Only the 4 plugin contexts below re-declare `Set<T>()`. |
| `EfRepository<T>.Entities` → `IDbSet<T>` (protected) | → `DbSet<T>` | Affects subclasses of `EfRepository<T>` only; none in tree. |
| `EfRepository<T>.GetFullErrorText(DbEntityValidationException)` (protected) | `GetFullErrorText(DbUpdateException)` | EF Core has no client-side validation and no `DbEntityValidationException`. All six `catch` blocks in `EfRepository` now catch `DbUpdateException`. **Validation errors that EF6 raised before hitting the database now surface as provider errors from the database instead.** |
| `QueryableExtensions.IncludeProperties<T>(…)` | same, plus `where T : class` | EF Core's `Include` expression overload is `where TEntity : class`. No in-tree caller — all entities derive from `BaseEntity`. |
| `CreateTablesIfNotExist<TContext> : IDatabaseInitializer<TContext>` | `: INopDatabaseInitializer<TContext>` (new, in `Nop.Data.Initializers`) | `InitializeDatabase(TContext)` signature is unchanged; only the interface it satisfies changed. |
| — | **additive** `NopObjectContext(DbContextOptions<NopObjectContext>)` | For tests/hosts that configure the provider themselves. |
| — | **additive** `static SqlServerDataProvider.DatabaseInitializer` | Replaces EF6's global `Database.SetInitializer` (see 4.8). |
| `Nop.Data.Initializers.SqlCeInitializer<T>`, `CreateCeDatabaseIfNotExists<T>`, `DropCreateCeDatabaseAlways<T>`, `DropCreateCeDatabaseIfModelChanges<T>` | **deleted** | Referenced by nothing outside each other. |

**Unchanged — do not touch:**

- `Nop.Core.Data.IRepository<T>` — completely unchanged. `EfRepository<T>` still implements it
  with identical public signatures, so every `IRepository<T>` consumer in `Nop.Services` and the
  plugins compiles as-is.
- `Nop.Core.Data.IDataProvider` — unchanged, including `DbParameter GetParameter()`.
  **Nop.Core was not modified by this task.**
- `IDbContext.SaveChanges`, `ExecuteStoredProcedureList<T>`, `SqlQuery<T>`, `ExecuteSqlCommand`,
  `Detach`, `ProxyCreationEnabled`, `AutoDetectChangesEnabled` — all signatures preserved
  (behavior notes in §5 and 4.7).
- `NopObjectContext(string nameOrConnectionString)` — preserved (see 4.9 for the value it now
  needs).
- `DataReaderExtensions` — unchanged; it was already pure ADO.NET/reflection and is now also
  used by `NopObjectContext.SqlQuery<T>`.

### Required edit to the four plugin `*Map` classes

`Mapping/NopEntityTypeConfiguration.cs` changed shape: derived maps no longer configure in a
**constructor**, they override
`public override void Configure(EntityTypeBuilder<T> builder)` and call `base.Configure(builder)`
at the end (the base implementation is what runs the `PostInitialize()` hook). The mechanical
edit applied to all 105 maps in `Nop.Data` must also be applied to these four, in their own
plugin tasks:

- `Nop.Plugin.Feed.GoogleShopping/Data/GoogleProductRecordMap.cs` (task 11.2)
- `Nop.Plugin.Pickup.PickupInStore/Data/StorePickupPointMap.cs` (task 13.1)
- `Nop.Plugin.Shipping.FixedOrByWeight/Data/ShippingByWeightRecordMap.cs` (task 14.4)
- `Nop.Plugin.Tax.FixedOrByCountryStateZip/Data/TaxRateMap.cs` (task 15.1)

Their contexts also register mappings the EF6 way and must move to
`modelBuilder.ApplyConfigurationsFromAssembly(...)`, as `NopObjectContext.OnModelCreating` now
does.


---

# Nop.Services — System.Web → ASP.NET Core, ImageResizer → ImageSharp, ASMX → HttpClient (task 4.2)

`Nop.Services` reached **0 errors / 11 warnings**. As with the two earlier stages, the compiler
cannot see the runtime consequences of replacing an ambient hosting framework with an injected
one. Everything below was introduced knowingly by task 4.2.

## 7. Open deferrals — Nop.Services

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 13 | Cookie authentication is not configured — nobody can sign in | 6.4, 7.2 | **Highest** |
| 14 | `IHttpContextAccessor` is not registered — 4 services see a null context | 6.4 | **Highest** |
| 15 | Session state is not configured — external authentication round-trip fails closed | 6.4, 7.2 | High |
| 16 | EU VAT service endpoint is a compiled-in constant, not configuration | 7.4 | Medium |
| 17 | Compare / recently-viewed cookie payload format changed — stale cookies ignored | none (accept) | Low |
| 18 | ImageSharp emits a licence *error*, currently downgraded to a warning | business decision | **Blocking for release** |
| 19 | `Nop.Data` deferrals 4.7 / 4.8 / 4.11 name task 4.2 as an owner but are **not** fixable inside `Nop.Services` | 7.2 | see below |

### 7.13 Cookie authentication is not configured — nobody can sign in — HIGHEST IMPACT

- **What changed:** `Authentication/FormsAuthenticationService.cs` was built on
  `System.Web.Security` forms authentication. `FormsAuthentication`,
  `FormsAuthenticationTicket` and `FormsIdentity` do not exist on .NET 10 in any form. The class
  now uses ASP.NET Core **cookie authentication over a claims principal**:
  `HttpContext.SignInAsync` / `SignOutAsync` / `HttpContext.User`.
- **Current runtime behavior:** `SignIn` throws `InvalidOperationException`
  ("No authentication handler is registered for the scheme 'Cookies'") until the host calls
  `services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)`
  and `app.UseAuthentication()`. `GetAuthenticatedCustomer()` returns null because
  `HttpContext.User` is never populated.
- **Fix (tasks 6.4 / 7.2):** register the cookie handler and add `UseAuthentication()` to the
  pipeline **before** anything that reads `IWorkContext`. The options below replace the
  `<forms>` element of the old `web.config`; **do not leave them at defaults silently** —
  each one used to be an explicit nopCommerce setting:

  | System.Web `<forms>` attribute | ASP.NET Core equivalent |
  |---|---|
  | `name` (`FormsAuthentication.FormsCookieName`) | `CookieAuthenticationOptions.Cookie.Name` |
  | `path` (`FormsCookiePath`) | `Cookie.Path` |
  | `domain` (`CookieDomain`) | `Cookie.Domain` |
  | `requireSSL` (`RequireSSL`) | `Cookie.SecurePolicy = CookieSecurePolicy.Always` |
  | `timeout` (`FormsAuthentication.Timeout`) | `ExpireTimeSpan` |
  | `<forms>` ticket encryption | data protection (automatic) — **key ring must be persisted** |

- **Impact if unfixed:** login is completely broken and every request is anonymous.
- **Security note — read before signing this off:**
  * `HttpOnly` is `true` by default on the ASP.NET Core auth cookie, so the explicit
    `cookie.HttpOnly = true` of the old code is not a regression.
  * `Secure` is **not** on by default; it comes from `Cookie.SecurePolicy`. The old code set
    `cookie.Secure = FormsAuthentication.RequireSSL`. **If `requireSSL="true"` was set in the
    3.90 `web.config`, `CookieSecurePolicy.Always` MUST be configured or the port is a
    downgrade.**
  * The ticket payload is now a claims cookie protected by ASP.NET Core Data Protection. In a
    multi-instance deployment the data-protection key ring must be shared (`PersistKeysTo*`),
    otherwise auth cookies stop validating across instances — the same class of problem as
    the old `machineKey`.
  * `AuthenticationProperties.ExpiresUtc` is always set from a **30-minute** default baked
    into the service constructor, because there is no ambient `FormsAuthentication.Timeout` to
    read. That is the ASP.NET Framework forms-auth default. If 3.90's `web.config` used a
    different `timeout`, set `ExpireTimeSpan` on the handler to match — the handler value wins.

### 7.14 `IHttpContextAccessor` is not registered — four services see a null context

- **What changed:** the `HttpContextBase` constructor parameter of four services became
  `IHttpContextAccessor` (table in §9 below). `Nop.Web.Framework/DependencyRegistrar.cs`
  currently registers `HttpContextBase`, `HttpRequestBase`, `HttpResponseBase`,
  `HttpServerUtilityBase` and `HttpSessionStateBase` (lines 74–90) — **all five of those
  registrations no longer resolve to anything and no longer have a consumer in
  `Nop.Services`.**
- **Fix (task 6.4):** call `services.AddHttpContextAccessor()` (or register
  `HttpContextAccessor` as `IHttpContextAccessor`, single instance) and delete lines 74–90.
  Note `Nop.Core`'s `WebHelper` and `PerRequestCacheManager` need the same registration
  (breaking-change table in §3), so a single registration serves both stages.
- **Impact if unfixed:** every affected service silently degrades rather than throwing —
  each one null-checks the context first. Concretely: compare-products and recently-viewed
  lists stay empty, `IUserAgentHelper.IsSearchEngine()` always returns false (so crawlers are
  treated as customers), and authentication is dead (7.13).

### 7.15 Session state is not configured — external authentication round-trip fails closed

- **What changed:** `Authentication/External/ExternalAuthorizerHelper.cs` resolved
  `System.Web.HttpSessionStateBase` from the container and used its **object** indexer to park
  an `OpenAuthenticationParameters` instance across the redirect to the external provider.
  ASP.NET Core's `ISession` is a byte-array store with no object indexer and no implicit
  serialization, so the values are now stored as UTF-8 JSON (`System.Text.Json`).
- **Current runtime behavior:** `HttpContext.Session` throws `InvalidOperationException` when
  the session feature is absent. The helper catches that and returns null / no-ops, so external
  authentication **fails closed** instead of throwing.
- **Fix (tasks 6.4 / 7.2):** `services.AddSession()` plus `app.UseSession()`, ordered before
  MVC. A distributed session store is required for multi-instance deployments.
- **Serialization contract (permanent change):** `OpenAuthenticationParameters` is abstract, so
  the concrete type's assembly-qualified name is persisted next to the JSON payload.
  Rehydration is **deliberately constrained**: the recorded type must resolve *and* must be
  assignable to `OpenAuthenticationParameters`, otherwise the entry is discarded. This is what
  stops the session value being usable as an arbitrary-type deserialization gadget, which a
  naive `$type` round-trip would allow. Consequences for the plugin tasks (11.1 in particular):
  every concrete `OpenAuthenticationParameters` subclass must be **JSON round-trippable** —
  public settable properties, public parameterless constructor. `[Serializable]` no longer
  means anything here.

### 7.16 EU VAT service endpoint is a compiled-in constant, not configuration

- **What changed:** `Tax/TaxService.cs` called a generated ASMX proxy
  (`Web References/EuropaCheckVatService/Reference.cs`, deriving from
  `System.Web.Services.Protocols.SoapHttpClientProtocol`) whose endpoint URL came from
  `Properties/Settings.settings` via `System.Configuration.ApplicationSettingsBase`. All three
  files were **deleted**; the SOAP call is now hand-built over `HttpClient` +
  `System.Xml.Linq`. The URL lives in `TaxService.DefaultEuropaCheckVatServiceUrl` and is read
  through the `protected virtual TaxService.EuropaCheckVatServiceUrl` property.
- **Fix (task 7.4, config migration):** surface it as configuration — either a new key on
  `Nop.Core.Domain.Tax.TaxSettings` (already injected into `TaxService`) or an
  `appsettings.json` entry bound via `IConfiguration` — and override
  `EuropaCheckVatServiceUrl` to read it.
- **Also:** the retired WSDL declares the endpoint over plain **HTTP**. When this becomes
  configurable, default it to `https://ec.europa.eu/taxation_customs/vies/services/checkVatService`.
- **Impact if unfixed:** the endpoint cannot be redirected to a test double or an updated URL
  without a recompile. Functionally correct in the meantime.

### 7.17 Compare / recently-viewed cookie payload format changed

- `System.Web.HttpCookie.Values` was a multi-valued sub-key collection ("a=1&a=2" inside one
  cookie). ASP.NET Core's `IRequestCookieCollection`/`IResponseCookies` are flat string
  key/value, so `Catalog/CompareProductsService.cs` and
  `Catalog/RecentlyViewedProductsService.cs` now store a single comma-separated value under the
  **same cookie names** (`nop.CompareProducts`, `NopCommerce.RecentlyViewedProducts`).
- **Impact:** a visitor holding a 3.90-era cookie is parsed as "no ids" and simply starts with
  an empty list; the next write replaces the cookie. `HttpOnly` and the 10-day expiry are
  preserved. No fix required — recorded so it is not mistaken for a bug.

### 7.18 ImageSharp emits a licence error, currently downgraded to a warning

- `SixLabors.ImageSharp` 4.1.1's build targets emit
  *"No Six Labors license found … Please obtain a license from https://sixlabors.com/pricing/"*.
  MSBuild reports it as a **warning only because the task runs with `ContinueOnError=true`** —
  the underlying diagnostic is an error. This is the Six Labors Split License already flagged
  in `src/Directory.Packages.props`.
- **Action:** a business decision, not a code one. Either set `$(SixLaborsLicenseKey)` /
  `$(SixLaborsLicenseFile)` / add `sixlabors.lic`, or pin back to the last purely Apache-2.0
  line (2.1.x). It affects `Nop.Services` now and `Nop.Admin` at task 8.6.
- These two lines are 2 of the 11 warnings at the 4.3 gate.

### 7.19 `Nop.Data` deferrals that name task 4.2 but are not fixable here

Task 4.2 is listed as an owner of Nop.Data deferrals **4.7, 4.8 and 4.11**. Each was
investigated and left open, for the reasons below.

- **4.7 lazy loading is off.** The two available fixes are (1) add
  `Microsoft.EntityFrameworkCore.Proxies` and call `UseLazyLoadingProxies()` in
  `NopObjectContext.OnConfiguring` — that is a `Nop.Data` edit, and `Nop.Data` has already
  passed its gate; or (2) add explicit `Include`/`ThenInclude` at hundreds of `Nop.Services`
  query sites. Neither is required for the compile gate and (2) should not be undertaken
  before the (1)-versus-(2) decision is made. **Still owned by the decision at 7.2.**
  Note `Media/PictureService.cs`'s `StoreInDb` setter still toggles
  `IDbContext.ProxyCreationEnabled` as a performance hack; per 4.7 that is now a no-op with
  respect to proxies, and it becomes meaningful again if option (1) is taken.
- **4.8 schema initializer never invoked.** `Nop.Services` never constructs a
  `NopObjectContext` and contains no reference to `SqlServerDataProvider.DatabaseInitializer`;
  `SetDatabaseInitializer()` is called from `Nop.Data/EfStartUpTask`. The
  `DatabaseInitializer?.InitializeDatabase(...)` call therefore belongs in the host
  installation path. **Wholly owned by 7.2.**
- **4.11 `ExecuteSqlCommand` now opens a real transaction.** The affected `Nop.Services` call
  site is `Installation/SqlFileInstallationService.ExecuteSqlFile`, which already splits the
  script on `GO` and issues one `ExecuteSqlCommand(stmt)` per batch — so every batch of
  `create_required_data.sql` / `create_sample_data.sql` now runs inside its own transaction.
  Changing that to `doNotEnsureTransaction: true` is a **database semantics decision** and was
  deliberately **not** made silently: it would remove per-batch atomicity from installation.
  Flagged for review at 7.2 when installation is first exercised against a real database. The
  `TRUNCATE TABLE` sites (`DefaultLogger`, `CustomerActivityService`, `QueuedEmailService`) and
  `MaintenanceService`'s `DBCC CHECKIDENT` are unchanged and run fine inside a transaction.

### 7.20 Paths inherited from Nop.Core deferral 1.5

`Common/MaintenanceService.GetBackupDirectoryPath()` used
`HttpRequest.PhysicalApplicationPath`, which does not exist in ASP.NET Core. It now uses
`CommonHelper.MapPath("~/")` + `Path.Combine(..., "Administration", "db_backups")`. That
inherits **open deferral 1.5** verbatim: until the host sets `CommonHelper.BaseDirectory` to
the content root, database backups resolve under `bin/`. `Helpers/UserAgentHelper` likewise
still depends on **deferral 1.4** (`NopConfig.UserAgentStringsPath` unset ⇒ the crawler parser
never loads and `IsSearchEngine()` returns false).

---

## 8. Final behavioral changes — Nop.Services, intentional, no future fix needed

### Image processing (design §7)

- **`Media/PictureService.cs` resize semantics.** ImageResizer was always called with an
  explicit `Width` **and** `Height`, for which its default `FitMode` is `Pad`: it produced
  exactly the requested box and filled any sub-pixel remainder with background. (The
  `Math.Round` comment in `CalculateDimensions`, and the nopCommerce forum link beside it,
  record that the authors were fighting precisely that white padding.) ImageSharp is used with
  **`ResizeMode.Max`**, which scales to fit inside the same box preserving aspect ratio and
  **never pads**. Observable difference: an output edge may be **up to one pixel shorter** than
  ImageResizer's padded output, and no background colour is ever introduced.
  Everything else is preserved: `CalculateDimensions` is **untouched** (same `ResizeType`
  LongestSide/Width/Height rules, same 1px minimum clamp, same `Math.Round`), and
  `Scale = ScaleMode.Both` (upscaling permitted) maps onto `ResizeMode.Max`, which also scales
  up.
- **`ValidatePicture`** used ImageResizer's `MaxWidth`/`MaxHeight`, i.e. shrink-to-fit and
  never enlarge. Reproduced as `ResizeMode.Max` guarded by an explicit
  `image.Width > max || image.Height > max` test. As before, the binary is always re-encoded
  (so `DefaultImageQuality` is applied) even when no resize happens.
- **Resampler:** `KnownResamplers.Bicubic`, the closest match to ImageResizer's default
  high-quality interpolation and ImageSharp's own `Resize` default.
- **Encoder:** the decoded format is preserved via `image.Metadata.DecodedImageFormat`.
  `MediaSettings.DefaultImageQuality` maps to `JpegEncoder.Quality`; every other format uses
  ImageSharp's registered default encoder, whose GIF encoder performs the palette quantization
  that `ImageResizer.Plugins.PrettyGifs` used to provide. An unrecognised format falls back to
  JPEG, matching `GetFileExtensionFromMimeType`'s own JPEG default.
- **Corrupt-binary handling:** `new Bitmap(stream)` threw GDI+'s `ArgumentException`
  ("Parameter is not valid"); the replacement catches ImageSharp's `ImageFormatException`
  (base of `UnknownImageFormatException` and `InvalidImageContentException`). The
  log-and-return-empty-url behaviour is unchanged.
- **`ExportImport/ExportManager.cs` was NOT changed, and does not need to be.** `tasks.md`
  step 4.2 and design §7 both list it as an ImageSharp target; that overstates the case. Its
  only `System.Drawing` usage is `Color.FromArgb(184, 204, 228)` for an EPPlus cell fill.
  `System.Drawing.Color` lives in **`System.Drawing.Primitives`**, which is part of the
  cross-platform `net10.0` shared framework — not in the Windows-only
  `System.Drawing.Common`. Verified on the built assembly: `Nop.Services.dll` references
  `System.Drawing.Primitives 10.0.0.0` and **no** `System.Drawing.Common`,
  `ImageResizer` or `System.Web*` assembly. Rewriting it to ImageSharp would be churn with no
  portability benefit.

### System.Web replacements

- **`System.Web.HttpUtility` → `System.Net.WebUtility`** in `Catalog/ProductAttributeFormatter`,
  `Common/AddressAttributeFormatter`, `Customers/CustomerAttributeFormatter`,
  `Orders/CheckoutAttributeFormatter`, `Messages/MessageTokenProvider`, `Messages/Tokenizer`.
  `HtmlEncode`/`HtmlDecode` are identical. **`WebUtility.UrlEncode` encodes a space as `%20`
  where `HttpUtility` used `+`** — both decode to a space server-side. The three affected
  URL-encoding sites are the password-recovery, account-activation and email-revalidation
  links in `MessageTokenProvider` (lines ~1083–1085), which encode an **email address**, so
  the difference is unobservable in practice.
  `HttpUtility.HtmlEncode` had an `object` overload; `WebUtility.HtmlEncode` is string-only, so
  `Tokenizer.ReplaceTokens` now calls `.ToString()` explicitly — same result.
- **`HttpServerUtilityBase.HtmlEncode` → `WebUtility.HtmlEncode`** at the four contact-us sites
  in `Messages/WorkflowMessageService`. This removed the class's only use of the HTTP context.
- **`System.Web.MimeMapping.GetMimeMapping` →
  `Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider.TryGetContentType`** in
  `ExportImport/ImportManager.GetMimeTypeFromFilePath`. Both are extension-driven. The
  provider signals "unknown" by returning `false`, which is mapped onto
  `MimeTypes.ApplicationOctetStream` so the existing "little hack" that rewrites
  octet-stream to `image/jpeg` still fires for the same inputs. One shared static instance
  (the provider is thread-safe for lookups).
- **`System.Web.WebPages` `string.IsEmpty()` → `String.IsNullOrEmpty(...)`** at the three call
  sites in `ExportImport/ImportManager` (`categoryIds`, `sku`, `manufacturerIds`). Identical
  semantics.
- **`System.Linq.Dynamic` → `System.Linq.Dynamic.Core`** in `Messages/Tokenizer`. The
  string-predicate `Where()` moved from `IEnumerable` to `IQueryable`
  (`DynamicQueryableExtensions`), so `ReplaceConditionalStatements` now does
  `new[] { statement }.AsQueryable().Where(conditionString)`. The expression language and
  evaluation semantics are unchanged, as is the surrounding `try { } catch { }` that treats an
  unparsable condition as "not met".
- **`System.Data.SqlClient` → `Microsoft.Data.SqlClient`** in `Common/MaintenanceService`
  (`SqlConnection`, `SqlCommand`, `SqlConnectionStringBuilder`,
  `SqlConnection.ClearAllPools`). Same type names; this is also what
  `Nop.Data.SqlServerDataProvider` already returns, so there is one SQL client in the process.
- **`Microsoft.WindowsAzure.Storage[.Blob]` → `Microsoft.Azure.Storage[.Blob]`** in
  `Media/AzurePictureService` — a `using`-only change (Microsoft renamed the same codebase).
  `CloudStorageAccount`/`CloudBlobClient`/`CloudBlobContainer` are unchanged. The follow-up to
  `Azure.Storage.Blobs` 12.x remains unscheduled (flagged in `Directory.Packages.props`).

### EU VAT SOAP client

- **`Tax/TaxService.DoVatCheck` keeps its exact public signature**
  (`VatNumberStatus DoVatCheck(string, string, out string, out string, out Exception)`), and so
  does `GetVatNumberStatus`. Only the innards changed: `new EuropaCheckVatService.checkVatService()`
  + `s.checkVat(ref, ref, out, out, out)` became a new
  `protected virtual void CheckVatEuropa(string, string, out bool, out string, out string)`
  that POSTs a hand-built SOAP 1.1 envelope with `HttpClient` and reads the reply with
  `System.Xml.Linq`. No NuGet package was added — both are in the shared framework.
- **Failure behavior is preserved exactly.** The original had a
  `try { … } catch (Exception ex) { name = address = string.Empty; exception = ex; return VatNumberStatus.Unknown; } finally { normalise nulls }`.
  That structure is **unchanged and still wraps the call**; only the `finally`'s
  `s.Dispose()` was dropped (there is no proxy to dispose — the `HttpClient` is a shared static
  and the `HttpResponseMessage` is in a `using`). Everything that used to throw still throws:
  a SOAP Fault (the WSDL documents `INVALID_INPUT`, `SERVICE_UNAVAILABLE`, `MS_UNAVAILABLE`,
  `TIMEOUT`, `SERVER_BUSY`), a transport failure, a non-success status code, a timeout, or an
  unparsable/incomplete body. Faults are checked **before** the status code, because VIES may
  return a fault with either 200 or 500. The *declared* exception types differ
  (`HttpRequestException`/`NopException`/`TaskCanceledException` instead of
  `SoapException`/`WebException`), which is invisible to callers because the catch is
  `catch (Exception)`. Absent or nil `name`/`address` elements still come back as
  `string.Empty`.
- **Wire contract** reproduced from the retired `checkVatService.wsdl`: SOAP 1.1,
  `soapAction=""` (sent as `SOAPAction: ""`, which SOAP 1.1 requires to be present),
  envelope namespace `http://schemas.xmlsoap.org/soap/envelope/`, message namespace
  `urn:ec.europa.eu:taxud:vies:services:checkVat:types` with `elementFormDefault="qualified"`.
  Request `checkVat` → `countryCode`, `vatNumber`. Response `checkVatResponse` → `countryCode`,
  `vatNumber`, `requestDate`, `valid`, `name?`, `address?`. Only the five fields the caller
  consumes are read; `checkVatApprox` was never used by nopCommerce and was not ported.
- **A single static `HttpClient`** with a 30-second timeout (standing in for the ASMX proxy's
  inherited `SoapHttpClientProtocol.Timeout`) avoids socket exhaustion. It is not created per
  call.

### PDF generation

- **`Common/PdfService.cs`: 16 constant references renamed** for
  `iTextSharp.LGPLv2.Core` 3.8.5, which derives from the 4.1.6-era tree and uses PascalCase
  where iTextSharp 5.5.10 used SCREAMING_CASE. Verified by reflecting over the package's
  `lib/net10.0` assembly:
  | 5.5.10 | iTextSharp.LGPLv2.Core 3.8.5 | sites |
  |---|---|---|
  | `PageSize.LETTER` | `PageSize.Letter` | 3 |
  | `BaseColor.BLACK` | `BaseColor.Black` | 3 |
  | `BaseColor.LIGHT_GRAY` | `BaseColor.LightGray` | 10 |
  These were the **only** 5.5.10-era API gaps in the file — every other type and member
  `PdfService` uses (`Document`, `PdfWriter`, `PdfPTable`, `PdfPCell`, `Paragraph`, `Phrase`,
  `Font`, `BaseFont`, `BaseColor`, `PageSize`, `Rectangle`, `Image`, `Anchor`) resolved
  unchanged. `PageSize.A4` was already PascalCase in both. No switch to itext7 was needed or
  made.

### Sync-over-async

Three newly introduced blocking waits, all on interfaces that are synchronous and whose call
sites (8 in `Nop.Web/Controllers/CustomerController.cs` for auth, plus `ITaxService`) must not
change:

- `FormsAuthenticationService.SignIn` → `HttpContext.SignInAsync(...).GetAwaiter().GetResult()`
- `FormsAuthenticationService.SignOut` → `HttpContext.SignOutAsync(...).GetAwaiter().GetResult()`
- `TaxService.CheckVatEuropa` → `HttpClient.Send(...)` (the synchronous overload) and
  `response.Content.ReadAsStringAsync().GetAwaiter().GetResult()`

ASP.NET Core installs no `SynchronizationContext`, so none of these can deadlock; they occupy
the request thread for the duration. Making `IAuthenticationService` and `ITaxService` async is
a legitimate follow-up but is out of scope for a compile-gated port.

### Files deleted

- **`Web References/EuropaCheckVatService/`** — the whole folder (`Reference.cs`,
  `checkVatService.wsdl`, `Reference.map`, `matchCode.datasource`). The `WebReference` build
  feature does not exist in the SDK, and `SoapHttpClientProtocol` has no net10.0
  implementation in the framework or in any NuGet package.
- **`Properties/Settings.Designer.cs`** and **`Properties/Settings.settings`** — these existed
  solely to hand the deleted proxy its endpoint URL through
  `System.Configuration.ApplicationSettingsBase`. `Properties/AssemblyInfo.cs` is retained
  (matching Nop.Core and Nop.Data).

### Confirmations — items in the task description that turned out to be non-issues

- **`ConfigurationManager`:** no file under `Nop.Services` references
  `System.Configuration.ConfigurationManager`. Re-verified by grep after the port. The only
  `System.Configuration` surface in the project was the deleted `Settings.Designer.cs`.
  Nothing to migrate to `IConfiguration`/options here — the equivalent work was done in
  `Nop.Core` (see deferral 1.4, `NopConfigurationManager`).
- **AutoMapper:** no file under `Nop.Services` references AutoMapper, and the project declares
  no `PackageReference` for it. It reaches the assets graph only transitively through
  `Nop.Core`, and the built `Nop.Services.dll` has **no** AutoMapper assembly reference. No
  API updates were needed.
- **`ICacheManager`:** unchanged and untouched. `Nop.Services` consumes it through
  `Nop.Services/Caching` and the per-service cache keys only; the re-basing onto
  `IMemoryCache`/`IHttpContextAccessor` happened entirely inside `Nop.Core` (task 2.4) behind a
  stable interface, so nothing in `Nop.Services` had to change.
- **`IDependencyRegistrar`:** `Nop.Services` contains **no** `IDependencyRegistrar`
  implementation — the interface lives in `Nop.Core`, and the registrations for services live
  in `Nop.Web.Framework/DependencyRegistrar.cs` and the plugins. Nothing to preserve, nothing
  touched.
- **`Media/PictureService.cs` and `HttpContextBase`:** the comment block in
  `Nop.Services.csproj` lists `PictureService` among the six files with a `HttpContextBase`
  constructor parameter. It never had one. The sixth file is
  **`Messages/WorkflowMessageService.cs`**.

---

## 9. Breaking constructor / signature changes — Nop.Services

### 9a. Constructor changes — owned by task 6.x (`Nop.Web.Framework/DependencyRegistrar.cs`)

All six services are registered with reflection-based `builder.RegisterType<T>()`, so Autofac
picks up the new constructors automatically **provided `IHttpContextAccessor` is registered**
(deferral 7.14). No `.WithParameter` call needs editing.

| Before | After | Note |
|---|---|---|
| `FormsAuthenticationService(HttpContextBase, ICustomerService, CustomerSettings)` | `FormsAuthenticationService(IHttpContextAccessor, ICustomerService, CustomerSettings)` | also needs the cookie handler — deferral 7.13 |
| `CompareProductsService(HttpContextBase, IProductService, CatalogSettings)` | `CompareProductsService(IHttpContextAccessor, IProductService, CatalogSettings)` | |
| `RecentlyViewedProductsService(HttpContextBase, IProductService, CatalogSettings)` | `RecentlyViewedProductsService(IHttpContextAccessor, IProductService, CatalogSettings)` | |
| `UserAgentHelper(NopConfig, HttpContextBase)` | `UserAgentHelper(NopConfig, IHttpContextAccessor)` | |
| `MaintenanceService(IDataProvider, IDbContext, CommonSettings, HttpContextBase)` | `MaintenanceService(IDataProvider, IDbContext, CommonSettings)` | **parameter dropped** — the only use was `Request.PhysicalApplicationPath` |
| `WorkflowMessageService(…, IEventPublisher, HttpContextBase)` | `WorkflowMessageService(…, IEventPublisher)` | **parameter dropped** — the only use was `Server.HtmlEncode` |

**DI registrations to delete** — `Nop.Web.Framework/DependencyRegistrar.cs` lines **74–90**:
`HttpContextBase` (incl. the `HttpContext.Current != null ? new HttpContextWrapper(...) : new FakeHttpContext("~/")`
branch), `HttpRequestBase`, `HttpResponseBase`, `HttpServerUtilityBase`, `HttpSessionStateBase`.
None of the five has a consumer left in `Nop.Services`. Replace with
`services.AddHttpContextAccessor()`. See the `FakeHttpContext` note in §3 — it was
re-implemented over a composed `DefaultHttpContext` and `WebWorkContext.cs` line 186's
`_httpContext is FakeHttpContext` check still needs a decision now that nothing injects
`HttpContextBase`.

### 9b. Public interface / type signature changes — owned by tasks 6.x, 7.x, 8.x and the plugin tasks

| Member | Before | After | Consumers to fix |
|---|---|---|---|
| `ISitemapGenerator.Generate` (2 overloads) and 7 `protected virtual` helpers on `SitemapGenerator` | `System.Web.Mvc.UrlHelper` | `Microsoft.AspNetCore.Mvc.IUrlHelper` | `Nop.Web` sitemap controller. `IUrlHelper.RouteUrl(name, values, protocol)` is signature-compatible (`UrlHelperExtensions`), so only the type changes. |
| `IMiscPlugin.GetConfigurationRoute`, `IWidgetPlugin` (2), `IPaymentMethod` (2), `IShippingRateComputationMethod`, `IPickupPointProvider`, `ITaxProvider`, `IExternalAuthenticationMethod` (2) | `System.Web.Routing.RouteValueDictionary` | `Microsoft.AspNetCore.Routing.RouteValueDictionary` | **every one of the 20 plugin projects** implements at least one of these. `using` change only — the type name and members are the same. |
| `AuthorizeState.Result` | `System.Web.Mvc.ActionResult` | `Microsoft.AspNetCore.Mvc.ActionResult` | `Nop.Web` external-auth controller, `Nop.Plugin.ExternalAuth.Facebook` (task 11.1). `new RedirectResult("~/")` still resolves `~/` — the Core `RedirectResultExecutor` runs the URL through `IUrlHelper.Content`. |
| `PropertyByName<T>.DropDownElements`, `PropertyManager<T>.SetSelectList(string, SelectList)`, `Nop.Services.Extensions.ToSelectList<T>` (2 overloads) | `System.Web.Mvc.SelectList` | `Microsoft.AspNetCore.Mvc.Rendering.SelectList` | `Nop.Admin` export/import wiring and any view calling `ToSelectList`. The `SelectList(items, dataValueField, dataTextField, selectedValue)` constructor and `SelectListItem.Text`/`.Value` are unchanged, and `SelectList` still enumerates as `IEnumerable<SelectListItem>`. |
| `Media.Extensions.GetDownloadBits`, `GetPictureBits` (extension methods) | `this System.Web.HttpPostedFileBase` | `this Microsoft.AspNetCore.Http.IFormFile` | **every upload action in `Nop.Web` and `Nop.Admin`** must change its parameter from `HttpPostedFileBase` to `IFormFile`. `InputStream` → `OpenReadStream()`, `ContentLength` (int) → `Length` (long). The single `Read` call was also replaced by `Stream.CopyTo`, because one `Read` is not guaranteed to fill the buffer on a non-buffered ASP.NET Core request stream — this fixes a latent truncation bug. |
| `PictureService.CalculateDimensions(Size, int, ResizeType, bool)` (`protected virtual`) | `System.Drawing.Size` | `SixLabors.ImageSharp.Size` | **no in-tree override** — verified: `AzurePictureService` is the only subclass and overrides only `DeletePictureThumbs`, `GetThumbLocalPath`, `GetThumbUrl`, `GeneratedThumbExists`, `SaveThumb`. `IPictureService` never exposed `Size`, so the public surface is unchanged. |

### 9c. New members added by task 4.2 (additive — nothing to fix, listed for discoverability)

- `FormsAuthenticationService`: `public const string UsernameOrEmailClaimType`
  (`"Nop.Customer.UsernameOrEmail"`, the replacement for `FormsAuthenticationTicket.UserData`),
  `public const string ClaimsIssuer`, `protected virtual HttpContext HttpContext`,
  `protected virtual string AuthenticationScheme`,
  `protected virtual Customer GetAuthenticatedCustomerFromPrincipal(ClaimsPrincipal)`
  (replaces `GetAuthenticatedCustomerFromTicket(FormsAuthenticationTicket)`).
- `TaxService`: `public const string DefaultEuropaCheckVatServiceUrl`,
  `protected virtual string EuropaCheckVatServiceUrl`,
  `protected virtual void CheckVatEuropa(string, string, out bool, out string, out string)`.
- `PictureService`: `protected virtual IResampler Resampler`,
  `protected virtual IImageEncoder GetImageEncoder(IImageFormat, int)`,
  `protected virtual byte[] ResizeImage(Image, int)`,
  `protected virtual byte[] EncodeImage(Image)`.
- `CompareProductsService`: `protected virtual HttpContext HttpContext`,
  `protected virtual void SetComparedProductIds(IEnumerable<int>)`.
- `RecentlyViewedProductsService`: `protected virtual HttpContext HttpContext`, plus a
  `RECENTLY_VIEWED_PRODUCTS_COOKIE_NAME` constant replacing the repeated literal.
- `ImportManager`: `private static readonly FileExtensionContentTypeProvider MimeTypeProvider`.

**Unchanged — do not touch:** `IPictureService`, `IAuthenticationService`, `ITaxService`,
`IMaintenanceService`, `IWorkflowMessageService`, `IUserAgentHelper`, `ICompareProductsService`,
`IRecentlyViewedProductsService`, `ITokenizer`, `IImportManager`, `IExportManager` and
`IPdfService` all keep their exact 3.90 signatures.

---

## 10. Security pin — `System.Drawing.Common` (task 4.2 preliminary step)

`EPPlus` 4.5.3.3 pulls `System.Drawing.Common >= 4.7.0` through its `netstandard2.0` asset.
**4.7.0 carries GHSA-rxg9-xrhp-64gj / CVE-2021-24112 (CRITICAL, remote code execution via a
crafted metafile)**, which NuGet reported as `NU1904` on every restore. It is fixed in
**4.7.2** — 4.7.1 is still affected, so 4.7.2 is the lowest stable release that clears the
advisory.

`src/Directory.Packages.props` now sets
`<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` and adds
`<PackageVersion Include="System.Drawing.Common" Version="4.7.2" />`, with the reason recorded
in a comment beside it. Transitive pinning is what allows a purely transitive package to be
lifted without declaring it as a direct `PackageReference` in every consuming project.

Verified after the change: restore is silent (**no `NU1904`**), `project.assets.json` resolves
`System.Drawing.Common/4.7.2` and lists it under **no** project's direct dependencies, and
`Nop.Services.deps.json` records `System.Drawing.Common/4.7.2`.

Runtime caveat, unchanged by the pin: `System.Drawing.Common` is Windows-only. Nothing in this
repository calls into it — `Nop.Services` only names `System.Drawing.Color`, which is in the
cross-platform `System.Drawing.Primitives`. EPPlus needs the package for autofit column
measurement and embedded images, neither of which `Nop.Services` uses (no `AutoFitColumns`,
`Drawings` or `AddPicture` call sites). Revisit if autofit is ever used on Linux — this is the
same trade-off already recorded against the EPPlus entry.
