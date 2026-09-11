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

All items below originate from **Nop.Core task 2.4**. **Every one is now RESOLVED by task 7.2 —
see §24 for the resolving line of code and §19.1 for the runtime verification.**

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 1 | ~~Plugin discovery never runs~~ | — | ✅ **RESOLVED by 7.2** (§24) — `UseNopHostingEnvironment`, verified fired |
| 2 | ~~Plugin assemblies invisible to the Razor compiler~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — call order guaranteed in `Program.cs` |
| 3 | ~~Per-request DI scope not shared within a request~~ | — | ✅ **RESOLVED by 6.4**, made live by 7.2's `AutofacServiceProviderFactory` (§21) — single container verified |
| 4 | Configuration source unset — all `NopConfig` settings at defaults | — | ✅ **FULLY RESOLVED**: seam by 7.2 (§24), `appsettings.json` authored by **7.4** (§32) — all 14 properties verified to bind |
| 5 | ~~`CommonHelper.MapPath` resolves relative to `bin/`~~ | — | ✅ **RESOLVED by 7.2** (§24), verified: `MapPath("~/App_Data/x")` lands under the content root |
| 6 | ~~`WebHelper.RestartAppDomain` throws instead of restarting~~ | — | ✅ **RESOLVED by 7.2** (§24) — `IHostApplicationLifetime` verified resolvable from the nop container |

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
  **6.5 supplied the mechanism and verified it live — see §18.1.** 7.2 gets it from
  `builder.Environment.UseNopHostingEnvironment(builder.Configuration)`.
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
| 7 | ~~Lazy loading is off — all `virtual` navigations return null~~ | — | **RESOLVED** (see 4.7) |
| 8 | ~~Schema initializer is never invoked — a fresh install creates no tables~~ | — | ⚠️ **7.2's fix was INERT — re-opened and genuinely RESOLVED by 7.7** (§42.1). Measured: a real install failed with `Invalid object name 'Store'` |
| 9 | `NopObjectContext` now needs a real connection string, not a database name | 3.4, ~~6.4~~ | Medium — **6.4 verified clean**, see §17.1 |
| 10 | `CreateDatabaseScript()` output is `GO`-batched — 4 plugin contexts will fail | 11.2, 13.1, 14.4, 15.1 | Medium |
| 11 | `ExecuteSqlCommand(doNotEnsureTransaction: false)` now opens a real transaction | ~~4.2~~, 7.7 | Medium — **reviewed at 7.2, no change made** (§24.2); needs a real database |
| 12 | ~~Many-to-many join **column** names follow EF Core conventions, not 3.90's~~ | — | ⚠️ **SEVERITY WAS WRONG — this was a BLOCKER, not a parity nicety. RESOLVED by 7.7** (§42.2): the stored procedures join these columns by their 3.90 names, so installation failed with `Invalid column name 'ProductTag_Id'` |

### 4.7 Lazy loading — ✅ **RESOLVED** (fixed ahead of task 7.2)

**Status: RESOLVED.** Fixed deliberately ahead of task 7.2 rather than at it, because the change
lands in `Nop.Data`, which had already passed its clean-compile gate (3.3) and was committed —
doing it knowingly here is preferable to discovering empty product listings at the 7.7 smoke check.

- **What was wrong:** EF6 created dynamic proxies by default, so every `public virtual` navigation
  on `Nop.Core.Domain.**` lazy-loaded on first access. EF Core has **no proxy layer in the core
  package**, so `order.Customer`, `product.ProductCategories`, `customer.CustomerRoles` and
  hundreds of similar walks returned `null` or an empty collection **silently, with no exception**.

#### What was done — option 1 of the two choices above

Option 2 (explicit `Include`/`ThenInclude` at hundreds of query sites in `Nop.Services`) was
**explicitly rejected** as too invasive for this migration.

1. `src/Directory.Packages.props` — added
   `<PackageVersion Include="Microsoft.EntityFrameworkCore.Proxies" Version="10.0.12" />`
   to the *Data access* group, aligned with the three EF Core 10.0.12 pins. Verified on nuget.org:
   stable (`isPrerelease: false`), listed, MIT, published by Microsoft, native **`lib/net10.0`**
   asset. Its `net10.0` dependency group requires `Microsoft.EntityFrameworkCore [10.0.12, )`,
   `Castle.Core [5.2.1, )`, `Microsoft.Extensions.Caching.Memory [10.0.12, )` and
   `Microsoft.Extensions.Logging [10.0.12, )`. Only **Castle.Core 5.2.1** is genuinely new to the
   graph; the other two already resolve to the pinned 10.0.12.
2. `src/Libraries/Nop.Data/Nop.Data.csproj` — added a version-less
   `<PackageReference Include="Microsoft.EntityFrameworkCore.Proxies" />`.
3. `NopObjectContext.OnConfiguring` — calls `optionsBuilder.UseLazyLoadingProxies()`.

No other file was touched. `Nop.Core`, `Nop.Services` and `Nop.Web.Framework` are unmodified.

#### `UseLazyLoadingProxies()` is OUTSIDE the `IsConfigured` guard — and must be

`DbContextOptionsBuilder.IsConfigured` reports whether a **database provider** has been selected,
so it is **`true`** for a context built from `DbContextOptions<NopObjectContext>` (the tests' /
alternate-host path added in 4.9). Placing the call inside
`if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_nameOrConnectionString))` would
therefore have given proxies to the **connection-string constructor only** and left every
options-built context with exactly the silent null-navigation behavior this fix exists to remove.
Lazy loading is orthogonal to provider selection, so it is applied unconditionally on every
construction path; the call is idempotent, so a caller that already enabled proxies is unaffected.
Both paths were verified live (see the probe below).

#### Proxy-requirement audit across `Nop.Core.Domain` — no violations

Castle DynamicProxy must be able to subclass each entity. EF Core enforces this at
**model-build time**, and both failure modes were confirmed empirically to be **BLOCKING (throw),
not degrading** — the error is
`Property 'X.Y' is not virtual. … 'UseLazyLoadingProxies' requires only the navigation properties
be virtual.` / `Entity type 'X' is sealed. …`, both `InvalidOperationException`. A violation would
therefore have converted a silent regression into a **hard startup failure**.

Audited surface: **105 mapped entity types** — 103 `Mapping/**/*Map.cs` plus two configurations
not following the `*Map.cs` naming (`Mapping/Orders/ReturnRequestAction.cs`,
`Mapping/Orders/ReturnRequestReason.cs`).

| Requirement | Result |
|---|---|
| public, not sealed | **105 / 105 pass** — zero `sealed` and zero non-public classes anywhere under `Nop.Core/Domain` |
| accessible parameterless constructor | **105 / 105 pass** — only 4 entities declare a constructor at all (`Customer`, `CustomerPassword`, `ExchangeRate`, `Setting`) and every one is parameterless; `Setting` additionally has `Setting(string, string, int)` alongside it |
| navigations `public virtual` | **140 / 140 pass** — 89 reference navigations `{ get; set; }` and 51 collection navigations with `get`/`protected set`; **zero** non-virtual, **zero** non-public, **zero** get-only |
| no entity dragged in by convention | **0** — every CLR entity type in the finalized model has an explicit `IEntityTypeConfiguration<>`; no navigation points at an unmapped `Nop.Core` class |

`protected set` on the 51 collection navigations is fine — proxies only require the member be
overridable, not publicly settable.

**`BaseEntity` itself:** `public abstract partial`, not mapped (no configuration targets it), so it
is never proxied. `abstract` is irrelevant to proxying a *derived* concrete type. It has no
declared constructor, so it has an implicit public parameterless one. Its `Equals(BaseEntity)` is
`virtual` and `GetUnproxiedType()` returns `GetType()` — i.e. the **proxy** type for a proxied
instance — but the comparison is
`thisType.IsAssignableFrom(otherType) || otherType.IsAssignableFrom(thisType)`, and a proxy derives
from its entity type, so proxy-vs-plain equality still holds. **No change needed.**

There are also **8 implicit shared-type join entities** (`Dictionary<string, object>`) for the
many-to-many tables listed in 4.12 — `CustomerAddresses`, `Customer_CustomerRole_Mapping`,
`PermissionRecord_Role_Mapping`, `ShippingMethodRestrictions`,
`Discount_AppliedToCategories`, `Discount_AppliedToManufacturers`, `Discount_AppliedToProducts`,
`Product_ProductTag_Mapping` — reached through 16 skip navigations. All 8 proxy successfully.

#### Probe result — the model demonstrably builds with proxies

A compile cannot prove this. A throwaway console probe (created outside the repository, run in the
`mcr.microsoft.com/dotnet/sdk:10.0` container, referencing `Nop.Data.csproj`, with
`Microsoft.EntityFrameworkCore.Sqlite` added **to the probe only**) forced model creation and
exercised the live behavior. It has since been deleted; `git status` shows only the three
intended files. **Result: PASS**, all checks green:

| Check | Outcome |
|---|---|
| `DbContextOptions<NopObjectContext>` path: proxies in effect | ✅ `ProxiesOptionsExtension` present, `UseLazyLoadingProxies = true` |
| `NopObjectContext(string)` path: proxies in effect | ✅ same, provider `Microsoft.EntityFrameworkCore.SqlServer` |
| model builds (SQLite **and** SqlServer) | ✅ **113 entity types** (105 CLR + 8 shared-type), 126 navigations + 16 skip navigations, no exception |
| Castle can subclass every entity | ✅ **105/105** CLR types and **8/8** shared-type join entities instantiate as `Castle.Proxies.*` |
| reference navigation lazy-loads with no `Include` | ✅ `LocaleStringResource.Language` → `'English'` |
| collection navigation lazy-loads with no `Include` | ✅ `Language.LocaleStringResources` → count 1 |
| `GetUnproxiedEntityType()` peels the proxy | ✅ `Castle.Proxies.LocaleStringResourceProxy` → `Nop.Core.Domain.Localization.LocaleStringResource`, for all 105 |

#### `IDbContext.ProxyCreationEnabled` now does what EF6's did — verified

The property still cannot map onto EF Core proxy *creation* (that is an immutable options-level
decision, not a per-instance runtime toggle), so it remains backed by a private field that drives
`ChangeTracker.LazyLoadingEnabled`. Verified live: setting it to `false` leaves the entity a proxy
instance but makes its navigations return `null`; the getter round-trips; setting it back restores
loading. `PictureService.GetPictureHashes` therefore once again achieves its stated intent — a bulk
picture scan that does not trigger per-row navigation loads. The residual difference from EF6 is
allocation-level only (a proxy object is still constructed), never query-level: **no extra `SELECT`
is issued either way.** The XML doc comment on the property was updated accordingly.

#### `Extensions.GetUnproxiedEntityType` — verified correct on its first real proxy

This is the first time the helper can actually encounter a proxy. Confirmed working for all 105
entity types: proxies land in namespace `Castle.Proxies`, assembly `DynamicProxyGenAssembly2` with
`IsDynamic == true`, so **both** of its detection predicates fire, and it walks up exactly one
level to the declared entity type. Plain (unproxied) instances are still returned unchanged.

#### Performance note — N+1 is restored, not introduced

Lazy loading reintroduces the N+1 query patterns that were latent in 3.90. Every navigation walk
inside a loop without `Include` is now one query per iteration. This is a **restoration of 3.90
behavior**, not a new problem, and it is the price of option 1. If a specific hot path shows up in
the 7.7 smoke check or later profiling, the targeted remedy is to add `Include` at that one query
site — option 2 applied surgically rather than wholesale.

---

### 4.7a `AsNoTracking` and lazy loading — the expected gap does **not** exist, but disposal now throws

This was expected to be a residual gap and it turns out **not** to be one, so it is recorded here
rather than as an open deferral.

- **The assumption:** EF Core lazy loading requires a *tracked* entity, so navigations on
  `EfRepository.TableNoTracking` and `NopObjectContext.ExecuteStoredProcedureList` results (both
  use `AsNoTracking()`) would not load.
- **Verified false on EF Core 10.** With `ChangeTracker.Entries()` empty and
  `Entry(x).State == Detached`, both a reference navigation and a collection navigation still
  lazy-loaded correctly. EF Core injects the `ILazyLoader` service into the proxy during
  materialization independently of change tracking. `TableNoTracking` consumers are therefore
  **not** degraded. (`ExecuteStoredProcedureList` is doubly safe — it re-attaches every row through
  `AttachEntityToContext`, so those entities are tracked anyway.)
- **The real residual risk is lifetime, not tracking.** A proxy whose originating
  `NopObjectContext` has been **disposed** throws `InvalidOperationException` on first navigation
  access instead of returning `null` (verified). Anything that lets an entity outlive its context —
  entities placed in `ICacheManager`, `PerRequestCacheManager` or `TempData` — is exposed. This is
  **3.90 parity**: EF6 raised the equivalent *"The ObjectContext instance has been disposed"*, so it
  is a restored behavior, not a new one. It becomes reachable again only because navigations are
  live again.

### 4.7b Serialization of entities can now cycle — new, caused by this fix

**Confirmed by direct A/B measurement, and this one is genuinely new.** Serializing an entity
instance with `System.Text.Json`:

| `ChangeTracker.LazyLoadingEnabled` | Result |
|---|---|
| `false` | serialized fine, 220 bytes |
| `true` | **`JsonException`: "A possible object cycle was detected"** |

The serializer walks `Language.LocaleStringResources`, which triggers a lazy load; each child's
`Language` back-reference lazy-loads the parent again, and the graph never terminates. Before this
fix, navigations were empty/null and the same object serialized cleanly — so **any code path that
serializes a `Nop.Core.Domain` entity directly is now at risk**, whether or not the declared
generic type argument is the entity type (both `Serialize<Language>(x)` and `Serialize(x)` fail
identically — this is not a proxy-type-name problem).

Secondary, lower-severity serialization notes:

- **Type identity.** A materialized entity's `GetType().Name` is now e.g. `LanguageProxy`, in the
  dynamic assembly `DynamicProxyGenAssembly2`. Anything that emits or switches on a runtime type
  name — `$type`-style polymorphic serializers, `TempData` round-trips, type-keyed caches, log
  formatting — will see the proxy name rather than the declared entity name. `GetUnproxiedEntityType()`
  is the fix for such call sites and it works.
- `XmlSerializer` also fails on these entities, but that is **pre-existing and unrelated**: it
  throws for the *declared* type (`Nop.Core.Domain.Localization.Language`) because of its
  `ICollection<>` members, with or without proxies.
- **Mitigation:** nopCommerce's own convention already avoids this — controllers map entities to
  view models rather than serializing entities. Tasks 7.x / 8.x should keep to that, and any place
  that must serialize an entity should either project to a DTO or set
  `ReferenceHandler.Preserve` / `MaxDepth`.

#### Verification performed (containerized, `mcr.microsoft.com/dotnet/sdk:10.0`)

`obj/` and `bin/` for `Nop.Core` and `Nop.Data` were deleted first (`dotnet clean` is unreliable
with the ephemeral container NuGet cache).

| Build | Result |
|---|---|
| **GATE 3.3** `Nop.Data` `-c Debug --no-incremental` | **0 errors**, 3 warnings — gate re-passed. The 3 warnings are the pre-existing `Nop.Core` `SYSLIB0011`/`SYSLIB0051`/`SYSLIB0014` obsolescence notices, unchanged |
| `Nop.Services` | **0 errors**, 10 warnings — no downstream regression |
| `Nop.Web.Framework` | **exactly 79 errors** (128 `CS0246` + 28 `CS0234` + 2 `CS0535` diagnostic lines), 10 warnings — matches the known task-6.4 residual exactly, not one more |
| restore advisories | **no `NU1901`–`NU1904`** |
| swallowed-error check | verbose log grep for `"converted to a warning"` and `"ContinueOnError"` → **0 hits each** |
| probe residue | `git status` shows only `src/Directory.Packages.props`, `src/Libraries/Nop.Data/Nop.Data.csproj`, `src/Libraries/Nop.Data/NopObjectContext.cs` |

### 4.8 Schema initializer is never invoked — a fresh install creates no tables

> **⚠️ STATUS CORRECTION BY TASK 7.7.** This was marked RESOLVED by task 7.2 on the strength of
> `Nop.Web/Program.InitializeDatabaseSchema()`. **That fix could never work**, and task 7.7 proved
> it by installing against a real SQL Server: the installer failed with the exact symptom predicted
> below, `Entity: Store State: Added … Invalid object name 'Store'.` The real fix is in
> `SqlServerDataProvider.InitDatabase()` — see **§42.1**. Nothing else in this entry has changed.

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

> **⚠️ SEVERITY CORRECTION BY TASK 7.7 — RESOLVED.** This entry closed with "Not required for the
> compile gate", which was true and misleading: it **is** required to install. `SqlServer.StoredProcedures.sql`
> joins `Product_Id`/`ProductTag_Id`/`Customer_Id`/`CustomerRole_Id` **by name**, so creating the
> stored procedures against an EF Core-generated schema failed with
> `Invalid column name 'ProductTag_Id'. Invalid column name 'Product_Id'.` and the installation
> aborted. All eight join tables are now pinned to their 3.90 column names — see **§42.2**.

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
  up past dynamically emitted / `Castle.Proxies` subclasses instead. **Proxies are now enabled
  (deferral 4.7 is RESOLVED)**, so this path is live and was verified against real
  `Castle.Proxies.*` instances for all 105 mapped entity types; plain instances are still returned
  unchanged.

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
  (behavior notes in §5, and §4.7 / §4.7a / §4.7b for `ProxyCreationEnabled` now that lazy
  loading is enabled).
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
| 13 | ~~Cookie authentication is not configured — nobody can sign in~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24, §24.1) — scheme and all four `<forms>` values verified |
| 14 | ~~`IHttpContextAccessor` is not registered~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — verified resolvable |
| 15 | ~~Session state is not configured — external authentication round-trip fails closed~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — `ISessionStore` verified. A **distributed** store was considered and declined at 7.4 (§32.1): 3.90 shipped `<sessionState>` commented out, so in-process is parity. Multi-instance consequence recorded there |
| 16 | ~~EU VAT service endpoint is a compiled-in constant, not configuration~~ | — | ✅ **RESOLVED by 7.4** (§32.3) — `Tax:EuropaCheckVatServiceUrl` in `appsettings.json`, read by `TaxService.EuropaCheckVatServiceUrl`. **The default also moved from `http` to `https`, deliberately** |
| 17 | Compare / recently-viewed cookie payload format changed — stale cookies ignored | none (accept) | Low |
| 18 | ImageSharp emits a licence *error*, currently downgraded to a warning | business decision | **Blocking for release** |
| 19 | `Nop.Data` deferrals 4.7 / 4.8 / 4.11 name task 4.2 as an owner but are **not** fixable inside `Nop.Services` | ~~7.2~~ | ✅ 4.7 resolved; 4.8 **RESOLVED by 7.2** (§24); 4.11 reviewed, moved to 7.7 (§24.2) |

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

- **4.7 lazy loading is off.** ✅ **RESOLVED** — no longer open. Option (1) was approved and
  applied ahead of task 7.2: `Microsoft.EntityFrameworkCore.Proxies` 10.0.12 was added and
  `NopObjectContext.OnConfiguring` now calls `UseLazyLoadingProxies()` unconditionally (see the
  RESOLVED §4.7 above, plus the new §4.7a and §4.7b). Option (2) — explicit `Include`/`ThenInclude`
  at hundreds of `Nop.Services` query sites — was **rejected as too invasive** and must not be
  undertaken wholesale. `Media/PictureService.cs`'s `StoreInDb` setter, which toggles
  `IDbContext.ProxyCreationEnabled` as a performance hack, is **meaningful again**: it now
  genuinely suppresses lazy loading (verified). No `Nop.Services` edit is required.
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


---

# Nop.Web.Framework — System.Web → ASP.NET Core, part 1: HTTP context, controllers, filters, model binding, validation (task 6.2)

Task 6.2 took the project from **378 declaration-phase errors to 238**, with **zero** errors left in
any file it owns. It did **not** reach a clean compile, and was not meant to: the 238 remaining
errors all sit in files owned by tasks **6.3** (159) and **6.4** (79), and the gate is 6.6.

Everything below was introduced knowingly.

> **Verification note.** Roslyn does not bind method bodies while a compilation still has
> declaration-phase errors — verified empirically by planting a deliberate body error and observing
> that it was not reported. The 238 remaining errors are therefore still declaration-only, and
> 6.1's 700–900 total estimate still stands for 6.3/6.4. To validate its own method bodies, task
> 6.2 built a throwaway project that compiled only its files plus their non-6.3/6.4 dependencies
> (with two-line stubs for `IPageHeadBuilder` and `LocalizedRoute`). That probe compiled with
> **0 errors / 0 warnings** and was deleted. **Tasks 6.3 and 6.4 should expect the same trap**: a
> falling error count does not mean the code they wrote binds.

## 11. Open deferrals — Nop.Web.Framework (task 6.2)

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 20 | ~~FluentValidation is not hooked into model validation — server-side validation gap~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — **asserted present in `MvcOptions.ModelValidatorProviders`**, §19.1 |
| 21 | ~~`NopMetadataProvider` is not registered — `AdditionalValues` is empty~~ | — | ✅ **RESOLVED by 7.2** (§24), verified |
| 22 | ~~`NopModelBinderProvider` is not registered — string inputs are no longer trimmed~~ | — | ✅ **RESOLVED by 7.2** (§24), verified at index 0 |
| 23 | ~~`JsonResult` property naming will change to camelCase unless the host is configured~~ | — | ✅ **RESOLVED by 7.2** (§24) — `PropertyNamingPolicy == null` verified |
| 24 | ~~`LanguageSeoCodeAttribute` no-ops until localizable endpoints carry `LocalizedRoute` metadata~~ | — | **RESOLVED by 6.4** (§17.1) |
| 25 | ~~`ChallengeResult` throws until cookie authentication is registered~~ | — | ✅ **RESOLVED by 7.2** (§24) — same handler as deferral 13 |
| 26 | ~~`IAntiforgery` must be registered or the XSRF filters throw~~ | — | ✅ **RESOLVED by 7.2** (§24), verified resolvable |
| 27 | `BaseNopModel.BindModel` is no longer invoked by the framework | none (accept) | Low |
| 28 | ~~`TempData` notification lists round-trip through a serializer~~ | 7.3 / 8.4 (consumers) | ✅ **provider RESOLVED by 7.2** (§24) — `CookieTempDataProvider` verified. Views must still read `IList<string>`, not cast to `List<string>` |
| 29 | ~~`IWebHelper.IsCurrentConnectionSecured()` behind a TLS-terminating proxy~~ | — | ✅ **RESOLVED by 7.2** (§24) — `app.UseForwardedHeaders()` |

### 11.20 FluentValidation is not hooked into model validation — HIGHEST IMPACT

- **What changed:** 3.90 wired FluentValidation into MVC 5 from `Nop.Web/Global.asax.cs` line 74 —
  `ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new NopValidatorFactory()))`.
  `FluentValidationModelValidatorProvider` lives in `FluentValidation.Mvc`, which is MVC5-only.
  FluentValidation stays on **7.6.105** per the user decision, and 7.x has **no**
  `FluentValidation.AspNetCore` package (that line starts at 8.x).
- **What task 6.2 did:** hand-wired the integration rather than upgrading or adding a package.
  `NopValidatorFactory.cs` now also contains `NopFluentValidationModelValidatorProvider`
  (`IModelValidatorProvider`) and `NopFluentValidationModelValidator` (`IModelValidator`), which run
  the resolved `IValidator` and project `ValidationFailure.PropertyName`/`ErrorMessage` onto
  `ModelValidationResult`. It attaches only at `MetadataKind != Property` so a whole-object
  validator is not re-run once per property. `NopValidatorFactory` itself is **unchanged**.
- **Fix (task 7.2):**
  ```csharp
  services.AddControllersWithViews(options =>
      options.ModelValidatorProviders.Add(
          new NopFluentValidationModelValidatorProvider(new NopValidatorFactory())));
  ```
- **Impact if unfixed — SECURITY-RELEVANT:** FluentValidation is nopCommerce's server-side input
  validation for login, registration, change-password, checkout and every admin form. Without the
  registration those validators never execute and `ModelState.IsValid` returns true for input they
  would have rejected. DataAnnotations validation is unaffected, so this is a partial gap, not a
  total one.

### 11.21 `NopMetadataProvider` is not registered

- 3.90 installed it from `Global.asax.cs` line 66 (`ModelMetadataProviders.Current = new NopMetadataProvider();`).
- **Fix (7.2):** `services.AddControllersWithViews(o => o.ModelMetadataDetailsProviders.Add(new NopMetadataProvider()));`
- **Impact if unfixed:** `ModelMetadata.AdditionalValues` stays empty, so helpers/templates reading
  `NopResourceDisplayName` or `AdditionalInfo` out of metadata fall back to defaults. Note the port
  made the provider **additive** (it no longer replaces the default provider), so ordinary display
  names and validation metadata work with or without it.

### 11.22 `NopModelBinderProvider` is not registered — no string trimming

- The MVC 5 mechanism was a type-level `[ModelBinder(typeof(NopModelBinder))]` on `BaseNopModel`,
  which had to be removed (see §12 for why keeping it would have broken all model binding).
- **Fix (7.2):** `services.AddControllersWithViews(o => o.ModelBinderProviders.Insert(0, new NopModelBinderProvider()));`
- **Impact if unfixed:** submitted strings are no longer trimmed. Fails open in the harmless
  direction — untrimmed input, i.e. as if every property carried `[NoTrim]`.

### 11.23 `JsonResult` property naming — camelCase vs PascalCase

- **What changed:** MVC 5's `JsonResult` used `JavaScriptSerializer`, which emitted property names
  **exactly as declared** (PascalCase). ASP.NET Core's `JsonResult` defers to the configured output
  formatter, and the default `System.Text.Json` formatter applies
  `JsonNamingPolicy.CamelCase`.
- **Scope:** every `return Json(...)` in `Nop.Web` and `Nop.Admin` — most importantly the Kendo grid
  read/AJAX actions, whose JavaScript reads PascalCase field names, and `DataSourceResult`
  (`Data`, `Total`, `Errors`, `ExtraData`).
- **Task 6.2 deliberately did NOT guess the host's formatter.** `ConverterJsonResult` and
  `NullJsonResult` were kept on explicit Newtonsoft serialization (`JsonConvert.SerializeObject`)
  precisely so their payloads stay byte-identical to 3.90 regardless of host configuration; setting
  `JsonResult.SerializerSettings` instead would throw whenever the host's formatter choice did not
  match the settings type (`JsonSerializerOptions` for System.Text.Json,
  `JsonSerializerSettings` for Newtonsoft). Framework-produced `Json(...)` results are **not**
  covered by that and remain host-dependent.
- **Fix (task 7.2) — pick one and state it explicitly:**
  - System.Text.Json: `.AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = null);`
  - Newtonsoft: `.AddNewtonsoftJson(o => o.SerializerSettings.ContractResolver = new DefaultContractResolver());`
- **Also gone, and worth a decision at 7.2:** MVC 5's `JsonRequestBehavior.DenyGet` default, which
  refused to serialise JSON in response to a GET as a JSON-hijacking mitigation. ASP.NET Core has
  no equivalent guard and no such default. This is a **relaxation** relative to 3.90, though the
  mitigation itself is obsolete for modern browsers. `MaxJsonLength` and `RecursionLimit` are also
  gone.

### 11.24 `LanguageSeoCodeAttribute` no-ops until endpoints carry `LocalizedRoute` metadata

- **What changed:** the filter's "is this route localizable?" test was
  `filterContext.RouteData.Route is LocalizedRoute`. ASP.NET Core's `RouteData` exposes no single
  `Route`; the equivalent is endpoint metadata, so the filter now does
  `HttpContext.GetEndpoint()?.Metadata.GetMetadata<LocalizedRoute>()`.
- **Fix (task 6.4):** when `LocalizedRoute` is reimplemented over endpoint routing, add a
  `LocalizedRoute` instance (or, if the class is retired, an equivalent marker — and then update the
  `GetMetadata<>` call) to the metadata of every localizable endpoint.
- **Impact if unfixed:** SEO language codes are never injected into or validated on URLs. Fails
  open with respect to redirects only; no authorization decision depends on it.

### 11.25 `ChallengeResult` throws until cookie authentication is registered

- `System.Web.Mvc.HttpUnauthorizedResult` emitted a bare 401 that `FormsAuthenticationModule`
  rewrote into a 302 to the login URL on the way out. ASP.NET Core has no outbound module, so the
  faithful equivalent is `ChallengeResult`, which hands the refusal to the registered
  authentication handler. Used by `AdminAuthorizeAttribute`, `AdminVendorValidation` and
  `PublicStoreAllowNavigationAttribute`.
- Until deferral **7.13** (cookie authentication) is fixed, `ChallengeResult` throws
  `InvalidOperationException` instead of redirecting. **The request is still refused** — this is a
  loud failure, not a bypass, and was chosen over a silent 401 so the missing host wiring cannot go
  unnoticed.

### 11.26 `IAntiforgery` must be resolvable

- `AdminAntiForgeryAttribute` / `PublicAntiForgeryAttribute` now call
  `IAntiforgery.ValidateRequestAsync` (see §13). `services.AddControllersWithViews()` registers
  `IAntiforgery` implicitly; a host that assembles the MVC service graph piecemeal must call
  `services.AddAntiforgery()`. `GetRequiredService` throws if it is absent — deliberately
  fail-closed rather than skipping the XSRF check.

### 11.27 `BaseNopModel.BindModel` is no longer invoked — accepted

- MVC 5 called the hook from inside `NopModelBinder`, a `DefaultModelBinder` subclass. ASP.NET Core
  exposes **no** seam that wraps the default complex-object binder: `ComplexObjectModelBinder` is
  internal and `ComplexTypeModelBinder` is obsolete. The hook is retained as an extension point but
  the framework no longer calls it.
- **Verified before changing it:** no type in the entire solution — `Nop.Web`, `Nop.Admin` or any of
  the 20 plugins — overrides `BindModel`. Nothing observable is lost today. Anything that needs it
  later should invoke it from an action filter over `ActionExecutingContext.ActionArguments`.
  `PostInitialize()` is a constructor hook and is completely unaffected.

### 11.28 `TempData` notification lists round-trip through a serializer

- `BaseController.AddNotification(..., persistForTheNextRequest: true)` stores a `List<string>` in
  `TempData`. In System.Web `TempData` held live CLR objects; ASP.NET Core serialises it, and on the
  next request the value comes back through `DefaultTempDataSerializer`. The writer was changed to
  read through `IList<string>` and re-assign rather than mutate in place.
- **Consumers must not hard-cast to `List<string>`.** Any view or layout in `Nop.Web` (task 7.3) or
  `Nop.Admin` (task 8.4) that does `(List<string>)TempData["nop.notifications.Success"]` should read
  it as `IList<string>`/`IEnumerable<string>`.
- Also required: `TempData` needs a provider — cookie (default) or session. That is host wiring at
  7.2, and it interacts with deferral **7.15** if the session provider is chosen.

### 11.29 `IsCurrentConnectionSecured()` behind a reverse proxy

- `NopHttpsRequirementAttribute`'s SSL redirect decisions are unchanged, but they rest on
  `IWebHelper.IsCurrentConnectionSecured()`, which reads `HttpRequest.IsHttps`. Behind a
  TLS-terminating proxy that is false unless the host adds forwarded-headers middleware
  (`app.UseForwardedHeaders(...)`, owned by 6.4/7.2). Without it, `ForceSslForAllPages` produces a
  **redirect loop**, not an insecure page.

---

## 12. Final behavioral changes — Nop.Web.Framework task 6.2, intentional, no future fix needed

- **`filterContext.IsChildAction` was removed from 11 filters**
  (`CheckAffiliate`, `CustomerLastActivity`, `LanguageSeoCode`, `PublicStoreAllowNavigation`,
  `StoreClosed`, `StoreIpAddress`, `StoreLastVisitedPage`, `ValidatePassword`,
  `AdminVendorValidation`, `AdminValidateIpAddress`, `AdminAntiForgery`, `PublicAntiForgery`,
  `NopHttpsRequirement`, `WwwRequirement`). ASP.NET Core has no child actions — they were replaced
  by View Components, which **do not execute the action filter pipeline at all**. The guard is
  therefore unnecessary rather than merely unavailable; nothing it used to suppress can now occur.

- **`OutputCacheAttribute.IsChildActionCacheActive` guard removed** from
  `AdminAuthorizeAttribute`. It threw `InvalidOperationException` when `[AdminAuthorize]` was used
  under an active child-action output cache. Neither child actions nor MVC 5 `OutputCacheAttribute`
  exist, so the condition is unreachable. **This removed a throw, not an authorization check** — the
  `IPermissionService.Authorize(AccessAdminPanel)` test still runs in every case where it ran before.

- **`HttpRequestBase.IsLocal` reimplemented** in `WwwRequirementAttribute` as a private
  `IsLocalRequest(HttpContext)`: loopback remote address, or remote address equal to local address —
  the same two tests System.Web applied. A request with no remote address at all (in-process) is
  treated as local, matching System.Web.

- **`StoreClosedAttribute` topic-id parsing fixed as part of the port.** The original did
  `RouteData.Values["topicId"] as int?`, which worked only because System.Web's route boxed the value
  as an `int`. ASP.NET Core route values are always `string`, so the cast would have silently
  produced `null` and **blocked every topic** when the store is closed. It now parses explicitly.
  This is a correctness fix forced by the platform, not a behaviour change relative to 3.90.

- **`Request.Form` now requires a `HasFormContentType` guard.** System.Web returned an empty
  collection for a non-form request; ASP.NET Core throws `InvalidOperationException`. Guards were
  added in `FormValueRequiredAttribute`, `ParameterBasedOnFormName(AndValue)Attribute`,
  `HoneypotValidatorAttribute` and `CaptchaValidatorAttribute`. Each preserves the original outcome
  for a non-form request (no value found), and in the two security-relevant cases that means
  fail-closed: honeypot sees no bot value, captcha yields `captchaValid = false`.

- **Response-writing APIs.** `Response.Write` → `Response.WriteAsync`;
  `Response.BinaryWrite` → `Response.Body.WriteAsync`; `Response.Output` (a `TextWriter`) →
  buffer into a `StringWriter`/`StringBuilder` and write once; `Response.Charset` → folded into
  `ContentType`; `Response.AddHeader` → `Response.Headers[...]`;
  `JsonResult.ContentEncoding` dropped (no `Response.ContentEncoding` exists). Emitted bytes are
  unchanged in every case.

- **`Response.End()` removed** from `XmlDownloadResult` and `RemotePost`. It has no equivalent and
  none is needed — an action result returns to the pipeline instead of aborting the request.
  **Consequence to be aware of:** in System.Web `Response.End()` suppressed anything written after
  it. It no longer does, so a caller that returns a view *after* calling `RemotePost.Post()` would
  append to the document. All in-tree callers return immediately.

- **`System.Web.HttpUtility.HtmlEncode` → `System.Net.WebUtility.HtmlEncode`** in `RemotePost`,
  the same swap already made across Nop.Core and Nop.Services. Identical behaviour for
  `HtmlEncode`.

- **Cookies in `WebWorkContext`.** `HttpCookie`/`HttpCookieCollection` → `IRequestCookieCollection`
  (read) and `IResponseCookies.Append`/`Delete` (write). Cookie name (`Nop.customer`), the
  `HttpOnly` flag and the 24×365-hour lifetime are preserved. The old "clear by expiring in the
  past" branch for `Guid.Empty` is now a `Delete`, which emits exactly that.
  A `Response.HasStarted` guard was added because ASP.NET Core throws if headers are already sent.

- **`Request.UserLanguages` → parsed `Accept-Language`.** `WebWorkContext.GetLanguageFromBrowserSettings`
  now reads `Request.GetTypedHeaders().AcceptLanguage`, orders by quality and takes the first
  non-empty value — which is what System.Web did internally to build `UserLanguages`.

- **`Request.AppRelativeCurrentExecutionFilePath` → `"~" + Request.Path`**, and
  `Request.ApplicationPath` → `Request.PathBase` mapped to `"/"` when empty. The `"/"` mapping is
  required: `LocalizedUrlExtenstions.IsVirtualDirectory` treats `"/"` as "not a virtual directory"
  and **throws** on an empty string. `Request.RawUrl` → `UriHelper.GetEncodedPathAndQuery()`, which
  yields `PathBase + Path + QueryString` — the same shape `RawUrl` produced.
  `LocalizedUrlExtenstions.cs` itself needed **no change** (pure string handling).

- **`Request.UserHostAddress` → `HttpContext.Connection.RemoteIpAddress`** in
  `CaptchaValidatorAttribute` (passed to reCAPTCHA as `remoteip`).

- **`RenderPartialViewToString` re-based on `ICompositeViewEngine`.** The static
  `ViewEngines.Engines` collection does not exist. `FindPartialView` → `FindView(actionContext, name, isMainPage: false)`,
  `IView.Render` → `RenderAsync`, and the `ViewContext` constructor additionally takes
  `HtmlHelperOptions` (taken from the configured `MvcViewOptions` so a rendered partial obeys the
  same HTML options as a normal view). A missing view now throws `NopException` with the view name
  instead of `NullReferenceException`. `RouteData.GetRequiredString("action")` →
  `ControllerContext.ActionDescriptor.ActionName`.

- **Sync-over-async, three new sites**, consistent with the trade-off already accepted in task 4.2
  (ASP.NET Core installs no `SynchronizationContext`, so none can deadlock):
  `BaseController.RenderPartialViewToString` (called from dozens of synchronous actions),
  `RemotePost.Post` (called from synchronous payment-plugin actions), and nothing else.

- **`ActionDescriptor.ActionName` requires a cast.** ASP.NET Core's `ActionDescriptor` has no
  `ActionName`; only `ControllerActionDescriptor` does. Affects `ValidatePasswordAttribute`,
  `StoreClosedAttribute`, `PublicStoreAllowNavigationAttribute`, `AdminAuthorizeAttribute`.

- **`new UrlHelper(filterContext.RequestContext)` → `IUrlHelperFactory.GetUrlHelper(filterContext)`**
  resolved from `HttpContext.RequestServices`, in `ValidatePasswordAttribute` and
  `StoreClosedAttribute`. `ActionExecutingContext` derives from `ActionContext` in ASP.NET Core, so
  the filter context is passed straight through.

- **Pre-existing 3.90 defect deliberately NOT fixed.** `ValidatePasswordAttribute` compares
  `filterContext.Controller.ToString()` — the controller's *full type name*, e.g.
  `"Nop.Web.Controllers.CustomerController"` — against the literal `"Customer"`, so the
  CustomerController exemption has never fired. `StoreClosedAttribute` does the same comparison but
  against the full name and therefore works. The behaviour is preserved verbatim rather than
  silently changed; `filterContext.Controller` is still `object` in ASP.NET Core so `.ToString()`
  yields the same string.

- **`System.Linq.Dynamic` → `System.Linq.Dynamic.Core` DID have to change — correcting a task 6.1
  finding.** 6.1 reported that `using System.Linq.Dynamic;` "still resolves as-is with the `.Core`
  package". It resolves, but only because `System.Linq.Dynamic` exists as a **parent namespace** of
  `System.Linq.Dynamic.Core`, so the directive binds to an empty namespace and is legal. The members
  are not visible through it, which only surfaces once method bodies bind. Both files needed edits:
  - `Kendoui/QueryableExtensions.cs` — `using` changed; the string-predicate `Where`/`OrderBy` live
    in `System.Linq.Dynamic.Core.DynamicQueryableExtensions`. Call syntax unchanged.
  - `Validators/BaseNopValidator.cs` — `using` changed **and**
    `DynamicExpression.ParseLambda<T, TResult>(expression, values)` →
    `DynamicExpressionParser.ParseLambda<T, TResult>(ParsingConfig.Default, createParameterCtor: true, expression)`.
    The static entry point was renamed and the generic two-type-argument overload now requires an
    explicit `ParsingConfig` plus the `createParameterCtor` flag the legacy API applied implicitly.
    Return type (`Expression<Func<T, TResult>>`) and the expression language are unchanged, so the
    `RuleFor(...)` calls are untouched.
  This is the same class of correction as `Nop.Services`' `Messages/Tokenizer` in task 4.2.

- **Anti-forgery failure status.** MVC 5 let `HttpAntiForgeryException` propagate, producing a 500.
  ASP.NET Core's own antiforgery filter converts a validation failure into a **400**, which is what
  `BadRequestResult` reproduces. Either way the action does not run.

- **`NopMetadataProvider`'s duplicate-name `NopException` now fires once per member.** ASP.NET Core
  caches metadata per type/member, where MVC 5 re-created it. Same condition, same message.

- **`Localization/LocalizedString.cs` needed no change** and was left alone. It implements
  `System.Web.IHtmlString`, and `System.Web.IHtmlString`/`HtmlString` **do** exist on net10.0 — they
  ship in the in-box `System.Web.HttpUtility` assembly. It compiles as-is. Task 6.3 owns whether to
  move it to `IHtmlContent` alongside the rest of the `MvcHtmlString` work.

## 13. Breaking constructor / signature changes — task 6.2

### 13a. Constructor changes

| Before | After | Consumer impact |
|---|---|---|
| `WebWorkContext(HttpContextBase, …12 more…)` | `WebWorkContext(IHttpContextAccessor, …12 more…)` | `DependencyRegistrar` registers it reflectively (`RegisterType<WebWorkContext>()`), so Autofac picks the new constructor up automatically **provided task 6.4 calls `services.AddHttpContextAccessor()`** (deferral 7.14). No `.WithParameter` to edit. |
| `RemotePost(HttpContextBase, IWebHelper)` | `RemotePost(IHttpContextAccessor, IWebHelper)` | The parameterless overload now resolves `IHttpContextAccessor` from `EngineContext` instead of `HttpContextBase`. Callers: `Nop.Plugin.Payments.PayPalDirect` / `PayPalStandard` (tasks 12.3/12.4) use the parameterless form; `Nop.Web`'s checkout flow likewise. No signature-level change for them. |
| `ConverterJsonResult(params JsonConverter[])` | same, but now chains `base(null)` | ASP.NET Core's `JsonResult` has no parameterless constructor. No caller impact. |
| — | **additive** `NullJsonResult()` | Explicit constructor added for the same reason. No caller impact. |

### 13b. Public type / member signature changes

| Member | Before | After | Consumers to fix |
|---|---|---|---|
| `BasePaymentController.ValidatePaymentForm`, `GetPaymentInfo` | `System.Web.Mvc.FormCollection` | `Microsoft.AspNetCore.Http.IFormCollection` | **All five Payments plugins** (tasks 12.1–12.5) override both. `IFormCollection` was chosen over the concrete `Microsoft.AspNetCore.Http.FormCollection` because `HttpRequest.Form` is typed as the interface, so a concrete parameter would force every caller to cast. Indexing (`form["key"]`) is source-compatible; `AllKeys` → `Keys`; the indexer yields `StringValues`, which converts implicitly to `string`. |
| `BaseNopModel.BindModel` | `(ControllerContext, ModelBindingContext)` — MVC 5 types | `(Microsoft.AspNetCore.Mvc.ModelBinding.ModelBindingContext)` | **None** — verified no override anywhere in the solution. See deferral 11.27: the framework no longer calls it. |
| `BaseNopModel` type-level `[ModelBinder(typeof(NopModelBinder))]` | present | **REMOVED** | The ASP.NET Core attribute of the same name compiles but means something destructive here: a type-level `[ModelBinder]` makes MVC use `BinderTypeModelBinder`, which hands the whole model to the named binder and performs **no property binding**. MVC 5's attribute selected a `DefaultModelBinder` subclass that still did all the default work. Keeping it would leave every nopCommerce model unbound. Replaced by `NopModelBinderProvider` (deferral 11.22). |
| `NopModelBinder` | `: System.Web.Mvc.DefaultModelBinder` | `: IModelBinder`, constructed with an inner `IModelBinder` | No in-tree subclass. `DefaultModelBinder` has no ASP.NET Core counterpart — `IModelBinder` is a single `BindModelAsync` with no `SetProperty`/`GetPropertyValue` seams. Behaviour retained: trims `string` members that are not `[NoTrim]` (the 14 `[NoTrim]` members across Nop.Web/Nop.Admin keep their exact submitted value). |
| — | — | **additive** `NopModelBinderProvider : IModelBinderProvider` | New type in `Mvc/NopModelBinder.cs`. |
| `CommaSeparatedModelBinder` | `: System.Web.Mvc.DefaultModelBinder`, overriding `BindModel` + `GetPropertyValue` | `: IModelBinder` with `BindModelAsync` | **The four Nop.Admin call sites need no change beyond the `using`**: `CustomerController` ~line 799 and `OrderController` ~lines 1028–1030 use `[ModelBinder(typeof(CommaSeparatedModelBinder))]` on an action parameter, and ASP.NET Core's `ModelBinderAttribute` has a `ModelBinderAttribute(Type)` constructor with exactly that semantic. The `GetPropertyValue` override and the `?? base.BindModel(...)` fallback were dropped (no base, no seam); when the CSV path does not apply the binder leaves `Result` unset, which MVC treats as "not bound". `ValueProviderResult.AttemptedValue` → `FirstValue`; a missing value is `ValueProviderResult.None`, not null. |
| `NopMetadataProvider` | `: System.Web.Mvc.DataAnnotationsModelMetadataProvider`, overriding `CreateMetadata(IEnumerable<Attribute>, Type, Func<object>, Type, string)` | `: IDisplayMetadataProvider` with `CreateDisplayMetadata(DisplayMetadataProviderContext)` | **A different contract, not a rename.** MVC 5 had one provider class that *created* the whole `ModelMetadata`, customised by subclassing. ASP.NET Core splits metadata into three additive interfaces (`IBindingMetadataProvider`, `IDisplayMetadataProvider`, `IValidationMetadataProvider`) contributing to a details object the framework owns — no base class, nothing to call `base` on, and the `modelAccessor` concept is gone entirely (metadata is per-type/member, value-free and cached). This type now only contributes the `AdditionalValues` entries; the inherited data-annotations behaviour comes from the framework's own `DataAnnotationsMetadataProvider`, which stays registered. `AdditionalValues` widened from `IDictionary<string, object>` to `IDictionary<object, object>` (read side `IReadOnlyDictionary<object, object>`), but the keys written are the same strings (`"NopResourceDisplayName"`, `"AdditionalInfo"`), so string-key lookups still resolve. |
| `FormValueRequiredAttribute.IsValidForRequest` | `(ControllerContext, System.Reflection.MethodInfo)` | `(Microsoft.AspNetCore.Routing.RouteContext, Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor)`; base moves to `Microsoft.AspNetCore.Mvc.ActionConstraints.ActionMethodSelectorAttribute` | No in-tree subclass. The body never used the `MethodInfo`, so nothing is lost. |
| `AdminAntiForgeryAttribute.OnAuthorization`, `PublicAntiForgeryAttribute.OnAuthorization` | `void OnAuthorization(AuthorizationContext)` (`IAuthorizationFilter`) | `Task OnAuthorizationAsync(AuthorizationFilterContext)` (`IAsyncAuthorizationFilter`) | No in-tree subclass. Required because `IAntiforgery.ValidateRequestAsync` is asynchronous; the alternative was a gratuitous sync-over-async in the request pipeline. MVC 5's trick of instantiating `ValidateAntiForgeryTokenAttribute` and calling its `OnAuthorization` inline is impossible — ASP.NET Core's version is an `IFilterFactory` with no invocable `OnAuthorization`. |
| `AdminAuthorizeAttribute`, `AdminVendorValidation`, `HoneypotValidatorAttribute`, `NopHttpsRequirementAttribute`, `WwwRequirementAttribute` | `: System.Web.Mvc.FilterAttribute, IAuthorizationFilter`, `OnAuthorization(AuthorizationContext)` | `: Attribute, Microsoft.AspNetCore.Mvc.Filters.IAuthorizationFilter`, `OnAuthorization(AuthorizationFilterContext)` | There is no `FilterAttribute` base class in ASP.NET Core. No in-tree subclasses. |
| `ParameterBasedOnFormNameAttribute`, `ParameterBasedOnFormNameAndValueAttribute` | `: FilterAttribute, IActionFilter` | `: Attribute, Microsoft.AspNetCore.Mvc.Filters.IActionFilter` | Also `filterContext.ActionParameters` → `ActionExecutingContext.ActionArguments`, and `filterContext.RequestContext.HttpContext` → `filterContext.HttpContext` (no `RequestContext` in ASP.NET Core). |
| `CaptchaValidatorAttribute` | writes `filterContext.ActionParameters["captchaValid"]` | writes `filterContext.ActionArguments["captchaValid"]` | Transparent to the captcha-guarded actions in Nop.Web — they keep their `bool captchaValid` parameter unchanged. |
| `ModelStateExtensions` private helpers | `System.Web.Mvc.ModelState` | `Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry` | The two public extension methods (`SerializeErrors`, `ToDataSourceResult`) keep their shape; `ModelStateDictionary` just moves namespace. `modelState.Value.AttemptedValue` → `ModelStateEntry.AttemptedValue` (ASP.NET Core folded MVC 5's separate `ValueProviderResult Value` object into the entry). Emitted JSON shape — `{ "<key>": { "errors": [ … ] } }`, consumed by the Kendo grid scripts — is unchanged. |
| `Extensions.SelectionIsNotPossible(IList<SelectListItem>, bool)` | `System.Web.Mvc.SelectListItem` | `Microsoft.AspNetCore.Mvc.Rendering.SelectListItem` | Namespace change only; `Text`/`Value`/`Selected` unchanged. Matches the `SelectList` move already recorded for `Nop.Services` in §9b. |
| `ConverterJsonResult`, `NullJsonResult` | `ExecuteResult(ControllerContext)`; `Data`; inherited `ContentEncoding` | `ExecuteResultAsync(ActionContext)`; `Value`; **`ContentEncoding` gone** | No in-tree caller set `ContentEncoding`. See deferral 11.23 for why explicit Newtonsoft serialization was retained instead of `JsonResult.SerializerSettings`. |
| `XmlDownloadResult`, `RssActionResult` | `: System.Web.Mvc.ActionResult`, `ExecuteResult(ControllerContext)` | `: Microsoft.AspNetCore.Mvc.ActionResult`, `ExecuteResultAsync(ActionContext)` | Callers only construct and return them. |
| — | — | **additive** `NopFluentValidationModelValidatorProvider`, `NopFluentValidationModelValidator` | New types in `NopValidatorFactory.cs`; see deferral 11.20. |
| — | — | **additive** `protected virtual HttpContext WebWorkContext.HttpContext` | Convenience accessor over `IHttpContextAccessor`, mirroring the same addition made to four `Nop.Services` types in §9c. |

**Unchanged — do not touch:**

- `NopValidatorFactory.GetValidator(Type)` — byte-for-byte unchanged; it compiles as-is on net10.0.
  FluentValidation stays on 7.6.105.
- `Validators/CreditCardPropertyValidator.cs`, `Validators/DecimalPropertyValidator.cs`,
  `Validators/ValidatorExtensions .cs` — unchanged; FluentValidation 7.x `PropertyValidator` /
  `IRuleBuilder` bind unmodified.
- `IWorkContext`, `IStoreContext` — no interface change. `WebStoreContext` needed **no edit at all**
  (it only consumes `IWebHelper`, already ported in task 2.4).
- `NopResourceDisplayName` — unchanged. It derives from `System.ComponentModel.DisplayNameAttribute`,
  and ASP.NET Core's `DataAnnotationsMetadataProvider` reads that attribute into
  `DisplayMetadata.DisplayName` as a `Func<string>` re-evaluated per call, so the per-request
  localization (which is the whole point of the class) still works.
- `Mvc/IModelAttribute.cs`, `Mvc/NoTrimAttribute.cs`, `Mvc/ActionConfirmationModel.cs`,
  `Mvc/DeleteConfirmationModel.cs`, `Mvc/DependencyRegistrarExtensions.cs`,
  `Kendoui/DataSourceRequest.cs`, `Kendoui/DataSourceResult.cs`, `Kendoui/Filter.cs`,
  `Kendoui/Sort.cs`, `Security/SslRequirement.cs`, `Security/Captcha/GReCaptchaValidator.cs`,
  `Localization/LocalizedUrlExtenstions.cs` — no System.Web surface, no edit.

### 13c. Files task 6.2 deliberately left alone

Left for **task 6.3** (Razor infrastructure, HTML/URL helpers, bundling) — 159 of the 238 remaining
errors: `HtmlExtensions.cs`, `UI/LayoutExtensions.cs`, `UI/PageHeadBuilder.cs`,
`UI/IPageHeadBuilder.cs`, `UI/AsIsBundleOrderer.cs`, `UI/DataListExtensions.cs`,
`UI/Paging/Pager.cs`, `Themes/ThemeableRazorViewEngine.cs`,
`Themes/ThemeableVirtualPathProviderViewEngine.cs`, `ViewEngines/Razor/WebViewPage.cs`,
`UrlHelperExtensions.cs`, `Events/AdminTabStripCreated.cs`,
`Security/Captcha/GRecaptchaControl.cs`, `Security/Captcha/HtmlExtensions.cs`,
`Security/Honeypot/HtmlExtensions.cs`, and `Localization/LocalizedString.cs` (which compiles
today — see §12).

Left for **task 6.4** (routing, modules → middleware, DI) — 79 of the 238: `DependencyRegistrar.cs`
(including the `HttpContextBase`/`HttpRequestBase`/`HttpResponseBase`/`HttpServerUtilityBase`/`HttpSessionStateBase`
registrations at lines 74–90 that §9a says to delete, and the `SettingsSource`/`IRegistrationSource`
break from the newer Autofac), `Localization/LocalizedRoute.cs`,
`Localization/LocalizedRouteExtensions.cs`, `Seo/GenericPathRoute.cs`,
`Seo/GenericPathRouteExtensions.cs`, `Seo/CustomUrlRecordEntityNameRequested.cs`,
`Mvc/Routes/*`, `Menu/SiteMapNode.cs`, `Menu/XmlSiteMap.cs`.

Task **6.5** (configuration / obsolete files) contributes **zero** compile errors; nothing was
touched for it.


---

# Nop.Web.Framework — System.Web → ASP.NET Core, part 2: Razor infrastructure, HTML/URL helpers, bundling (task 6.3)

Task 6.3 took the project from **238 declaration-phase errors to 79**, with **zero** errors left in
any file it owns. It did not reach a clean compile and was not meant to: all 79 remaining errors
sit in the twelve files owned by task **6.4**, and the gate is 6.6. Measured per file, the residue is
exactly 6.2's forecast: `Localization/LocalizedRoute.cs` 17,
`Localization/LocalizedRouteExtensions.cs` 15, `Seo/GenericPathRouteExtensions.cs` 14,
`Seo/GenericPathRoute.cs` 13, `Mvc/Routes/GuidConstraint.cs` 6,
`Seo/CustomUrlRecordEntityNameRequested.cs` 3, `Mvc/Routes/RoutePublisher.cs` 2,
`Mvc/Routes/IRoutePublisher.cs` 2, `Mvc/Routes/IRouteProvider.cs` 2, `Menu/SiteMapNode.cs` 2,
`DependencyRegistrar.cs` 2, `Menu/XmlSiteMap.cs` 1.

Everything below was introduced knowingly.

> **Method bodies were validated with a throwaway probe, and it mattered.** 6.2's finding holds:
> Roslyn does not bind method bodies while declaration-phase errors exist. 6.3 built a probe project
> compiling every `Nop.Web.Framework` file except 6.4's twelve, with two-type stubs for
> `LocalizedRoute` and `SiteMapNode`. The probe was proven to bind bodies by planting a deliberate
> bad call and observing it reported, then reverting. Result: **0 errors in any 6.3-owned file**, and
> **one pre-existing defect discovered in a file nobody owns** — see 14.34. The probe was deleted and
> `git status` verified clean of it.
>
> Separately, five behavioural assumptions were verified empirically against the net10.0 reference
> assemblies rather than assumed. All five mattered; two of them were wrong in the task brief:
>
> | Assumption checked | Result |
> |---|---|
> | `TagBuilder.CreateSanitizedId` was removed | **FALSE — it still exists**, but the signature gained a required argument: `CreateSanitizedId(string name, string invalidCharReplacement)`. See 15.6. |
> | `TagBuilder.ToString()` renders the element | **FALSE — it returns `"Microsoft.AspNetCore.Mvc.Rendering.TagBuilder"`.** This is the single most dangerous silent defect class in this task. See §15 and 14.30. |
> | Passing an `IDictionary<string,object>` as `object htmlAttributes` applies the attributes | TRUE — `DefaultHtmlGenerator.GetHtmlAttributeDictionaryOrNull` returns the *same instance* for an `IDictionary<string,object>`. |
> | `HtmlHelper.AnonymousObjectToHtmlAttributes(null)` returns null | FALSE — returns an **empty `Dictionary<string,object>`**. Relevant to 14.31. |
> | `TagBuilder.Attributes.Add(key, null)` renders `key=""` | TRUE — identical to MVC 5, so `""` and `null` are interchangeable. |

## 14. Open deferrals — Nop.Web.Framework (task 6.3)

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 30 | ~~Theming stops working until the view-location expander is registered~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — `ThemeableViewLocationExpander` verified at index 0 |
| 31 | ~~`PageHeadBuilder` needs `IFileVersionProvider` + `IHttpContextAccessor` resolvable~~ | — | ✅ **RESOLVED by 7.2** (§24) — both verified resolvable |
| 32 | ~~No `Widget` view component exists — `@Html.Widget(...)` throws~~ | — | ✅ **RESOLVED by 7.3** (§30) — `Nop.Web/Components/WidgetViewComponent.cs` + `Views/Shared/Components/Widget/Default.cshtml`; all 190 call sites unchanged |
| 33 | ~~Cache busting silently no-ops for assets outside the web root~~ | — | ✅ **RESOLVED by 7.4** (§32) — the same `IFileProvider` instance is on `WebRootFileProvider`, so `IFileVersionProvider` sees every asset that is served; a real `?v=<sha256>` verified live |
| 34 | ~~`Security/FilePermissionHelper.cs` has a body-level compile error nobody owns~~ | — | **RESOLVED by 6.4** (§17.1) — gate 6.6 residual is **0**, not 1 |
| 35 | Minification is gone; no build-time replacement is scheduled | post-migration | Low |

### 14.30 Theming stops working until the expander is registered — HIGHEST IMPACT

- **What changed:** `Themes/ThemeableRazorViewEngine.cs` and
  `Themes/ThemeableVirtualPathProviderViewEngine.cs` were **deleted** and replaced by
  `Themes/ThemeableViewLocationExpander.cs`. Full detail in §16.
- **What is missing:** in 3.90 the engine installed itself from `Nop.Web/Global.asax.cs` lines 60–62
  (`ViewEngines.Engines.Clear(); ViewEngines.Engines.Add(new ThemeableRazorViewEngine());`). Task 7.2
  deletes `Global.asax` outright, so nothing registers the replacement.
- **Fix (task 6.4 if it owns the framework's `IServiceCollection` contribution, otherwise 7.2 in
  `Program.cs`):**
  ```csharp
  services.Configure<RazorViewEngineOptions>(options =>
      options.ViewLocationExpanders.Add(new ThemeableViewLocationExpander()));
  ```
- **Impact if unfixed — SILENT:** there is no exception. The Razor engine simply falls back to its
  default location formats (`/Views/{1}/{0}.cshtml`, `/Views/Shared/{0}.cshtml`, and the `Areas/…`
  equivalents). Consequences: **every theme is ignored** and the store renders from the base
  `Views/` folder; **the `/Administration/Views/…` locations are not searched at all**, so if task
  8.x leaves the admin views where 3.90 put them the whole admin UI 404s on view lookup.
- **Note the ordering requirement:** ASP.NET Core searches expanders' output *before* it searches
  what the previous expander returned only in the order expanders are added. If any other expander
  is added later, add this one first.

### 14.31 `PageHeadBuilder` needs two new services resolvable

- **What changed:** `PageHeadBuilder(SeoSettings)` became
  `PageHeadBuilder(SeoSettings, IFileVersionProvider, IHttpContextAccessor)` — see §15.
- **Why it is probably already fine:** `DependencyRegistrar.cs` line 330 registers it reflectively
  (`builder.RegisterType<PageHeadBuilder>().As<IPageHeadBuilder>().InstancePerLifetimeScope()`), so
  Autofac selects the new constructor automatically. There is no `.WithParameter` to edit.
- **What must be true at runtime:**
  - `Microsoft.AspNetCore.Mvc.ViewFeatures.IFileVersionProvider` must be registered. MVC's view
    services register it (`AddControllersWithViews()` / `AddRazorViewEngine()` →
    `TryAddSingleton<IFileVersionProvider, DefaultFileVersionProvider>()`). A host that assembles
    the MVC graph piecemeal must ensure the view services are in. It also needs
    `IWebHostEnvironment.WebRootFileProvider` and `IMemoryCache` — the latter is already required by
    deferral §3's `MemoryCacheManager(IMemoryCache)`.
  - `IHttpContextAccessor` must be registered — the same requirement task 6.2 recorded for
    `WebWorkContext` (deferral 7.14). `services.AddHttpContextAccessor()`.
  - Autofac must be able to see the `IServiceCollection` registrations, i.e. the
    `AutofacServiceProviderFactory` + `Populate` integration task 6.4 owns.
- **Impact if unfixed:** Autofac throws a resolution exception when `IPageHeadBuilder` is first
  requested, which is on essentially every page. This is a **loud** failure, not a silent one. The
  class additionally null-guards both dependencies, so if a future host registers `null` the only
  consequence is unversioned asset URLs, not a crash.

### 14.32 No `Widget` view component exists yet

- **What changed:** `HtmlExtensions.Widget(...)` no longer calls
  `Html.Action("WidgetsByZone", "Widget", …)`. It resolves `IViewComponentHelper`, contextualizes it
  with the ambient `ViewContext`, and invokes a view component **named `"Widget"`**.
- **Fix (task 7.3):** convert `Nop.Web/Controllers/WidgetController.WidgetsByZone` (which carries
  `[ChildActionOnly]`) into a view component whose class name resolves to `"Widget"` — i.e.
  `WidgetViewComponent` — with an `Invoke`/`InvokeAsync(string widgetZone, object additionalData)`
  signature, returning the same `_widgetModelFactory.GetRenderWidgetModels(widgetZone, additionalData)`
  model and the same partial (returning empty content when the model is empty).
- **Impact if unfixed:** `InvokeAsync` throws `InvalidOperationException: A view component named
  'Widget' could not be found` on the first page render. Loud, not silent. There are **205
  `@Html.Widget(...)` call sites** across the Nop.Web and Nop.Admin views, and none of them need to
  change.
- **Two behaviour notes for 7.3/8.4:** the `area` parameter is now **inert** (view components are
  resolved application-wide, not per area), and view components **do not execute the action-filter
  pipeline** — the property task 6.2 already relied on when it removed the `IsChildAction` guards
  from 11 filters. Any filter behaviour the widget child action used to inherit is gone.

### 14.33 Cache busting no-ops for assets outside the web root

- **What changed:** every asset URL emitted by `GenerateScripts`/`GenerateCssFiles` is passed
  through `IFileVersionProvider.AddFileVersionToPath(Request.PathBase, url)`, which appends
  `?v=<SHA256 of the file content>`.
- **Behaviour:** `DefaultFileVersionProvider` resolves the path against
  `IWebHostEnvironment.WebRootFileProvider` and, when the file is not found, **returns the path
  unchanged**. That is the desired failure mode for absolute/CDN URLs, but it also means any asset
  served from outside `wwwroot` gets **no version suffix and no warning**.
- **Why this matters now:** in 3.90 the static assets lived at `~/Content/…`, `~/Scripts/…`,
  `~/Themes/<theme>/Content/…` and `~/Administration/Content/…`, served by `System.Web`'s handler
  pipeline from the application root — there was no `wwwroot`. Design §6 says the admin's
  `Content/`/`Scripts/` "move under `wwwroot` (or are served via the static-file middleware
  configured by the host)". **Whichever of those two the host picks, the file provider used for
  versioning must cover it**, or cache busting quietly reverts to 3.90-with-bundling-off behaviour.
- **Fix (task 7.2, with 8.x for admin assets):** either relocate the static asset trees under
  `wwwroot`, or add a composite `IFileProvider` covering the extra roots and make it the
  `WebRootFileProvider`. Verify by loading a page and confirming `?v=` is present on the emitted
  `<script>`/`<link>` URLs.
- **Impact if unfixed:** stale CSS/JS after a deploy — the exact regression the cache-busting
  requirement exists to prevent. Silent.

### 14.34 `Security/FilePermissionHelper.cs` does not compile — DISCOVERED BY THE PROBE, BLOCKS THE 6.6 GATE

- **This is not a 6.3 file and not a 6.4 file.** It was reported as clean by 6.2 only because its
  error is in a method **body**, and bodies do not bind while declaration errors exist. The probe
  bound bodies and found it.
- **The error**, at `Security/FilePermissionHelper.cs` line 37:
  ```
  error CS1929: 'Directory' does not contain a definition for 'GetAccessControl' and the best
  extension method overload 'FileSystemAclExtensions.GetAccessControl(DirectoryInfo,
  AccessControlSections)' requires a receiver of type 'System.IO.DirectoryInfo'
  ```
- **Why:** .NET Framework had `System.IO.Directory.GetAccessControl(string)`. On .NET the ACL API
  moved to extension methods on `DirectoryInfo`/`FileInfo`
  (`System.IO.FileSystemAclExtensions`), so the static `Directory` overload no longer exists.
- **The fix is one line** — `new DirectoryInfo(path).GetAccessControl()`:
  ```csharp
  rules = new DirectoryInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
  ```
  The surrounding `try { … } catch { return true; }` already swallows every failure, and these APIs
  throw `PlatformNotSupportedException` off Windows, so the Windows-first posture recorded in
  `Nop.Web.Framework.csproj` is unaffected.
- **6.3 deliberately did NOT apply it**, to keep the task boundary honest — it is neither Razor
  infrastructure nor an HTML/URL helper, and it is not in either task's error inventory.
  **Task 6.6 owns it**, and 6.6 must expect the residual error count after 6.4 to be **1, not 0**.
- **Warning for 6.4 and 6.6:** the probe technique is the only way to see errors of this class.
  When 6.4 finishes, the declaration errors reach zero and the compiler will bind all bodies for the
  first time — **expect a fresh crop of body-level errors across files every earlier task considered
  finished.** Budget for it.

### 14.35 Minification is gone and nothing replaces it

- Bundling's other benefit was minification (`WebGrease` + `Antlr`). Design §8 records that this is
  better solved at build/deploy time, and that `LigerShark.WebOptimizer.Core` or a `NUglify`-based
  step are the candidates. Nothing in the plan schedules it.
- **Impact:** larger CSS/JS payloads than 3.90 *would have had if bundling were enabled*. Since
  `CodeFirstInstallationService` seeds `EnableJsBundling = false` / `EnableCssBundling = false`, a
  stock 3.90 install was **not** minifying either, so this is not a regression against the default —
  only against the opt-in configuration. Recorded for completeness, not as a defect.

---

## 15. Breaking constructor / signature changes — task 6.3

Every public/protected member below changed shape. Downstream owners: **7.3** (Nop.Web views),
**8.4** (Nop.Admin views), **6.4** (`DependencyRegistrar`, host registration), plugin tasks 10–15.

### 15a. The two cross-cutting type substitutions

`MvcHtmlString` → `IHtmlContent` (concretely `Microsoft.AspNetCore.Html.HtmlString`) and
`System.Web.Mvc.HtmlHelper`/`HtmlHelper<TModel>` → `IHtmlHelper`/`IHtmlHelper<TModel>` were applied
to **every** public helper in the files 6.3 owns. `MvcHtmlString.Create(s)` → `new HtmlString(s)`;
`MvcHtmlString.Empty` → `HtmlString.Empty`. `UrlHelper` → `IUrlHelper` throughout.

**View call sites are almost entirely unaffected**, because Razor writes an `IHtmlContent` directly
and `@Html` is already an `IHtmlHelper` in ASP.NET Core. The one thing that would have broken en
masse is `.ToHtmlString()`, which the views use extensively — see the new shim next.

### 15b. New public type: `Nop.Web.Framework.HtmlContentExtensions`

**Additive**, in the new file `HtmlContentExtensions.cs`:

| Member | Purpose |
|---|---|
| `string ToHtmlString(this IHtmlContent content)` | Renders an `IHtmlContent` via `HtmlEncoder.Default`, with a fast path for `HtmlString`. Deliberately named `ToHtmlString` so the many `@Html.Xxx().ToHtmlString()` sites in Nop.Web/Nop.Admin views compile unchanged. |
| `string ToHtmlString(this TagBuilder tagBuilder, TagRenderMode renderMode)` | Replaces MVC 5's `TagBuilder.ToString(TagRenderMode)`. |

**Why this exists rather than a mechanical rename.** `MvcHtmlString` was a string wrapper:
`ToString()` and `ToHtmlString()` both returned markup, and nopCommerce composes markup by
concatenating helper results into `StringBuilder`s in dozens of places. `IHtmlContent` is a
*writer*: markup only exists after `WriteTo(TextWriter, HtmlEncoder)`. **Verified: `TagBuilder`
does not override `ToString()` — it returns the literal string
`"Microsoft.AspNetCore.Mvc.Rendering.TagBuilder"`.** Every `ToString()`/implicit-concat site left in
place would therefore have emitted the type name into the page: a bug that compiles cleanly, passes
the gate, and is only visible in the rendered HTML. The sites converted for exactly this reason were
`Hint`, `RequiredHint`, `NopLabelFor`, `NopDisplayFor`, `NopDisplay`, `LabelFor`,
`DatePickerDropDowns` (three `TagBuilder`s used in string concatenation), `RenderBootstrapTabContent`,
`RenderBootstrapTabHeader`, `OverrideStoreCheckboxFor`, `NopEditorFor`, `NopDropDownList`,
`NopDropDownListFor`, `NopTextAreaFor`, `DeleteConfirmation`, `ActionConfirmation`,
`Pager.CreatePageLink`, `GRecaptchaControl.RenderControl`, `Honeypot.GenerateHoneypotInput` and
`DataListExtensions.DataList`.

### 15c. `IPageHeadBuilder` / `PageHeadBuilder`

| Before | After | Consumer impact |
|---|---|---|
| `PageHeadBuilder(SeoSettings)` | `PageHeadBuilder(SeoSettings, IFileVersionProvider, IHttpContextAccessor)` | **Reported as a signature change for 6.4 per the task brief.** No `DependencyRegistrar` *edit* is needed — line 330 uses reflective `RegisterType<PageHeadBuilder>()`, so Autofac picks the new constructor up. What 6.4/7.2 must guarantee is that both services are resolvable — deferral 14.31. |
| `IPageHeadBuilder.GenerateScripts(UrlHelper, ResourceLocation, bool?)` | `GenerateScripts(IUrlHelper, ResourceLocation, bool?)` | Only the URL helper type changes. `bundleFiles` is **kept and ignored**. |
| `IPageHeadBuilder.GenerateCssFiles(UrlHelper, ResourceLocation, bool?)` | `GenerateCssFiles(IUrlHelper, ResourceLocation, bool?)` | As above. |
| `protected virtual string PageHeadBuilder.GetBundleVirtualPath(string, string, string[])` | **DELETED** | It computed a SHA256 bundle path via `System.Web.HttpServerUtility.UrlTokenEncode`. Nothing to name with no bundles. `protected virtual`, so a plugin subclass overriding it breaks — none in tree. |
| `protected virtual IItemTransform PageHeadBuilder.GetCssTranform()` | **DELETED** | Returned `CssRewriteUrlTransform`, a `System.Web.Optimization` type. Its job was rewriting relative `url(...)` inside a *bundled* stylesheet served from a different path; unbundled stylesheets are served from their own location, so relative URLs resolve natively. |
| `Nop.Web.Framework.UI.AsIsBundleOrderer` | **DELETED** (file removed) | Implemented `System.Web.Optimization.IBundleOrderer` to keep declaration order *within a bundle*. With no bundles there is nothing to order. Referenced only from `PageHeadBuilder`. |
| — | **additive** `protected virtual string PageHeadBuilder.GetAssetUrl(IUrlHelper, string)` | The cache-busting seam: `urlHelper.Content(part)` then `IFileVersionProvider.AddFileVersionToPath`. `virtual` so a plugin can substitute a CDN/fingerprinting scheme. |

**All other 20+ `IPageHeadBuilder` members are byte-identical, including every `excludeFromBundle`
parameter.** `Nop.Web/Factories/CommonModelFactory.cs` (`GetEditPageUrl()`) and
`Controllers/BaseController.cs` (`AddEditPageUrl(...)`) need no change. **Zero view call sites
change** — the design §8 constraint is met.

### 15d. `LayoutExtensions` — `MvcHtmlString`→`IHtmlContent`, `HtmlHelper`→`IHtmlHelper`, `UrlHelper`→`IUrlHelper`

All 27 members re-typed; parameter lists otherwise identical, `excludeFromBundle`/`bundleFiles`
retained. `NopScripts`/`NopCssFiles` still take the URL helper as their second argument, so
`@Html.NopScripts(Url, ResourceLocation.Head)` in the layouts is unchanged — `Url` is an
`IUrlHelper` in an ASP.NET Core view. `NopPageCssClasses` still returns `null` (not empty content)
when there are no classes; Razor writes nothing for a null `IHtmlContent`, as it did for a null
`MvcHtmlString`.

### 15e. `HtmlExtensions`

| Member | Before | After | Consumers to fix |
|---|---|---|---|
| `LocalizedEditor<T, TLocalizedModelLocal>` | `HelperResult` built from `System.Web.WebPages.HelperResult(Action<TextWriter>)` | `Microsoft.AspNetCore.Mvc.Razor.HelperResult(Func<TextWriter, Task>)` | **None.** `HelperResult` is still the type Razor templated delegates (`@<text>…</text>`) produce, so `Func<int, HelperResult>` / `Func<T, HelperResult>` parameters bind unchanged in the ~40 admin `_CreateOrUpdate.cshtml` views. Only the constructor's delegate shape changed, and that is internal to this method. |
| `AddFormControlClassToHtmlAttributes` | `RouteValueDictionary AddFormControlClassToHtmlAttributes(IDictionary<string, object>)` | `IDictionary<string, object> AddFormControlClassToHtmlAttributes(IDictionary<string, object>)` | No in-tree caller outside this file. **See the bug note below — this is the one place the port had to change behaviour to preserve behaviour.** |
| `FieldIdFor<T, TResult>` | `TemplateInfo.GetFullHtmlFieldId(...)` + manual `'['/']' → '_'` | `html.IdFor(expression)` | None; signature unchanged. `TemplateInfo.GetFullHtmlFieldId` **does not exist in ASP.NET Core**. Output verified identical: `CreateSanitizedId("Locales[0].Name", "_")` returns `Locales_0__Name`, exactly what MVC 5's `GetFullHtmlFieldId` + manual replacement produced. |
| `FieldNameFor<T, TResult>` | `TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(...))` | `html.NameFor(expression)` | None; same composition, same output. |
| `LabelFor<TModel, TValue>(expression, object, string)` | `ModelMetadata.FromLambdaExpression`, `TagBuilder.CreateSanitizedId(GetFullHtmlFieldId(name))`, `SetInnerText` | `IModelExpressionProvider`, `html.IdFor(expression)`, `InnerHtml.SetContent` | None; signature unchanged. Note the custom 3-arg overload still resolves against the framework's own `LabelFor(expression, string labelText, object htmlAttributes)` because the parameter types differ in order. |
| `NopLabelFor`, `NopEditorFor` | `ModelMetadata.FromLambdaExpression(expression, ViewData)` | `IModelExpressionProvider.CreateModelExpression(...).Metadata` | None; signatures unchanged. `FromLambdaExpression` has **no** ASP.NET Core counterpart — `ExpressionMetadataProvider` is `internal`. A directly constructed `ModelExpressionProvider(helper.MetadataProvider)` is the fallback when request services are absent. |
| `GetSelectedTabName` | `helper.ViewContext.Controller.TempData` | `helper.TempData` | None. `ViewContext.Controller` does not exist in ASP.NET Core; `IHtmlHelper.TempData` is the same dictionary. Keep this synchronized with `BaseAdminController.SaveSelectedTab` as the original comment says. |
| `DeleteConfirmation`, `ActionConfirmation` | `RouteData.GetRequiredString("controller"/"action")` | private `GetRequiredRouteValue(ViewContext, key)` → `RouteData.Values[key]?.ToString()`, throwing when absent | None. `GetRequiredString` is MVC 5 only. Route values are always strings in ASP.NET Core (task 6.2's `StoreClosedAttribute` note). |
| `Widget(this IHtmlHelper, string, object, string)` | `helper.Action("WidgetsByZone", "Widget", new { widgetZone, additionalData, area })` | `IViewComponentHelper.InvokeAsync("Widget", new { widgetZone, additionalData })`, contextualized via `IViewContextAware`, awaited synchronously | **Signature unchanged** so all 205 view call sites compile. `area` is now inert. Needs the view component from 7.3 — deferral 14.32. Adds a fourth sync-over-async site to the three 6.2 accepted. |
| `LocalizedEditor`, `DeleteConfirmation`, `ActionConfirmation`, `RenderBootstrapTab*`, `Hint`, `RequiredHint`, `NopLabelFor`, `NopEditorFor`, `NopDropDownList`, `NopDropDownListFor`, `NopTextAreaFor`, `NopDisplayFor`, `NopDisplay`, `OverrideStoreCheckboxFor` (4 overloads), `DatePickerDropDowns`, `LabelFor` | return `MvcHtmlString` | return `IHtmlContent` | Views are unaffected (Razor writes `IHtmlContent`; `.ToHtmlString()` still works via §15b). A **plugin** that assigns the result to an explicitly typed `MvcHtmlString` local must change the type. |
| `new UrlHelper(helper.ViewContext.RequestContext)` inside `LocalizedEditor` | — | `IUrlHelperFactory.GetUrlHelper(helper.ViewContext)` from request services | Internal. Same substitution 6.2 made in `ValidatePasswordAttribute`/`StoreClosedAttribute`. Hoisted out of the language loop, so it is constructed once instead of once per locale. |
| `HttpUtility.HtmlEncode` in `LocalizedEditor` | `System.Web.HttpUtility` | `System.Net.WebUtility` | Internal. Identical behaviour for `HtmlEncode`; the same swap made across Nop.Core, Nop.Services and 6.2's `RemotePost`. |

**The one behaviour change required to preserve behaviour —
`AddFormControlClassToHtmlAttributes`.** MVC 5's `HtmlHelper.AnonymousObjectToHtmlAttributes`
returned a `System.Web.Routing.RouteValueDictionary`, whose indexer returns `null` for an absent
key. ASP.NET Core's returns a plain `Dictionary<string, object>` (verified). Two consequences, both
silent-or-fatal if ported literally:

1. `return htmlAttributes as RouteValueDictionary;` would now yield **`null`**, dropping every html
   attribute — hence the return type change.
2. `htmlAttributes["class"]` on a missing key throws **`KeyNotFoundException`** instead of returning
   `null`. That would have thrown for every `NopDropDownList` / `NopDropDownListFor` /
   `NopTextAreaFor` call that does not pass an explicit class — i.e. most of them, across the whole
   admin UI. Rewritten with `TryGetValue`, which reproduces MVC 5 exactly.

### 15f. Other type/member changes

| Member | Before | After | Consumers to fix |
|---|---|---|---|
| `ViewEngines.Razor.WebViewPage<TModel>` | `: System.Web.Mvc.WebViewPage<TModel>` | `: Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>` | The class stays `abstract`, so `RazorPageBase.ExecuteAsync()` (abstract, generated per `.cshtml` by the Razor compiler) needs no override here. Views select this base with `@inherits`/`_ViewImports` — tasks 7.3/8.4. |
| `WebViewPage<TModel>.InitHelpers()` override | `public override void InitHelpers()` | **REMOVED**, replaced by **additive** `protected virtual ILocalizationService GetLocalizationService()` | `InitHelpers` is a `System.Web.WebPages.WebPageBase` lifecycle hook; `RazorPageBase` has **no** per-page initialization callback at all. The resolve moved to first use of `T`. Small deliberate improvement: the localizer now null-guards the service, so an install-mode view renders the raw format string instead of throwing `NullReferenceException` as 3.90 would have. |
| `WebViewPage<TModel>.Layout` override | `public override string Layout { get; set; }` — resolved the layout name through the view engine to a themed path | **REMOVED** | `RazorPageBase.Layout` is **not virtual**, so it cannot be overridden; and the override is unnecessary — ASP.NET Core resolves a named layout through the same view-location pipeline as any view, so `ThemeableViewLocationExpander` themes layouts automatically. Views that set `Layout` to an explicit `~/...` path (which most nopCommerce views do) are unaffected either way. |
| `ThemeableRazorViewEngine`, `ThemeableVirtualPathProviderViewEngine` (incl. nested `ViewLocation`, `AreaAwareViewLocation` and ~14 `protected virtual` members) | classes | **DELETED** | See §16 for the full inventory of what each member's behaviour became. `Nop.Web/Global.asax.cs` lines 60–62 reference `ThemeableRazorViewEngine`; task 7.2 deletes that file anyway. |
| — | — | **additive** `Themes.ThemeableViewLocationExpander : IViewLocationExpander` | Public `ThemeKey` const, `virtual PopulateValues`, `virtual ExpandViewLocations`, `protected virtual GetCurrentTheme()`. Declared `partial`, matching nopCommerce's convention for extensible framework types. |
| `UI.Paging.Pager` | `: System.Web.IHtmlString` | `: Microsoft.AspNetCore.Html.IHtmlContent`, with **additive** `virtual void WriteTo(TextWriter, HtmlEncoder)` | `Pager(IPageableModel, ViewContext)` is unchanged apart from `ViewContext` moving to `Microsoft.AspNetCore.Mvc.Rendering`. The instance `ToHtmlString()` is retained (no longer an interface member) so `Html.Pager(...)` chains and `pager.IsEmpty()` in ~20 Nop.Web views are untouched. `Nop.Web`'s `Html.Pager` extension (task 7.3) must re-type its `HtmlHelper` parameter. |
| `protected virtual string Pager.CreateDefaultUrl(int)` | `UrlHelper.GenerateUrl(null, null, null, routeValues, RouteTable.Routes, viewContext.RequestContext, true)` | rebuilds `Request.PathBase + Request.Path` + a `QueryBuilder`-composed query string | Signature unchanged. **Reimplementation, not a rename** — see the note below. |
| `UI.DataListExtensions.DataList<T>` | `IHtmlString DataList<T>(this HtmlHelper, …, Func<T, HelperResult>)` | `IHtmlContent DataList<T>(this IHtmlHelper, …, Func<T, HelperResult>)` (`HelperResult` from `Microsoft.AspNetCore.Mvc.Razor`) | View call sites unchanged. |
| `UrlHelperExtensions.LogOn/LogOff` | `this UrlHelper` | `this IUrlHelper` | Call syntax and generated URL unchanged; `Action(string, string, object)` is now itself an extension (`Microsoft.AspNetCore.Mvc.UrlHelperExtensions`). |
| `Events.AdminTabStripCreated` | `AdminTabStripCreated(HtmlHelper, string)`; `HtmlHelper Helper`; `IList<MvcHtmlString> BlocksToRender` | `AdminTabStripCreated(IHtmlHelper, string)`; `IHtmlHelper Helper`; `IList<IHtmlContent> BlocksToRender` | The ~30 admin `_CreateOrUpdate.cshtml` views pass `this.Html` (already an `IHtmlHelper`) and render with `@eventBlock` (writes an `IHtmlContent`) — **no view change**. Any **plugin** event consumer that adds a block must produce an `IHtmlContent`; `Html.Partial(...)` already returns one. |
| `Security.Captcha.GRecaptchaControl.RenderControl` | `RenderControl(System.Web.UI.HtmlTextWriter)` | `RenderControl(System.IO.TextWriter)` | **`HtmlTextWriter` has no ASP.NET Core equivalent** (Web Forms). Nothing used its HTML-aware API — every call was a plain `Write(string)` — so a bare `TextWriter` is a complete replacement. In-tree caller is `Captcha/HtmlExtensions.GenerateCaptcha`, updated. A plugin calling `RenderControl` must pass a `TextWriter`. |
| `Security.Captcha.HtmlExtensions.GenerateCaptcha` | `this HtmlHelper` → `string` | `this IHtmlHelper` → `string` | Return type deliberately still `string` (3.90's shape). 3.90 wrapped a `StringWriter` in an `HtmlTextWriter` and then read it back through `InnerWriter.ToString()` — the `HtmlTextWriter` was pure ceremony. Emitted markup byte-identical. |
| `Security.Honeypot.HtmlExtensions.GenerateHoneypotInput` | `MvcHtmlString GenerateHoneypotInput(this HtmlHelper)` | `IHtmlContent GenerateHoneypotInput(this IHtmlHelper)` | `helper.TextBox(name)` now returns `IHtmlContent`, so it is rendered rather than `ToString()`-ed. |
| `Localization.LocalizedString` | `: MarshalByRefObject, System.Web.IHtmlString` | `: MarshalByRefObject, Microsoft.AspNetCore.Html.IHtmlContent`, with **additive** `WriteTo(TextWriter, HtmlEncoder)` | Instance `ToHtmlString()` retained (no longer an interface member), so the many `T("…").ToHtmlString()` view sites compile. `Localizer` delegate, `Text`/`Scope`/`TextHint`/`Args`/`TextOrDefault`/`Equals`/`GetHashCode` all unchanged. **See why this had to move, below.** |

**Why `LocalizedString` had to move even though it compiled.** Task 6.2 correctly observed that
`System.Web.IHtmlString`/`HtmlString` still ship on net10.0 in the in-box `System.Web.HttpUtility`
assembly, and left the decision to 6.3. The decision is to port it: ASP.NET Core's Razor engine,
`TagBuilder` and `IHtmlContentBuilder` recognise **only** `IHtmlContent`. A `LocalizedString`
implementing only the legacy interface would be written through `object.ToString()` and then
**HTML-encoded by Razor**, silently double-encoding every localized resource containing markup or an
apostrophe — across the entire UI. `MarshalByRefObject` is retained (harmless, and removing it would
be a gratuitous hierarchy change).

**Why `Pager.CreateDefaultUrl` is a reimplementation.** 3.90 called
`UrlHelper.GenerateUrl(null, null, null, routeValues, RouteTable.Routes, requestContext,
includeImplicitMvcValues: true)`: it took the query string, folded in the implicit
action/controller/area, and asked the **global static `RouteTable`** to regenerate a matching URL.
ASP.NET Core has neither a static route table nor `UrlHelper.GenerateUrl`, and `LinkGenerator` is not
equivalent — nopCommerce's SEO URLs come from `GenericPathRoute` (task 6.4), so round-tripping
through link generation could not reproduce a slug path. Since every value fed into `routeValues`
here comes from the **query string** and the only thing that changes is the page parameter, the
current path is by definition the correct path; the port keeps `PathBase + Path` and rebuilds only
the query. For the URLs that matter (`/category-slug?pagenumber=2`) the output string is the same,
and it no longer depends on a reverse-routable route existing. `Request.QueryString.AllKeys` →
`Request.Query.Keys`; the `renderEmptyParameters` hack and its
`IWebHelper.ModifyQueryString` follow-up are preserved verbatim.

**Unchanged — do not touch:**

- `Localization/Localizer.cs`, `ILocalizedModel.cs`, `ILocalizedModelLocal.cs`,
  `Localization/LocalizedUrlExtenstions.cs` — no edit needed.
- `Themes/IThemeContext.cs`, `IThemeProvider.cs`, `ThemeConfiguration.cs`, `ThemeContext.cs`,
  `ThemeProvider.cs` — **no edit**. The theming *model* is unchanged; only view *location* changed.
- `UI/Paging/IPageableModel.cs`, `BasePageableModel.cs`, `UI/ResourceLocation.cs`,
  `UI/NotifyType.cs`, `Menu/IAdminMenuPlugin.cs`, `Menu/Extensions.cs` — no System.Web surface.
- `Nop.Core.Domain.Seo.SeoSettings` — **NOT touched** (it is in Nop.Core, committed and gated).
  `EnableJsBundling` / `EnableCssBundling` survive on the entity; no DB migration. They are now
  inert. Task **8.4** removes the two now-dead checkboxes from
  `Nop.Web/Administration/Views/Setting/GeneralCommon.cshtml`.

---

## 16. The themeable view engine redesign, in full

3.90's theming was two classes, ~330 lines, subclassing `System.Web.Mvc.VirtualPathProviderViewEngine`
and re-implementing its `GetPath`/`GetPathFromGeneralName`/`FindView`/`FindPartialView` internals
(the source comment says: *"the original implementation can be found at
aspnetwebstack.codeplex.com … we make some methods protected virtual because they are overridden by
some plugin vendors"*). **Neither base class has an ASP.NET Core counterpart, and there is nothing
to subclass**: ASP.NET Core's `RazorViewEngine` is a framework service, and view location is
customised by contributing an `IViewLocationExpander` to `RazorViewEngineOptions.ViewLocationExpanders`.
You supply location *formats* and expansion; you do not own lookup.

The replacement is `Themes/ThemeableViewLocationExpander.cs`, ~180 lines including commentary.

### 16.1 What is PRESERVED

| 3.90 behaviour | How |
|---|---|
| Per-theme public-store views: `~/Themes/{theme}/Views/{controller}/{view}.cshtml` then `~/Themes/{theme}/Views/Shared/{view}.cshtml`, before the non-themed defaults | Emitted first in the expanded location list, in that order |
| Non-themed fallbacks `~/Views/{controller}/…`, `~/Views/Shared/…` | Next in the list |
| Admin views resolvable from `~/Administration/Views/…` for **non-area** lookups | Last two entries of the non-area list, same order as 3.90's `ViewLocationFormats` |
| Per-theme **area** views `~/Areas/{area}/Themes/{theme}/Views/…` before `~/Areas/{area}/Views/…` | Area list, same order |
| The "little hack to get nop's admin area to be in /Administration/ instead of /Nop/Admin/ or Areas/Admin/", applied **only** when the area name equals `admin` (case-insensitive) | Two entries prepended to the area list for that area only |
| 3.90's exact ordering quirk inside that hack | 3.90 did two `Insert(0, …)` calls, so `…/Views/Shared/{0}.cshtml` ends up **before** `…/Views/{1}/{0}.cshtml`. Preserved **verbatim** rather than "corrected", so admin view resolution behaves as it did. Flagged here because it is surprising: a same-named Shared view shadows the controller-specific one. |
| Theme resolved per lookup through `IThemeContext.WorkingThemeName` via `EngineContext` (3.90's `GetCurrentTheme()`) | `protected virtual GetCurrentTheme()`, same seam, same cost. Wrapped in try/catch so a not-yet-installed store resolves from the non-themed locations instead of throwing inside view lookup |
| Per-theme cache correctness (3.90's `CreateCacheKey(prefix, name, controller, area, theme)`) | The theme name is written to `ViewLocationExpanderContext.Values`, which forms part of the framework's view-lookup cache key. **This is required, not cosmetic** — without it the first theme's resolved path would be cached and served to every store/theme |
| `protected virtual` extensibility for plugin vendors | `PopulateValues`, `ExpandViewLocations` and `GetCurrentTheme` are `virtual`; the class is `partial` |
| Layout ("master") theming | Now automatic: ASP.NET Core resolves a named layout through the same location pipeline, so the expander themes layouts too. This is why `WebViewPage.Layout`'s override could be deleted |

### 16.2 What is SIMPLIFIED — six format arrays collapse to two

3.90 maintained `ViewLocationFormats`, `PartialViewLocationFormats`, `MasterLocationFormats` and the
three `Area*` equivalents. ASP.NET Core resolves views, partials **and** layouts through one location
list, so there are now one non-area and one area list. In 3.90 `ViewLocationFormats` and
`PartialViewLocationFormats` were already identical; `MasterLocationFormats` was the only one that
differed — it **omitted** the two `~/Administration/Views/…` entries. The unified list is therefore a
strict superset: a layout referenced **by name** can now also be found under `~/Administration/`.
In practice nopCommerce's admin views set `Layout` to an explicit `~/Administration/Views/Shared/_AdminLayout.cshtml`
path, which bypasses location formats entirely, so nothing observable changes.

Placeholder mapping also changed and is worth stating: 3.90's formats used `{2}`=theme for non-area
and `{2}`=area/`{3}`=theme for area formats. ASP.NET Core fixes `{0}`=view, `{1}`=controller,
`{2}`=area, so the theme **cannot** be a positional placeholder. It is substituted into the format
string at expansion time (a `{theme}` token), which is the documented expander pattern. Paths also
lose the leading `~`: ASP.NET Core location formats are rooted `/`.

### 16.3 What is LOST

| Lost | Consequence |
|---|---|
| `System.Web.WebPages` **display modes** (`DisplayModeProvider`, `IDisplayMode`, `DisplayInfo`, `AppendDisplayModeToCacheKey`, `ControllerContext.DisplayMode`) | ASP.NET Core has no display-mode concept. **Verified nothing is lost in practice:** nopCommerce 3.90 registers no custom display mode and ships no `*.Mobile.cshtml` file anywhere in the solution (`grep` returns nothing) — 3.90 is responsive-design, not mobile-view based. Only System.Web's implicit `.Mobile` convention is gone, and it was unused. |
| `FileExtensions` / `FilePathIsSupported` / `GetExtensionThunk` (`VirtualPathUtility.GetExtension`) | The engine only ever allowed `cshtml`; ASP.NET Core's Razor engine only handles `.cshtml`. No behaviour change. |
| `IsSpecificPath(name)` and `GetPathFromSpecificName` — the `~`/`/`-prefixed "this is a literal path" branch | ASP.NET Core's `RazorViewEngine` has the same branch built in (`GetView`/`GetPage` for path-like names, `FindView`/`FindPage` for names). Location formats are not consulted for a literal path in either framework. Behaviour preserved by the framework, not by this code. |
| `ViewLocationCache` manipulation, including "cache both hits and misses so a later lookup can distinguish an evicted entry from a nonexistent file" | The framework owns view-lookup caching entirely. Cache correctness is delivered by the `Values` key instead. |
| `GetAreaName(RouteData)` / `GetAreaName(RouteBase)` including the `IRouteWithArea` and `Route.DataTokens["area"]` probes | ASP.NET Core supplies the area name directly as `ViewLocationExpanderContext.AreaName`. |
| The `protected virtual` members `CreateCacheKey`, `AppendDisplayModeToCacheKey`, `GetPath`, `GetPathFromGeneralName`, `GetPathFromSpecificName`, `GetViewLocations`, `FilePathIsSupported`, `IsSpecificPath`, `GetAreaName` ×2, `CreatePartialView`, `CreateView`, and the public nested `ViewLocation`/`AreaAwareViewLocation` classes | **Any plugin that subclassed `ThemeableVirtualPathProviderViewEngine` or `ThemeableRazorViewEngine` to override these has no migration path** and must be rewritten as its own `IViewLocationExpander`. Verified: **no such subclass exists in this solution** — the only reference outside the two files was `Nop.Web/Global.asax.cs` constructing `ThemeableRazorViewEngine`, and 7.2 deletes that file. This is nonetheless a real break for third-party plugins, and it is unavoidable: the base classes do not exist. |
| `ViewEngines.Engines.Clear()` — 3.90 removed **all** other view engines so only the themeable one ran | The expander **adds to** the framework defaults rather than replacing them: the framework's own `ViewLocationFormats` are appended as a final fallback. This is deliberate, not an oversight — it is what lets views contributed by a Razor class library or a plugin **application part** still resolve, which deferral 1.2 (plugin Razor views) needs. The practical difference is that a view found in a conventional ASP.NET Core location would now be used where 3.90 would have reported "view not found". |

### 16.4 Not preserved because it never existed

No mobile/desktop variants, no `.Mobile.cshtml`, no custom `IDisplayMode`. Checked, not assumed.



---

# Nop.Web.Framework — System.Web → ASP.NET Core, part 3: routing, modules → middleware, DI (task 6.4)

Task 6.4 took the project from **79 errors to 0** — the **6.6 gate is a formality**. It also
folded in deferral **34** (`FilePermissionHelper`), which 6.3 had left for 6.6, so the residual
error count at 6.6 is **0, not 1**.

| Measurement | Value |
|---|---|
| errors at start (per 6.3's handover, re-measured) | **79**, all declaration-phase, in the 12 files 6.4 owns |
| errors at end | **0** |
| **body-level wave after declarations cleared** | **1** — only `FilePermissionHelper.cs` line 43, i.e. exactly the one item 6.3's probe had already predicted (deferral 34). **No other file produced a body-level error.** |
| warnings at end | **59** — 10 pre-existing `SYSLIB*` obsolescence notices from `Nop.Core`/`Nop.Services` (unchanged since 6.3) plus **49 new `CA1416`**, all in `Security/FilePermissionHelper.cs`, which the analyser could not see until the file compiled. They are the Windows-only ACL/`WindowsIdentity` surface the `.csproj` already documents as an accepted Windows-first trade-off. Non-blocking (Req 3.3). |
| swallowed-error check | verbose log grep for `"converted to a warning"` → **0**, `"ContinueOnError"` → **0**, `NU19*` restore advisories → **0** |
| residual legacy references in `Nop.Web.Framework.dll` | **none** — no `System.Web*`, no `Autofac.Integration.Mvc`, no `System.Web.Optimization`, no `ImageResizer`, no `System.Drawing*`. The only `System.Web` string left anywhere in the project is inside a doc comment in `IRouteProvider.cs` telling implementers what to replace. |

> **The zero body-level wave is a result, not luck.** 6.3 warned to budget for a fresh crop of
> body errors once declarations cleared. 6.4 verified bodies really were binding by planting a
> deliberate `CS0103` in `Menu/Extensions.cs` and confirming it was reported, then reverting
> (`git status` clean of it). Bodies bind, and they are clean — because 6.2 and 6.3 each
> pre-validated theirs with a throwaway probe. The technique paid for itself twice.
>
> 6.4 used the same probe technique **before** writing any code, to verify eleven API shapes
> against the real net10.0 reference assemblies and Autofac 9.3.2 rather than assuming them.
> One assumption was wrong and it changed the design — see §17.3.

---

## 17. Task 6.4 — what changed, deferral by deferral

### 17.1 Deferrals CLOSED, and deferrals handed to 7.2 with the work already done

| # | Status after 6.4 | How |
|---|---|---|
| **3** (1.3) per-request DI scope | ✅ **RESOLVED** | `DependencyRegistrar` now calls `builder.RegisterBuildCallback(...)` and assigns `ContainerManager.CurrentScopeProvider` — see §17.5 for why that location. |
| **24** `LanguageSeoCodeAttribute` no-ops | ✅ **RESOLVED** | `LocalizedRoute` is now an endpoint-metadata marker attached by `MapLocalizedRoute`; the filter calls the new `LocalizedRoute.IsLocalizableRequest(HttpContext)`. |
| **34** `FilePermissionHelper` | ✅ **RESOLVED** | `Directory.GetAccessControl(path)` → `new DirectoryInfo(path).GetAccessControl()`. One line, in this project, folded in so 6.6 is clean. |
| **9** (4.9) `NopObjectContext` connection string | ✅ **verified clean for this project** | Both `IDbContext` registrations already pass `DataConnectionString`. Still open only for the `Nop.Data.Tests` fixtures (task 3.4, skipped). |
| **6** (1.6) `IHostApplicationLifetime` | ✅ **no 6.4 work possible or needed** | The .NET generic host registers it on the `IServiceCollection`, and `Autofac.Extensions.DependencyInjection`'s `Populate` copies it into the nopCommerce container. It becomes resolvable the moment 7.2 uses `AutofacServiceProviderFactory`. Documented in `NopServiceCollectionExtensions`. |
| **2** (1.2) plugin application parts | ⚠️ **mechanism written, ordering is 7.2's** | `NopServiceCollectionExtensions.AddPluginApplicationParts(ApplicationPartManager)` adds an `AssemblyPart` for every `PluginManager.ReferencedPlugins` assembly, and `AddNopFramework()` wires it via `ConfigureApplicationPartManager`. It is a **silent no-op until `PluginManager.Initialize()` has run** (deferral 1, task 7.2), so `Program.cs` must call `PluginManager.Initialize()` **before** `AddNopFramework()`. |
| **13** cookie auth · **15** session · **20** FluentValidation · **21** metadata provider · **22** model-binder provider · **23** JSON naming · **25** `ChallengeResult` · **26** `IAntiforgery` · **29/11.29** forwarded headers · **30** theming expander · **31** `PageHeadBuilder` services | ⚠️ **all written, all host-side** | Every one is an `IServiceCollection`/`IApplicationBuilder` concern and therefore cannot live in `DependencyRegistrar`, which receives an Autofac `ContainerBuilder`. They are all implemented in the two new helper classes described in §17.6. 7.2 closes them by calling two methods. |

**Deferrals 6.4 explicitly does NOT close and does not own:** 1 (plugin discovery), 4 (config
source), 5 (`CommonHelper.MapPath`), 8 (schema initializer), 32 (`Widget` view component),
33 (cache busting outside `wwwroot`), 35 (minification). All remain with 6.5/7.2/7.3/8.x as
already recorded.

### 17.2 The routing redesign, class by class

`RouteBase`, `Route`, `IRouteHandler`, `MvcRouteHandler`, `RouteCollection`, `RouteTable` and
`VirtualPathData` have **no ASP.NET Core counterparts and nothing to subclass**. Endpoint
routing inverts the model: you register URL *patterns* on an `IEndpointRouteBuilder` and
customise behaviour with metadata, parameter policies, `DynamicRouteValueTransformer`, or
middleware around `UseRouting()`. Each class below was therefore reimplemented against the
mechanism that owns its behaviour, and the dead legacy class was removed rather than left as a
half-ported shell.

#### `Localization/LocalizedRoute.cs` — `Route` subclass → **metadata marker + middleware + `PathBase`**

3.90 did two things in one class. They split:

| 3.90 behaviour | Now | Preserved? |
|---|---|---|
| `GetRouteData`: if SEO-friendly language URLs are on and the path is localized, `httpContext.RewritePath(...)` to strip `/en` so the ordinary patterns match | **`Localization/SeoFriendlyUrlsMiddleware`** (new), registered **before** `UseRouting()` | ✅ **fully**, including shape-only detection (a two-character first segment is stripped whether or not it names an installed language — 3.90 relied on `LanguageSeoCodeAttribute` to redirect away from an unknown code, and still does) |
| `GetVirtualPath`: prefix the generated path with the current language code | the same middleware moves the stripped segment into **`HttpRequest.PathBase`**; ASP.NET Core prefixes `PathBase` onto every URL from `LinkGenerator`/`IUrlHelper`/`Url.Content` | ✅ **behaviourally**, ❌ **not per-route** — see the note below |
| `ClearSeoFriendlyUrlsCachedValue()` clearing a per-route cached copy of the setting | there are no route instances; the middleware resolves `LocalizationSettings` per request, already served from nopCommerce's static settings cache (exactly what `LanguageSeoCodeAttribute` does). `LocalizedRoute.ClearSeoFriendlyUrlsCachedValue()` is retained as a **no-op** | ✅ same observable behaviour, cheaper |
| being a `LocalizedRoute` was the marker `LanguageSeoCodeAttribute` tested | `LocalizedRoute` survives as a **plain marker class** placed in endpoint metadata by `MapLocalizedRoute`, plus the new static `LocalizedRoute.IsLocalizableRequest(HttpContext)` | ✅ deferral 24 closed |

**BEHAVIOUR NOT PRESERVED — outbound prefixing is now request-scoped, not route-scoped.** In
3.90 only URLs generated *through a `LocalizedRoute`* got the `/en` prefix. With `PathBase`,
**every** URL generated during a request that arrived on a localized URL is prefixed — including
non-localized routes such as `widgetsbyzone`. Those URLs still resolve, because the inbound
middleware strips a leading two-character segment from any request. Requests that arrive without
a language segment (the whole admin area, and the public store with the setting off) have an
empty `PathBase` and are bit-for-bit unaffected. The alternative — a `LinkGenerator` decorator —
was rejected: it cannot tell which endpoint a link is being generated for either, so it would
have had exactly the same scope while adding four abstract-method overrides and a service
decoration to the host.

**One consequential knock-on, fixed in the same task.** `WebWorkContext.GetLanguageFromUrl()`
(a 6.2 file) detected the language by re-parsing `Request.Path`. Once the middleware moves the
code into `PathBase` that parse finds nothing, and language-from-URL detection would have
**silently** fallen back to cookie/browser — the exact class of defect this register exists for.
`GetLanguageFromUrl()` now reads `HttpContext.Items["nop.LanguageSeoCode"]`, which the middleware
sets, and keeps the old path-parsing as a fallback for when the middleware did not run.

#### `Seo/GenericPathRoute.cs` — **DELETED**, replaced by `Seo/SlugRouteTransformer.cs` + `Seo/SlugRedirectMiddleware.cs`

**Decision: `DynamicRouteValueTransformer` + `MapDynamicControllerRoute<T>`.** Verified present
in the net10.0 reference assemblies before committing to it. This is the idiomatic ASP.NET Core
mechanism for database-driven route resolution and maps almost one-to-one onto what
`GetRouteData` did: the transformer receives the values matched by `{generic_se_name}` and
returns the values MVC should use for action selection.

Preserved **verbatim**: the `GetBySlugCached` lookup and its commented-out non-cached
alternative; `urlRecord == null` → `Common/PageNotFound`; inactive record → `GetActiveSlug` →
**301**; no active slug → `Common/PageNotFound`; slug differs for the working language →
**302**; all seven entity-name cases with their exact route-value key names (`productid`,
`categoryid`, `manufacturerid`, `vendorid`, `newsItemId`, `blogPostId`, `topicId`, plus
`SeName`); and the `CustomUrlRecordEntityNameRequested` event for unknown entity names.

Two things had to be rebuilt rather than renamed:

1. **The two redirects.** 3.90 wrote `Response.Status`/`RedirectLocation` and called
   `Response.End()` *from inside route matching*, returning `null`. A transformer cannot
   terminate the pipeline, and setting a status code then returning `null` would be **clobbered
   by ASP.NET Core's terminal 404 middleware**, which assigns 404 unconditionally when no
   endpoint matched. The transformer therefore parks a
   `SlugRouteTransformer.PendingRedirect` on `HttpContext.Items` and returns `null`, and
   **`SlugRedirectMiddleware` — registered immediately after `UseRouting()`, where middleware
   runs whether or not an endpoint matched** — issues `Response.Redirect(location, permanent)`
   and short-circuits. Same status codes, same `Location`, same "nothing else runs".
2. **Localizability.** `GenericPathRoute` derived from `LocalizedRoute`, so slug URLs were
   localizable. `MapDynamicControllerRoute<T>` **returns `void`** (verified — this was the one
   assumption that turned out wrong, see §17.3), so the dynamic route cannot be given endpoint
   metadata. The transformer instead sets
   `HttpContext.Items["nop.LocalizableRequest"]`, and `LocalizedRoute.IsLocalizableRequest`
   checks both sources. The transformer runs during routing, so the flag is set before any
   action filter.

**Nothing about slug routing is lost.** `SlugRouteTransformer.Transform` is `protected virtual`,
preserving the fact that 3.90's `GetRouteData` was overridable.

#### `Mvc/Routes/GuidConstraint.cs` — `IRouteConstraint`, new signature

`IRouteConstraint` exists in `Microsoft.AspNetCore.Routing`; `Match` changed to
`Match(HttpContext, IRouter, string routeKey, RouteValueDictionary, RouteDirection)`. Both
`httpContext` and `route` may be null during link generation; 3.90's body touched neither, so
the logic is byte-for-byte identical (plus a null guard on `values`/`routeKey`). Instances
remain usable in a `constraints` object because `IRouteConstraint : IParameterPolicy` and the
route-pattern factory accepts a policy instance — verified. The four
`new GuidConstraint(false)` call sites in `Nop.Web/Infrastructure/RouteProvider.cs` need no
change beyond that file's own port.

#### `Menu/SiteMapNode.cs`, `Menu/XmlSiteMap.cs`, `Seo/CustomUrlRecordEntityNameRequested.cs`

`System.Web.Routing.RouteValueDictionary` → `Microsoft.AspNetCore.Routing.RouteValueDictionary`
— same name, same members, `using` change only for `SiteMapNode`/`XmlSiteMap` and for the
admin `Menu.cshtml` and any `IAdminMenuPlugin`. `CustomUrlRecordEntityNameRequested` is a real
break, see §17.4.

### 17.3 The one wrong assumption, and the eleven verified right ones

Before writing code, a throwaway probe project (repo root, `Microsoft.AspNetCore.App`
`FrameworkReference` + Autofac 9.3.2 / Autofac.Extensions.DependencyInjection 11.0.2, built in
`mcr.microsoft.com/dotnet/sdk:10.0`, since deleted — `git status` verified clean) compiled every
API shape the redesign depends on.

| Checked | Result |
|---|---|
| `MapDynamicControllerRoute<TTransformer>(pattern)` returns an `IEndpointConventionBuilder` so the slug route can carry metadata | **FALSE — it returns `void`.** This is why localizability is carried on `HttpContext.Items` rather than endpoint metadata. Had it been assumed, `LanguageSeoCodeAttribute` would have silently stopped working on exactly the URLs nopCommerce's SEO exists for. |
| `IRouteConstraint.Match(HttpContext, IRouter, string, RouteValueDictionary, RouteDirection)` | TRUE |
| `DynamicRouteValueTransformer.TransformAsync(HttpContext, RouteValueDictionary) → ValueTask<RouteValueDictionary>` | TRUE |
| `MapControllerRoute(name, pattern, defaults, constraints, dataTokens)` → `IEndpointConventionBuilder`; `WithMetadata`, `WithOrder`, `WithName` | TRUE |
| `MapAreaControllerRoute(name, areaName, pattern)` (task 8.2's area registration) | TRUE |
| `Request.Path.StartsWithSegments(PathString, out PathString)` and assigning `Request.PathBase`/`Request.Path` | TRUE |
| `Response.Redirect(url, permanent)` from a middleware placed after `UseRouting()` | TRUE |
| Autofac 9.3.2 `IRegistrationSource.RegistrationsFor(Service, Func<Service, IEnumerable<ServiceRegistration>>)` | TRUE — the signature really did change (see §17.5) |
| Autofac 9.3.2 `RegistrationBuilder.ForDelegate(...).InstancePerLifetimeScope().CreateRegistration()` | TRUE, unchanged |
| `ContainerBuilder.RegisterBuildCallback(Action<ILifetimeScope>)`, `Populate`, `AutofacServiceProviderFactory` | TRUE |
| `new DirectoryInfo(path).GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier))` | TRUE — the extension lives in `System.IO.FileSystemAclExtensions`, so `FilePermissionHelper.cs`'s existing `using System.IO;` is sufficient |

### 17.4 Breaking signature changes — task 6.4

#### 17.4a THE PLUGIN CONTRACT — `IRouteProvider` (tasks 7.3, 8.2, 10.x–15.x)

```csharp
namespace Nop.Web.Framework.Mvc.Routes
{
    public interface IRouteProvider
    {
        void RegisterRoutes(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder routeBuilder);
        int Priority { get; }
    }
}
```

`Priority` is **unchanged** (`int`, providers still invoked in descending order, so
`GenericUrlRouteProvider` at `-1000000` still runs last). **14 implementations must change**:
`Nop.Web/Infrastructure/RouteProvider.cs`, `GenericUrlRouteProvider.cs`,
`BackwardCompatibility1XRouteProvider.cs`, `BackwardCompatibility2XRouteProvider.cs`, plus
`RouteProvider.cs` in `DiscountRules.CustomerRoles`, `DiscountRules.HasOneProduct`,
`ExternalAuth.Facebook`, `Payments.PayPalDirect`, `Payments.PayPalStandard`,
`Pickup.PickupInStore`, `Shipping.FixedOrByWeight`, `Tax.FixedOrByCountryStateZip` — and
`Nop.Admin` gains one at task 8.2.

The mechanical edit per file:

```csharp
// before
using System.Web.Mvc;
using System.Web.Routing;
public void RegisterRoutes(RouteCollection routes)
{
    routes.MapRoute("Plugin.X.Y", "Plugins/X/Y",
        new { controller = "X", action = "Y" },
        new[] { "Nop.Plugin.X.Controllers" });
}

// after
using Microsoft.AspNetCore.Builder;   // MapControllerRoute lives here
using Microsoft.AspNetCore.Routing;
public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
{
    routeBuilder.MapControllerRoute("Plugin.X.Y", "Plugins/X/Y",
        new { controller = "X", action = "Y" });
}
```

Four gotchas, in order of how much damage they do if missed:

1. **`string[] namespaces` is gone.** It became `DataTokens["Namespaces"]` in MVC 5 and has no
   ASP.NET Core counterpart — controller discovery is application-part based. Just delete the
   argument. `MapLocalizedRoute`/`MapGenericPathRoute` keep their `namespaces` overloads for
   source compatibility and **ignore** the value, so those call sites need no edit at all.
2. **`UrlParameter.Optional` is gone.** `"p/{productId}/{SeName}"` +
   `new { …, SeName = UrlParameter.Optional }` becomes `"p/{productId}/{SeName?}"`. This is
   `BackwardCompatibility2XRouteProvider`'s five routes and `Global.asax`'s `Default` route.
3. **Equal-precedence patterns now throw instead of resolving by registration order.**
   MVC 5's `RouteCollection` stopped at the first match. Endpoint routing raises
   `AmbiguousMatchException`. **This is a live defect in `GenericUrlRouteProvider`**: it maps
   `"{generic_se_name}"` and then seven `"{SeName}"` routes (`Product`, `Category`,
   `Manufacturer`, `Vendor`, `NewsItem`, `BlogPost`, `Topic`) that exist purely so views can
   generate URLs by route *name* and were never matched in 3.90. Task 7.3 must give each of the
   seven a losing order:
   ```csharp
   routeBuilder.MapLocalizedRoute("Product", "{SeName}",
           new { controller = "Product", action = "ProductDetails" })
       .WithOrder(1000);
   ```
   (`Microsoft.AspNetCore.Builder.RoutingEndpointConventionBuilderExtensions.WithOrder` — this
   is why `MapLocalizedRoute` returns the convention builder.) The generic-path route keeps
   order 0 and wins, reproducing 3.90's effective behaviour.
4. **An empty route name.** `BackwardCompatibility2XRouteProvider` registers five routes named
   `""`. `MapLocalizedRoute` normalises an empty/null name to `null` (unnamed) because endpoint
   route names must be unique; a direct `MapControllerRoute("")` call would not.

#### 17.4b Other signature changes

| Member | Before | After | Consumers to fix |
|---|---|---|---|
| `IRoutePublisher.RegisterRoutes` | `RouteCollection` | `IEndpointRouteBuilder` | `Nop.Web/Global.asax.cs` line 34 — deleted by 7.2 anyway. `RoutePublisher` (impl) is otherwise unchanged apart from a new null guard, see §17.7. |
| `GuidConstraint.Match` | `(HttpContextBase, Route, string parameterName, RouteValueDictionary, RouteDirection)` | `(HttpContext, IRouter, string routeKey, RouteValueDictionary, RouteDirection)` | none — no in-tree subclass; the four construction sites are unaffected |
| `LocalizedRouteExtensions.MapLocalizedRoute` ×6 | `this RouteCollection` → `System.Web.Routing.Route` | `this IEndpointRouteBuilder` → `IEndpointConventionBuilder` | the 14 route providers (parameter type + `using` only); no caller uses the return value |
| `LocalizedRouteExtensions.ClearSeoFriendlyUrlsCachedValueForRoutes` | `this RouteCollection` | `this IEndpointRouteBuilder`, now a **no-op** | **`Nop.Web/Administration/Controllers/SettingController.cs` line ~2024** does `System.Web.Routing.RouteTable.Routes.ClearSeoFriendlyUrlsCachedValueForRoutes();`. Task 8.x: replace with `LocalizedRoute.ClearSeoFriendlyUrlsCachedValue();` or delete the line — equivalent. |
| `GenericPathRouteExtensions.MapGenericPathRoute` ×6 | `this RouteCollection` → `Route` | `this IEndpointRouteBuilder` → **`void`** | `Nop.Web/Infrastructure/GenericUrlRouteProvider.cs`, which discards the result. `void` because `MapDynamicControllerRoute<T>` is `void`. `name`/`defaults`/`constraints`/`namespaces` are accepted and **ignored** — a dynamic controller route takes its controller/action from the transformer, which is exactly what `GenericPathRoute.GetRouteData` did by overwriting them unconditionally. |
| `LocalizedRoute` | `class LocalizedRoute : System.Web.Routing.Route` with 4 ctors, `GetRouteData`, `GetVirtualPath`, `virtual ClearSeoFriendlyUrlsCachedValue()`, `protected SeoFriendlyUrlsForLanguagesEnabled` | `partial class LocalizedRoute` — a marker with `const LocalizableRequestItemKey`, `const LanguageSeoCodeItemKey`, `static IsLocalizableRequest(HttpContext)`, `static SeoFriendlyUrlsForLanguagesEnabled`, `static ClearSeoFriendlyUrlsCachedValue()` | **any plugin that subclassed `LocalizedRoute` or constructed one has no migration path** — the base class does not exist. Verified: no such subclass in this solution (`GenericPathRoute` was the only one and it is deleted). Third-party route subclasses must be rewritten as an `IEndpointRouteBuilder` registration plus, if they customised matching, an `IViewLocationExpander`-style policy or middleware. |
| `GenericPathRoute` | `partial class GenericPathRoute : LocalizedRoute` | **DELETED** — file removed | nothing in tree constructs it (only `MapGenericPathRoute` did). Behaviour lives in `SlugRouteTransformer`. |
| `CustomUrlRecordEntityNameRequested(RouteData, UrlRecordForCaching)`; `RouteData RouteData { get; }` | `System.Web.Routing.RouteData` | `CustomUrlRecordEntityNameRequested(RouteValueDictionary, UrlRecordForCaching)`; `RouteValueDictionary RouteValues { get; }` | **no consumer anywhere in this solution** (verified by grep across `Nop.Web`, `Nop.Admin` and all 20 plugins). `Microsoft.AspNetCore.Routing.RouteData` does exist, so keeping the old type would have compiled — and broken the extension point **silently**, because `new RouteData(values)` *copies* the dictionary, so a consumer writing `RouteData.Values["controller"] = …` would have mutated a throwaway. Exposing the live dictionary is what keeps "developers could insert their own types" working. |
| `SiteMapNode.RouteValues` | `System.Web.Routing.RouteValueDictionary` | `Microsoft.AspNetCore.Routing.RouteValueDictionary` | `using` change only — `Nop.Web/Administration/Views/Shared/Menu.cshtml` (task 8.4) and any `IAdminMenuPlugin` |
| `SettingsSource.RegistrationsFor` | `Func<Service, IEnumerable<IComponentRegistration>>` | `Func<Service, IEnumerable<ServiceRegistration>>` | none — internal to `DependencyRegistrar.cs`. Autofac split the resolve pipeline (`ServiceRegistration`) out of the component registration. The parameter was never used, so the body is unchanged. |
| — | — | **additive** `Seo.SlugRouteTransformer` (+ nested `PendingRedirect`), `Seo.SlugRedirectMiddleware`, `Localization.SeoFriendlyUrlsMiddleware`, `Localization.WorkingCultureMiddleware`, `Infrastructure.InstallUrlMiddleware`, `Infrastructure.NopServiceCollectionExtensions`, `Infrastructure.NopApplicationBuilderExtensions` | new public surface, nothing to fix |

**Unchanged — do not touch:** `IEngine`, `NopEngine`, `EngineContext`, `ContainerManager`,
`IDependencyRegistrar` — **no signature change**, as promised in §3. `DependencyRegistrar.Order`
still returns 0. `IRouteProvider.Priority`, `Menu/Extensions.cs`, `Menu/IAdminMenuPlugin.cs`,
`Localization/LocalizedUrlExtenstions.cs` (pure string handling, needed no edit — the new
middleware calls exactly the same four methods with exactly the same arguments 3.90 passed).

### 17.5 DI integration — what was done, and where

**`Autofac.Integration.Mvc` (Autofac.Mvc5) is gone from the source.** Its two jobs:

- `AutofacDependencyResolver` + `RequestLifetimeScopeProvider` → `AutofacServiceProviderFactory`
  from `Autofac.Extensions.DependencyInjection`, wired by the host (task 7.2). ASP.NET Core's
  own per-request scope *is* the Autofac per-request `ILifetimeScope`.
- `ContainerBuilder.RegisterControllers(assemblies)` → an explicit
  `builder.RegisterAssemblyTypes(typeFinder.GetAssemblies().ToArray()).Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract).InstancePerLifetimeScope()`.
  Paired with `AddControllersAsServices()` in `AddNopFramework()`, this makes MVC activate
  controllers through the nopCommerce container and keeps
  `EngineContext.Current.Resolve<SomeController>()` working — which 3.90's `Application_Error`
  relied on to render `CommonController.PageNotFound`.

**The five dead registrations were deleted**, as §9a instructed: `HttpContextBase` (including
the `HttpContext.Current != null ? new HttpContextWrapper(...) : new FakeHttpContext("~/")`
branch), `HttpRequestBase`, `HttpResponseBase`, `HttpServerUtilityBase`,
`HttpSessionStateBase`.

**The `FakeHttpContext` branch is resolved by deletion.** It existed only because
`HttpContext.Current` is a static ambient value that is null outside a request;
`IHttpContextAccessor` models that correctly by returning null, so nothing constructs a
`FakeHttpContext` any more. `WebWorkContext` line ~265's `httpContext == null || httpContext is FakeHttpContext`
background-task test therefore now fires through its **`== null`** half, which is the correct
ASP.NET Core signal. The `is FakeHttpContext` half is dead but harmless and compiles — it was
6.2's deliberate choice to keep `FakeHttpContext` alive for the test seams, and removing the
test would be churn with no behavioural gain.

**`ContainerManager.CurrentScopeProvider` — decision and reasoning (deferral 3/1.3).**
Assigned **inside `DependencyRegistrar.Register`, via `builder.RegisterBuildCallback(...)`**:

```csharp
builder.RegisterBuildCallback(scope =>
{
    var httpContextAccessor = scope.ResolveOptional<IHttpContextAccessor>();
    if (httpContextAccessor == null) return;
    ContainerManager.CurrentScopeProvider = () =>
        httpContextAccessor.HttpContext?.RequestServices?.GetService(typeof(ILifetimeScope)) as ILifetimeScope;
});
```

Why here rather than deferring the whole thing to 7.2:

- It needs `IHttpContextAccessor` **from the container**, and a build callback is the earliest
  point at which that is available: `AutofacServiceProviderFactory.CreateBuilder(services)`
  runs `Populate(services)` *first*, then the `ConfigureContainer` callbacks (which is where
  `NopEngine` runs the `IDependencyRegistrar`s), then `Build()` — which fires the callback. So
  the host's `AddHttpContextAccessor()` is already visible.
- It runs unconditionally as part of `NopEngine` initialization, so no host can forget it.
- Doing it in `Program.cs` would work but would put an Autofac-specific detail in the host and
  leave this project's container unusable standalone.
- `ResolveOptional`, not `Resolve`: a standalone container (tests) has no `IHttpContextAccessor`
  and must not fail to build because of it. In that case `CurrentScopeProvider` stays null and
  `ContainerManager.Scope()` keeps its documented fresh-scope fallback.
- `RequestServices.GetService(typeof(ILifetimeScope))` is correct because under
  `AutofacServiceProviderFactory` `RequestServices` is an `AutofacServiceProvider` over the
  request lifetime scope, and Autofac self-registers `ILifetimeScope` in every scope.

**Also registered on the Autofac side:** `SlugRouteTransformer` as
`InstancePerDependency()` — ASP.NET Core resolves a `DynamicRouteValueTransformer` from
`HttpContext.RequestServices` (Autofac-backed) and requires a transient lifetime.

### 17.6 What is left for task 7.2, and exactly how to call it

Two new classes in `Nop.Web.Framework/Infrastructure/`. They exist so the *knowledge* of what
the host must register stays where the types live, instead of 7.2 rediscovering it.

**`NopServiceCollectionExtensions.AddNopFramework(this IServiceCollection services, bool requireSsl = false, Action<CookieAuthenticationOptions> configureCookie = null, Action<MvcOptions> configureMvc = null)`**
returns the `IMvcBuilder`. It performs: `AddHttpContextAccessor`, `AddMemoryCache`,
`AddDistributedMemoryCache` + `AddSession`, `AddAuthentication(Cookies).AddCookie(...)` with
3.90's `<forms>` values (`NOPCOMMERCE.AUTH`, `/login`, 43200-minute `ExpireTimeSpan`, path `/`,
`SlidingExpiration = true`, `SecurePolicy` from `requireSsl`), `AddAntiforgery`,
`Configure<ForwardedHeadersOptions>`, `AddControllersWithViews(...)` adding
`NopFluentValidationModelValidatorProvider`, `NopMetadataProvider` and `NopModelBinderProvider`,
`AddJsonOptions(PropertyNamingPolicy = null)`, `AddControllersAsServices()`,
`ConfigureApplicationPartManager(AddPluginApplicationParts)`, and
`Configure<RazorViewEngineOptions>` inserting `ThemeableViewLocationExpander` at index 0.

**`NopApplicationBuilderExtensions.UseNopPipeline(this IApplicationBuilder app)`** — the whole
request pipeline in the required order. Plus granular `UseNopInstallUrl`,
`UseNopSeoFriendlyUrls`, `UseNopSlugRedirect`, `UseNopWorkingCulture`, `UseNopEndpoints` for a
host that wants to interleave its own middleware.

Intended `Program.cs` (task 7.2):

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// deferrals 1, 4, 5 - MUST precede AddNopFramework (plugin application parts need
// ReferencedPlugins) and engine initialization
CommonHelper.BaseDirectory = builder.Environment.ContentRootPath;      // deferral 5
NopConfigurationManager.Configuration = builder.Configuration;          // deferral 4
PluginManager.Initialize();                                             // deferral 1

builder.Services.AddNopFramework(requireSsl: false);                    // <-- one line
builder.Host.ConfigureContainer<ContainerBuilder>(c => { /* EngineContext.Initialize */ });

var app = builder.Build();
app.UseForwardedHeaders();                                              // deferral 29
app.UseExceptionHandler("/error");                                      // Application_Error
app.UseStatusCodePagesWithReExecute("/page-not-found");                 //   "
app.UseStaticFiles();
app.UseNopPipeline();                                                   // <-- one line
app.Run();
```

`requireSsl` is a deliberate parameter, not a default: 3.90's shipped `Web.config` has
`requireSSL="false"`, but any deployment that had it `true` **must** pass `true` or the port is
a security downgrade (deferral 13).

### 17.7 Middleware inventory and the order it must run in

| # | Component | Replaces | Owner |
|---|---|---|---|
| 1 | `UseForwardedHeaders()` | nothing in 3.90 — required so `IsCurrentConnectionSecured()` is right behind a TLS proxy, else `ForceSslForAllPages` **loops** (deferral 29) | 7.2 (options supplied by 6.4) |
| 2 | `UseExceptionHandler` / `UseStatusCodePagesWithReExecute` | `Application_Error` (logging + re-execute `CommonController.PageNotFound`) | **7.2 — not ported by 6.4** |
| 3 | `UseStaticFiles()` | `system.webServer` static handling; `Application_BeginRequest`'s "ignore static resources" exit; **and** the `DenyAccessToPluginDLLs` `HttpForbiddenHandler` (`.dll` is not in the default content-type map, so static files refuses it and the request 404s) | 7.2 |
| 4 | **`UseNopInstallUrl()`** — `InstallUrlMiddleware` | the install-mode redirect in `Application_BeginRequest`, with both early exits (static resources, keep-alive URL) preserved | **6.4** |
| 5 | **`UseNopSeoFriendlyUrls()`** — `SeoFriendlyUrlsMiddleware` | `LocalizedRoute.GetRouteData`'s `RewritePath`. **MUST be before `UseRouting`** | **6.4** |
| 6 | `UseRouting()` | `System.Web.Routing.UrlRoutingModule`, and with it the three `system.webServer/handlers` entries `SitemapXml`, `RobotsTxt` and `MiniProfiler` whose only job was forcing those extensionless paths through `UrlRoutingModule` — in ASP.NET Core every path goes through routing, so all three vanish with nothing to replace | 7.2 |
| 7 | **`UseNopSlugRedirect()`** — `SlugRedirectMiddleware` | `GenericPathRoute`'s 301/302 + `Response.End()`. **MUST be immediately after `UseRouting`** | **6.4** |
| 8 | `UseSession()` | `<sessionState>` (deferral 15) | 7.2 |
| 9 | `UseAuthentication()` | `<authentication mode="Forms">` / `FormsAuthenticationModule` (deferrals 13, 25) | 7.2 |
| 10 | **`UseNopWorkingCulture()`** — `WorkingCultureMiddleware` | `Application_AuthenticateRequest` → `SetWorkingCulture()`. **MUST be after `UseAuthentication`** — 3.90's own comment says *"we don't do it in Application_BeginRequest because a user is not authenticated yet"* | **6.4** |
| 11 | `UseAuthorization()` | framework requirement between routing and endpoints | 7.2 |
| 12 | **`UseNopEndpoints()`** | `Global.asax`'s `RegisterRoutes(RouteTable.Routes)` + `AreaRegistration.RegisterAllAreas()`; runs `IRoutePublisher` then appends the `Default` `{controller=Home}/{action=Index}/{id?}` route at `WithOrder(int.MaxValue)` so it stays last | **6.4** |

`UseNopPipeline()` performs steps 4–12 in exactly that order.

**`WorkingCultureMiddleware` behaviour note.** `Thread.CurrentThread.CurrentCulture` /
`CurrentUICulture` became `CultureInfo.CurrentCulture` / `CurrentUICulture`. On .NET these are
the async-local ambient culture and are what flows across `await`; assigning the *thread's*
culture would be lost the moment the request resumed on another thread. This is deliberately
**not** `UseRequestLocalization()`, which would resolve the culture from its own provider chain
instead of `IWorkContext.WorkingLanguage` (URL SEO code → customer setting → store default →
browser). Reproducing the 3.90 rule is the point.

**Handlers and modules with nothing to port, confirmed rather than assumed:** `<modules>` in
`Nop.Web/Web.config` is **empty** (the only entries are commented out), and the four `<handlers>`
entries are accounted for above. `httpProtocol/customHeaders` `<remove name="X-Powered-By"/>`,
the `staticContent` `mimeMap` edits and `<customErrors>` are IIS/host configuration and belong to
task **7.4**. `Nop.Web/Web.config` was **read, not modified** — it is 7.4's file.

### 17.8 Deliberately NOT ported, with the reason

- **MiniProfiler.** `Application_BeginRequest`/`EndRequest` started and stopped it, and
  `Application_Start` added `new ProfilingActionFilter()` to `GlobalFilters`. MiniProfiler 3.x /
  `StackExchange.Profiling.Mvc` is MVC5-only and is not a `PackageReference` of any migrated
  project (task 6.1 confirmed its absence from this project's `packages.config`). Profiling is
  **dropped**, and `StoreInformationSettings.DisplayMiniProfilerInPublicStore` becomes **inert** —
  the same treatment as `SeoSettings.EnableJsBundling` in §8. Note for task 7.3:
  `Nop.Web/Views/Shared/_Root.Head.cshtml` lines 10–11 and 54–56 compute
  `displayMiniProfiler` and call `@StackExchange.Profiling.MiniProfiler.RenderIncludes()`; that
  block must go.
- **`ServicePointManager.SecurityProtocol = Tls12`** — TLS 1.2+ is the .NET default.
- **`MvcHandler.DisableMvcResponseHeader = true`** — the `X-AspNetMvc-Version` header does not
  exist in ASP.NET Core.
- **`routes.IgnoreRoute("favicon.ico")` / `IgnoreRoute("{resource}.axd/{*pathInfo}")`** — there
  are no `.axd` handlers, and `UseStaticFiles` serves `favicon.ico` before routing.
- **`AreaRegistration.RegisterAllAreas()`** — no counterpart; areas are
  `MapAreaControllerRoute` from an `IRouteProvider` (task 8.2).

### 17.9 One small robustness fix made along the way

`RoutePublisher.FindPlugin` iterated `PluginManager.ReferencedPlugins`, which is **null** until
`PluginManager.Initialize()` runs (deferral 1, owned by 7.2). 3.90 could not observe this because
`[PreApplicationStartMethod]` guaranteed initialization before `Application_Start`. Left as-is it
would `NullReferenceException` on the very first route registration and take the whole host down
before anything else could report the real cause. It now returns `null` when the list is absent,
which means every discovered provider is treated as "not from a plugin" and registered — exactly
the outcome 3.90 produced for the `Nop.Web`/`Nop.Admin` providers. It is not a substitute for
fixing deferral 1: with no plugin list, no plugin's routes can be filtered by installed state.


---

# Nop.Web.Framework — configuration migration and obsolete-file removal (task 6.5)

Task 6.5 was largely an **audit**, and the audit is the primary deliverable: task 6.1 had already
removed this project's `app.config` and `packages.config`, and had already reported that the
project has no `Views/` folder and no `ConfigurationManager` call site. All four of those findings
were re-verified independently and **all four hold** — see §18.2. Almost nothing needed removing.

What did need doing was the part of 6.5 that is not about deleting files: providing the mechanism
for **runtime deferral 5** (`CommonHelper.MapPath` resolving under `bin/`), which the register
names 6.5 and 7.2 as joint owners of.

| Measurement | Value |
|---|---|
| errors before / after | **0 / 0** |
| warnings before / after | **59 / 10** |
| warnings originating in `Nop.Web.Framework` after | **0** — the 10 remaining are the pre-existing `SYSLIB0014`/`SYSLIB0021`/`SYSLIB0023`/`SYSLIB0045`/`SYSLIB0051` obsolescence notices in `Nop.Core` and `Nop.Services`, unchanged since 6.3 |
| swallowed-error check | verbose log grep: `"converted to a warning"` → **0**, `"ContinueOnError"` → **0**, `NU1901`–`NU1904` → **0** |
| residual legacy references in `Nop.Web.Framework.dll` | **none** — `System.Web*`, `Autofac.Integration.Mvc`, `System.Web.Optimization`, `ImageResizer`, `System.Drawing*` all 0 occurrences |

## 18. Task 6.5 — what changed

### 18.1 Deferral 5 (`CommonHelper.MapPath`) — mechanism DONE, verified live; deferrals 1 and 4 narrowed

**New file: `Nop.Web.Framework/Infrastructure/NopHostingExtensions.cs`.** Same pattern 6.4
established with `AddNopFramework` / `UseNopPipeline`: host-side knowledge lives next to the code
that depends on it, so 7.2 writes one line instead of rediscovering an ordering constraint.

```csharp
// task 7.2, Program.cs — FIRST nopCommerce statement, before AddNopFramework()
builder.Environment.UseNopHostingEnvironment(builder.Configuration);
```

That single call replaces the three separate lines the §17.6 draft `Program.cs` showed, and does
them in the **one order that is correct**:

1. `CommonHelper.BaseDirectory = environment.ContentRootPath` — **deferral 5**
2. `NopConfigurationManager.Configuration = configuration` — **deferral 4** (when a configuration
   is passed; omit the argument to skip)
3. `PluginManager.Initialize()` — **deferral 1** (`initializePlugins: false` to skip)

**The ordering is the whole reason this is a separate call and not folded into
`AddNopFramework`.** `PluginManager.Initialize()` calls `CommonHelper.MapPath` in its first three
statements (`~/Plugins`, `~/Plugins/bin`, `~/App_Data/InstalledPlugins.txt`) and it must run
*before* `AddNopFramework()`, because `AddPluginApplicationParts` needs
`PluginManager.ReferencedPlugins` to be populated (deferral 2, §17.1). Assigning the content root
inside `AddNopFramework` would therefore have been **too late**: plugin discovery would already
have shadow-copied every plugin assembly into `bin/Debug/net10.0/Plugins/bin` instead of
`<contentroot>/Plugins/bin`, and `ReferencedPlugins` would have been empty. Step 2 must also
precede step 3, because `PluginManager.Initialize()` reads the
`ClearPluginsShadowDirectoryOnStartup` app setting through `NopConfigurationManager`.

Also public:

- **`NopHostingExtensions.SetContentRoot(string)`** — step 1 alone, for a host that has a
  content-root path but no `IHostEnvironment` (test fixtures). Throws `NopException` with an
  explicit remedy when handed null/blank, rather than silently leaving `MapPath` on `bin/`.
- **`NopHostingExtensions.ContentRootConfigured`** — a `bool` the **task 7.7 smoke check should
  assert**. `CommonHelper.BaseDirectory` cannot answer this itself: its getter falls back to
  `AppDomain.CurrentDomain.BaseDirectory`, so from outside, "never assigned" and "assigned to the
  output folder" are indistinguishable. Deliberately **not** enforced by `AddNopFramework` — a host
  that assigns `CommonHelper.BaseDirectory` directly (legitimate, and what §17.6 originally
  documented) would then fail spuriously.

The parameter is `IHostEnvironment`, not `IWebHostEnvironment`: only `ContentRootPath` is needed,
and the narrower dependency keeps the method usable from a non-web host.

**Status of each deferral after 6.5:**

| # | Before 6.5 | After 6.5 |
|---|---|---|
| **5** | open, owners 6.5 + 7.2 | **6.5's half is DONE.** Nothing further in this project. 7.2 closes it with the one line above. |
| **4** | open, owners 7.2 + 7.4 | **RESOLVED.** The *assignment* is part of the same one line; **task 7.4 authored the `NopConfig` and `appSettings` sections in `appsettings.json`** — see §32. All 14 properties verified to bind. |
| **1** | open, owner 7.2 | **narrowed.** `PluginManager.Initialize()` now runs as part of that one line, in the right place relative to both the content root and `AddNopFramework`. 7.2 no longer has to know the ordering — only to make the call. |

**Verified live, not just compiled.** A throwaway `Microsoft.NET.Sdk.Web` probe outside the
repository (`ProjectReference` to `Nop.Web.Framework.csproj`, run in the
`mcr.microsoft.com/dotnet/sdk:10.0` container, since deleted — `git status` confirmed no residue)
executed the intended `Program.cs` prologue:

| Check | Result |
|---|---|
| `ContentRootConfigured` after `UseNopHostingEnvironment` | ✅ `True` |
| `CommonHelper.BaseDirectory` | ✅ the content root, **not** `bin/Debug/net10.0` |
| `CommonHelper.MapPath("~/App_Data/x")` | ✅ `<contentroot>/App_Data/x` — deferral 5 demonstrably fixed |
| `SetContentRoot("   ")` | ✅ throws `NopException` with the remedy in the message |
| all four `AddNopFramework` overloads resolve unambiguously from a real call site | ✅ |
| `NopAuthenticationConfig` defaults | ✅ `NOPCOMMERCE.AUTH` / `/login` / `43200` / sliding `true` / path `/` / `requireSsl false` — byte-for-byte 3.90's `<forms>` |

### 18.2 The audit — four items, four findings, no invented work

1. **Legacy config files: NONE remain.** Recursive `find` under the project for `*.config`,
   `*.asax`, `*.ashx`, `*.axd` and `Global.asax*` (excluding `bin`/`obj`) returns **0 hits**.
   `app.config` and `packages.config` were the only two and 6.1 deleted both. There is no
   `Views/`, `Areas/` or `App_Start/` directory anywhere in the project, so there is **no View
   `web.config`**, no `_ViewStart` to re-base, and no `Web.Debug.config`/`Web.Release.config`
   transform. 6.1's report was accurate.
2. **Config-reading API surface: NONE.** No file names `ConfigurationManager`,
   `WebConfigurationManager` or `System.Configuration`. The only occurrences of those words in the
   project are inside comments — this project's `.csproj` and a doc comment in
   `Infrastructure/NopApplicationBuilderExtensions.cs`. There was therefore **no `appSettings`
   surface to route through `NopConfigurationManager`**, and none of that machinery was
   duplicated.
3. **`Properties/AssemblyInfo.cs` is KEPT**, consistent with the solution-wide decision in task
   2.3 (`Directory.Build.props` sets `GenerateAssemblyInfo=false` so hand-kept files stay
   authoritative) and with `Nop.Core`, `Nop.Data` and `Nop.Services`. Every attribute in it is
   valid on net10.0 and none is duplicated by the SDK while `GenerateAssemblyInfo` is false:
   `AssemblyTitle`, `AssemblyDescription`, `AssemblyConfiguration`, `AssemblyCompany`,
   `AssemblyProduct`, `AssemblyCopyright`, `AssemblyTrademark`, `AssemblyCulture`, `ComVisible`,
   `Guid`, `AssemblyVersion`, `AssemblyFileVersion`. **It carries no System.Web hosting hook** —
   no `[assembly: PreApplicationStartMethod]`, no `[assembly: WebActivator...]` — so unlike
   `Nop.Core`'s `PluginManager` (task 2.4 / deferral 1.1) nothing had to be removed. The only
   `PreApplicationStartMethod` string left in the project is inside an explanatory comment in
   `Mvc/Routes/RoutePublisher.cs`.
4. **No orphans from 6.3/6.4.** `Seo/GenericPathRoute.cs`, the two themeable view engines and
   `UI/AsIsBundleOrderer.cs` were deleted with every reference to them removed; the project
   compiles at 0 errors with no unreferenced residue.

### 18.3 The one genuine settings surface — `<forms>` → typed options

**New file: `Nop.Web.Framework/Infrastructure/NopAuthenticationConfig.cs`.**

Task 6.4 had to transcribe `Nop.Web/Web.config`'s `<authentication mode="Forms"><forms .../>`
element into constants on `NopServiceCollectionExtensions` plus a `requireSsl` method parameter,
because no configuration model existed yet. Requirement 1.4 says configuration must move to the
.NET 10 configuration model, so 6.5 made them bindable:

```jsonc
// appsettings.json — authored by task 7.4
"Authentication": {
  "CookieName":        "NOPCOMMERCE.AUTH",
  "LoginPath":         "/login",
  "TimeoutMinutes":    43200,
  "RequireSsl":        false,
  "SlidingExpiration": true,
  "CookiePath":        "/"
}
```

```csharp
builder.Services.AddNopFramework(builder.Configuration);   // <-- new overload
```

`AddNopFramework` now has **three** overloads, all funnelling through the third:

| Overload | Purpose |
|---|---|
| `AddNopFramework(IConfiguration, …)` | **what 7.2 should call.** Binds `NopAuthenticationConfig` from the `Authentication` section and also registers `services.Configure<NopAuthenticationConfig>(section)` so it is injectable as `IOptions<NopAuthenticationConfig>`. A missing section is not an error — every value falls back to 3.90's shipped default. |
| `AddNopFramework(bool requireSsl = false, …)` | **unchanged signature**, 6.4's original. Still compiles and behaves identically. |
| `AddNopFramework(NopAuthenticationConfig, …)` | explicit options, for tests and for a host that composes the values itself. |

This removes the hardcoded `requireSsl:` literal from `Program.cs` that **deferral 7.13** flags as
a security hazard — `RequireSsl` still defaults to `false` to match 3.90's shipped `Web.config`,
but it is now a configuration value a deployment can set without recompiling.

**Why a new POCO rather than extending `NopConfig`:** `NopConfig` is the migrated `<NopConfig>`
custom section and is a deliberate byte-for-byte translation of it; adding members would change a
committed, gated project and blur which legacy element each setting came from. `<forms>` is a
different legacy section (`system.web`) with a different owner. No configuration *machinery* is
duplicated — binding uses the same `IConfiguration`/`Bind` mechanism, and the pre-container static
seam remains `Nop.Core.Configuration.NopConfigurationManager`.

### 18.4 `CA1416` — 49 warnings cleared declaratively, and the intended knock-on for 7.3 / 8.3

`Security/FilePermissionHelper.CheckPermissions` now carries
**`[SupportedOSPlatform("windows")]`**, which is the documented way to declare a per-member
platform constraint. This clears **all 49 `CA1416`** warnings — the entire delta between 59 and
10 — and it was done **without** a `NoWarn` suppression and **without changing a single statement
of the method body**. The attribute is purely declarative; it states the constraint the code has
always had, which the migration already records as an accepted Windows-first trade-off
(design §7).

It is on the **method, not the class**: `GetDirectoriesWrite` and `GetFilesWrite` are plain path
arithmetic and are platform-neutral, so annotating the class would export a constraint they do not
have and would double the downstream noise.

**Intended knock-on — tasks 7.3 and 8.3 will each see new `CA1416` warnings.** The four call sites
are `Nop.Web/Controllers/InstallController.cs` lines ~288 and ~293 and
`Nop.Web/Administration/Controllers/CommonController.cs` lines ~420 and ~439. They are reachable
on all platforms, so each now warns. **That is the attribute working as designed**: it surfaces at
compile time what is otherwise a runtime `PlatformNotSupportedException` from
`WindowsIdentity.GetCurrent()` on Linux (note that call is *outside* the method's `try`, so today
it propagates rather than being swallowed by the `catch { return true; }`). Recommended fix at
those four sites — and it is a genuine improvement, not just warning suppression:

```csharp
if (OperatingSystem.IsWindows() && !FilePermissionHelper.CheckPermissions(dir, false, true, true, false))
    ...
```

The analyser recognises `OperatingSystem.IsWindows()` as a platform guard, so the warning clears,
**and** the install page and admin System Info page start working off Windows instead of throwing.
Alternatively annotate the containing action. Either is a two-line change per file.

### 18.5 `FilePermissionHelper`'s file/directory lists are stale — ✅ **RESOLVED by task 7.5** (partially; see below)

`GetFilesWrite()` still asks for write permission on `~/Global.asax` and `~/web.config`, and
`GetDirectoriesWrite()` builds its paths with hard-coded `\\` separators. Both are pre-existing and
**neither was touched by 6.5**, because changing them alters what the installer and the admin System
Info page report — a behaviour change outside 6.5's scope.

It is **harmless today**: `CheckPermissions` wraps the ACL read in `try { … } catch { return true; }`,
so a path that does not exist is reported as "permission OK". But it is dead weight once task 7.5
deletes `Global.asax` and task 7.4 reduces `web.config` to an optional ANCM shim.

**Recommendation for task 7.5** (which owns removing obsolete `Nop.Web` files): drop `Global.asax`
from `GetFilesWrite()`, and decide whether `web.config` stays (keep it only if the ANCM shim is
kept). The `\\` separators should become `Path.Combine` segments if any non-Windows deployment is
ever in scope — but that is coupled to the `CA1416` guard decision in §18.4 and belongs with it.

#### Resolution at task 7.5

`src/Presentation/Nop.Web.Framework/Security/FilePermissionHelper.cs`, `GetFilesWrite()`:

- **`Global.asax` entry REMOVED.** Task 7.2 deleted the file, so the check was asking about a path
  that cannot exist. It never *failed* — the swallowing `catch` reported the missing path as
  "permission OK" — but it made the installer and the admin System Info page assert something
  meaningless.
- **`web.config` entry KEPT**, per the recommendation's own condition: 7.4 retained `web.config` as
  the IIS/ANCM hosting shim, and the ASP.NET Core Module rewrites it on publish, so write access is
  still a legitimate thing to verify on an IIS deployment.
- **Incidental finding, now corrected by coincidence:** this list has always spelled the file
  lowercase `web.config` while 3.90's file on disk was `Web.config`. On a case-sensitive filesystem
  the check therefore silently missed the real file — until task 7.4 renamed it to all-lowercase
  (§31.2). The two are consistent for the first time.
- **The `\\` separators are deliberately left alone**, exactly as 6.5 argued: they are coupled to the
  Windows-first posture on `CheckPermissions` and changing them alters what those two pages report.
  A `<remarks>` block now records this on the method so the next reader does not re-litigate it.

This edits a project that had already passed its gate, so **`Nop.Web.Framework` was re-gated:
0 errors / 10 warnings**, matching its recorded 6.6 baseline exactly (no new `CA1416` — the change
is inside a method with no platform-annotated call).


---

# Nop.Web — SDK-style project conversion and package migration (task 7.1)

Task 7.1 is project-file plumbing only: no `.cs`, no `.cshtml`, no `Global.asax` and no
`Web.config` was edited. Unlike every earlier stage, **its success criterion was never a clean
compile** — it is a clean *restore* plus a project that MSBuild evaluates, with the surviving
errors being the CS\*/RZ\* inventory tasks 7.2–7.5 consume.

| Measurement | Value |
|---|---|
| restore | **clean** — all 5 projects, **0** `NU*` diagnostics of any severity |
| project load / MSBuild evaluation | **clean** — **0** `MSB*`, **0** `NETSDK*` |
| upstream projects | **all 4 still build at 0 errors** — `Nop.Core`, `Nop.Data`, `Nop.Services`, `Nop.Web.Framework`. The 10 warnings are the pre-existing `SYSLIB0014`/`SYSLIB0021`/`SYSLIB0023`/`SYSLIB0045`/`SYSLIB0051` obsolescence notices, unchanged since 6.5 |
| `Nop.Web` errors | **1117** (2234 diagnostic lines — MSBuild prints each twice), **42 unique**, **100% `CS*`/`RZ*`** across **87 source files**. Zero errors of any other category |
| new central package pins added | **0** — all four `PackageReference`s already had a `PackageVersion` entry |
| packages removed | **40 of 44** |

## 19. Task 7.1 — what changed

### 19.1 The error inventory handed to 7.2 / 7.3

All 1117 errors trace to **four** missing legacy namespaces plus one Razor-syntax family.
Nothing is a packaging problem; every one is source that a later task owns.

| Root cause | Unique errors | Owner |
|---|---|---|
| `System.Web.Mvc` (144 `CS0234` lines) — `ActionResult`, `AllowHtml`, `HttpPost`, `ChildActionOnly`, `ValidateInput`, `ActionName`, `NonAction`, `FormCollection`, `SelectListItem`, `HttpContextBase`, `HttpPostedFileBase`, `HtmlHelper<>`, `MvcHtmlString`, `JsonResult`, `UrlHelper`, `RedirectResult`, `HttpVerbs` | the bulk | **7.3** |
| `System.Web.Routing` (28 lines) — `RouteCollection`, `RouteValueDictionary` | — | **7.2** (`Global.asax.cs`) / **7.3** (`Infrastructure/*RouteProvider.cs`, now `IEndpointRouteBuilder` per §17.4a) |
| `StackExchange.Profiling` (4 lines) | — | **7.2** (`Global.asax.cs`) / **7.3** (`Views/Shared/_Root.Head.cshtml`) — MiniProfiler is dropped, §17.8 |
| `FluentValidation.Mvc` (2 lines) | — | **7.2** — `Global.asax.cs` line 74 only; the file is deleted |
| `WebGrease.Css.Extensions` (2 lines) | — | **7.3** — **not** an orphan `using`; see 19.4 |
| Razor: **20** `RZ` errors — 8 `RZ1002` "the helper directive is not supported", 16-line `RZ1011`, 16-line `RZ2005` "the 'attribute' directive must appear at the start of the line" | 20 | **7.3** |

Distribution by directory (diagnostic lines): `Controllers/` 1560, `Models/` 520,
`Factories/` 46, `Views/` 40, `Infrastructure/` 30, `Extensions/` 24.

**The Razor SDK ran and reported independently of the failed C# pass** — a useful difference
from tasks 6.2/6.3, where Roslyn's refusal to bind method bodies during declaration errors
forced a throwaway probe. Modern Razor parsing is not gated on the C# compilation, so 7.3
already has a real (if partial) `.cshtml` inventory: the 8 `@helper` directives are the notable
one, since ASP.NET Core removed `@helper` outright and each must become a local function,
partial view or tag helper.

### 19.2 `Administration\**` had to be excluded from the default globs — structural, not hygiene

`Nop.Admin` is **nested inside** `Nop.Web` (`src/Presentation/Nop.Web/Administration/Nop.Admin.csproj`).
Under the classic project this was invisible because every compiled file was listed explicitly.
Under SDK implicit globbing it is a correctness problem: the default `Compile`/`Content`/`None`
globs would swallow Nop.Admin's whole tree into Nop.Web.

Measured: **508** `.cs` files under this directory of which only **231** are Nop.Web's, and
**518** `.cshtml` of which only **193** are Nop.Web's. Left unexcluded the result would be
duplicate type definitions, two `IDependencyRegistrar` implementations, admin views compiled
into the storefront assembly, and task 8 pre-empted by an unreviewed half-migration.

`<DefaultItemExcludes>$(DefaultItemExcludes);Administration\**;Plugins\**</DefaultItemExcludes>`
was chosen over per-glob `<Compile Remove=…/>` items so it applies to **every** default glob the
SDK and Razor SDK derive, present and future.

Verified by evaluating the resolved item lists rather than trusting the exclusion:

| Item | Count | Leakage |
|---|---|---|
| `Compile` | **231** — `Models` 127, `Factories` 42, `Controllers` 28, `Validators` 20, `Infrastructure` 9, `Extensions` 3, `Global.asax.cs`, `Properties/AssemblyInfo.cs` | **0** |
| `Content` | **199** — 193 `.cshtml` (192 `Views/`, 1 `Themes/`) + 6 `.config` | **0** |

231 is exactly the length of the legacy explicit `<Compile>` list, so the glob reproduces the
old compilation set precisely — no file silently gained or lost. Zero `Administration/` or
`Plugins/` paths appear anywhere in the 2234 diagnostic lines.

`Plugins\**` is excluded for the same reason although the directory is absent from source
control: `PluginManager` shadow-copies plugin assemblies into `~/Plugins/bin` at startup, and
deployed plugin folders carry their own `.cshtml` and `.config`.

### 19.3 Open deferrals — Nop.Web task 7.1

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 36 | ~~Redis session state provider dropped with no successor~~ | — | ✅ **CLOSED BY DECISION at 7.4** (§32.1) — the `<sessionState>` element was harvested (`localhost,ssl=true`) before `web.config` was reduced, and NOT wired: it was commented out in 3.90, so in-process session is 3.90 parity. Recipe + multi-instance consequences recorded |
| 37 | ~~MiniProfiler packages removed — two source sites still reference them~~ | — | ✅ **FULLY RESOLVED**: `Global.asax.cs` half by 7.2 (§20), `Views/Shared/_Root.Head.cshtml` half by **7.3** (§30) |
| 38 | ~~No `launchSettings.json` — the IIS Express / dev-server settings were discarded~~ | — | ✅ **RESOLVED by 7.2** (§24) — `Properties/launchSettings.json`. Its `http://` dev URL was reconciled against `Authentication:RequireSsl` at 7.4 (§33.5): consistent, no change needed |
| 39 | ~~`ExcludeFilesFromDeployment` publish shaping lost with the Web Application Project targets~~ | — | ✅ **RESOLVED by 7.4** (§32, §31.4) — publish shaping in `Nop.Web.csproj` **plus** the static-file allow-list for the runtime half the deferral did not mention. Verified against planted secrets. **Nop.Admin's `db_backups` half is structurally out of reach from here — new deferral 7.4-2, task 8.1** |
| 40 | ~~Static asset trees still at their 3.90 locations, outside `wwwroot`~~ | — | ✅ **RESOLVED by 7.4** (§32, §33.1) — allow-listed `IFileProvider` over the content root rather than relocation; three code paths require the files to stay put. **Admin assets are new deferral 7.4-2, task 8.5** |
| 41 | ~~`WebGrease.Css.Extensions.ForEach` is load-bearing at two call sites~~ | — | ✅ **RESOLVED by 7.3** (§30) — replaced with `foreach`; confirmed it was the `IEnumerable<T>` extension, not `List<T>.ForEach` |

#### 7.1-1 (deferral 36) Redis session state provider dropped, no successor

`Microsoft.Web.RedisSessionStateProvider` 2.2.3 is a `System.Web`
`<sessionState mode="Custom" customProvider=…>` implementation. **There is no ASP.NET Core
equivalent and no successor package** — Core's session is `IDistributedCache`-backed via
`services.AddSession()` plus a distributed store. `StackExchange.Redis.StrongName` 1.2.1 went
with it (it was only the transport); the mainline `StackExchange.Redis` still reaches the graph
transitively through `Nop.Core`'s `RedisCacheManager`.

- **Current state:** nothing is broken *today* — task 6.4's `AddNopFramework` already calls
  `AddDistributedMemoryCache()` + `AddSession()`, so session works **in-process**.
- **What 7.4 must decide:** whether the deployment needs a distributed session store. §17.7
  step 8 and deferral **15** (external-authentication round-trip) already note that a
  multi-instance deployment requires one. The replacement is
  `services.AddStackExchangeRedisCache(...)` reading the connection string 3.90 kept in
  `Web.config`'s `<sessionState>` element — **that element is the only place that connection
  string exists, so 7.4 must harvest it before reducing `Web.config`.**
- **Impact if unfixed:** in-memory session on a multi-instance deployment means sessions bound
  to one instance — external logins fail intermittently rather than cleanly.

#### 7.1-2 (deferral 37) MiniProfiler removed — two source sites still reference it

Already recorded as §17.8; restated here because 7.1 is where the packages actually left the
graph, making it a compile error rather than a latent decision. `MiniProfiler` 3.2.0.157 and
`MiniProfiler.MVC4` 3.0.11 are MVC5-only with no net10.0 release. The 4 `CS0234` lines are
`Global.asax.cs` (`MiniProfiler.Start`/`Stop`, `ProfilingActionFilter`, lines 21–22, 83,
128–143) and `Views/Shared/_Root.Head.cshtml` (lines 10–11 and 54–56,
`MiniProfiler.RenderIncludes()`). 7.2 deletes the first file; **7.3 must delete the
`_Root.Head.cshtml` block** or the view will not compile.
`StoreInformationSettings.DisplayMiniProfilerInPublicStore` and
`DisplayMiniProfilerForAdminOnly` become inert, same treatment as
`SeoSettings.EnableJsBundling` (§8) and `SeoSettings.EnableCssBundling`.

#### 7.1-3 (deferral 38) No `launchSettings.json`

The classic project carried its dev-server configuration in properties and in
`<ProjectExtensions><VisualStudio><WebProjectProperties>`: `UseIISExpress=true`,
`UseIIS=False`, `AutoAssignPort=True`, `DevelopmentServerPort=15536`,
`IISUrl=http://localhost:2451/`, `DevelopmentServerVPath=/`, `NTLMAuthentication=False`,
`IISExpressAnonymousAuthentication=enabled`, `IISExpressWindowsAuthentication=enabled`,
`IISExpressUseClassicPipelineMode=false`. **All discarded** — none has an SDK property
counterpart; the ASP.NET Core equivalent is `Properties/launchSettings.json`.

Deliberately **not** created by 7.1: it is hosting configuration, which is 7.2/7.4's remit, and
authoring it here would have meant guessing the profile shape before `Program.cs` exists.

- **Impact if unfixed:** `dotnet run` binds Kestrel's default ports instead of 2451/15536, and
  there is no F5 profile. Cosmetic, but note the interaction with **deferral 13**: if the
  chosen dev URL is `http://` while `NopAuthenticationConfig.RequireSsl` is `true`, the auth
  cookie will not be set and login will appear broken for a configuration reason.

#### 7.1-4 (deferral 39) `ExcludeFilesFromDeployment` publish shaping is gone — SECURITY-RELEVANT

Two deleted targets, `ExcludeRootBinariesDeployment` and `ExcludeRootBinariesPackage`, built a
~25-entry exclusion list and fed it to `ExcludeFromPackageFiles`. Both hook
`ExcludeFilesFromPackage`, an **MSDeploy / Web Application Project** target that **does not
exist in the Web SDK**, and `dotnet publish` has an entirely different model — so they are not
portable and were not re-expressed.

What 3.90 deliberately withheld from a publish, and now would not be:

```
**\*.cs   **\*.csproj   **\*.csproj.user   **\obj\**   **\bin\*.xml   **\packages.config
Administration\bin\**            Administration\db_backups\*.bak
App_Data\InstalledPlugins.txt    App_Data\Settings.txt    App_Data\Nop.Db.sdf
App_Data\browscap.crawlersonly.xml
bin\Nop.Admin.dll.config         bin\Nop.Web.dll.config
Content\Files\ExportImport\*.txt Content\Files\ExportImport\*.xml
Content\Images\Thumbs\*.{jpg,jpeg,png,gif}
Properties\**\*                  **\*.Debug.config   **\*.Release.config
```

Some of this the SDK handles for free (`.cs`, `.csproj`, `obj/`, and `packages.config` no longer
exists). **The rest does not, and two entries matter:**

- **`App_Data\Settings.txt` is `DataSettings` — it contains the database connection string.**
- **`Administration\db_backups\*.bak` are database backups.**

Publishing either into a web-served directory is a data-exposure bug, not untidiness.

**Fix (task 7.4):** re-express the surviving entries as `<Content Remove=…/>` /
`<None Remove=…/>` items or as `ExcludeFromSingleFile`/publish-time excludes, **and** confirm
`App_Data` is not reachable by the static-file middleware (in 3.90 `System.Web` blocked
`App_Data` implicitly; ASP.NET Core does not, and `App_Data` sits under the content root, so it
is only safe as long as `wwwroot` is the static root — which interacts directly with deferral
40 below).

#### 7.1-5 (deferral 40) Static assets still outside `wwwroot`

`Content/`, `Scripts/`, `Themes/DefaultClean/Content/`, `favicon.ico`, `ErrorPage.htm` and
`FileNotFound.htm` remain at their 3.90 locations. **Moving them was explicitly out of 7.1's
scope**; they do not break the build (none of those extensions is in a default `Compile` or
`Content` glob, so the SDK classifies them as `None` with no copy action).

In 3.90 `System.Web`'s handler pipeline served them from the application root. ASP.NET Core's
static-file middleware serves `IWebHostEnvironment.WebRootPath`, i.e. `wwwroot/`.

**Fix (7.3/7.4):** either relocate the trees under `wwwroot/`, or point `WebRootFileProvider` at
them with a composite `IFileProvider`. **Whichever is chosen must also cover the file-version
provider**, or the cache busting task 6.3 added to `PageHeadBuilder` silently no-ops — that is
already **deferral 33 (§14.33)**, and Nop.Web is where it gets decided. Note the tension with
7.1-4: widening the static root to the content root would expose `App_Data`.

`App_Data/` needs **no** copy action and none was configured: it is read through
`CommonHelper.MapPath("~/App_Data/...")`, which resolves against `CommonHelper.BaseDirectory`,
set to the content root by `builder.Environment.UseNopHostingEnvironment(builder.Configuration)`
at 7.2 (deferral **5**, mechanism from 6.5). This is why the ~700 legacy `<Content>` entries
that existed only to copy `App_Data`, `Content` and `Scripts` into the output were not
recreated. Also note `OutputPath` moves from the flat 3.90 `bin\` to `bin\Debug\net10.0\`.

#### 7.1-6 (deferral 41) `WebGrease.Css.Extensions.ForEach` is load-bearing

`WebGrease` 1.6.0 had to go — it is bundling's minifier and design §8 drops bundling with no
successor. It turns out **one source file depends on it for something unrelated to bundling**:

`Factories/CustomerModelFactory.cs` line 29 has `using WebGrease.Css.Extensions;`, and lines
**393** and **496** both do:

```csharp
var customAttributes = PrepareCustomCustomerAttributes(...);
customAttributes.ForEach(model.CustomerAttributes.Add);
```

**This was checked rather than assumed to be a stale `using`.**
`PrepareCustomCustomerAttributes` returns `IList<CustomerAttributeModel>`, **not** `List<T>` —
so this is *not* `List<T>.ForEach`; it resolves to WebGrease's `IEnumerable<T>` extension. The
`using` is the only WebGrease reference left anywhere in the solution.

**Fix (7.3):** replace both lines with a plain loop and delete the `using`:

```csharp
foreach (var attribute in customAttributes)
    model.CustomerAttributes.Add(attribute);
```

No package, no `System.Linq` equivalent needed — `IEnumerable<T>` has no `ForEach` in the BCL by
design.

### 19.4 Final notes — intentional, no future fix needed

- **`CopySqlCeBinaries` deleted.** It copied `Microsoft.SqlServer.Compact` `x86`/`amd64`
  `NativeBinaries` into the output. SQL CE is out of scope project-wide (design §10, and the
  `Nop.Data` note that `EfDataProviderManager` now throws `NopException` for a `"sqlce"`
  provider name), and both `EntityFramework.SqlServerCompact` and `Microsoft.SqlServer.Compact`
  are gone.
- **`CreatePluginsBinDirectory` deleted, and the `<Folder Include="Plugins\bin\" />`
  placeholder with it.** Verified harmless: `PluginManager.Initialize()` calls
  `Directory.CreateDirectory` for both `~/Plugins` and `~/Plugins/bin`
  (`Nop.Core/Plugins/PluginManager.cs` lines 84–85), so the directory is created on demand. The
  target was already vestigial in 3.90 — it copied `Content\Files\Index.htm`, and
  `Content/Files/` **does not exist in this tree**, so the `Copy` was a no-op saved only by
  `ContinueOnError="true"`.
- **`MvcBuildViews` deleted.** It ran `<AspNetCompiler VirtualPath="temp" …/>`. There is no
  `aspnet_compiler` on .NET; the Razor SDK compiles views as part of the build. The property was
  `false` in this project, so the target never fired.
- **No explicit `FrameworkReference`.** `Microsoft.NET.Sdk.Web` adds
  `Microsoft.AspNetCore.App` implicitly and restating it fails with **`NETSDK1086`**. This is
  the one place the `Nop.Web.Framework` pattern must *not* be copied — that project is on
  `Microsoft.NET.Sdk` and therefore declares it by hand.
- **`Properties/AssemblyInfo.cs` KEPT** (deletion is task 7.5), consistent with the
  solution-wide `GenerateAssemblyInfo=false` decision from task 2.3. `RootNamespace` and
  `AssemblyName` were both dropped as redundant — both were `Nop.Web`, which is the SDK default
  derived from the file name.
- **No solution-file edit was needed.** `NopCommerce.sln` already records `Nop.Web` under the
  plain C# project GUID `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`; the Web Application Project
  flavour GUID `{349c5851-…}` lived only in the csproj's `ProjectTypeGuids`. Task **18.1** owns
  the solution file.
- **No `Nop.Admin` `ProjectReference` was added, and none was removed.** The legacy file had
  exactly four project references (`Nop.Core`, `Nop.Data`, `Nop.Services`,
  `Nop.Web.Framework`) and no admin reference — confirming design §6's sibling relationship. In
  3.90 `Nop.Admin` merely dropped its build output into `Nop.Web\bin` as the MVC `Admin` area,
  a build/deploy relationship that task **8.2** re-expresses as `MapAreaControllerRoute`
  contributed through `IRouteProvider`.
- **`Web.config`, `Web.Debug.config`, `Web.Release.config`, `Views/Web.config`,
  `Themes/DefaultClean/Views/Web.config` and `Themes/DefaultClean/theme.config` are all picked
  up by the Web SDK's `**\*.config` default `Content` glob** and were left untouched. They are
  read-only as far as 7.1 is concerned: **7.4** owns `Web.config` (including removing the
  ImageResizer `resizer` `configSections` declaration) and **7.5** owns deleting the two
  `Views/Web.config` files. Harmless during build; the XDT `.Debug`/`.Release` transforms are a
  Web Application Project feature the Web SDK does not run.
- **Confirmed absent, nothing to decide:** no file under `Nop.Web` (excluding `Administration/`)
  references `System.Drawing`, `ImageResizer`, `System.Runtime.Caching`, `AutoMapper`,
  `System.Configuration`/`ConfigurationManager`, `System.Data.Entity`, `OfficeOpenXml`,
  `iTextSharp`, `MaxMind`, `Newtonsoft.Json`, `System.Linq.Dynamic`, `Owin`, `System.Web.Http`,
  `Microsoft.Data.OData` or `System.Web.Optimization`. In particular **Nop.Web never bound to
  the bundling API at all** — that binding lived entirely in `Nop.Web.Framework`'s
  `UI/PageHeadBuilder.cs`, already re-based by task 6.3 — and **no `SixLabors.ImageSharp`
  reference is needed here** (unlike `Nop.Admin` at task 8.1, whose `RoxyFilemanController`
  does use imaging).



---

# Nop.Web — hosting model: Global.asax → Program.cs (task 7.2)

Task 7.2 is the task every earlier stage deferred its host-side wiring to. It **closes 17 open
deferrals** and narrows two more. It does **not** reach a clean compile and was not meant to:
gate 7.6 is 7.3's and 7.4's to reach.

| Measurement | Value |
|---|---|
| errors at start (7.1 handover, re-measured) | **1117** across 86 files |
| errors after deleting `Global.asax`/`Global.asax.cs` (stage A) | **1110** |
| errors at end (stages B, C, D) | **1110** — every host file added by this task contributes **0** |
| errors in files 7.2 owns | **0** — `Program.cs`, `Infrastructure/NopHostedEngine.cs`, `Infrastructure/NopErrorLoggingMiddleware.cs`, `Infrastructure/SuppressImplicitRequiredValueTypeMetadataProvider.cs` |
| residual error composition (unique, MSBuild double-prints) | **1001 `CS0246` + 84 `CS0234` + 8 `RZ2005` + 8 `RZ1011` + 4 `RZ1002` + 4 `CS0535` + 1 `CS0103`** |
| residual root causes | **only two**: `System.Web.Mvc` (71 sites) and `System.Web.Routing` (13 sites), plus the Razor `@helper`/attribute-directive family and `WebGrease` (deferral 41). **`StackExchange.Profiling`, `FluentValidation.Mvc` and `System.Web.Optimization` are now at zero occurrences** |
| residual error distribution | `Models` 32 files, `Controllers` 28, `Factories` 11, `Views` 8, `Infrastructure` 4 (the route providers), `Extensions` 2 — i.e. **100% task 7.3**, nothing left for the host |
| restore | clean — **0** `NU19*`, **0** `error MSB*` |

## 19. Verification — the probe, and the masking it exposed

**Roslyn masking is not theoretical here, and it was measured.** 1110 declaration-phase errors
remain, so method bodies in `Program.cs` never bind during a `Nop.Web` build. A throwaway probe
project (`/tmp/probe72`, `Microsoft.NET.Sdk.Web`, compiling **only** this task's four files against
`Nop.Core`/`Nop.Data`/`Nop.Services`/`Nop.Web.Framework` — **no stubs were needed**, because none of
the four references any type owned by 7.3/7.4) was built in the
`mcr.microsoft.com/dotnet/sdk:10.0` container.

The probe was validated the way 6.2/6.3/6.4 validated theirs — by planting a deliberate bad call:

| Build | Sees `app.DeliberateProbeCanaryDoesNotExist()` |
|---|---|
| `Nop.Web.csproj` | **NO** — error count stayed at exactly **1110** |
| probe | **YES** — `CS1061` at `Program.cs(132,17)` |

So the probe binds bodies and the main build does not. Canary reverted, probe deleted;
`git status` shows only the intended files.

**With bodies binding, the probe reported 0 errors / 0 warnings in all four files.**

### 19.1 The probe was then RUN, and asserted 28 runtime facts — all PASS

A compile cannot show that a registration is *reached*. Deferral 11.20 in particular is
security-relevant (`ModelState.IsValid` returns **true for invalid input** until the
FluentValidation provider is in `MvcOptions`), so it was asserted rather than assumed. The probe
executed the real `Program.cs` prologue against a scratch content root:

| Assertion | Result |
|---|---|
| `NopHostingExtensions.ContentRootConfigured` | ✅ |
| `CommonHelper.MapPath("~/App_Data/x")` resolves under the content root, not `bin/` | ✅ |
| `NopConfigurationManager.Configuration` non-null | ✅ |
| `PluginManager.ReferencedPlugins` non-null (deferral 1.1 — plugin discovery **fired**) | ✅ 0 plugins in the scratch root |
| `engine.ContainerManager` assigned by the build callback | ✅ |
| **`engine.ContainerManager.Container` is reference-equal to `app.Services.GetAutofacRoot()`** | ✅ **ONE container** |
| `ContainerManager.CurrentScopeProvider` assigned (deferral 1.3) | ✅ |
| `EngineContext.Current` is the hosted engine, and resolves `IWebHelper` | ✅ |
| `MvcOptions.ModelValidatorProviders` contains `NopFluentValidationModelValidatorProvider` | ✅ `Default…, DataAnnotations…, NopFluentValidation…` |
| `NopMetadataProvider` in `ModelMetadataDetailsProviders` | ✅ |
| `NopModelBinderProvider` at `ModelBinderProviders[0]` | ✅ |
| the implicit-required suppressor runs **after** `DataAnnotationsMetadataProvider` | ✅ `… → DataAnnotationsMetadataProvider → SuppressImplicitRequiredValueTypeMetadataProvider → HasValidators…` |
| a non-nullable `int` member has `IsRequired == false` (3.90 parity) | ✅ |
| a non-nullable `int` member with explicit `[Required]` still has `IsRequired == true` | ✅ |
| `JsonOptions.JsonSerializerOptions.PropertyNamingPolicy == null` (PascalCase) | ✅ |
| `RazorViewEngineOptions.ViewLocationExpanders[0] is ThemeableViewLocationExpander` | ✅ |
| `Cookies` scheme registered → `CookieAuthenticationHandler` | ✅ |
| the four `<forms>` values carried over | ✅ `NOPCOMMERCE.AUTH` / `/login` / `30.00:00:00` (43200 min) / sliding `True` / `SecurePolicy=SameAsRequest` |
| `IHttpContextAccessor`, `IAntiforgery`, `IFileVersionProvider` resolvable | ✅ |
| `IHostApplicationLifetime` resolvable **from the nopCommerce container** (deferral 1.6) | ✅ |
| `ITempDataProvider` registered (deferral 11.28) | ✅ `CookieTempDataProvider` |
| `ISessionStore` registered (deferral 7.15) | ✅ |
| `SqlServerDataProvider.DatabaseInitializer` null **before** startup tasks | ✅ confirms the ordering constraint in `InitializeDatabaseSchema` |
| `engine.RunStartupTasks(nopConfig)` executes without throwing | ✅ |

## 20. `Global.asax` / `Global.asax.cs` are DELETED — task 7.5's scope shrinks

Both files carried `System.Web.Mvc`, `System.Web.Routing`, `FluentValidation.Mvc` and
`StackExchange.Profiling` and were the subject of this task, so deleting them here rather than at
7.5 was unavoidable. `tasks.md` step 7.5 has been annotated. `Properties/AssemblyInfo.cs` and the
`Views/Web.config` files are **untouched** and remain 7.5's.

Where each of the eight `MvcApplication` members went:

| 3.90 member | Now |
|---|---|
| `Application_Start` → `EngineContext.Initialize(false)` | `Program.Main`: `new NopHostedEngine()` + `EngineContext.Replace` + `ConfigureContainer` + `engine.RunStartupTasks(nopConfig)` (§21) |
| `Application_Start` → `ViewEngines.Engines.Clear()/Add(new ThemeableRazorViewEngine())` | `AddNopFramework` → `RazorViewEngineOptions.ViewLocationExpanders.Insert(0, new ThemeableViewLocationExpander())` (deferral 30) |
| `Application_Start` → `ModelMetadataProviders.Current = new NopMetadataProvider()` | `AddNopFramework` → `MvcOptions.ModelMetadataDetailsProviders` (deferral 11.21) |
| `Application_Start` → `AreaRegistration.RegisterAllAreas()` + `RegisterRoutes(RouteTable.Routes)` | `app.UseNopPipeline()` → `UseNopEndpoints()` → `IRoutePublisher` + the `Default` route at `WithOrder(int.MaxValue)` |
| `Application_Start` → `DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false` | new `SuppressImplicitRequiredValueTypeMetadataProvider` (§22) |
| `Application_Start` → `ModelValidatorProviders.Providers.Add(new FluentValidationModelValidatorProvider(new NopValidatorFactory()))` | `AddNopFramework` → `MvcOptions.ModelValidatorProviders` (deferral 11.20) |
| `Application_Start` → `TaskManager.Instance.Initialize()/Start()` | `Program.StartScheduledTasks()` |
| `Application_Start` → `logger.Information("Application started")` | `Program.LogApplicationStart()` |
| `Application_Start` → `ServicePointManager.SecurityProtocol = Tls12`, `MvcHandler.DisableMvcResponseHeader = true`, `GlobalFilters.Add(new ProfilingActionFilter())` | **not ported** — TLS 1.2+ is the .NET default, `X-AspNetMvc-Version` does not exist, MiniProfiler is dropped (§17.8) |
| `RegisterRoutes` → `IgnoreRoute("favicon.ico")`, `IgnoreRoute("{resource}.axd/{*pathInfo}")` | **not ported** — `UseStaticFiles` serves `favicon.ico` before routing; there are no `.axd` handlers |
| `Application_BeginRequest` (install redirect + both early exits) | `InstallUrlMiddleware`, via `UseNopInstallUrl()` (written by 6.4) |
| `Application_BeginRequest`/`EndRequest` MiniProfiler start/stop | **not ported** (§17.8). `StoreInformationSettings.DisplayMiniProfilerInPublicStore` is now inert |
| `Application_AuthenticateRequest` → `SetWorkingCulture()` | `WorkingCultureMiddleware`, via `UseNopWorkingCulture()` (written by 6.4), placed **after** `UseAuthentication()` |
| `Application_Error` presentation | `UseDeveloperExceptionPage()` in Development, else `UseExceptionHandler(...)` serving `~/ErrorPage.htm` — the file `<customErrors defaultRedirect>` named (§23) |
| `Application_Error` 404 → `errorController.Execute(routeData)` | `UseStatusCodePagesWithReExecute("/page-not-found")` — the route `RouteProvider` registers for `Common/PageNotFound` |
| `Application_Error` → `LogException` | new `NopErrorLoggingMiddleware` (§23) |

## 21. `NopHostedEngine` — why a new type, and why two phases

**File: `src/Presentation/Nop.Web/Infrastructure/NopHostedEngine.cs`.**

§17.5 records the intended integration exactly: *"`CreateBuilder(services)` runs
`Populate(services)` first, then the `ConfigureContainer` callbacks (which is where `NopEngine` runs
the `IDependencyRegistrar`s), then `Build()`"*. **But `NopEngine.Initialize` as written constructs
its own `ContainerBuilder` and calls `Build()` on it**, and §17.4b forbids changing
`IEngine`/`NopEngine`/`ContainerManager` — all three live in `Nop.Core`, which passed gate 2.5 and is
committed.

So a naive `EngineContext.Initialize(false)` from `Program.cs` would produce **two containers**, and
that is a correctness failure, not untidiness:

1. every `SingleInstance()` registration would exist twice — two `MemoryCacheManager`s, two settings
   caches, two `IWorkContext` graphs;
2. **deferral 1.3 would regress into something worse than it was.** 6.4's `DependencyRegistrar`
   build callback sets `ContainerManager.CurrentScopeProvider` to
   `() => HttpContext.RequestServices.GetService<ILifetimeScope>()` — a scope of the **host's**
   container — while `ContainerManager.Container` would be nopCommerce's. Every
   `EngineContext.Current.Resolve<T>()` during a request would resolve out of a container with no
   nopCommerce registrations in it. Silently, and only at runtime.

`NopHostedEngine` is therefore a `NopEngine` subclass **in `Nop.Web`** that registers into the
host's builder and never builds. `Nop.Core` is untouched. The probe asserted reference-equality of
the two containers (§19.1).

Two phases, because container construction straddles the host's build boundary:

| Phase | Called from | Does |
|---|---|---|
| `RegisterInto(ContainerBuilder, NopConfig)` | `builder.Host.ConfigureContainer<ContainerBuilder>(...)` | overridden `RegisterDependencies` (base body minus the two statements that create and build a private container) + base `RegisterMapperConfiguration`, plus a `RegisterBuildCallback` that captures the built `IContainer` into `ContainerManager` |
| `RunStartupTasks(NopConfig)` | `Program.Main`, after `builder.Build()` | honours `NopConfig.IgnoreStartupTasks`, then runs `IStartupTask`s |

`RunStartupTasks()` (the protected, parameterless one) is also overridden: the base version reads
`NopEngine`'s **private** container-manager field, which this subclass deliberately leaves null, so
inheriting it would `NullReferenceException`.

`IEngine`, `NopEngine`, `EngineContext`, `ContainerManager` and `IDependencyRegistrar` all keep
their exact signatures, as §17.4b requires. `Nop.Web`'s own
`Infrastructure/DependencyRegistrar.cs` is **unmodified** and is still found by the
`typeFinder.FindClassesOfType<IDependencyRegistrar>()` scan — `NopEngine` is not bypassed, it is
subclassed.

**Autofac.Mvc5 is fully gone at the host level too:** `builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())`
replaces `AutofacDependencyResolver`/`RequestLifetimeScopeProvider`, and ASP.NET Core's per-request
scope **is** the Autofac request `ILifetimeScope`.

## 22. `SuppressImplicitRequiredValueTypeMetadataProvider` — a parity item that would otherwise regress silently

**File: `src/Presentation/Nop.Web/Infrastructure/SuppressImplicitRequiredValueTypeMetadataProvider.cs`.**

`Global.asax.cs` line 71 set
`DataAnnotationsModelValidatorProvider.AddImplicitRequiredAttributeForValueTypes = false`, so a
blank `int`/`decimal`/`bool` field produced **no** validation error and FluentValidation was the
single source of truth for what is mandatory.

ASP.NET Core has no such switch, and its `DataAnnotationsMetadataProvider` sets
`ValidationMetadata.IsRequired = true` for every non-nullable value-typed member. Dropping the line
would have been a **behaviour regression across the whole storefront** — registration, checkout and
every admin form rejecting input 3.90 accepted, with messages no nopCommerce validator produced.

The supported fix is a later `IValidationMetadataProvider` that clears the flag. Ordering is
load-bearing and was verified live, not reasoned about:
`AddControllersWithViews(configure)` applies the caller's `MvcOptions` delegate **after** the
framework's own option setups, so a provider contributed through
`AddNopFramework(..., configureMvc: ...)` runs after `DataAnnotationsMetadataProvider` and wins.
An explicit `[Required]` is respected — the MVC 5 switch only suppressed the *implicit* attribute.

## 23. `Application_Error` split three ways, and `Log404Errors` rescued

**File: `src/Presentation/Nop.Web/Infrastructure/NopErrorLoggingMiddleware.cs`** plus the pipeline
block in `Program.cs`.

`Application_Error` did three things that belong to three different ASP.NET Core mechanisms
(presentation, 404 re-execution, logging). The non-obvious part is the third.

3.90's `LogException` had a 404 rule — *"ignore 404 HTTP errors ... unless
`CommonSettings.Log404Errors`"* — and in System.Web a 404 arrived as an `HttpException`, so one
`catch` covered both cases. **In ASP.NET Core a 404 is a status code, not an exception: nothing
throws.** An exception-only handler would therefore have left `CommonSettings.Log404Errors`
permanently dead — a setting the admin UI still exposes. `NopErrorLoggingMiddleware` observes both
paths: a `catch` for real exceptions (logged, then **rethrown** — it observes, it does not handle),
and an inspection of `Response.StatusCode == 404` after `_next`, which applies the `Log404Errors`
check and the static-resource skip.

Everything else is 3.90 verbatim: nothing is logged before the database is installed, the customer
comes from `IWorkContext.CurrentCustomer`, and a failure *inside* logging is swallowed (the database
is the log sink and is usually the thing that just failed).

**Presentation:** `<customErrors defaultRedirect="errorpage.htm" mode="RemoteOnly"/>` becomes
`UseDeveloperExceptionPage()` in Development and, otherwise, an `UseExceptionHandler` branch that
sends `~/ErrorPage.htm` — the very file `defaultRedirect` named. It is sent with
`Response.SendFileAsync(CommonHelper.MapPath("~/ErrorPage.htm"))` rather than resolved through the
static-file middleware, because the static asset trees are still at their 3.90 locations outside
`wwwroot` (deferral 7.1-5/40).

**Known imprecision, accepted:** `UseStatusCodePagesWithReExecute("/page-not-found")` re-executes
for *any* empty-bodied 4xx/5xx, where 3.90 special-cased 404 and sent everything else to
`errorpage.htm`. In practice 500s already have a body from the exception handler, so only the
uncommon bare 403/400 differs.

## 24. Deferrals CLOSED by task 7.2

Each row names the line of code that closes it. All are in
`src/Presentation/Nop.Web/Program.cs` unless stated.

| # | Deferral | Closed by |
|---|---|---|
| **1.1** | Plugin discovery never runs | `builder.Environment.UseNopHostingEnvironment(builder.Configuration)` — `initializePlugins` defaults to `true`. **Verified fired** (§19.1): `PluginManager.ReferencedPlugins` is non-null after the call. Placed **before** `AddNopFramework()`, which is what deferral 1.2 requires |
| **1.2** | Plugin assemblies invisible to the Razor compiler | the call-order guarantee above + `AddNopFramework` → `ConfigureApplicationPartManager(AddPluginApplicationParts)` |
| **1.3** | Per-request DI scope not shared within a request | `builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())` makes 6.4's build callback fire on the **host's** container. **Verified**: single container, `CurrentScopeProvider` assigned |
| **1.4** | Configuration source unset | the `UseNopHostingEnvironment(builder.Configuration)` argument. Narrowed at 7.2; **fully closed by 7.4**, which authored `appsettings.json` (§32) |
| **1.5** | `CommonHelper.MapPath` resolves under `bin/` | same call, first statement. **Verified**: `MapPath("~/App_Data/x")` lands under the content root |
| **1.6** | `WebHelper.RestartAppDomain` throws | `UseServiceProviderFactory` + `Populate`. **Verified**: `IHostApplicationLifetime` resolvable from the nopCommerce container |
| **4.8** | Schema initializer never invoked | ⚠️ **7.2's fix is INERT.** `Program.InitializeDatabaseSchema()` early-returns on `!DatabaseIsInstalled()` and runs once at startup, so it can never fire for the case 4.8 is about. Task 7.7 measured a real install failing with `Invalid object name 'Store'` and moved the call into `SqlServerDataProvider.InitDatabase()`. See §42.1 |
| **7.13** | Cookie authentication not configured | `AddNopFramework(builder.Configuration)` + `UseNopPipeline()`'s `UseAuthentication()`. **Verified**: scheme present, all four `<forms>` values correct |
| **7.14** | `IHttpContextAccessor` not registered | `AddNopFramework`. **Verified resolvable** |
| **7.15** | Session state not configured | `AddNopFramework` + `UseNopPipeline()`'s `UseSession()`. **Verified**: `ISessionStore` registered |
| **11.20** | FluentValidation not hooked into model validation (**SECURITY**) | `AddNopFramework`. **Verified present in `MvcOptions.ModelValidatorProviders`** — asserted, not assumed |
| **11.21** | `NopMetadataProvider` not registered | `AddNopFramework`. **Verified** |
| **11.22** | `NopModelBinderProvider` not registered | `AddNopFramework`. **Verified at index 0** |
| **11.23** | `JsonResult` camelCase | `AddNopFramework` → `AddJsonOptions(PropertyNamingPolicy = null)`. **Verified** |
| **11.25** | `ChallengeResult` throws | `UseNopPipeline()`'s `UseAuthentication()` (same handler as 7.13) |
| **11.26** | `IAntiforgery` must be resolvable | `AddNopFramework`. **Verified resolvable** |
| **11.28** | `TempData` needs a provider | satisfied by the framework default. **Verified**: `CookieTempDataProvider`. Note it does **not** depend on session, so it is independent of 7.15 |
| **11.29 / 29** | `IsCurrentConnectionSecured()` behind a TLS proxy | `app.UseForwardedHeaders()`, first in the pipeline; options from `AddNopFramework` |
| **14.30** | Theming stops working (**was HIGHEST**) | `AddNopFramework` → `RazorViewEngineOptions`. **Verified**: `ThemeableViewLocationExpander` at index 0 |
| **14.31** | `PageHeadBuilder` needs two services | `AddNopFramework`. **Verified**: `IFileVersionProvider` and `IHttpContextAccessor` both resolvable |
| **38 / 7.1-3** | No `launchSettings.json` | `src/Presentation/Nop.Web/Properties/launchSettings.json` — `Project` and `IIS Express` profiles on 3.90's `http://localhost:2451`, `ASPNETCORE_ENVIRONMENT=Development`, anonymous auth, no Windows auth |
| **37 / 7.1-2** | MiniProfiler — the `Global.asax.cs` half | file deleted. `Views/Shared/_Root.Head.cshtml` lines 10–11 and 54–56 **remain open for 7.3** |

### 24.1 `<forms requireSSL>` — what 3.90's `Web.config` actually said

**`requireSSL="false"`.** Read directly from `src/Presentation/Nop.Web/Web.config`:

```xml
<forms name="NOPCOMMERCE.AUTH" loginUrl="~/login" protection="All"
       timeout="43200" path="/" requireSSL="false" slidingExpiration="true" />
```

So `CookieSecurePolicy.Always` is **not** set, and `NopAuthenticationConfig.RequireSsl`'s `false`
default is correct parity. Critically, `Program.cs` calls the **`IConfiguration` overload** of
`AddNopFramework`, not the `bool requireSsl` one — so the value is a configuration setting bindable
from an `Authentication:RequireSsl` key rather than a literal in `Program.cs`. **Any deployment
whose own `Web.config` had `requireSSL="true"` MUST set that key to `true`, or the port is a
security downgrade** (deferral 7.13). `protection="All"` has no counterpart and needs none — the
claims cookie is protected by ASP.NET Core Data Protection; §7.13's key-ring caveat for
multi-instance deployments still stands and is 7.4's.

### 24.2 Deferral 4.11 — reviewed at 7.2 as §7.19 asked, no change made

`ExecuteSqlCommand(doNotEnsureTransaction: false)` now opens a real transaction, and §7.19 flagged
`SqlFileInstallationService.ExecuteSqlFile` for review "at 7.2 when installation is first exercised
against a real database". Installation has **not** been exercised against a real database in this
task (no database is reachable from the build container), so the per-batch atomicity question is
**still open** and moves to task **7.7** (smoke check). No code was changed.

## 25. NEW deferrals opened by task 7.2

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7.2-1 | Startup now fails fast on an unreachable database | 7.7 (decision) | Medium |
| 7.2-2 | ~~`IRouteProvider` implementations still unported, so `UseNopEndpoints()` cannot run~~ | — | ✅ **RESOLVED by 7.3** (§28) — all four ported; note 7.3 also **corrected** the `.WithOrder(1000)` advice, which would have created an `AmbiguousMatchException` |
| 7.2-3 | `TaskManager.Instance.Stop()` is never called on shutdown | none (3.90 parity) | Low |
| 7.2-4 | ~~`appsettings.json` does not exist, so every bindable setting is at its default~~ | — | ✅ **RESOLVED by 7.4** (§32) — `src/Presentation/Nop.Web/appsettings.json`; all 14 `NopConfig` properties, the live `appSettings` key and all 6 `Authentication` values verified to bind (§31.1) |

### 7.2-1 Startup now fails fast on an unreachable database

`Program.InitializeDatabaseSchema()` deliberately does **not** swallow exceptions.
`CreateTablesIfNotExist.InitializeDatabase` throws `NopException("No database instance")` when
`Database.CanConnect()` is false, so a misconfigured or temporarily unreachable database now takes
the **host** down at startup, where EF6 deferred the failure to the first query and 3.90 rendered an
error page.

- **Why it was left this way:** silently continuing recreates precisely the failure mode deferral 4.8
  exists to prevent — a store that boots happily and then throws "Invalid object name" on the first
  page.
- **The cost:** a supervised process (ANCM / systemd / container orchestrator) will restart-loop
  instead of serving `ErrorPage.htm` while the database is down.
- **Decision for 7.7:** either accept it, or wrap the call and defer to a health check. It is a
  three-line change either way.
- The call is otherwise cheap and idempotent on an installed store — it probes
  `INFORMATION_SCHEMA.TABLES` for `Customer`/`Discount`/`Order`/`Product`/`ShoppingCartItem` and
  returns immediately.

### 7.2-2 The four `IRouteProvider` implementations are still unported — `UseNopEndpoints()` cannot run

`UseNopEndpoints()` resolves `IRoutePublisher` and calls `RegisterRoutes(IEndpointRouteBuilder)`.
`Nop.Web`'s four providers still implement the **MVC 5** signature and are the source of the four
`CS0535` errors in the residual count:

- `Infrastructure/RouteProvider.cs`
- `Infrastructure/GenericUrlRouteProvider.cs`
- `Infrastructure/BackwardCompatibility1XRouteProvider.cs`
- `Infrastructure/BackwardCompatibility2XRouteProvider.cs`

These were **left to task 7.3 on purpose** — `tasks.md` step 7.3 owns *"Register routing over
endpoint routing"*, and §17.4a already documents the exact mechanical recipe plus its four gotchas
(drop `string[] namespaces`; `UrlParameter.Optional` → `{id?}`; **`GenericUrlRouteProvider` needs
`.WithOrder(1000)` on its seven by-name routes or endpoint routing raises
`AmbiguousMatchException` where MVC 5 resolved by registration order**; and the five
empty-named routes in `BackwardCompatibility2XRouteProvider`).

Until they are ported the host builds its pipeline but no request can be routed. This is the single
largest thing standing between 7.2's output and a running store.

Also still owned by 7.3, from the same §17.4a list:
`Nop.Web/Administration/Controllers/SettingController.cs` ~line 2024's
`RouteTable.Routes.ClearSeoFriendlyUrlsCachedValueForRoutes()` (now a no-op; replace with
`LocalizedRoute.ClearSeoFriendlyUrlsCachedValue()` or delete) — though that file is `Nop.Admin`'s,
i.e. task 8.x.

### 7.2-3 `TaskManager.Instance.Stop()` is never called on shutdown

`Program.StartScheduledTasks()` reproduces `Application_Start`'s
`TaskManager.Instance.Initialize()` + `Start()`, and `TaskManager` was deliberately **not** rebased
onto `IHostedService`: it owns its own `TaskThread`/`System.Threading.Timer` machinery and lives in
`Nop.Services`, which has passed its clean-compile gate. Nothing calls `Stop()` — which is **3.90
parity**, since `Application_End` did not call it either. Converting `TaskManager` to an
`IHostedService` (which would give it ordered shutdown for free) is a legitimate post-migration
follow-up.

### 7.2-4 `appsettings.json` does not exist

`WebApplication.CreateBuilder` already wires the configuration **sources**
(`appsettings.json` and `appsettings.{Environment}.json`, both optional, plus environment variables,
command line, and user secrets in Development), and `Program.cs` publishes that `IConfiguration` on
`NopConfigurationManager` — so deferral 1.4's *seam* is closed. But **no `appsettings.json` file was
authored**: that is explicitly task **7.4**'s scope (`tasks.md` step 7.4), and guessing at it here
would collide with 7.4's `Web.config` harvest.

Consequence today: every bound setting is at its default. The sections 7.4 must author, and where
each comes from:

| Section | Source in `Nop.Web/Web.config` | Consumer |
|---|---|---|
| `NopConfig` | the `<NopConfig>` custom section | `NopConfigurationManager.GetNopConfig()` — Redis, Azure blob, `UserAgentStringsPath`, web farms, installation |
| `appSettings` | `<appSettings>` — `Use_HTTP_CLUSTER_HTTPS`, `Use_HTTP_X_FORWARDED_PROTO`, `ForwardedHTTPheader`, `ClearPluginsShadowDirectoryOnStartup` | `NopConfigurationManager.GetAppSetting` |
| `Authentication` | `<system.web><authentication><forms>` | `NopAuthenticationConfig` (§18.3) — **including `RequireSsl`, see §24.1** |

Note the four legacy `appSettings` keys `owin:AutomaticAppStartup`, `webpages:Version`,
`webpages:Enabled` and `PreserveLoginUrl` are System.Web/OWIN artifacts with no counterpart and
should not be carried over. `ClientValidationEnabled` and `UnobtrusiveJavaScriptEnabled` are MVC 5
view-level switches; their ASP.NET Core equivalent is
`HtmlHelperOptions.ClientValidationEnabled`/`MvcViewOptions`, a 7.3/7.4 decision.

## 26. Deferrals explicitly NOT closed by 7.2, with the reason

| # | Item | Why not here |
|---|------|---|
| **32** | No `Widget` view component — `@Html.Widget(...)` throws | 7.3: `WidgetController.WidgetsByZone` → `WidgetViewComponent` |
| **33 / 40 / 7.1-5** | Cache busting no-ops for assets outside `wwwroot`; static trees still at 3.90 locations | 7.3/7.4. `app.UseStaticFiles()` is registered, but `WebRootFileProvider` has nothing under it until the trees move or a composite `IFileProvider` is supplied. **This also interacts with 7.1-4**: widening the static root to the content root would expose `App_Data/Settings.txt` (the connection string) |
| **35** | Minification is gone, nothing replaces it | post-migration |
| **36 / 7.1-1** | Redis session state provider dropped | **7.4 harvested the connection string (`localhost,ssl=true`) from `Web.config`'s commented-out `<sessionState>` before reducing the file, then CLOSED IT BY DECISION** — see §32.1 |
| **39 / 7.1-4** | `ExcludeFilesFromDeployment` publish shaping lost (**security-relevant**) | **RESOLVED by 7.4** (§32, §31.4) |
| **41** | `WebGrease.Css.Extensions.ForEach` load-bearing in `CustomerModelFactory` | 7.3 — still 1 error in the residual count |
| **9 / 4.9** | `NopObjectContext` needs a real connection string | 3.4 (`Nop.Data.Tests` fixtures only) |
| **4.10** | `GO`-batched `CreateDatabaseScript()` in four plugin contexts | 11.2, 13.1, 14.4, 15.1 |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted, no override exists anywhere |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision |


---

# Nop.Web — controllers, views, routing and pipeline (task 7.3)

Task 7.3 is the largest single task in the plan and it **reaches zero errors**, so the 7.6 gate
criterion is met.

| Measurement | Value |
|---|---|
| errors at start (7.2 handover, re-measured) | **1110** across 87 files |
| errors at end | **0** |
| warnings at end | **15** — 10 pre-existing upstream `SYSLIB0014/0021/0023/0045/0051` in `Nop.Core`/`Nop.Services` (unchanged since 6.5) and 5 `CS0618` on FluentValidation 7.x's obsolete `Custom(...)` in `Validators/Common/AddressValidator.cs`, `Validators/Customer/CustomerInfoValidator.cs` and `Validators/Customer/RegisterValidator.cs`. **`git diff` confirms those three files are untouched by this task** — the warnings were previously masked, not introduced, and the FluentValidation pin is a deliberate design decision (design §9) |
| `CA1416` at end | **0** — the six sites task 6.5 predicted (§18.4) were fixed with the `OperatingSystem.IsWindows()` guard it recommended |
| `MVC1000` at end | **0** — all 106 `Html.Partial` and 2 `Html.RenderPartial` call sites converted to the async forms (§30) |
| swallowed-error check | verbose log grep: `"converted to a warning"` → **0**, `"ContinueOnError"` → **0**, `NU1901`–`NU1904` → **0**, `error MSB*` → **0** |
| residual `System.Web` in `Nop.Web` source | **none in code.** A scripted check that blanks `//`, `/* */` and `@* *@` comments before searching finds **0** occurrences; the 24 remaining textual matches are all explanatory prose in comments |
| residual legacy references in `Nop.Web.dll` | **none** — no `System.Web*`, `Autofac.Integration.Mvc`, `System.Web.Optimization`, `ImageResizer`, `WebGrease`, `StackExchange.Profiling` or `MiniProfiler` |

## 27. Error trajectory, and the masking that shaped it

Roslyn does not bind method bodies while declaration-phase errors exist, so the count **rose**
partway through — which was progress, not regression. The phases and their measured effect:

| Phase | Work | Errors after |
|---|---|---|
| baseline | — | **1110** |
| 1 | four `IRouteProvider` implementations → `IEndpointRouteBuilder` | 1095 |
| 2 | `Models/` + `Validators/` + `Extensions/` type substitutions | **823** |
| 3 | `Controllers/` — 28 files | **43** |
| 3b | the residual `.cs` sites that only became visible once declarations bound | **1974** ← bodies of 193 views started binding here |
| 4 | `Views/_ViewImports.cshtml` | **24** |
| 5 | 8 `@attribute` collisions, 4 Razor-v2 `helper` declarations, `SearchModel` ambiguity | 220 ← the rest of the views' bodies started binding |
| 6 | `Html.Action` bridge, `Widget` view component, 9 mechanical view rules | 10 |
| 7 | last 10 individual sites | **0** |

The single biggest lever was `Views/_ViewImports.cshtml`: it took the count from **1974 to 24**
in one file, because `Views/Web.config`'s `pageBaseType` is what supplies the `T("…")` localizer
and its absence produced 1974 `CS0103: The name 'T' does not exist`.

**Warning for tasks 8.3/8.4:** `Nop.Admin` has 325 views and the identical dependency on
`Areas/Admin/Views/Web.config`'s `pageBaseType` and `<namespaces>`. Create its
`_ViewImports.cshtml` **first**; it will collapse the great majority of the admin view errors
before any per-view work begins.

## 28. The routing correction — an upstream instruction that was wrong

### 28.1 `.WithOrder(1000)` would have CREATED the ambiguity it was meant to prevent

`GenericPathRouteExtensions`'s remarks (§17.4a gotcha 3) said the eight single-segment patterns
registered by `GenericUrlRouteProvider` — `{generic_se_name}` plus seven `{SeName}` routes — are
of identical precedence and would raise `AmbiguousMatchException`, and recommended
`.WithOrder(1000)` on the seven. **A live probe against net10.0 showed the reasoning is
inverted.** Two measured facts:

1. **`MapControllerRoute` does not leave `Endpoint.Order` at 0.** MVC's
   `ControllerActionEndpointConventionBuilder` assigns an **auto-incrementing order per call**
   (1, 2, 3, …), and `EndpointComparer` compares `Order` *before* precedence. Registration
   sequence therefore already decides the winner — which is a faithful reproduction of MVC 5's
   "`RouteCollection` stops at the first match", and is why the eight patterns do **not** collide
   by default. Probe endpoint dump:

   | order | inbound precedence | raw pattern |
   |---|---|---|
   | 1 | 3 | `{generic_se_name}` |
   | 2 | 1 | `cart/` |
   | 3 | 1.1 | `producttag/all/` |
   | 4 | 1.3 | `wishlist/{customerGuid?}` |

2. **Forcing all seven to `WithOrder(1000)` collapses seven distinct orders into one and
   reproduces the exception.** Measured, with the transformer returning `null` (the redirect
   case, which invalidates the dynamic candidate):
   - seven at their natural auto-assigned orders → `200 "ProductDetails"`, no ambiguity;
   - seven at `WithOrder(1000)` → **`599 "THREW AmbiguousMatchException: The request matched
     multiple endpoints"`**;
   - seven at distinct orders 1001–1007 → `200 "ProductDetails"`, no ambiguity.

**What was applied instead:** each of the seven carries
`Microsoft.AspNetCore.Routing.SuppressMatchingMetadata`, which removes it from *inbound
matching* while leaving it usable for *link generation*. That is the exact semantic these routes
have always had — in 3.90 they were never matched, because `GenericPathRoute` either resolved the
slug itself or aborted with `Response.End()`. Relying on the auto-assigned order would work today
only by coincidence and would break silently if a plugin registered another `{SeName}`-shaped
route or provider ordering changed.

**Verified, not assumed:** with `SuppressMatchingMetadata` applied,
`Url.RouteUrl("Product", new { SeName = "my-slug" })` — the exact call shape the views use —
still returns `/my-slug` for all seven names. `IUrlHelper.RouteUrl` resolves through
`RouteValuesAddressScheme`, which skips only `ISuppressLinkGenerationMetadata`, not
`ISuppressMatchingMetadata`.

### 28.2 A second, larger finding from the same probe: provider ordering is load-bearing

Because `Order` beats precedence, registering the slug route **first** makes
`{generic_se_name}` swallow every single-segment path: the probe showed `/cart` resolving to
`Common/GenericUrl` instead of the `"cart/"` route, *even though the literal pattern has strictly
better precedence*. `GenericUrlRouteProvider.Priority` staying at `-1000000` (so `IRoutePublisher`
registers it last) is therefore not cosmetic — it is what keeps every named route reachable.
**Any future task that changes `IRouteProvider.Priority` values, or registers routes outside the
publisher, can silently break slug or literal routing.**

### 28.3 `UrlParameter.Optional` — the two rules applied, and why

`UrlParameter.Optional` has no counterpart; the *default* was dropped everywhere (22 occurrences)
but the *pattern segment* was made optional only under rule **O1**:

- **O1 — trailing parameter with no constraint → `{x?}`.** 13 patterns: `wishlist/{customerGuid?}`,
  `producttag/{productTagId}/{SeName?}`, `download/getdownload/{orderItemId}/{agree?}`,
  `boards/topic/{id}/{slug?}`, `boards/forum/{id}/{slug?}`, `boards/forumgroup/{id}/{slug?}`,
  `privatemessages/{tab?}`, and the six `BackwardCompatibility2X` `…/{SeName?}` patterns. MVC 5
  could match these URLs with the segment absent, so ASP.NET Core must too.
- **O2 — non-trailing parameter, or a parameter carrying a constraint → pattern left unchanged.**
  7 patterns: `checkout/completed/{orderId}`, `backinstocksubscriptions/manage/{page}`,
  `boards/forumsubscriptions/{page}`, `boards/activediscussions/page/{page}`,
  `boards/topic/{id}/{slug}/page/{page}`, `boards/forum/{id}/{slug}/page/{page}`,
  `privatemessages/{tab}/page/{page}`. Reason: a middle segment can never be omitted, and MVC 5
  evaluated a constraint against the *absent* value — which a `\d+` regex or a `GuidConstraint`
  always fails — so the route could not match with the segment omitted. Marking it optional would
  have **added** a matchable URL that 3.90 never served. In each `page` case a sibling route
  already serves the no-page URL.

  Note the probe found ASP.NET Core does **not** reject a non-trailing `{x?}` on net10.0
  (`privatemessages/{tab?}/page/{page}` parses), so O2 rests on behaviour, not on a parser
  constraint.

### 28.4 Other routing changes

- `BackwardCompatibility1XRouteProvider` registered all **eleven** routes with the name `""`.
  Normalised to `null` (unnamed), matching what `MapLocalizedRoute` already does for
  `BackwardCompatibility2X`'s five. Nothing generates URLs to them by name.
- Route patterns with a **trailing slash** (`"login/"`, `"cart/"`, `"compareproducts/"`,
  `"producttag/all/"` — ~30 of them) were verified to parse correctly and to match both
  `/cart` and `/cart/`. No edit needed.
- `namespaces` (`new[] { "Nop.Web.Controllers" }`) dropped at **162** call sites.
  `MapRoute` → `MapControllerRoute` at 21 sites. `GuidConstraint` construction unchanged.

---

## 29. Open deferrals opened by task 7.3

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7.3-1 | The `Html.Action` bridge lives in `Nop.Web`; `Nop.Admin` and the plugins need it too | 8.3 (decision) | Medium |
| 7.3-2 | Session-stored `ProcessPaymentRequest.CustomValues` round-trips as `JsonElement` | 12.1–12.5 | Medium |
| 7.3-3 | ASP.NET request validation is gone — every model property now behaves as `[AllowHtml]` | none (accept) | **Low–Medium, security-relevant** |
| 7.3-4 | ~~48 former `[ChildActionOnly]` actions are now reachable by URL~~ | — | ✅ **RESOLVED ahead of 8.3** (§45.1) — `NopChildActionOnlyAttribute` + `NopChildActionOnlyConvention` in `Nop.Web.Framework`, marker applied to all 48. 7.4 declined it deliberately and re-targeted it to 8.3; it was pulled forward so `Nop.Admin` ports onto the finished mechanism |
| 7.3-5 | Server-side browser detection removed (IE8 CSS/JS, mobile `readonly`) | none (accept) | Low |
| 7.3-6 | The child-action bridge does not run action filters | none (accept) | Low |

### 7.3-1 The `Html.Action` bridge is in `Nop.Web`, but is needed more widely

`Nop.Web/Extensions/ChildActionExtensions.cs` reimplements `Html.Action` / `Html.RenderAction`.
It was unavoidable: **five of the 101 call sites name the controller and action from data at
runtime** — `IWidgetPlugin` (`widget.ActionName`/`ControllerName`/`RouteValues`),
`IPaymentMethod` (`Model.PaymentInfoActionName`, `Model.ButtonPaymentMethodActionNames[i]`) and
`IExternalAuthenticationMethod` (`eam.ActionName`/`ControllerName`). A view component is selected
by CLR type or component name at compile time and cannot be selected from a runtime
controller/action pair, and those three plugin contracts still expose action/controller/
`RouteValueDictionary` triples (task 6.2 changed only the `RouteValueDictionary` namespace on
them). Rewriting the contracts to return view-component names is owned by tasks 10.x–15.x.

- **What 8.3 must decide:** `Nop.Admin` has its own `@Html.Action` call sites. Either duplicate
  the file or **promote it to `Nop.Web.Framework`** (the better option — it is framework-shaped
  code, and the plugins would then get it for free). Promotion means editing a project that
  passed gate 6.6, which earlier tasks have done deliberately when justified (task 3.2 did
  exactly that for the lazy-loading fix).
- **Impact if unfixed:** `Nop.Admin`'s views will not compile at 8.8 until it has the same bridge.

**Verified live, not merely compiled.** A throwaway probe (deleted; the repository is clean of
it) copied the bridge into a standalone ASP.NET Core app with its own controllers and views and
asserted nine behaviours — the probe was proven able to fail via a deliberate canary:

| Assertion | Result |
|---|---|
| action with no parameters | ✅ |
| `int` + `string` route values bound, constructor-injected service resolved | ✅ |
| `enum` route value bound | ✅ |
| declared parameter default used when the value is absent | ✅ |
| `int?` bound | ✅ |
| `Content("")` renders nothing | ✅ |
| the 2-argument overload targets the **current** controller | ✅ |
| a `RouteValueDictionary` argument — the plugin-contract shape | ✅ |
| `PartialView("OtherName")` honoured | ✅ |

### 7.3-2 `ProcessPaymentRequest.CustomValues` loses CLR type through the session

`CheckoutController` stored `Session["OrderPaymentInfo"] = paymentInfo` and read it back with a
cast; `ISession` is a `byte[]` store with no object indexer, so
`Nop.Web/Extensions/SessionExtensions.cs` serialises with `System.Text.Json` — the same treatment
task 4.2 gave `ExternalAuthorizerHelper`, and what upstream nopCommerce 4.x does at this exact
call site. Six write sites and four read sites in `CheckoutController` plus one read in
`ShoppingCartModelFactory`.

- **The gap:** `ProcessPaymentRequest.CustomValues` is `Dictionary<string, object>`. JSON cannot
  recover the original CLR type of an `object` value, so entries come back as `JsonElement`
  rather than as the type a payment plugin put in. Newtonsoft.Json behaves identically (values
  become `JObject`/`JValue`) unless `TypeNameHandling` is enabled, which is a
  deserialization-gadget hazard task 4.2 explicitly refused to introduce.
- **Fix (tasks 12.1–12.5):** each payment plugin that round-trips a non-string `CustomValues`
  entry through the session must either use string values or read defensively.
- The helper **fails soft** (returns `default`, writes are no-ops) when the session feature is
  absent or a payload will not deserialize, so a lost stash restarts payment entry — which is
  what a 3.90 session expiry did.

### 7.3-3 ASP.NET request validation is gone — SECURITY-RELEVANT RELAXATION

**99 `[AllowHtml]` attributes and 42 `[ValidateInput(false)]` attributes were deleted**, because
neither has any counterpart: they existed only to opt *out* of ASP.NET **request validation**
(the framework-level *"A potentially dangerous Request.Form value was detected from the client"*
guard). ASP.NET Core has no request validation at all.

- **Net effect:** 3.90 blocked HTML-looking input on every model property except the 99 marked
  `[AllowHtml]`. Every property now behaves as if it carried `[AllowHtml]`. This is a
  **relaxation relative to 3.90 that cannot be restored**, because the feature no longer exists.
- **Why it is accepted rather than reimplemented:** request validation was always a defence in
  depth, not the primary control; the primary control is output encoding, and Razor encodes by
  default. nopCommerce also deliberately turned it off wherever it mattered — the 42
  `[ValidateInput(false)]` actions are exactly the ones that accept rich text.
- **What reviewers should know:** the properties that were `[AllowHtml]` (BBCode/rich-text fields
  on forum posts, product reviews, blog/news comments, private messages) are unchanged in risk.
  The change is that *previously-protected* fields are no longer screened. Any place that renders
  a model value with `@Html.Raw(...)` is where this matters, and `Nop.Core.Html.HtmlHelper.FormatText`
  remains the sanitiser on the rich-text paths.
- `Views/Web.config` also carried `<pages validateRequest="false">`, i.e. 3.90 already disabled
  request validation for *view* rendering; only the controller-input direction changes.

### 7.3-4 Former child actions are now URL-reachable — ✅ RESOLVED, see §45.1

`[ChildActionOnly]` was deleted from **48 actions** (no counterpart — the attribute existed to
make an action invocable only via `Html.Action`). Those actions are now matched by the `Default`
`{controller}/{action}/{id?}` route, so e.g. `/Common/Footer` or `/ShoppingCart/OrderSummary`
returns the bare partial's HTML.

- **Impact:** information exposure of partial fragments, not of data the visitor could not
  otherwise see — each partial renders for the current customer with the same authorisation the
  parent page applies (`BasePublicController`'s class-level filters still run for a direct URL
  request). Rated Low–Medium because it widens the attack surface without granting new access.
- **Fix options:** an `IActionModelConvention` (or a marker attribute plus an
  `IEndpointSelectorPolicy`) that applies `SuppressMatchingMetadata` to those actions —
  the same mechanism §28.1 uses for the seven name-only routes. That is a ~30-line convention
  registered in `AddNopFramework`, and it would restore 3.90's behaviour exactly.
- Not done here because the 48 actions are not individually marked any more, so the convention
  needs a marker attribute reintroduced, which is a design choice better made alongside 7.4's
  configuration work than bolted onto an already-large task.

### 7.3-5 Server-side browser detection removed

`HttpRequest.Browser` (`System.Web.HttpBrowserCapabilities`) does not exist in ASP.NET Core and
has no replacement — capability sniffing was driven by `browscap.xml` and was dropped from the
platform. Three call sites:

- `Themes/DefaultClean/Views/Shared/Head.cshtml` — the IE8 branches that loaded
  `Themes/DefaultClean/Content/css/ie8.css`, `Scripts/selectivizr.min.js` and
  `Scripts/respond.min.js`. **Removed.** IE8 cannot run this storefront regardless, and those
  three files are now unreferenced (noted for 7.5).
- `Views/Product/_RentalInfo.cshtml` ×2 — a conditional `readonly` on the two datepicker inputs
  for mobile. **Removed.** This is a *relaxation*: the field becomes editable where it was
  read-only; the datepicker still works and `ShoppingCartController.ParseRentalDates`
  re-validates server-side.
- `App_Data/browscap.xml` survives only because `Nop.Services`' `IUserAgentHelper` parses it for
  `IsSearchEngine()`; it exposes no browser-version data, so it is not a route back to this.

Inventing User-Agent parsing was rejected as scope creep with a poor accuracy/benefit ratio.

### 7.3-6 The child-action bridge does not run action filters

MVC 5 ran the action-filter pipeline for child actions; the bridge does not (it invokes the
action method directly). **This is consistent with a decision already recorded for this
migration**: task 6.2 removed the `filterContext.IsChildAction` guard from eleven filters
precisely because "view components do not execute the action filter pipeline at all" (§12). The
filters concerned (`CheckAffiliate`, `StoreClosed`, `PublicStoreAllowNavigation`,
`LanguageSeoCode`, `NopHttpsRequirement`, `WwwRequirement`, …) are declared on
`BasePublicController` and have already run for the parent request. Accepted, not deferred.

---

## 30. Final behavioural changes — task 7.3, intentional, no future fix needed

### Controllers

- **`new HttpUnauthorizedResult()` → `new ChallengeResult()`, 70 sites.** MVC 5's result emitted a
  bare 401 that `FormsAuthenticationModule` rewrote into a 302 to the login URL on the way out.
  ASP.NET Core has no outbound module, so `ChallengeResult` — which hands the refusal to the
  registered cookie handler, which redirects to `LoginPath` — is the faithful equivalent. Exactly
  the substitution task 6.2 made in `AdminAuthorizeAttribute`, `AdminVendorValidation` and
  `PublicStoreAllowNavigationAttribute` (§11.25).
- **`BasePublicController.InvokeHttp404()` reimplemented.** 3.90 executed a second controller
  in-process (`IController errorController = …; errorController.Execute(new RequestContext(…))`).
  There is no `IController`, no `IController.Execute` and no `RequestContext`. It now returns
  `NotFound()`, and task 7.2's `app.UseStatusCodePagesWithReExecute("/page-not-found")`
  re-executes `Common/PageNotFound` **at the original URL** — the same observable outcome
  (HTTP 404 + the PageNotFound page, URL unchanged).
- **`FormCollection` → `IFormCollection`, 37 sites**, and `form.AllKeys` → `form.Keys`. The
  interface rather than the concrete type, because `HttpRequest.Form` is typed as the interface —
  the same choice task 6.2 made for `BasePaymentController.ValidatePaymentForm`.
  **The indexer now yields `StringValues`**, so 25 locals were declared `string` explicitly to
  force the implicit conversion. That conversion is behaviourally identical to
  `NameValueCollection`'s indexer: a single value is returned as-is, multiple values are joined
  with `","` (which the `Checkboxes` branches' `Split(',')` depends on), and a missing key yields
  `null`. Three `List<int>` initialisers that the same indexer feeds use
  `StringValues.IsNullOrEmpty(...)` plus an explicit `(string)` cast, because `!= null` against
  `StringValues` is ambiguous between its two `!=` operators.
- **`HttpPostedFileBase` → `IFormFile`, 7 sites**; `.InputStream` → `.OpenReadStream()`,
  `.ContentLength` (int) → `.Length` (long).
- **The three valums-uploader blocks contained a latent truncation bug, now fixed.** They did
  `var fileBinary = new byte[stream.Length]; stream.Read(fileBinary, 0, fileBinary.Length);`.
  `Request.Body` is **not seekable** in ASP.NET Core so `.Length` throws
  `NotSupportedException`, and a single `Read()` is not guaranteed to fill the buffer even on a
  seekable stream. Replaced with `CopyTo` over a `MemoryStream` — the **same** latent bug task
  4.2 fixed in `Nop.Services`' `Media.Extensions.GetPictureBits`/`GetDownloadBits`, fixed here
  for the same reason. `Request["qqfile"]` became a new `protected virtual GetRequestValue(key)`
  that searches Query then Form (System.Web's indexer order), with the `HasFormContentType`
  guard task 6.2 added in five filters. `Request.Files[0]` → `Request.Form.Files[0]`, likewise
  guarded.
- **`TryUpdateModel` → `TryUpdateModelAsync(...).GetAwaiter().GetResult()`, 5 sites.** ASP.NET
  Core offers only the async form; making the one-page-checkout actions async would change their
  public signatures. Sync-over-async cannot deadlock — no `SynchronizationContext`.
- **`DependencyResolver.Current.GetService(type)` → `EngineContext.Current.Resolve(type)`,
  2 sites.** MVC 5's service-locator hook was pointed at Autofac by `Autofac.Mvc5`, a package
  task 6.4 removed. `IEngine.Resolve(Type)` resolves out of the same container.
- **`Json(x, JsonRequestBehavior.AllowGet)` → `Json(x)`, 2 sites.** ASP.NET Core has no
  JSON-hijacking guard and no such parameter; `JsonRequestBehavior.DenyGet` is gone, which task
  6.2 already recorded as a relaxation (§11.23). `[AcceptVerbs(HttpVerbs.Get)]` → `[HttpGet]`.
- **`CommonController.PageNotFound`: `Response.TrySkipIisCustomErrors` removed.** It stopped IIS
  replacing an already-bodied response with its own error page; ASP.NET Core responses are
  written by the app and ANCM does not substitute them.
- **`CommonController.RobotsTextFile`: `Response.Write(content); return null;` →
  `return Content(content, MimeTypes.TextPlain)`.** `HttpResponse.Write` does not exist (the
  equivalent is the async `WriteAsync`), and ASP.NET Core throws on a `null` action result where
  MVC 5 treated it as "response already written". Identical bytes emitted.
- **`HttpRequest.ApplicationPath` → `PathBase`, mapped to `"/"` when empty.** The mapping is
  required, not cosmetic: `LocalizedUrlExtenstions.IsVirtualDirectory` **throws** on an empty
  string. The same mapping task 6.2 applied in `WebWorkContext`.
- **`BackwardCompatibility1XController`: `Request.RawUrl` →
  `Request.GetEncodedPathAndQuery()`** (yields `PathBase + Path + QueryString`, the shape
  `RawUrl` produced — task 6.2's substitution), and `Request.QueryString["x"]` →
  `Request.Query["x"]` ×8. In ASP.NET Core `QueryString` is a struct holding the raw string; the
  parsed collection is `Query`.
- **`InstallController`: `System.Data.SqlClient` → `Microsoft.Data.SqlClient`** (the client tasks
  3.1/4.2 standardised on; the `System.Data.SqlClient` types are type-forwarded on net10.0 and
  would need the out-of-band package). **`Server.ScriptTimeout = 300` removed** ×2:
  `HttpServerUtility.ScriptTimeout` raised System.Web's per-request execution timeout, and
  ASP.NET Core/Kestrel has no request execution timeout at all — the nearest equivalents are host
  configuration (7.4). Installation now runs untimed, which is strictly more permissive.
- **`CheckoutController` lost its `HttpContextBase` constructor parameter entirely** — its only
  use was `Session`, which `Controller.HttpContext` supplies directly. A **breaking constructor
  change**, but the controller is registered reflectively so no DI edit is needed.

### Factories

- **`HttpContextBase` → `IHttpContextAccessor` in `CatalogModelFactory` and
  `ShoppingCartModelFactory`; dropped entirely from `CommonModelFactory`.** All three are
  **breaking constructor changes**, all registered reflectively.
- **`Request.Params["q"]` → `Query`/`Form` lookup** (`CatalogModelFactory`). `Params` was a merged
  view over QueryString, Form, Cookies and ServerVariables; `q` is the search box's query-string
  parameter. Note the surrounding `try/catch` is now dead for its stated purpose — its comment
  describes catching *"A potentially dangerous Request.QueryString value was detected"*, which
  cannot be thrown any more (deferral 7.3-3).
- **`Request.PhysicalApplicationPath` → `CommonHelper.MapPath("~/…")`** (`CommonModelFactory`
  favicon lookup), resolving against the content root task 7.2 assigns (deferral 1.5) — the same
  substitution task 4.2 made for `MaintenanceService.GetBackupDirectoryPath`.
- **`ISitemapGenerator`'s `UrlHelper` → `IUrlHelper`** propagated to
  `ICommonModelFactory.PrepareSitemapXml` and its implementation. Task 6.2 had already changed
  the framework side (§9b).
- **`WebGrease.Css.Extensions.ForEach` → `foreach`, 2 sites — deferral 41 RESOLVED.** Confirmed
  as 7.1 reported: `PrepareCustomCustomerAttributes` returns `IList<T>`, so this was
  WebGrease's `IEnumerable<T>` extension, **not** `List<T>.ForEach`. The BCL has no
  `IEnumerable<T>.ForEach` by design.
- **`HttpUtility` → `WebUtility`** in `ProductModelFactory` and `CustomerController` — the swap
  already made across Nop.Core (2.4), Nop.Services (4.2) and Nop.Web.Framework (6.2).

### Models, Extensions, Installation

- **`SelectListItem`/`SelectList` → `Microsoft.AspNetCore.Mvc.Rendering`** and
  **`RouteValueDictionary` → `Microsoft.AspNetCore.Routing`** — namespace moves only, across 32
  model files and 11 factories.
- **`Nop.Web/Extensions/HtmlExtensions`**: `MvcHtmlString` → `IHtmlContent`/`HtmlString`,
  `HtmlHelper<T>` → `IHtmlHelper<T>`. **All 13 `html.RouteLink(...)`/`html.ActionLink(...)`
  results are now explicitly rendered with `.ToHtmlString()`** — this is the silent-defect class
  task 6.3 flagged (§15b): those helpers return a `TagBuilder`, and
  `StringBuilder.Append(object)` would have emitted the literal string
  `"Microsoft.AspNetCore.Mvc.Rendering.TagBuilder"` into the pager markup. Five of the thirteen
  had nested parentheses that a naive regex missed and were caught by paren-matching.
- **`AttributeParserHelper.ParseCustomAddressAttributes`: `FormCollection` → `IFormCollection`**,
  with the three `StringValues` locals forced to `string` (see above).
- **`InstallationLocalizationService`**: `HttpContextBase` → `IHttpContextAccessor`;
  `HttpCookie` → `IRequestCookieCollection` (read) / `IResponseCookies.Delete` + `Append` with
  `CookieOptions` (write, preserving `HttpOnly` and the 24-hour lifetime, behind a
  `Response.HasStarted` guard); `Request.UserLanguages` → the typed `Accept-Language` header
  ordered by quality — the substitution task 6.2 made in
  `WebWorkContext.GetLanguageFromBrowserSettings`.
- **`InstallController`'s ACL check is now guarded by `OperatingSystem.IsWindows()`.** This is the
  fix task 6.5 recommended (§18.4) and it is a real one, not warning suppression:
  `WindowsIdentity.GetCurrent()` sits *outside* `CheckPermissions`'s swallowing `try/catch`, so
  on Linux the install page **threw** instead of rendering. Clears all 6 `CA1416`.

### Views

- **`Views/_ViewImports.cshtml` created** — the replacement for `Views/Web.config`'s
  `<system.web.webPages.razor><pages>` element. It carries `@inherits
  Nop.Web.Framework.ViewEngines.Razor.WebViewPage<TModel>` (the `pageBaseType`, and the reason
  `T("…")` resolves) plus the `<namespaces>` list. `System.Web.Mvc.Ajax` has **no** counterpart
  and needed none — zero `Ajax.` occurrences under `Views/`. `Nop.Web.Models.Boards` and
  `Nop.Web.Models.Catalog` are deliberately **not** imported globally: both declare a
  `SearchModel`, which made the name ambiguous in the two `Search.cshtml` views.
  **`Views/Web.config` is not deleted — that is task 7.5's scope.**
- **Four Razor-v2 `helper` declarations converted to `void` methods in `@functions`**
  (`CategoryNavigation.RenderCategoryLine`, `TopMenu.RenderCategoryLine`,
  `_FilterPriceBox.FormatPriceRangeText`, `CustomerProductReviews.GetReviewRow`). Markup inside
  such a method is written straight to the page output, so the same HTML lands in the same place
  and **the bodies are unchanged**. Chosen over a partial view (four extra files, a view-engine
  lookup per loop iteration) and over a tag helper (public types for view-local formatting), and
  necessary over a templated `Func<T, IHtmlContent>` because all four mix statements with markup
  and two recurse. Call sites: `@Name(x)` → `Name(x);` in a **code** context and
  `@{ Name(x); }` in a **markup** context — mixing these up produces `RZ1010`.
- **`@attribute.DefaultValue` → `@(attribute.DefaultValue)`, 8 sites** in
  `_CustomerAttributes`, `_ProductAttributes`, `_AddressAttributes`, `_CheckoutAttributes`.
  ASP.NET Core 3.0 introduced a reserved **`@attribute` directive**, and a loop variable named
  `attribute` collides with it. The parentheses make it an explicit expression again.
- **`MvcHtmlString.IsNullOrEmpty(x)` → `x.IsNullOrEmpty()`, 40 sites**, via a new
  `IHtmlContent` extension. `MvcHtmlString` was a string wrapper; `IHtmlContent` is a *writer*,
  so emptiness can only be determined by rendering. A more specific `Pager` overload uses
  `Pager.IsEmpty()` — which task 6.3 retained for exactly this — so the pager sites do not
  materialise every page link twice.
- **`new ViewDataDictionary()` → `Html.NewViewData()`, 28 sites.** ASP.NET Core's
  `ViewDataDictionary` has no parameterless constructor. The helper produces an **empty**
  dictionary rather than `new ViewDataDictionary(ViewData)`: the copy constructor would inherit
  the parent's entries *and* its `TemplateInfo.HtmlFieldPrefix`, and several call sites set
  `HtmlFieldPrefix` themselves — inheriting one would produce doubled field names such as
  `BillingNewAddress.BillingNewAddress.FirstName`.
- **`HttpUtility.JavaScriptStringEncode` → `JavaScriptHelper.Encode`
  (`System.Text.Encodings.Web.JavaScriptEncoder`), 9 sites.** `HttpUtility` *does* exist on
  net10.0 (in the in-box `System.Web.HttpUtility` assembly) but using it would put a
  `System.Web*` assembly reference back into `Nop.Web`, which the gate checks for.
  `JavaScriptEncoder` is *more* aggressive (it escapes non-ASCII, `&`, `<`, `>`, `'`, `"` as
  `\uXXXX`); the escaped forms are equivalent JavaScript, so a script sees the same value and
  only the bytes on the wire differ. All 9 sites embed the result in a single-quoted literal.
  A null is handled in the helper because `JavaScriptStringEncode(null)` returned `""` where
  `Encode(null)` throws.
- **`HttpContext.Current.Request.RawUrl` → `Context.Request.GetEncodedPathAndQuery()`, 7 sites**;
  **`Request.Url.AbsoluteUri` → `Context.Request.GetDisplayUrl()`, 4 sites**;
  **`this.Request.Url.Scheme` → `Context.Request.Scheme`, 7 sites**;
  **`Request.QueryString[...]` → `Context.Request.Query[...]`, 3 sites**. `RazorPage` exposes the
  ambient `HttpContext` as `Context`; `HttpContext.Current` does not exist, and ASP.NET Core
  splits the request URL into `Scheme`/`Host`/`PathBase`/`Path`/`QueryString` rather than a `Uri`.
- **`Url.RequestContext.RouteData` → `ViewContext.RouteData`, 9 sites** in
  `Views/Shared/_ColumnsTwo.cshtml`. `IUrlHelper` has no `RequestContext` — that was
  `System.Web.Routing.RequestContext`. Note the surrounding `Convert.ToInt32(…Values["categoryId"].ToString())`
  is unchanged and safe: `RouteValueDictionary` is case-insensitive, so it still matches the
  lowercase `categoryid` that `SlugRouteTransformer` writes.
- **MiniProfiler removed from `Views/Shared/_Root.Head.cshtml`** — the `displayMiniProfiler` gate
  and `@StackExchange.Profiling.MiniProfiler.RenderIncludes()`. Closes the second half of
  deferral **37 / 7.1-2**; `StoreInformationSettings.DisplayMiniProfilerInPublicStore` and
  `DisplayMiniProfilerForAdminOnly` are now **inert**, the same treatment
  `SeoSettings.EnableJsBundling` received. Task 8.4 should remove the admin checkboxes.
- **`Views/Widget/WidgetsByZone.cshtml` → `Views/Shared/Components/Widget/Default.cshtml`.** A
  view component renders `Components/{ComponentName}/{ViewName}`, so this resolves through
  `ThemeableViewLocationExpander`'s existing `/Views/Shared/{0}.cshtml` format — which also keeps
  it **themeable** at `/Themes/{theme}/Views/Shared/Components/Widget/Default.cshtml`. Hardcoding
  `View("~/Views/Widget/WidgetsByZone.cshtml")` would have compiled and rendered but silently
  bypassed the expander. (In practice the only theme view override in the tree is
  `Themes/DefaultClean/Views/Shared/Head.cshtml`, so no shipped behaviour depended on it — but
  third-party themes would have.)
- **`Html.Partial` → `await Html.PartialAsync` (106 sites) and `Html.RenderPartial` →
  `await Html.RenderPartialAsync` (2 sites).** Clears 216 `MVC1000` analyzer warnings. Done
  because it removes real sync-over-async rather than merely silencing a diagnostic, and it is
  safe here: all 106 are at markup position and both `RenderPartial` calls are statements in code
  blocks. Verified that **no** `Partial` call sits inside an `@functions` method, where `await`
  would be impossible.
- **`Html.BeginRouteForm(name, routeValues, FormMethod, htmlAttributes)` → the 5-argument
  overload with `antiforgery: null`, 2 sites** (`ProductTemplate.Simple`/`Grouped`). ASP.NET Core
  has no 4-argument overload in that shape. `null` keeps the framework default, which emits an
  anti-forgery token for a POST form where MVC 5 did not — harmless when nothing validates it,
  and the safer default.

### Deferrals CLOSED by task 7.3

| # | Deferral | Closed by |
|---|---|---|
| **32** | No `Widget` view component — `@Html.Widget(...)` throws | `Nop.Web/Components/WidgetViewComponent.cs` + `Views/Shared/Components/Widget/Default.cshtml`. All 190 `@Html.Widget(...)` call sites are unchanged |
| **41 / 7.1-6** | `WebGrease.Css.Extensions.ForEach` load-bearing in `CustomerModelFactory` | replaced with `foreach` at both sites (§30, Factories) |
| **37 / 7.1-2** | MiniProfiler — the `_Root.Head.cshtml` half | block removed; the `Global.asax.cs` half was closed by 7.2 |
| **7.2-2** | The four `IRouteProvider` implementations unported, so `UseNopEndpoints()` could not run | all four ported to `IEndpointRouteBuilder`; §28 records the ambiguity correction |
| **18.4** | The six `CA1416` sites task 6.5 predicted for 7.3 | `OperatingSystem.IsWindows()` guard in `InstallController` |

### Deferrals explicitly NOT closed by 7.3, with the reason

| # | Item | Why not here |
|---|------|---|
| **33 / 40 / 7.1-5** | Static asset trees still outside `wwwroot`; cache busting no-ops | **RESOLVED by 7.4** (§32, §33.1). 7.3's finding held: **no view forced the decision** — every asset reference goes through `AppendCssFileParts`/`AddScriptParts` with a `~/`-rooted path or through `Url.Content`, so all 193 views are correct under either option. 7.4 chose the composite/allow-list `IFileProvider` rather than relocation. §30 notes three assets that became unreferenced (`ie8.css`, `selectivizr.min.js`, `respond.min.js`) |
| **36 / 7.1-1** | Redis session state dropped | **CLOSED BY DECISION at 7.4** (§32.1) — harvested, then deliberately not wired because 3.90 shipped it commented out. Deferral 7.3-2's multi-instance consequence is recorded there |
| **39 / 7.1-4** | `ExcludeFilesFromDeployment` publish shaping lost (security-relevant) | **RESOLVED by 7.4** (§32, §31.4) |
| **35** | Minification gone, nothing replaces it | post-migration |
| **7.2-1** | Startup fails fast on an unreachable database | 7.7 (decision) |
| **7.2-3** | `TaskManager.Instance.Stop()` never called | none (3.90 parity) |
| **7.2-4** | `appsettings.json` does not exist | **RESOLVED by 7.4** (§32) |
| **4.11** | `ExecuteSqlCommand` per-batch transaction during installation | 7.7 — still needs a real database |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted; no override exists anywhere |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision |

### What task 7.7 should exercise first

7.7 is the first task that runs the ported storefront. The three things this task could not
verify without a database, in priority order:

1. **`@Html.Action(...)`** — the bridge's mechanism is verified (deferral 7.3-1, nine live
   assertions) but not against nopCommerce's real controllers. Load the home page: it fans out to
   ~15 child actions including `Logo`, `HeaderLinks`, `TopMenu`, `Footer` and `FlyoutShoppingCart`.
2. **Slug routing and the seven suppressed name-only routes** — request a product slug (expect the
   product page), and confirm a view that calls `Url.RouteUrl("Product", new { SeName = … })`
   emits `/the-slug`. §28 predicts both, from a probe rather than from this application.
3. **`Html.Widget`** — any page render exercises the new `Widget` view component; an empty widget
   zone must produce no output rather than an exception.



---

# Nop.Web — web.config → appsettings.json / IConfiguration (task 7.4)

Task 7.4 is the configuration-migration task, and it is the task four deferrals were explicitly
waiting on. It **closes six** (4/7.2-4, 16/7.16, 33/14.33, 39/7.1-4, 40/7.1-5, and the second half of
7.20), **closes one by decision** (36/7.1-1) and opens two.

| Measurement | Value |
|---|---|
| errors before / after | **0 / 0** — the 7.6 gate criterion is still met |
| warnings before / after | **15 / 15** — byte-for-byte the same set: 10 pre-existing upstream `SYSLIB0014/0021/0023/0045/0051` in `Nop.Core`/`Nop.Services`, plus 5 `CS0618` on FluentValidation 7.x's obsolete `Custom(...)` in three `Validators/` files that this task did not touch (`git diff` confirms). The FluentValidation pin is design §9 and was not "fixed" |
| swallowed-error check | verbose log grep: `"converted to a warning"` → **0**, `"ContinueOnError"` → **0**, `NU1901`–`NU1904` → **0** |
| residual `System.Web` in `Nop.Web` source | **0 files**, measured with `//`, `/* */` and `@* *@` comments stripped first |
| residual legacy assembly references in `Nop.Web.dll` | **none** — no `System.Web*`, `ImageResizer`, `WebGrease`, `Autofac.Integration.*`, `MiniProfiler`, `StackExchange.Profiling`, `System.Drawing.Common` or `System.Configuration.*` assembly-name string in the built assembly, and no such package in `Nop.Web.deps.json` |
| `ConfigurationManager.*` call sites in `Nop.Web` | **0** — nothing to rewrite. Confirmed by grep, and consistent with what task 7.1 reported. The legacy `appSettings` surface is consumed by `Nop.Core` (`WebHelper`, `PluginManager`) through the `NopConfigurationManager` seam, not by `Nop.Web` |

## 31. Verification — the probe, and the two real defects it and the publish audit exposed

A compile proves nothing about whether configuration *binds* or whether a file is *reachable*.
Both were measured.

### 31.1 A throwaway probe asserted 68 facts — all PASS

`/.probe74` (a `Microsoft.NET.Sdk.Web` project with a single `ProjectReference` to
`Nop.Web.csproj`, run in the `mcr.microsoft.com/dotnet/sdk:10.0` container, content root pointed
at the real `src/Presentation/Nop.Web`, since deleted — `git status` verified clean of it) started
a real Kestrel host and made real HTTP requests. It was proven able to fail: a `--canary` switch
adds one assertion that `GET /App_Data/Settings.txt` returns 200, and the probe duly reported
`FAIL: 1` for it.

| Group | Asserted | Result |
|---|---|---|
| A | `appsettings.json` is discovered and parsed (including its `//` comments — the JSON configuration provider uses `JsonCommentHandling.Skip`) | ✅ |
| B | all **14** `NopConfig` properties bind to the authored values via `NopConfigurationManager.GetNopConfig()` | ✅ 14/14 |
| C | `GetAppSetting("ClearPluginsShadowDirectoryOnStartup")` → `"false"`; the three commented-out load-balancer keys stay **null**, i.e. 3.90 parity; `webpages:Enabled` is gone | ✅ |
| D | all **6** `<forms>` values reach `NopAuthenticationConfig`, `RequireSsl == false` | ✅ |
| E | **the deferral-40 symptom, reproduced**: before the fix `WebRootFileProvider` is a `NullFileProvider` and `/Scripts/public.common.js` does not exist | ✅ |
| F | the allow-list serves all 8 sampled 3.90 asset paths | ✅ 8/8 |
| G | the allow-list refuses all 16 sampled non-asset paths, refuses enumeration of the content root and of `App_Data`, and allows enumeration of `Scripts` | ✅ 19/19 |
| H | **the deferral-33 fix**: `IFileVersionProvider.AddFileVersionToPath` returns `/Scripts/public.common.js?v=LlDNzco6ovFC0YjsIzhK3g8z0nSru9WmU5HJLfAqh3k` for an allowed asset and leaves a denied path unversioned | ✅ |
| I | **10 end-to-end HTTP requests**: 200 for three assets; **404 for `/App_Data/Settings.txt`, `/App_Data/browscap.xml`, `/appsettings.json`, `/web.config`, `/Views/Web.config`, `/Themes/DefaultClean/theme.config` and `/bin/Debug/net10.0/Nop.Web.dll`**. `Cache-Control: public, max-age=604800`. `.otf` mapped, `.bak` deliberately unmapped | ✅ |

### 31.2 DEFECT FOUND — `Web.config` with a capital W silently loses the ANCM handler on any Linux build

The SDK's `TransformWebConfig` task looks for the literal filename **`web.config`** in the publish
directory before merging the ASP.NET Core Module handler into it. On a case-sensitive filesystem —
i.e. every build this migration performs, per `build-environment.md` — `Web.config` is not that
name.

Measured, before the fix: `dotnet publish` emitted `Web.config` containing only the two IIS
elements and **no `<handlers>` and no `<aspNetCore>` element at all**. An IIS deployment built on
Linux would have failed to start with no diagnostic pointing at the cause; a Windows build would
have worked, because `Exists()` is case-insensitive there.

**Fixed by renaming the file to all-lowercase `web.config`** (which is also ASP.NET Core's own
convention). Re-verified: the published `web.config` now contains
`<add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" …/>` and
`<aspNetCore processPath="dotnet" arguments=".\Nop.Web.dll" hostingModel="inprocess" />`
**alongside** the two preserved IIS elements — the transform merges, it does not replace.

### 31.3 DEFECT FOUND — `Content/Images/Thumbs/placeholder.txt` had been swallowed by `.gitignore`

`Nop.Services.Media.PictureService.DeletePictureThumbs` calls
`Directory.GetFiles(CommonHelper.MapPath("~/content/images/thumbs"), …)`, which throws
`DirectoryNotFoundException` when the directory is absent, and `GetThumbLocalPath` only creates
the per-prefix **sub**directory used when `MediaSettings.MultipleThumbDirectories` is on. The
directory is therefore mandatory.

3.90 guaranteed it with `<Content Include="Content\Images\Thumbs\placeholder.txt" />` in the
classic project file (recovered from git history), while withholding only the *generated* images
(`*.jpg`, `*.jpeg`, `*.png`, `*.gif`) from deployment. But `.gitignore` line 162 read
`src/Presentation/Nop.Web/Content/Images/Thumbs/*` — the whole directory — so the placeholder is
absent from this checkout and the directory does not exist in a fresh clone.

Fixed three ways, all consistent with 3.90: the placeholder is restored; the `.gitignore` rule is
narrowed to exactly the four generated extensions 3.90's own exclusion list named; and the
publish exclusion in `Nop.Web.csproj` is likewise the four extensions rather than the whole tree.
Verified by publish: `placeholder.txt` present, a planted `0001_probe.jpeg` withheld.

### 31.4 Publish output audited against planted secrets

`App_Data/Settings.txt` (with a fake password in it), `App_Data/InstalledPlugins.txt`,
`App_Data/browscap.crawlersonly.xml`, a generated thumbnail and a generated
`Content/files/ExportImport/*.xml` were **created on disk** and then `dotnet publish` was run, so
the exclusions were tested against files that actually existed rather than against patterns.

| Must be present | Result |
|---|---|
| `appsettings.json`, `web.config` (transformed) | ✅ |
| `App_Data/browscap.xml`, `App_Data/GeoLite2-Country.mmdb`, `App_Data/Pdf/FreeSerif.ttf`, `App_Data/Install/*.sql`, `App_Data/Localization/Installation/*.xml` | ✅ |
| `Content/**`, `Scripts/**`, `Themes/DefaultClean/Content/**`, `Themes/DefaultClean/theme.config`, `Themes/DefaultClean/preview.jpg` | ✅ |
| `favicon.ico`, `ErrorPage.htm`, `FileNotFound.htm`, `Content/files/ExportImport/Index.htm`, `Content/Images/Thumbs/placeholder.txt` | ✅ |

| Must be absent | Result |
|---|---|
| **`App_Data/Settings.txt`** (the database connection string) | ✅ withheld |
| `App_Data/InstalledPlugins.txt`, `App_Data/browscap.crawlersonly.xml` | ✅ withheld |
| generated thumbnails, generated `Content/files/ExportImport/*.xml` | ✅ withheld |
| `Views/Web.config`, `Themes/DefaultClean/Views/Web.config` | ✅ withheld |
| `Properties/launchSettings.json`, `Web.Debug.config`, `Web.Release.config` | ✅ withheld |
| any `.cs`, `.cshtml` or `.csproj` | ✅ none |

**Note on the counterintuitive direction of the work.** Deferral 39 was written as "re-express the
exclusions", but the *larger* half turned out to be **inclusions**. Under the classic project ~700
explicit `<Content Include=…/>` entries copied `App_Data`, `Content`, `Scripts` and `Themes`. The
SDK classifies `.css`/`.js`/`.png`/`.xml`/`.sql`/`.ttf`/`.mmdb` as `None`, and `None` items default
to `CopyToPublishDirectory=Never` — so a publish of the pre-7.4 project produced an application
with **no static assets, no installation scripts, no browscap database and no PDF font**. That was
not called out anywhere and would have surfaced only on a first deployment.

`Update` metadata is used throughout rather than new `Include` items: every one of these files is
already an item from implicit globbing, so `Include` would fail the build with `NETSDK1022`, and
`Update` additionally no-ops harmlessly for the gitignored paths that only exist on a running
instance.

---

## 32. Deferrals CLOSED by task 7.4

| # | Deferral | Closed by |
|---|---|---|
| **4 / 7.2-4** | `NopConfig` and `appSettings` sections do not exist, so every setting sits at its CLR default | **`src/Presentation/Nop.Web/appsettings.json`** — all 14 `NopConfig` properties plus the live `appSettings` key, translated element-by-element from the legacy `<NopConfig>` custom section with the legacy source named in a comment above each block. Task 7.2 had already published the `IConfiguration` on `NopConfigurationManager`; this authors what it reads. **Verified: 14/14 bind** (§31.1 group B). A side effect worth naming: this also closes the second half of **deferral 7.20** — `NopConfig.UserAgentStringsPath` is now set, so `IUserAgentHelper.IsSearchEngine()` stops unconditionally returning false and crawlers are no longer treated as customers |
| **33 / 14.33** | Cache busting silently no-ops for assets outside the web root | **`Infrastructure/NopStaticFilesExtensions.UseNopStaticFileProvider`** — the *same provider instance* is installed on `IWebHostEnvironment.WebRootFileProvider`, which is what `DefaultFileVersionProvider` captures, so versioning and serving cannot drift apart. **Verified: a real `?v=<sha256>` suffix** (§31.1 group H) |
| **39 / 7.1-4** | `ExcludeFilesFromDeployment` publish shaping lost with the Web Application Project targets — SECURITY-RELEVANT | **the PUBLISH SHAPING block in `Nop.Web.csproj`** (both directions — see §31.4) **plus `Infrastructure/NopStaticFileProvider.cs`** for the runtime half the deferral did not mention: publish shaping stops a file being *deployed*, but nothing stopped the static-file middleware *serving* one. **Verified against planted secrets** (§31.4) and by 404s on live HTTP (§31.1 group I) |
| **40 / 7.1-5** | Static asset trees still at their 3.90 locations, outside `wwwroot` — **nothing served at all** | **`Infrastructure/NopStaticFileProvider.cs` + `NopStaticFilesExtensions.cs`**, registered from `Program.cs` as `app.UseNopStaticFiles()` at the same pipeline position as 7.2's bare `UseStaticFiles()`. Decision and reasoning in §33.1. **Verified: symptom reproduced, then 8/8 allowed paths serve and 19/19 denied paths refuse** (§31.1 groups E/F/G) |
| **16 / 7.16** | EU VAT service endpoint is a compiled-in constant, not configuration | **`appsettings.json` `Tax:EuropaCheckVatServiceUrl`** + `TaxService.EuropaCheckVatServiceUrl`. See §32.3 — including the deliberate `http` → `https` default change |

---

## 32.3 Deferral 16 / 7.16 (EU VAT service endpoint) — RESOLVED, with a deliberate HTTP → HTTPS change

The endpoint was a compiled-in constant, so it could not be redirected to a test double or an
updated URL without a recompile. It is configuration again:

- **`appsettings.json`** gains a `Tax` section with `EuropaCheckVatServiceUrl`;
- **`TaxService.EuropaCheckVatServiceUrl`** (still `protected virtual`) reads
  `NopConfigurationManager.Configuration["Tax:EuropaCheckVatServiceUrl"]` and falls back to
  `DefaultEuropaCheckVatServiceUrl` when no configuration source or no value is supplied;
- a new `public const string TaxService.EuropaCheckVatServiceUrlConfigurationKey` names the key.

**Verified** by a second throwaway probe (deleted): the key is present in the real
`appsettings.json`; with no configuration source the property returns the constant; with the real
configuration it returns the authored URL; an in-memory override wins **and is trimmed**, which is
the test-double redirection the deferral asked for; and a whitespace-only override does **not**
blank the endpoint.

**DELIBERATE BEHAVIOUR CHANGE — the default moved from `http://` to `https://`.** The retired WSDL
declared plain HTTP, so 3.90 sent VAT numbers and received company names and addresses in the
clear. Deferral 7.16 itself recommended HTTPS *"when this becomes configurable"*, and it now is, so
a deployment that must revert can do so from `appsettings.json` without a recompile. VIES serves
the same operation at the same path over TLS.

**Two design choices worth naming.** The deferral offered a `TaxSettings` key as the alternative
home; it was **not** chosen, because `TaxSettings` is persisted in the database, so the endpoint
would be unreadable in exactly the situation an operator most needs to change it. And the read goes
through the static `NopConfigurationManager` seam rather than an injected `IConfiguration`, to avoid
a breaking constructor change to one of the most widely consumed services in the application for
the sake of one string; the seam's documented null-fallback is precisely this property's fallback,
so unit tests and non-web hosts behave as before.

`Nop.Services` was re-gated after the edit: **0 errors, 10 warnings** — its recorded 4.3 baseline,
unchanged.

---

## 32.1 Deferral 36 / 7.1-1 (Redis session state) — CLOSED BY DECISION, with the recipe recorded
**Harvested first, as instructed.** `Web.config`'s `<sessionState>` element was the only place the
Redis session settings existed. Verbatim, before the file was reduced:

```xml
<!--<sessionState mode="Custom" customProvider="MySessionStateStore">
  <providers>
    <add name="MySessionStateStore" type="Microsoft.Web.Redis.RedisSessionStateProvider"
         host="localhost" accessKey="" ssl="true" />
  </providers>
</sessionState>-->
```

i.e. a StackExchange-style connection string of **`localhost,ssl=true`** with an empty access key.

**Decision: do NOT wire `AddStackExchangeRedisCache(...)`.** The element was **commented out** in
3.90's shipped `Web.config`, so 3.90 shipped with in-process session — which is exactly what
`AddNopFramework`'s `AddDistributedMemoryCache()` + `AddSession()` already provides. Wiring Redis
would turn a *disabled* 3.90 feature *on* by default, and would additionally require a running
Redis instance for the app to start. It would also be wrong to key it off
`NopConfig.RedisCachingEnabled`: that attribute governed `ICacheManager`, not session state, and
conflating the two would invent a coupling 3.90 did not have.

**Consequence, stated plainly.** On a **multi-instance** deployment, session state is bound to one
instance, so anything parked in session is lost when a request lands elsewhere. Two concrete
in-tree consequences, both already recorded upstream:

- **deferral 7.15** — `ExternalAuthorizerHelper` parks `OpenAuthenticationParameters` in session
  across the redirect to the external provider, so external logins fail intermittently.
- **deferral 7.3-2** — `CheckoutController` stashes `ProcessPaymentRequest` in session, so
  checkout can lose the payment request mid-flow.

**Re-targeted to deployment configuration; no task owns it, and it is not a code defect.** The
three-line recipe for an operator who needs it, recorded so the harvested value is not lost:

```csharp
// replaces AddNopFramework's AddDistributedMemoryCache()
builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost,ssl=true");
```

Two things must be done **together with** it, or it is not sufficient (both from deferral 7.13):
share the ASP.NET Core **Data Protection key ring** (`PersistKeysTo*` — the modern equivalent of
`<machineKey>`, without which auth cookies stop validating across instances), and set
`NopConfig.MultipleInstancesEnabled` to `true`. All three are noted next to
`MultipleInstancesEnabled` in `appsettings.json`. **Task 7.7** should confirm single-instance
session works; multi-instance is out of scope for a compile-gated port.

## 32.2 Deferral 7.3-4 (48 former `[ChildActionOnly]` actions are URL-reachable) — DEFERRED, re-targeted to task 8.3

Offered as optional. **Not implemented, deliberately.** The `IActionModelConvention` is the right
mechanism, but it needs a marker to select the 48 actions, and 7.3 removed `[ChildActionOnly]`
from all of them without leaving one — so implementing it means re-annotating 48 methods across
28 controller files that reached zero errors hours ago, for a change whose *behaviour* cannot be
verified without a running store with a database. That is a poor trade inside an already large
configuration task.

**Re-targeted to task 8.3**, not to a generic "post-migration", because 8.3's own task text
already says *"Convert `[ChildActionOnly]` actions plus their `Html.Action(...)` call sites to
View Components"* — so the identical decision must be made for the ~325-view admin surface, and
the exposure there is worse (admin partials). Doing it once, in `Nop.Web.Framework` where both
projects can share it, is strictly better than doing it twice. Recipe:

1. add a marker attribute (e.g. `Nop.Web.Framework.Mvc.NopChildActionOnlyAttribute`) and apply it
   to the 48 `Nop.Web` actions and the admin equivalents;
2. add an `IActionModelConvention` that, for a marked action, adds
   `Microsoft.AspNetCore.Routing.SuppressMatchingMetadata` — the **same** mechanism §28.1 uses for
   `GenericUrlRouteProvider`'s seven name-only routes, and already proven to remove an endpoint
   from inbound matching while leaving it usable for link generation;
3. register the convention in `AddNopFramework`.

Severity is unchanged (Low–Medium): the exposed partials render for the current customer under the
same class-level filters as the parent page, so this widens the attack surface without granting
new access.

---

## 33. Final behavioural changes — task 7.4, intentional

### 33.1 Static assets: the provider moved, the files did not (deferral 40)

The deferral offered two options and left the choice to this task. **Relocating under `wwwroot/`
was rejected**, on three grounds each of which was checked in the source rather than assumed:

| Blocker | Detail |
|---|---|
| `ThemeProvider` | enumerates `CommonHelper.MapPath("~/Themes/")` for `theme.config` and `ThemeConfiguration` keeps the resolved `DirectoryInfo.FullName`. Moving `Themes/DefaultClean/Content` to `wwwroot` splits a theme across two roots while its `Views/` and `theme.config` stay put |
| `CommonModelFactory.PrepareFaviconModel` | **probes** `MapPath("~/favicon-{storeId}.ico")` then `MapPath("~/favicon.ico")` — the content root — and only then emits a root-relative URL. A relocated favicon would be *served* but never *found*, so no favicon link would render |
| `PictureService` | **writes** generated thumbnails to `MapPath("~/content/images/thumbs")`. The write path and the read path must be one directory |

So `Infrastructure/NopStaticFileProvider.cs` is an **allow-listed `IFileProvider` over the content
root**, installed onto `IWebHostEnvironment.WebRootFileProvider`. What it permits:

- `Content/**` and `Scripts/**` in full;
- `Themes/<theme>/Content/**` and `Themes/<theme>/preview.jpg` — deliberately **not** the whole
  theme directory, so `Themes/<theme>/Views/**` and `theme.config` stay unreachable;
- at the application root: `favicon.ico`, `favicon-*.ico`, `ErrorPage.htm`, `FileNotFound.htm`;
- nothing else. Directory enumeration of the content root is refused outright.

Plus a denied-extension backstop (`.cs`, `.cshtml`, `.config`, `.csproj`, `.sln`, `.user`, `.dll`,
`.pdb`, `.exe`, `.bak`, `.mdf`, `.ldf`, `.sdf`) that is *mostly* redundant — `ServeUnknownFileTypes`
is left at its `false` default, so none of those extensions is in the content-type map and the
middleware would 404 them anyway, which is also how 3.90's `DenyAccessToPluginDLLs`
`HttpForbiddenHandler` is reproduced. It is stated explicitly so the refusal does not silently
depend on a framework default a future `ContentTypeProvider` edit could undo.

`IWebHostEnvironment.WebRootPath` is deliberately **left alone**. Only the provider needs to
change, and repointing `WebRootPath` at the content root would hand any
`Path.Combine(env.WebRootPath, …)` caller a writable handle on the whole application directory.
Verified: nothing in the solution reads `WebRootPath`.

**One thing this does NOT narrow, on purpose.** `Content/files/ExportImport` is served, because
`Nop.Plugin.Feed.GoogleShopping` (task 11.2) generates its feed there and links to the URL. That
directory was served by `System.Web`'s static handler in 3.90 too, so this is parity — but it does
mean generated export/feed output is publicly reachable, which is what the publish exclusions for
`*.txt`/`*.xml`/`*.xls`/`*.xlsx`/`*.pdf` in that folder mitigate for a *deployment*.

### 33.2 `<staticContent>` moved into code; `<urlCompression>` and `<httpProtocol>` stayed in `web.config`

A principled split, not an arbitrary one. Under ANCM every request is forwarded to the
application, so **IIS's static-file handler never runs** and its `<clientCache>` / `<mimeMap>`
settings are dead configuration — the application is now what serves static files, so those move
to `StaticFileOptions`. `<urlCompression>` and `<httpProtocol><customHeaders>` act on the response
*after* ANCM hands it back, so IIS still honours them and they stay where they always were.

| 3.90 | Now |
|---|---|
| `<clientCache cacheControlMode="UseMaxAge" cacheControlMaxAge="7.00:00:00"/>` | `StaticFileOptions.OnPrepareResponse` → `Cache-Control: public,max-age=604800`. **Verified live** |
| `<mimeMap .otf/.woff/.woff2/.json>` | `FileExtensionContentTypeProvider` mappings. Only `.otf` was genuinely missing from ASP.NET Core's default map; all four are set explicitly so the map does not depend on a framework default |
| `<mimeMap .bak application/octet-stream>` | **DELIBERATELY NOT REPRODUCED.** Its comment was *"Allow database backup (.bak) file loading"*, i.e. it made `Administration/db_backups/*.bak` downloadable over HTTP. `.bak` is additionally in the denied-extension list and `Administration/` is outside the allow-list, so a backup is refused three times over. Nop.Admin's backup download is a controller action that streams the file, so nothing depends on the mapping |
| `<remove name="X-Powered-By"/>` | kept in `web.config` — IIS adds the header, not the app. Kestrel never adds it |
| `<urlCompression doStatic doDynamic="true"/>` | kept in `web.config` — see new deferral **7.4-1** |

**Deliberate deviation, recorded:** 3.90's font MIME strings were the pre-IANA
`application/x-font-opentype`, `application/font-woff` and `application/font-woff2`. The registered
types `font/otf`, `font/woff`, `font/woff2` are used instead — which is what the framework map
already contains. Browsers ignore `Content-Type` for `@font-face` entirely (the format is sniffed),
so this is observationally identical while not re-introducing three deprecated strings.

### 33.3 `appSettings` keys dropped, and the two that needed no configuration at all

| Key | Disposition |
|---|---|
| `owin:AutomaticAppStartup` | dropped — there is no OWIN host |
| `webpages:Version`, `webpages:Enabled` | dropped — `System.Web.WebPages` does not exist |
| `PreserveLoginUrl` | dropped — a System.Web forms-authentication quirk switch |
| `ClientValidationEnabled` (`"true"`) | **needs no configuration**: `true` is already ASP.NET Core's default for `MvcViewOptions.HtmlHelperOptions.ClientValidationEnabled`, so 3.90's value is preserved by doing nothing |
| `UnobtrusiveJavaScriptEnabled` (`"true"`) | **needs no configuration**: ASP.NET Core emits only unobtrusive `data-val-*` attributes; there is no other mode |
| `Use_HTTP_CLUSTER_HTTPS`, `Use_HTTP_X_FORWARDED_PROTO`, `ForwardedHTTPheader` | all three were **commented out** in 3.90 and are carried over **still commented out**, so the shipped behaviour is identical. Verified: all three still return `null`. The supported replacement is 7.2's `app.UseForwardedHeaders()` (deferral 11.29); these remain only for a proxy using a non-standard header |
| `ClearPluginsShadowDirectoryOnStartup` | **absent from 3.90's shipped `Web.config`** but read by `PluginManager.Initialize()` through the `appSettings` seam, so it is stated explicitly with the `"false"` default the code documents. Flagged here because the faithfulness rule requires naming anything that had no literal legacy element |

One genuinely **new** section was added and is named as such: `Logging`. 3.90's nearest equivalent
was `<trace enabled="false" localOnly="true"/>`, which is not logging configuration. ASP.NET Core
needs no `Logging` section, but without one the console provider defaults to `Information` and
floods stdout with per-request framework noise. The values are the standard template's and change
no nopCommerce behaviour — nopCommerce's own logging remains `ILogger`/`DefaultLogger` writing to
the `Log` table.

### 33.4 `DataSettings` — already on the new config path, and now withheld from publish

The task asks that `DataSettings` load via the new configuration path. It already does, and no
code change was needed — which is worth stating explicitly because it is easy to mistake for an
omission:

- 3.90 **never** kept the connection string in `Web.config`. There is no `<connectionStrings>`
  element anywhere in the legacy file. `DataSettingsManager` reads `App_Data/Settings.txt`, a file
  the installer writes.
- That read is `Path.Combine(CommonHelper.MapPath("~/App_Data/"), "Settings.txt")`, and
  `CommonHelper.BaseDirectory` is the **content root** as of task 7.2's
  `UseNopHostingEnvironment` (deferral 1.5). Before 7.2 it resolved under `bin/`. So the path
  already flows through the new host configuration.
- No `ConfigurationManager` is involved anywhere in the chain.

Design §3 states this outcome directly: *"nopCommerce's `DataSettings` (persisted settings file)
continues to work but is loaded through the new configuration/startup path."* An `IConfiguration`
override was **not** added: it would mean editing `Nop.Core` (gated at 2.5) and changing the
behaviour of six `new DataSettingsManager()` call sites across three gated projects, to add a
capability 3.90 did not have.

What **did** change is that `App_Data/Settings.txt` is now excluded from publish output and
unreachable over HTTP — both verified against a planted file containing a fake password.

### 33.5 `launchSettings.json` × `RequireSsl` — checked, consistent, no change

The interaction the brief flags (deferrals 13 + 38) would bite if the dev URL were `http://` while
`RequireSsl` were `true`: `CookieSecurePolicy.Always` would suppress the auth cookie on a plain
HTTP request and login would fail with no error. Checked:
`launchSettings.json` serves `http://localhost:2451` (3.90's `IISUrl`) and
`Authentication:RequireSsl` is `false`, which `AddNopFramework` maps to
`CookieSecurePolicy.SameAsRequest`. The cookie is set over HTTP and login works in development.
**No change needed.** The pairing does become a trap the moment an operator sets `RequireSsl` to
`true` for production without also giving the dev profile an `https://` URL, so it is noted next to
the setting in `appsettings.json`.

### 33.6 Files deleted, and the one renamed

- **`Web.Debug.config`, `Web.Release.config` — DELETED.** XDT transforms are a Web Application
  Project feature the Web SDK does not run, and both files were unmodified Visual Studio
  boilerplate: the only non-comment line in either was `RemoveAttributes(debug)` on a
  `<compilation>` element that no longer exists.
- **`Web.config` → `web.config` — RENAMED.** See §31.2; this is a correctness fix, not tidying.
- **`<configSections><section name="resizer" …/>` and the `<resizer><plugins>` element — REMOVED**,
  as design §7 requires and as the task text calls out explicitly. `ImageResizer` has no net10.0
  release; task 4.2 replaced it with `SixLabors.ImageSharp`, which is configured in code and has no
  configuration section, and whose built-in GIF encoder performs the palette quantization
  `ImageResizer.Plugins.PrettyGifs` provided.
- **NOT deleted, and not this task's:** `Properties/AssemblyInfo.cs`, `Views/Web.config` and
  `Themes/DefaultClean/Views/Web.config` remain **task 7.5's**. 7.4 only set
  `CopyToPublishDirectory="Never"` on the two view configs, because both declare a
  `System.Web.WebPages.Razor` `<configSections>` group and IIS parses every file literally named
  `web.config` in the served tree — publishing them would produce an **HTTP 500.19** configuration
  error. Once 7.5 deletes them those two csproj entries become harmless no-ops and can go.
  `Themes/DefaultClean/theme.config` **stays**: `ThemeProvider` reads it, and IIS does not parse it.

---

## 34. NEW deferrals opened by task 7.4

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7.4-1 | A Kestrel-only deployment gets no response compression | none (deployment decision) | Low |
| 7.4-2 | Nop.Admin's static assets and `db_backups` need the same two fixes, and cannot get them from here | 8.5 / 8.1 | Medium — security-relevant |

### 7.4-1 A Kestrel-only deployment gets no response compression

`<urlCompression doStaticCompression="true" doDynamicCompression="true"/>` is preserved in
`web.config`, so **IIS-hosted deployments are unchanged**. A Kestrel-only deployment (`dotnet run`,
or a container behind a non-IIS proxy) gets nothing from it.

ASP.NET Core's `UseResponseCompression()` was **deliberately not added**, because it forces a
choice this migration should not make silently for every deployment:

- `EnableForHttps = false` (its default) yields **no compression at all** for an HTTPS storefront,
  which is a real regression against 3.90 for the common case;
- `EnableForHttps = true` reproduces 3.90's behaviour but also reproduces its **BREACH** exposure —
  and nopCommerce pages carry an antiforgery token in a form field, which is exactly the secret
  BREACH targets. (3.90's `doDynamicCompression="true"` has the same exposure today; preserving it
  is faithfulness, not endorsement.)

**Remedy:** terminate compression at the reverse proxy, or add `UseResponseCompression()` with an
explicitly reviewed `EnableForHttps`. Placement, if added: immediately after
`UseForwardedHeaders()` and before `UseNopStaticFiles()`, which keeps the mandated middleware order
intact.

### 7.4-2 Nop.Admin needs the same two fixes and cannot inherit them

Two items are structurally out of reach from `Nop.Web.csproj`, because `Administration\**` is in
`DefaultItemExcludes` (task 7.1) — nothing under it is an item of this project.

1. **`Administration/db_backups/*.bak` publish exclusion.** It is in 3.90's
   `ExcludeFilesFromDeployment` list and is one of the two entries that make deferral 39
   security-relevant. `Nop.Web.csproj` cannot exclude it; **task 8.1** must re-express it in
   `Nop.Admin.csproj`, which in 3.90 dropped its output into `Nop.Web\bin`.
2. **Admin static assets.** `NopStaticFileProvider`'s allow-list deliberately excludes
   `Administration/`, so the admin `Content/` and `Scripts/` trees will **not serve** — which is
   the same symptom deferral 40 described for the storefront. **Task 8.5** already owns *"Relocate
   the admin `Content/` and `Scripts/` trees under `wwwroot` (or expose them through the host's
   static-file middleware)"*. The cheapest correct fix is to widen the allow-list with
   `Administration/Content/**` and `Administration/Scripts/**` (three lines in
   `NopStaticFileProvider.AllowedRoots`-style rules), which automatically covers
   `IFileVersionProvider` for admin assets too — the deferral-33 property holds for anything the
   provider serves. It must **not** be done by widening the root, or `Administration/db_backups`
   and the admin `.cshtml` tree come with it.

---

## 35. Deferrals explicitly NOT closed by 7.4, with the reason

| # | Item | Why not here |
|---|------|---|
| **35** | Minification is gone, nothing replaces it | post-migration (design §8). Interacts with 7.4-1 — a build-time minifier and response compression are the two halves of the same payload-size question |
| **7.2-1** | Startup fails fast on an unreachable database | 7.7 (decision). Needs a real database |
| **7.2-3** | `TaskManager.Instance.Stop()` never called on shutdown | none (3.90 parity) |
| **4.11** | `ExecuteSqlCommand` per-batch transaction during installation | 7.7. Still needs a real database |
| **4.10** | `GO`-batched `CreateDatabaseScript()` in four plugin contexts | 11.2, 13.1, 14.4, 15.1 |
| **9 / 4.9** | `NopObjectContext` needs a real connection string | 3.4 (`Nop.Data.Tests` fixtures only) |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted; no override exists anywhere |
| **7.3-1** | The `Html.Action` bridge lives in `Nop.Web` but `Nop.Admin` and the plugins need it | 8.3 (decision). Recommend promoting it to `Nop.Web.Framework` |
| **7.3-2** | Session-stored `ProcessPaymentRequest.CustomValues` round-trips as `JsonElement` | 12.1–12.5. Note it now also depends on 32.1's in-process-session decision |
| **7.3-3** | ASP.NET request validation is gone | accepted; the feature does not exist to restore |
| **7.3-5** | Server-side browser detection removed | accepted |
| **7.3-6** | The child-action bridge does not run action filters | accepted |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision |

## 36. What task 7.7 should now additionally exercise

Adding to §30's list, and in priority order for the configuration surface:

1. **`appsettings.json` binding end to end in the real host.** §31.1 proved the sections bind, but
   through a probe host. Confirm in `Program.cs`'s own run that `NopConfig` reaches the Autofac
   singleton (`NopEngine.RegisterDependencies` does `RegisterInstance(config).As<NopConfig>()`), so
   that `RedisConnectionWrapper`, `AzurePictureService` and `UserAgentHelper` see the authored
   values rather than defaults.
2. **`IUserAgentHelper.IsSearchEngine()`**, newly live as of this task. The first call parses the
   46 MB `App_Data/browscap.xml` and then **writes** `App_Data/browscap.crawlersonly.xml` — exactly
   3.90's behaviour, but it is the first time that write happens under the new content root, so
   confirm the directory is writable and the first request is not pathologically slow.
3. **Static assets in the browser.** Load the home page and confirm the emitted `<link>`/`<script>`
   URLs carry `?v=` **and** return 200. §31.1 verified both mechanisms independently; 7.7 is the
   first chance to see them composed through `PageHeadBuilder` and a real theme.
4. **`GET /App_Data/Settings.txt` returns 404** on the running store. Verified in the probe; cheap
   to re-confirm, and it is the single highest-consequence assertion in this task.



---

# Nop.Web — obsolete-file removal (task 7.5) and the 7.6 clean-compile gate

Task 7.5 is the smallest task in group 7 — three earlier tasks had already removed most of its
nominal scope — but it is the first one where **the task text itself turned out to be wrong for this
solution**, and where a note handed down from an earlier task turned out to be **half right in a way
that would have caused a silent regression**. Both are recorded below in preference to quietly doing
the reasonable thing.

| Measurement | Value |
|---|---|
| errors before / after | **0 / 0** — the 7.6 gate criterion is met |
| warnings before / after | **15 / 15**, the identical set: 10 upstream `SYSLIB0014/0021/0023/0045/0051` in `Nop.Core`/`Nop.Services` plus 5 `CS0618` on FluentValidation 7.x's obsolete `Custom(...)` in three `Validators/` files this task did not touch (`git diff` confirms). **No warning added.** |
| `Nop.Web.Framework` re-gate (edited by this task) | **0 errors / 10 warnings** — its recorded 6.6 baseline, unchanged |
| upstream re-verification | `Nop.Core` **0**/3 · `Nop.Data` **0**/3 · `Nop.Services` **0**/10 · `Nop.Web.Framework` **0**/10 |
| files deleted | **5** — 2 view `Web.config`, 3 dead IE8 assets |
| files created | **1** — `Themes/DefaultClean/Views/_ViewImports.cshtml` (see §37.2: not optional) |
| files the task text asked to delete but which are KEPT | **1** — `Properties/AssemblyInfo.cs` (see §37.1: measured, deleting it is a real loss) |
| swallowed-error check | `-v:normal` log: `"converted to a warning"` **0**, `ContinueOnError` **0**, `NU1901`–`NU1904` **0**, `error MSB*` **0**, Six Labors licence lines **0** |
| residual `System.Web*` in `Nop.Web` source | **0 real hits across 435 files**, comment-blanked scan with a proven canary (§38.1) |
| residual banned refs in `Nop.Web.dll` | **0 of 59** assembly references (§38.2) |

## 37. What task 7.5 changed

### 37.1 `Properties/AssemblyInfo.cs` is KEPT — the task text is wrong for this solution, and it was measured

`tasks.md` step 7.5 says "Remove `AssemblyInfo`". **Do not.** Deleting it is a silent regression here,
for a reason specific to a decision taken at task 2.3.

`src/Directory.Build.props` sets **`<GenerateAssemblyInfo>false</GenerateAssemblyInfo>`** for the
whole solution, with the comment *"Hand-maintained Properties/AssemblyInfo.cs files are retained
where they still exist; the SDK must not emit a duplicate generated set."* With that property false
the SDK emits **no** assembly-identity attributes, so there is nothing to inherit the file's contents.

Measured both ways, via a `MetadataLoadContext` read of the built assembly:

| Build | `AssemblyVersion` |
|---|---|
| with `Properties/AssemblyInfo.cs` | **3.9.0.0** |
| with the file removed | **0.0.0.0** |

Also lost in the second case: `AssemblyTitle`, `AssemblyFileVersion`, `AssemblyCompany`,
`AssemblyProduct`, `AssemblyCopyright`, `ComVisible(false)` and the COM `Guid`. Note the three
identity properties `Directory.Build.props` *does* define (`Company`, `Product`, `Copyright`) are
inert while `GenerateAssemblyInfo` is false, and in any case carry **different values**
(`nopCommerce` vs the file's `Nop Solutions, Ltd`), so they are not a substitute.

Deleting the file would additionally have required flipping a solution-wide property for one project
and made `Nop.Web` the only project of the five migrated so far to differ — task 6.5 §18.2 recorded
exactly this decision for `Nop.Web.Framework`, and `Nop.Core`, `Nop.Data` and `Nop.Services` all keep
theirs. There are 26 projects still to migrate.

Checked before deciding, so the decision is not resting on convention alone: the file declares **no
`InternalsVisibleTo`**, nothing in the solution reflects over `AssemblyTitle`/`Guid`/version, and the
one project that references `Nop.Web` (`src/Tests/Nop.Web.MVC.Tests`, still a legacy project, task
17.x) needs no internals access.

**If a future task does want to move to SDK-generated assembly info, it is a solution-wide change**:
set `GenerateAssemblyInfo=true` in `Directory.Build.props`, add `Version`/`FileVersion`/`Title`
properties, and delete all five `AssemblyInfo.cs` files together. Doing it for one project is worse
than doing it for none.

### 37.2 `Themes/DefaultClean/Views/_ViewImports.cshtml` — a correction to task 7.3's handover note

7.3 recorded, and `tasks.md` repeats, that *"`Views/Web.config` and
`Themes/DefaultClean/Views/Web.config` are now fully superseded by `Views/_ViewImports.cshtml`"*.
**That is true of the first file and false of the second.**

Razor discovers `_ViewImports.cshtml` by walking from the view's **own** directory up to the project
root. For `Themes/DefaultClean/Views/Shared/Foo.cshtml` the search path is
`Themes/DefaultClean/Views/Shared/` → `Themes/DefaultClean/Views/` → `Themes/DefaultClean/` →
`Themes/` → project root. **`Views/` is never on it.**

**Measured, not reasoned about.** A throwaway probe view was planted at
`Themes/DefaultClean/Views/Shared/_ProbeThemeOverride.cshtml` containing a single
`@T("Account.Login")`:

| State | Result |
|---|---|
| before the new file exists | **`error CS0103: The name 'T' does not exist in the current context`** |
| after adding `Themes/DefaultClean/Views/_ViewImports.cshtml` | **0 errors** (probe also exercised `Html.NopPageCssClasses()`, i.e. a `Nop.Web.Framework.UI` type the new file supplies) |

Both probes were deleted; `git status` was verified clean of them. The published assembly's 195
compiled Razor view types include `Themes_DefaultClean_Views__ViewImports`, confirming it is compiled
and applied.

**Why this matters even though the build was green without it.** Today the theme tree holds exactly
one view, `Shared/Head.cshtml`, and that view self-declares its four `@using` directives and never
calls `T()` — so it compiles either way and no gate would ever have caught the gap. But a theme
exists to **override** base views, and `ThemeableViewLocationExpander` (task 6.3, §16.1) searches
`/Themes/{theme}/Views/{controller}/` and `/Themes/{theme}/Views/Shared/` **first** for every view in
the storefront. The moment anyone copies a base view there to customise it — the entire purpose of the
directory — that copy fails with a confusing `CS0103` on `T()`, which is on essentially every
nopCommerce view. Supplying the file makes the deletion lossless instead of merely green.

The new file is a direct translation of the deleted `Web.config`'s two payloads —
`pageBaseType="Nop.Web.Framework.ViewEngines.Razor.WebViewPage"` → `@inherits`, and `<namespaces>` →
`@using` — mirroring what 7.3 did for `Views/`, plus the handful of extra `@using` entries
`Views/_ViewImports.cshtml` carries so a copied base view behaves identically to its original.

### 37.3 Files deleted, and what superseded each

| Deleted | Superseded by |
|---|---|
| `Views/Web.config` | **`Views/_ViewImports.cshtml`** (task 7.3). Its only payloads were the `system.web.webPages.razor` `pageBaseType` + `<namespaces>`, both translated. The rest of the file was `System.Web`-only and unreadable on .NET 10: `<httpHandlers>`/`<handlers>` registering `System.Web.HttpNotFoundHandler` (whose job — refusing to serve `.cshtml` over HTTP — is now done by 7.4's static-file allow-list, which does not permit `.cshtml` at all and additionally denies the extension explicitly, §33.1), `webpages:Enabled` (no `System.Web.WebPages`), `<pages validateRequest="false">` (ASP.NET request validation does not exist — deferral 7.3-3), and `pageParserFilterType`/`pageBaseType`/`userControlBaseType`/`<controls>` pointing at MVC 5 Web Forms view types. |
| `Themes/DefaultClean/Views/Web.config` | **`Themes/DefaultClean/Views/_ViewImports.cshtml`**, created by this task — see §37.2. Byte-for-byte the same legacy content as above. |
| `Themes/DefaultClean/Content/css/ie8.css` | nothing — **dead asset**, see §37.4 |
| `Scripts/selectivizr.min.js` | nothing — **dead asset**, see §37.4 |
| `Scripts/respond.min.js` | nothing — **dead asset**, see §37.4 |

Both `Web.config` deletions also remove a live IIS hazard 7.4 had only been able to work around:
IIS parses **every** file literally named `web.config` in the served tree, and both declared a
`<configSections>` sectionGroup for an assembly that does not exist on .NET 10, so publishing either
produced **HTTP 500.19**. 7.4 suppressed that with interim `CopyToPublishDirectory="Never"` entries;
those entries are now **removed from `Nop.Web.csproj`** rather than left as dead no-ops, and publish
output was re-verified to contain neither path and exactly one `web.config` (the root ANCM shim).

### 37.4 The three IE8 assets — proven dead, then deleted

Task 7.3 removed the browser-capability branches from `Themes/DefaultClean/Views/Shared/Head.cshtml`
because `System.Web.HttpBrowserCapabilities` has no ASP.NET Core replacement (capability sniffing was
driven by `browscap.xml` and was dropped from the platform). Those branches were the **only**
consumer of these three files, so they are obsolete by platform change, not by preference.

**Proof of death before deletion**, not assertion: a repository-wide search across all file types
(excluding `.git`, `obj`, `bin`) for `ie8.css`, `selectivizr`, `respond.min.js`/`respond.js` returned
matches **only** inside the three files' own copyright banners, 7.3's explanatory comment in
`Head.cshtml`, and the two spec documents. Separately confirmed: **no**
`AppendCssFileParts`/`AddScriptParts`/`AppendScriptParts`/`AddCssFileParts` call anywhere in the
solution names any of them, and no CSS `@import`/`url()` pulls in `ie8.css` (the two `IE8` hits in
`jquery-ui-1.10.3.custom.css` are prose comments).

All three are IE8-only polyfills — selectivizr supplies CSS3 selectors to IE6–8, respond.js supplies
media queries, `ie8.css` patches IE8 layout — and IE8 cannot run this storefront regardless. They are
also inside 7.4's public static-file allow-list (`Scripts/**`, `Themes/<theme>/Content/**`), so
keeping them would ship and publicly serve three dead files in every deployment.

**`Themes/DefaultClean/Content/images/ie_warning.jpg` was checked and KEPT.** It is still live:
`Views/Shared/OldInternetExplorerWarning.cshtml` builds its path, and `Views/Shared/_Root.cshtml`
renders that partial via `@await Html.PartialAsync("OldInternetExplorerWarning")`. Dropping polyfills
while keeping the "your browser is too old" notice is coherent.

The stale forward-looking sentence in `Head.cshtml`'s comment (*"task 7.5 may delete them"*) was
updated to record what was actually done, including the note that the files are recoverable from git
history if a custom theme ever needs them.

### 37.5 Confirmed absent rather than assumed

The task text also asks for `Global.asax` and `RouteConfig`/`*Config` App_Start files. Verified
against the tree rather than taken on trust:

- **`Global.asax` / `Global.asax.cs`** — already deleted by task 7.2 (§20). Confirmed: zero
  `global.asax*` matches under `Nop.Web`.
- **`App_Start/`** — **no such directory has ever existed** in this project.
- **`RouteConfig.cs` / any `*Config.cs`** — **none exists** anywhere under `Nop.Web` (excluding
  `Administration/`). 3.90 put route registration in `Global.asax.cs`'s `RegisterRoutes` and the four
  `Infrastructure/*RouteProvider.cs` classes, not in App_Start; task 7.3 ported the latter to
  `IEndpointRouteBuilder`. So this clause of the task text had nothing to act on — recorded because
  "found nothing" and "did not look" are indistinguishable in a diff.
- **`Web.Debug.config` / `Web.Release.config`** — already deleted by task 7.4 (§33.6).
- **`web.config`** (root) — **KEPT**, deliberately. 7.4 reduced it to the IIS/ANCM shim carrying two
  live IIS settings (`urlCompression`, the `X-Powered-By` removal) and it is the file the SDK's
  `TransformWebConfig` merges the ANCM handler into. Its all-lowercase name is load-bearing (§31.2).
- **`Themes/DefaultClean/theme.config`** — **KEPT**. `ThemeProvider` reads it, and IIS does not parse
  it.

## 38. The 7.6 gate

### 38.1 No `System.Web*` in source — 0 real hits across 435 files, with a proven canary

Earlier tasks established that a naive `grep` for `System.Web` in this tree is meaningless: the
migration deliberately leaves explanatory prose naming the legacy types it replaced, and `Nop.Web`
currently contains 24 such textual matches, **all** in comments. The gate therefore used a scanner
that blanks `@* *@`, `/* */` and `//` comments — while tracking string, char and verbatim-string
literals so a `//` inside a URL is not mistaken for a comment — before searching the residue.

Tokens searched: the `System.Web.*` namespaces (`Mvc`, `Routing`, `Optimization`, `WebPages`,
`Helpers`, `Razor`, `Security`, `Caching`, `SessionState`, `UI`, `Hosting`, `Compilation`, `Http`),
bare `System.Web`, plus `HttpContext.Current`, `HttpContextBase`, `HttpPostedFileBase`,
`MvcHtmlString`, `ImageResizer`, `WebGrease`, `Autofac.Integration.Mvc`, `StackExchange.Profiling`,
`MiniProfiler`, `System.Data.Entity`, `System.Drawing.Common`, `System.Runtime.Caching` and
`System.Configuration`.

**The scanner was proven able to fail** before its clean result was trusted: a `_CanaryProbe.cs`
containing `using System.Web.Mvc;` was planted, the scanner reported it, and the file was removed.

Result on the real tree: **435 source files scanned, 0 real hits.**

### 38.2 No `System.Web*` in the emitted assembly — 0 banned of 59 references

Read from `Nop.Web.dll`'s **AssemblyRef metadata table** via `System.Reflection.Metadata`, which is
authoritative in a way that a text search over a binary is not. Identity: **`Nop.Web v3.9.0.0`**
(itself the confirmation that §37.1's `AssemblyInfo.cs` is in effect).

All 59 references are ASP.NET Core, `Microsoft.Extensions.*`, BCL, the four nopCommerce projects,
`Autofac`, `Autofac.Extensions.DependencyInjection`, `FluentValidation`, `Microsoft.Data.SqlClient`
and `System.ServiceModel.Syndication`. **Banned prefixes found: 0** — no `System.Web*`,
`ImageResizer`, `WebGrease`, `Autofac.Integration.*`, `MiniProfiler`, `StackExchange.Profiling`,
`EntityFramework` (EF6), `System.Runtime.Caching`, `System.Configuration`, `Antlr` or `Microsoft.Web.*`.
`deps.json` and the output directory agree, and EF Core is present under its own
`Microsoft.EntityFrameworkCore.*` names as expected.

### 38.3 One honest exception: `System.Drawing.Common` IS in the output

The gate checklist asks for no `System.Drawing.Common` in the emitted output. **It is there**, and
this is reported rather than glossed because the literal check fails even though nothing is wrong.

- It is a **direct `PackageReference` of no project.** `project.assets.json` shows exactly one
  incoming edge: **`EPPlus/4.5.3.3 → System.Drawing.Common`**.
- It resolves to **4.7.2**, not the 4.7.0 EPPlus asks for, because task 4.2 §10 pinned it centrally
  (with `CentralPackageTransitivePinningEnabled`) to clear **`NU1904` / GHSA-rxg9-xrhp-64gj /
  CVE-2021-24112** — CRITICAL remote code execution via a crafted metafile — which 4.7.0 carries.
  Removing the package would reintroduce a vulnerable one, not improve anything.
- **Nothing in the repository binds a type from it.** `Nop.Services` only names
  `System.Drawing.Color`, which lives in the cross-platform `System.Drawing.Primitives` in the
  net10.0 shared framework. `Nop.Web.dll`'s reference table has **no** entry for it.
- It is **pre-existing** — introduced at task 4.2, verified there, and untouched by 7.5, which
  changed no package file.

The runtime caveat from §10 is unchanged: `System.Drawing.Common` is Windows-only, EPPlus needs it
only for autofit column measurement and embedded images, and no call site uses either.

### 38.4 Publish audit

`dotnet publish -c Debug` was run and the output inspected, to confirm nothing deleted was
load-bearing and that 7.4's shaping still holds.

**Present:** `appsettings.json`; the transformed `web.config` with ANCM
(`AspNetCoreModuleV2`, `hostingModel="inprocess"`) **still merged alongside** 7.4's preserved
`urlCompression` and `X-Powered-By` elements, i.e. §31.2's case-sensitivity fix is intact;
`App_Data/browscap.xml`, `App_Data/Install`, `App_Data/Pdf/FreeSerif.ttf`;
`Content/Images/Thumbs/placeholder.txt` and `Content/files/ExportImport/Index.htm` (§31.3's fix
intact); `Scripts`, `Themes/DefaultClean/theme.config`, `preview.jpg`, `styles.css`; `favicon.ico`,
`ErrorPage.htm`, `FileNotFound.htm`.

**Absent:** `App_Data/Settings.txt` (the connection string), `App_Data/InstalledPlugins.txt`,
`App_Data/browscap.crawlersonly.xml`, **both** `Views/Web.config` paths, `launchSettings.json`,
`Web.Debug/Release.config`, the three IE8 assets, and **0** `.cs`/`.cshtml`/`.csproj` files. Exactly
one `web.config` exists in the publish tree.

Note that `.cshtml` files are correctly **absent**: the Razor SDK compiles views into the assembly
(195 compiled view types, including the new `Themes_DefaultClean_Views__ViewImports`), so a view's
absence from publish output is the expected ASP.NET Core behaviour and not a missing file.

## 39. NEW deferral opened by task 7.5

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7.5-1 | `_ViewImports.cshtml` is per-theme, so every future theme needs its own copy | 8.4 (awareness) / theme authors | Low |

### 7.5-1 A new theme will silently need its own `_ViewImports.cshtml`

`Themes/DefaultClean/Views/_ViewImports.cshtml` (§37.2) fixes the shipped theme. It **cannot** fix a
theme that does not exist yet: Razor's upward walk from
`Themes/<NewTheme>/Views/Shared/Foo.cshtml` reaches `Themes/` and the project root, neither of which
holds a `_ViewImports.cshtml`, so a new theme's view overrides get no `@inherits` and no `@using`.

- **Symptom:** `CS0103: The name 'T' does not exist in the current context` at build time on any
  theme view that uses the localizer — which is nearly every nopCommerce view. **Loud, not silent**:
  it is a compile error, which is why this is rated Low.
- **Fix:** copy `Themes/DefaultClean/Views/_ViewImports.cshtml` into the new theme's `Views/` folder.
  This is the same per-folder obligation 3.90 had — it shipped one `Views/Web.config` per theme view
  directory for exactly this reason — so it is parity, not a new burden.
- **A tempting alternative that does NOT work:** placing a single `_ViewImports.cshtml` at
  `Themes/_ViewImports.cshtml` or the project root would cover all themes, but it would also apply
  `@inherits WebViewPage<TModel>` to **every** `.cshtml` in the project, including
  `Administration/`'s ~325 views (task 8.x) which have their own base-type requirements. Not worth
  the coupling for a compile-time-visible error.
- **Note for task 8.4:** `Nop.Admin`'s views live under `Administration/Areas/Admin/Views/` and face
  the identical mechanism. 8.3/8.4 must create `_ViewImports.cshtml` for that tree from
  `Areas/Admin/Views/Web.config`'s `pageBaseType` + `<namespaces>`, and — per task 7.3's §27 finding
  — should do it **first**, since it is the single highest-leverage edit available (it took
  `Nop.Web` from 1974 errors to 24 in one file).

## 40. Deferrals explicitly NOT closed by 7.5, with the reason

| # | Item | Why not here |
|---|------|---|
| **35** | Minification gone, nothing replaces it | post-migration (design §8) |
| **7.2-1** | Startup fails fast on an unreachable database | 7.7 — needs a real database |
| **7.2-3** | `TaskManager.Instance.Stop()` never called on shutdown | none (3.90 parity) |
| **7.4-1** | A Kestrel-only deployment gets no response compression | deployment decision |
| **7.4-2** | Nop.Admin's `db_backups` publish exclusion and admin static assets | 8.1 / 8.5 — structurally out of reach from `Nop.Web.csproj` |
| **4.11** | `ExecuteSqlCommand` per-batch transaction during installation | 7.7 — needs a real database |
| **4.10** | `GO`-batched `CreateDatabaseScript()` in four plugin contexts | 11.2, 13.1, 14.4, 15.1 |
| **9 / 4.9** | `NopObjectContext` needs a real connection string | 3.4 (`Nop.Data.Tests` fixtures only) |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted; no override exists anywhere |
| **7.3-1** | The `Html.Action` bridge lives in `Nop.Web` but Nop.Admin and the plugins need it | 8.3 — recommend promoting it to `Nop.Web.Framework` |
| **7.3-2** | Session-stored `ProcessPaymentRequest.CustomValues` round-trips as `JsonElement` | 12.1–12.5 |
| **7.3-3** | ASP.NET request validation is gone | accepted; the feature does not exist to restore |
| **7.3-4** | 48 former `[ChildActionOnly]` actions are URL-reachable | 8.3 (re-targeted by 7.4 §32.2) |
| **7.3-5** | Server-side browser detection removed | accepted — and 7.5 deleted the three assets it orphaned (§37.4) |
| **7.3-6** | The child-action bridge does not run action filters | accepted |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision |
| **18.4** | `CA1416` at the two `Nop.Admin` `CommonController` call sites | 8.3 — 7.3 fixed the two `Nop.Web` ones |



---

# Nop.Web — the ASP.NET Core host smoke check (task 7.7)

Task 7.7 is the first task in this migration that **ran** the ported application. Groups 2–7 were
validated by compilation only; apart from the 4 fixtures in `src/Tests/Nop.Tests`, no ported line
had ever been executed, and roughly 40 entries in this register were marked RESOLVED on the strength
of code inspection or of throwaway probes run against **synthetic** host applications.

It found **five real defects**, of which **three were release blockers that made nopCommerce
impossible to install**, and it corrected the recorded status of **two deferrals that were marked
RESOLVED but were not**.

| Measurement | Value |
|---|---|
| new project | `src/Tests/Nop.Web.SmokeTests` — `WebApplicationFactory<Nop.Web.Program>` + NUnit 3.14.0, 49 tests |
| production source files changed to enable testing | **0** — `Program` was already a `public class` with a conventional `Main`, so no `partial` accommodation was needed |
| suite result, store installed | **36 passed / 0 failed / 13 skipped** |
| suite result, store not installed | **32 passed / 0 failed / 17 skipped** |
| harness proven able to fail | **yes** — 3 `[Explicit]` canaries, one per mechanism, all observed Failed (§41.2) |
| gate re-verification (`--no-incremental`, `obj`/`bin` removed) | `Nop.Core` **0**/3 · `Nop.Data` **0**/3 · `Nop.Services` **0**/10 · `Nop.Web.Framework` **0**/10 · `Nop.Web` **0**/15 — every one matching its recorded baseline, no warning added |
| `Nop.Tests` | **4 passed / 0 failed** — unchanged |
| swallowed-diagnostics check | `"converted to a warning"` **0** on all seven builds |

Environment: SQL Server 2022 (`mcr.microsoft.com/mssql/server:2022-latest`) on a Docker network
shared with the `mcr.microsoft.com/dotnet/sdk:10.0` build container. A store was installed **through
the real installer form**, with sample data, which is what surfaced the three blockers.

## 41. How the check is built, and why it can be trusted

### 41.1 It starts the real application, not a facsimile

`WebApplicationFactory<TEntryPoint>` resolves `Nop.Web.Program.Main` through
`HostFactoryResolver` and invokes it with `stopApplication: false`, so **`Main` runs to completion of
`app.Run()`**: the pre-container static seams, `AddNopFramework`, `AutofacServiceProviderFactory`,
`NopHostedEngine.RegisterInto`/`RunStartupTasks`, `InitializeDatabaseSchema`,
`StartScheduledTasks`, `LogApplicationStart`, then the pipeline in its real order. Only `IServer` is
substituted (Kestrel → `TestServer`), so no socket is bound and nothing survives disposal.

The content root is pinned through `ASPNETCORE_CONTENTROOT` **and then asserted**, because a wrong
one would make most of the suite pass vacuously: `UseNopHostingEnvironment` assigns it to
`CommonHelper.BaseDirectory`, which is what `MapPath`, `DataSettingsManager` (the connection string),
`PluginManager`, `ThemeProvider` and `NopStaticFileProvider` all resolve against.

The **only** modification to the application under test is one `IStartupFilter` that prepends
`SmokeProbeMiddleware`. It handles `/__smoke/*` and passes everything else through untouched before
doing anything else, so no other request sees a different pipeline. It exists because four things
7.7 must verify are observable only from inside a live request and no nopCommerce controller exposes
them: per-request Autofac scope sharing, `LinkGenerator` route generation, `IUserAgentHelper`, and
the endpoint table.

### 41.2 Canaries — the harness was proven able to fail before its green run was believed

`HarnessCanaryTests` is an `[Explicit]` fixture with one deliberately-false assertion per mechanism
the suite depends on: a real HTTP request through `TestServer`, a resolve through the real Autofac
container, and the probe middleware's text output. Run on demand:

```
dotnet test src/Tests/Nop.Web.SmokeTests --filter "FullyQualifiedName~HarnessCanaryTests"
```

Observed: **3 failed / 0 passed**, as required. This matters because this migration has twice been
bitten by silently-swallowed failure — the Six Labors licence task failing on every compile under
`ContinueOnError=true`, and Roslyn hiding a real `CS1929` in `FilePermissionHelper` across three
completed tasks.

### 41.3 Test outcomes are `Ignore`d, never quietly passed

Install-mode tests skip when a database **is** present; storefront tests skip when one is **not**,
with the reason in the message (`"NOT EXERCISED: no database is installed …"`). No assertion was
weakened to make it runnable in the wrong state.

---

## 42. BLOCKERS FOUND AND FIXED — nopCommerce could not be installed

All three were found by POSTing the real installer form. Each was fixed and the install re-run;
installation now completes with sample data and the storefront serves.

### 42.1 Deferral 4.8 was marked RESOLVED and was not — the schema initializer still never ran

**Symptom, measured:** `Setup failed: Entity: Store State: Added … Invalid object name 'Store'.`
— i.e. deferral 4.8's predicted failure, verbatim, on a first install.

**Why 7.2's fix could not work.** `Nop.Web/Program.InitializeDatabaseSchema()` opens with
`if (!DataSettingsHelper.DatabaseIsInstalled()) return;` and runs **once, at host startup**. The case
deferral 4.8 describes is *installing onto an empty database*, and at startup a not-yet-installed
store has no `App_Data/Settings.txt`, so the method early-returns. By the time the installer has
written that file, startup is long past. On every later start the store *is* installed and the
initializer short-circuits on its own table probe. **The call was inert in both directions** — which
a clean compile cannot detect and which no amount of code review had caught.

**Fix.** `Nop.Data.SqlServerDataProvider.InitDatabase()` now calls a new
`protected virtual CreateDatabaseSchema()` after `SetDatabaseInitializer()`. That is the correct
place and not an arbitrary one: `InitDatabase()` has exactly one caller,
`InstallController.Index(InstallModel)`, immediately after the installer writes `Settings.txt` and
immediately before `IInstallationService.InstallData(...)` — and it is where EF6's
`Database.SetInitializer` hook was registered and, moments later, fired. The context is resolved
through `EngineContext` (as `CommonHelper.MapPath` already is a few lines above) rather than
injected, because `IDataProvider.InitDatabase()` is parameterless and changing it would break
Nop.Core, Nop.Web.Framework and every plugin data provider for no behavioural gain.

**`Program.InitializeDatabaseSchema()` is KEPT**, downgraded to a safety net for the one residual
case the installer cannot cover — a hand-written or copied `Settings.txt` pointing at an empty
database — and made non-fatal (see §43.1).

### 42.2 Deferral 4.12's severity was wrong — the join column names break the stored procedures

**Symptom, measured:** `Setup failed: Invalid column name 'ProductTag_Id'. Invalid column name 'Product_Id'.`

Deferral 4.12 recorded that EF Core's skip-navigation convention names the join FK columns
`<Navigation>Id` where EF6 named them `<Entity>_<Key>`, and concluded "Fix if schema parity against
an existing 3.90 database is required … Not required for the compile gate." Both halves are
literally true and together they understate the problem: **`App_Data/Install/SqlServer.StoredProcedures.sql`
joins these columns by their 3.90 names**, in `ProductLoadAllPaged`, `ProductTagCountLoadAll` and
`CustomerLoadAllPaged`'s Guests-role test, and `App_Data/Install/Fast/create_*.sql` does the same.
So creating the stored procedures against an EF Core-generated schema fails, and installation aborts.
Even if it had not, product search and tag counts would have been broken at runtime.

**Fix.** All eight join tables now pin their column names via
`j.Property<int>("<EfName>").HasColumnName("<3.90 name>")`, exactly as deferral 4.12 itself
recommended. Only the *column* name is pinned; the shadow property keeps its EF Core name, so no
query or navigation code changes.

| Map | Table | EF Core produced | Now |
|---|---|---|---|
| `Catalog/ProductMap` | `Product_ProductTag_Mapping` | `ProductsId`, `ProductTagsId` | `Product_Id`, `ProductTag_Id` |
| `Customers/CustomerMap` | `Customer_CustomerRole_Mapping` | `CustomerId`, `CustomerRolesId` | `Customer_Id`, `CustomerRole_Id` |
| `Customers/CustomerMap` | `CustomerAddresses` | `CustomerId`, `AddressesId` | `Customer_Id`, `Address_Id` |
| `Security/PermissionRecordMap` | `PermissionRecord_Role_Mapping` | `PermissionRecordsId`, `CustomerRolesId` | `PermissionRecord_Id`, `CustomerRole_Id` |
| `Shipping/ShippingMethodMap` | `ShippingMethodRestrictions` | `RestrictedShippingMethodsId`, `RestrictedCountriesId` | `ShippingMethod_Id`, `Country_Id` |
| `Discounts/DiscountMap` | `Discount_AppliedToCategories` | `AppliedDiscountsId`, `AppliedToCategoriesId` | `Discount_Id`, `Category_Id` |
| `Discounts/DiscountMap` | `Discount_AppliedToManufacturers` | `AppliedDiscountsId`, `AppliedToManufacturersId` | `Discount_Id`, `Manufacturer_Id` |
| `Discounts/DiscountMap` | `Discount_AppliedToProducts` | `AppliedDiscountsId`, `AppliedToProductsId` | `Discount_Id`, `Product_Id` |

The EF Core names were read off the live database, not guessed; the 3.90 names were confirmed
against the column names the installer scripts join by.

### 42.3 NEW — EF Core's foreign-key index naming collides with 15 of nopCommerce's own indexes

**Symptom, measured:** `Setup failed: The operation failed because an index or statistics with name
'IX_StateProvince_CountryId' already exists on table 'StateProvince'.` — and because the custom
script aborts at the first failure, **none of the 57 later indexes was created either.**

**Root cause.** EF Core's `ForeignKeyIndexConvention` names its automatic FK index
`IX_<Table>_<Column>`; EF6's named it `IX_<Column>`. `CreateTablesIfNotExist` runs
`Database.GenerateCreateScript()` and then the 61 hand-written statements in
`App_Data/Install/SqlServer.Indexes.sql`, and 15 of those names now collide. Measured on the live
database: 100 non-primary-key indexes, of which 97 came from EF Core.

**Fix.** Every `CREATE INDEX` in `SqlServer.Indexes.sql` is now preceded by a guarded
`DROP INDEX`, using `sys.indexes`/`OBJECT_ID` rather than `DROP INDEX IF EXISTS` (which needs SQL
Server 2016+, where 3.90 supports 2008+). **Drop-and-create, not `IF NOT EXISTS`**, deliberately:
nopCommerce's definitions must win, because several are strictly richer than the convention index
they collide with — `IX_StateProvince_CountryId` carries `INCLUDE ([DisplayOrder])`, and skipping the
`CREATE` would have silently kept EF Core's narrower version. That is exactly the quiet degradation
this register exists to prevent. The guard also makes the script idempotent, which the 2.x upgrade
path needs.

**Left open for a schema review, and NOT decided here:** EF Core adds ~97 indexes that 3.90's schema
did not ask for, with the storage and write-amplification cost that implies on every table. Removing
`ForeignKeyIndexConvention` outright would restore the 3.90 index set but would also drop the FK
indexes EF6 *did* create under its own naming, so it is a genuine physical-design decision rather
than a parity fix. Recorded as **deferral 7.7-3** below.

### 42.4 NEW — eight filesystem paths break on any case-sensitive filesystem

The migration deliberately targets cross-platform `net10.0` (design §7), and design §4 says the
application is "tested on Windows first". Nothing had ever run it on Linux. Eight paths are spelled
differently from the directories on disk, and NTFS's case-insensitive lookup was hiding all of them.
This is the same defect class task 7.4 found when it renamed `Web.config` to `web.config`
(§31.2) — that fix was treated as a one-off; it was not.

| File | Asked for | On disk | Consequence on Linux |
|---|---|---|---|
| `Nop.Services/Media/PictureService.cs` ×3 | `~/content/images`, `~/content/images/thumbs` | `Content/Images`, `Content/Images/Thumbs` | **the entire filesystem picture store is dead** — `Directory.GetFiles` throws `DirectoryNotFoundException` on every picture delete, and no image or thumbnail can be written |
| `Nop.Services/Installation/CodeFirstInstallationService.cs` ×4 | `~/content/samples/` | `Content/samples` | **installing sample data fails**: `Could not find a part of the path '…/content/samples/category_computers.jpeg'` (measured) |
| `Nop.Services/Common/PdfService.cs` ×1 | `~/content/files/ExportImport` | `Content/files/ExportImport` | PDF-to-file export throws `DirectoryNotFoundException` |
| `Nop.Web/Views/Install/Index.cshtml` ×3 | `~/Content/Install/style.css`, `…/style.rtl.css`, `~/content/install/images/…gif` | `Content/install/…` | **the installation page renders with no stylesheet.** Measured: `GET /Content/Install/style.css` → 302 `/install`, `GET /Content/install/style.css` → 200 |

All eight are corrected to the on-disk casing. Two systematic audits were then written and run to
prove the class is closed, not just the instances:

- **every `MapPath("~/…")` literal** in `Nop.Core`/`Nop.Data`/`Nop.Services`/`Nop.Web.Framework`/`Nop.Web`
  (22 of them, comments excluded, `Administration/` excluded) resolved case-exactly → **0 mismatches**;
- **every `~/Content|Scripts|Themes/…` asset reference** in Nop.Web's views, stylesheets and scripts
  (30 of them) → **0 mismatches**. The four apparent misses are `Themes/{0}/…` format strings
  resolved at runtime with the theme name.

A permanent regression test, `Deferral_40_the_install_page_only_links_assets_that_actually_serve`,
now walks every same-origin asset the install page emits and requires a 200 from each.

**NOTE FOR GROUP 8:** `Administration/` was excluded from both audits (task 7.7's scope boundary).
Nop.Admin has ~325 views and its own `Content/`/`Scripts/` trees. **Task 8.4/8.5 should re-run both
audits over `Administration/`** — on this evidence it will find instances. Recorded as **deferral
7.7-4**.

---

## 43. Deferrals RESOLVED by task 7.7

| # | Deferral | How, and how it was proved |
|---|---|---|
| **4.8** | Schema initializer never invoked | §42.1. Proved by a real install completing where it previously failed with `Invalid object name 'Store'` |
| **4.12** | Join column names | §42.2. Proved by stored-procedure creation succeeding where it previously failed with `Invalid column name 'ProductTag_Id'` |
| **7.2-1** | Startup fails fast on an unreachable database | §43.1 — and 7.2-1 had only **half** the cause |
| **1.1** | Plugin discovery never runs | `PluginManager.ReferencedPlugins` is non-null in the running host, and `~/Plugins` + `~/Plugins/bin` exist. Count is **0**, which is the correct answer today: no plugin project has been migrated (groups 10–15), so there is no `Description.txt` to find. What is proved is that the scan **ran** |
| **1.3 / 3** | Per-request DI scope not shared | `EngineContext.Current.ContainerManager.Container` is **reference-equal** to `app.Services.GetAutofacRoot()` (one container), `CurrentScopeProvider` is assigned and returns the request's own `ILifetimeScope`, and two `IWebHelper` resolves inside one request return the **same instance**, which is also the instance `RequestServices` yields |
| **1.5** | `MapPath` resolves under `bin/` | `CommonHelper.BaseDirectory` equals the content root in the running host and `MapPath("~/App_Data/")` lands inside it |
| **1.6** | `RestartAppDomain` throws | `IHostApplicationLifetime` resolves from the nop container, and the installer's own `webHelper.RestartAppDomain()` demonstrably stopped the host at the end of installation (`Application is shutting down…`) |
| **4 / 7.2-4 / 1.4** | Configuration unset | All 14 `NopConfig` properties, the one live `appSettings` key, the three deliberately-commented-out load-balancer keys (still null), and all 6 `Authentication` values assert correct **off the Autofac singleton and off the live `CookieAuthenticationOptions`** — including `ExpireTimeSpan == 30 days`, which is the specific 30-minute silent fallback deferral 7.13 warned about |
| **7.13 / 11.25** | Cookie authentication | scheme registered, all four `<forms>` values carried, `SecurePolicy == SameAsRequest` (3.90 shipped `requireSSL="false"`) |
| **7.14 / 7.15 / 11.26 / 11.28 / 14.31** | `IHttpContextAccessor`, `ISessionStore`, `IAntiforgery`, `ITempDataProvider`, `IFileVersionProvider` | all resolve from the running host |
| **11.20** | FluentValidation not wired — **SECURITY** | Proved twice by execution, not by inspection: a token-less POST is refused, and a POST with a valid token and `Email=not-an-email` re-renders with `Wrong email` and the password-mismatch message. Separately, an empty-credentials install POST returns `Enter admin email` / `Enter admin password` / `Enter confirm password` — three messages that exist **only** in `InstallValidator` |
| **11.21 / 11.22** | Metadata and model-binder providers | present in `MvcOptions`; `NopModelBinderProvider` at index 0; the implicit-required suppressor asserted to sit **after** `DataAnnotationsMetadataProvider` |
| **11.23** | `JsonResult` camelCase | `PropertyNamingPolicy == null` |
| **14.30** | Theming expander | `ThemeableViewLocationExpander` at index 0, and the storefront demonstrably renders through `Themes/DefaultClean` |
| **32** | No `Widget` view component | the home page and `/cart` render with every widget zone empty, no exception, and no stray `widget-zone` wrapper |
| **33 / 14.33** | Cache busting inert | 9 emitted asset URLs all carry a real `?v=<sha256>` **and** all return 200 — e.g. `/Scripts/public.common.js?v=LlDNzco6ovFC0YjsIzhK3g8z0nSru9WmU5HJLfAqh3k` |
| **39 / 7.1-4** | Publish/serve of secrets — **SECURITY** | with `App_Data/Settings.txt` present **and containing a real connection string**, `GET /App_Data/Settings.txt`, `/appsettings.json` and `/web.config` all return **404** and no body contains `Data Source` |
| **7.20** | `IsSearchEngine()` dead | Googlebot → `True`, Chrome → `False`. Cold first call (46 MB `browscap.xml` parse) **1171 ms**, warm 19 ms, and `App_Data/browscap.crawlersonly.xml` is written — so App_Data is writable and the first request is slow but not pathological, which is what task 7.4 asked 7.7 to confirm |
| **§28.1** | The seven `SuppressMatchingMetadata` routes | Verified against the **real** endpoint set, where 7.3 could only use a synthetic probe app: all `{SeName}` endpoints carry `SuppressMatchingMetadata`, `LinkGenerator` still returns `/smoke-product-slug` for all seven, `/cart` is not swallowed by `{generic_se_name}`, and the live `searchtermautocomplete` JSON contains `"producturl":"/apple-macbook-pro-13-inch"` generated by `Url.RouteUrl("Product", …)` |
| **7.3-1** | The `Html.Action` child-action bridge | **the item task 7.3 called the single most important thing for 7.7 to exercise.** The home page's fan-out is proved by markup only each child action's partial can emit: `header-logo` (Common/Logo), `header-links` (Common/HeaderLinks), `top-menu` (Catalog/TopMenu), `footer` (Common/Footer), `flyout-cart` (ShoppingCart/FlyoutShoppingCart), `search-box` (Catalog/SearchBox). Note the deferral itself — the bridge lives in `Nop.Web` and Nop.Admin needs it — **remains open for task 8.3** |

### 43.1 Deferral 7.2-1 — RESOLVED, and it had a second cause nobody had identified

7.2-1 said startup now fails fast on an unreachable database and assigned the accept-or-defer
decision to 7.7. Verified by execution with a `Settings.txt` naming a non-existent host:
`Unhandled exception. Nop.Core.NopException: No database instance`, process **exit code 134**.

Making `InitializeDatabaseSchema()` non-fatal was not enough. Re-measured, the host still died —
now at `Program.StartScheduledTasks → TaskManager.Initialize → ScheduleTaskService.GetAllTasks`,
which reads the `ScheduleTask` table and therefore cannot succeed while the database is unreachable.
**7.2-1 attributed the behaviour solely to `InitializeDatabaseSchema()`; that was incomplete.**

**Decision: both are now non-fatal**, and this is a reversal of 7.2's "deliberately not swallowed".
The reasoning:

- provisioning is now owned by the installation path (§42.1), so the startup call is a safety net
  rather than the mechanism, and a net must not be more dangerous than what it guards;
- neither throw distinguishes "empty database" from "database briefly unreachable", and under
  ANCM/systemd/an orchestrator the second becomes a restart loop with the real cause buried in a
  crash log;
- **this is not 3.90 parity.** In System.Web a throw from `Application_Start` failed only the
  triggering request and ASP.NET re-ran `Application_Start` on the next one — the worker process
  survived and the site recovered by itself. An exception out of `Program.Main` terminates the
  process.

Bounded, visible cost: scheduled tasks do not start for the lifetime of that process and an operator
must restart it once the database is healthy. Verified after the change: the host **starts and stays
up** with an unreachable database (the run had to be killed by `timeout`).

### 43.2 Confirmations that were previously inference

- **`NopErrorLoggingMiddleware` works, including the `Log404Errors` rule** — the `Log` table
  contains `Error 404. The requested page (…) was not found.` rows for the 404s this suite provoked.
  That is the half of `Application_Error` that could not have been exercised by a compile, and it
  confirms the design decision to observe `Response.StatusCode == 404` rather than an exception.
- **`LogApplicationStart()` works** — `Application started` rows, level 20, in the same table.
- **The 404 path works end to end** — an unknown slug returns **404** with the re-executed
  `Common/PageNotFound` view (`html-not-found-page`), at the original URL.
- **`robots.txt` and `sitemap.xml`** return 200 with `text/plain` and a real `<urlset>`/`<loc>`,
  exercising task 7.3's `Response.Write` → `Content(...)` port and the
  `ISitemapGenerator` `UrlHelper` → `IUrlHelper` signature change.
- **Deferral 4.7b (entity JSON cycles)** — the two reachable JSON endpoints tested
  (`searchtermautocomplete`, `getstatesbycountryid`) return well-formed JSON. nopCommerce's
  project-to-view-model convention holds on those paths. Not a general clearance; see §44.
- **Deferral 7.3-3 (request validation gone)** — asserted: a `<script>` payload is accepted by the
  framework (3.90 would have thrown) and is **HTML-encoded** on the way back out, which is the
  control that actually matters.

---

## 44. NEW deferrals opened by task 7.7

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 7.7-1 | ~~A refused XSRF POST answers **404**, not 400~~ | — | ✅ **RESOLVED ahead of 8.3** (§45.2) — `Program.cs` now calls `UseNopStatusCodePages()` |
| 7.7-2 | The smoke project is not in `NopCommerce.sln` | 18.1 | Low |
| 7.7-3 | EF Core adds ~97 unrequested foreign-key indexes | schema review (post-migration) | Low–Medium |
| 7.7-4 | The two case-sensitivity audits have not been run over `Administration/` | 8.4 / 8.5 | Medium |
| 7.7-5 | Large parts of the application are still unexercised | 8.x / 16.x / 17.x | — (scope note) |

### 7.7-1 A refused XSRF POST answers 404 instead of 400 — ✅ RESOLVED, see §45.2

`PublicAntiForgeryAttribute` behaves correctly: a token-less POST is refused and the action does not
run. But `BadRequestResult` produces a **bodiless 400**, and `Program.cs` registers
`UseStatusCodePagesWithReExecute("/page-not-found")`, which fires for *any* empty-bodied 4xx/5xx and
re-executes `CommonController.PageNotFound` — which sets `Response.StatusCode = 404`. So the client
sees **404 "Page not found"** where it should see 400.

Task 7.2 recorded this imprecision (§23) but judged only "the uncommon bare 403/400" affected. It is
not uncommon: it hits **every** XSRF refusal on the public store today (`/register`, `/contactus`,
and every other `[PublicAntiForgery]` action — measured: `POST /login`, which has no
`[PublicAntiForgery]`, returns 200 while `POST /register` and `POST /contactus` return 404), and it
will hit the whole admin surface once `[AdminAntiForgery]` is live at task 8.3.

- **Not a security hole.** The request is refused and the action does not execute; both halves are
  asserted by tests.
- **Fix:** restrict the re-execute to 404 — either `UseStatusCodePages` with a predicate, or a small
  middleware that re-executes `/page-not-found` only for 404 and lets other statuses keep their own
  status and body. Two tests pin the current behaviour and will flip when it is fixed:
  `Deferral_11_26_a_token_less_POST_is_refused_SECURITY` and
  `Deferral_7_7_1_a_refused_XSRF_post_answers_404_instead_of_400_KNOWN_GAP`.

### 7.7-2 The smoke project is not in the solution

`src/Tests/Nop.Web.SmokeTests/Nop.Web.SmokeTests.csproj` is not referenced by `NopCommerce.sln`, so
`dotnet build NopCommerce.sln` does not build it. The solution file is **task 18.1's** and was
deliberately not edited here. 18.1 should add it. It must **not** become part of a clean-compile
gate: task 7.7 is non-gating by design and the suite requires a database for 13 of its 49 tests.

### 7.7-3 EF Core adds ~97 unrequested foreign-key indexes

Measured on a freshly installed database: 100 non-primary-key indexes, of which **97** came from
`Database.GenerateCreateScript()` and only 3 from nopCommerce's own script at the point of failure.
3.90's physical design is the 61-statement `SqlServer.Indexes.sql`; EF Core's
`ForeignKeyIndexConvention` roughly doubles the index count, with storage and write-amplification
cost on every insert and update.

This was **not** decided at 7.7, because it is not a parity fix: EF6 also created FK indexes, under
its own `IX_<Column>` naming, so removing the convention would drop indexes 3.90 had rather than
restore them. Deciding it needs a real schema diff against a 3.90 database. The mechanism, if wanted,
is one override — `ConfigureConventions(ModelConfigurationBuilder)` +
`Conventions.Remove(typeof(ForeignKeyIndexConvention))` — plus a review of which FK indexes to
re-declare explicitly.

### 7.7-4 The case-sensitivity audits have not covered `Administration/`

§42.4 found 8 defects in 52 audited references and closed the class for everything task 7.7 owns.
`Administration/` was excluded by scope. It has ~325 views and its own asset trees, and its
`RoxyFilemanController` does path arithmetic of its own. **Tasks 8.4 and 8.5 should re-run both
audits over it** (every `MapPath("~/…")` literal, and every `~/Content|Scripts|Themes/…` reference in
views and stylesheets), checked case-exactly against the filesystem. Note this interacts with
deferral 7.4-2: the admin asset trees do not serve at all yet, so a case defect there is currently
masked by a more basic one.

### 7.7-5 What is still unexercised — scope note, not a defect

Stated so a green suite is not mistaken for coverage.

- **The whole admin UI.** Not started (group 8). Task 7.7's only admin assertion is the known gap
  that its static assets 404 (deferral 7.4-2).
- **All 20 plugins.** Not migrated. Plugin discovery ran and correctly found zero, so nothing
  exercises `IWidgetPlugin`/`IPaymentMethod`/`IExternalAuthenticationMethod` — which is precisely
  where the `Html.Action` bridge's five runtime-named call sites live (deferral 7.3-1), where plugin
  Razor view compilation lives (deferral 1.2), and where `ProcessPaymentRequest.CustomValues`
  round-trips through the session (deferral 7.3-2).
- **Authentication end to end.** The cookie handler is registered and its options verified, but no
  customer was signed in, so `FormsAuthenticationService.SignIn`/`SignOut` and
  `GetAuthenticatedCustomer()` are not proved. Registration was only exercised on its *rejection*
  path.
- **Checkout, orders, payment, email.** Not touched.
- **Deferral 4.11** (`ExecuteSqlCommand` per-batch transactions during installation) is **still
  open**. It concerns `SqlFileInstallationService.ExecuteSqlFile`, which is the **Fast** installer
  (`NopConfig.UseFastInstallationService`); task 7.7 installed with the default
  `CodeFirstInstallationService`, so that path never ran.
- **Deferral 4.7 / 4.7a (lazy loading)** is exercised only incidentally by the pages that rendered.
  No test isolates a navigation walk, and the disposed-context and `AsNoTracking` edge cases in
  4.7a are not covered.
- **Image processing.** `PictureService`'s ImageSharp path is *reachable* again on Linux after
  §42.4, but no test generates a thumbnail. Sample-data installation did import ~90 pictures, which
  is why the case defect surfaced at all — but the resize/encode path was not asserted.
- **Windows.** Everything here ran on Linux. `FilePermissionHelper`'s ACL surface
  (`[SupportedOSPlatform("windows")]`, deferral 18.4) is skipped by `OperatingSystem.IsWindows()`
  and is therefore untested in both directions.



---

# Two defect fixes pulled forward out of task 8.3 — deferrals 7.3-4 and 7.7-1

**Not a numbered spec task.** Both items were assigned to task **8.3** (`Nop.Admin` controllers) on
the reasoning that the identical decision has to be made for the ~325-view admin surface. Both were
implemented early, on the user's approval, in **`Nop.Web.Framework`** — so that group 8 ports its
277 controllers and 325 views onto correct infrastructure instead of needing a second pass over the
whole admin surface once the mechanism finally exists. `tasks.md` step 8.3 has been rewritten
accordingly: for 7.3-4 its remaining job is only to apply the marker to the admin actions, and for
7.7-1 there is nothing left to do.

| Measurement | Value |
|---|---|
| `Nop.Core` / `Nop.Data` / `Nop.Services` / `Nop.Web.Framework` / `Nop.Web` re-gate, `--no-incremental` after `rm -rf obj bin` | **0/3 · 0/3 · 0/10 · 0/10 · 0/15** — every project matching its recorded baseline exactly, **no warning added** |
| `Nop.Web.SmokeTests`, no database | **48 passed / 0 failed / 18 skipped** (was 32/0/17). All 32 previously-passing tests still pass; **+16** new passing, **+1** new skipped |
| `HarnessCanaryTests` (`[Explicit]`) | **4 failed / 0 passed** — a fourth canary was added for the new probe (§45.3) |
| `Nop.Tests` | **4 passed / 0 failed** — unchanged |
| files touched | 3 new in `Nop.Web.Framework`, 2 modified (`NopServiceCollectionExtensions`, `NopApplicationBuilderExtensions` doc), `Nop.Web/Program.cs`, 16 `Nop.Web` controllers (+62 lines, all attribute/`using`), `Nop.Web/Components/WidgetViewComponent.cs` (doc correction), 5 smoke-test files |
| `Administration/` | **untouched** — group 8 owns it |
| `Validators/` | **untouched** — the 5 `CS0618` FluentValidation warnings are design §9's deliberate pin |

## 45. What changed

### 45.1 Deferral 7.3-4 — RESOLVED

**New:** `src/Presentation/Nop.Web.Framework/Mvc/NopChildActionOnlyAttribute.cs` and
`src/Presentation/Nop.Web.Framework/Mvc/NopChildActionOnlyConvention.cs`.
**Registered** in `NopServiceCollectionExtensions.AddNopFramework`'s `MvcOptions` delegate:
`options.Conventions.Add(new NopChildActionOnlyConvention());`.

The attribute is **inert** — it carries no behaviour and is purely a selector for the convention,
which adds `Microsoft.AspNetCore.Routing.SuppressMatchingMetadata` to every `SelectorModel` of a
marked action. That is the mechanism §28.1 established and measured for
`GenericUrlRouteProvider`'s seven name-only routes, and it is the only one that expresses "never
match": ordering tricks can only say "match later", and §28.1 already showed that forcing a shared
`Endpoint.Order` **creates** `AmbiguousMatchException` rather than preventing it.

**The 48 actions were identified from git history, not by inspection of the current tree.**
`git grep -n ChildActionOnly 9cb503f -- 'src/Presentation/Nop.Web/*'` (the pre-task-7.3 commit),
excluding `Administration/`, returns exactly 48 hits. A script applied `[NopChildActionOnly]` to
those 48 methods, and a second, independently written script then compared the two sets by
`(file, method)` — parsing attribute blocks upward from each `public virtual ActionResult` signature
in both the old and new trees:

```
pre-7.3 [ChildActionOnly] actions : 48
now [NopChildActionOnly] actions  : 48
missing (had it, not marked)      : []
EXTRA  (marked, never had it)     : []
```

The "EXTRA" line is the one that matters: marking an action that was never `[ChildActionOnly]`
would **delete a legitimate URL endpoint**. `ProfileController.Info` is marked and
`CustomerController.Info` and `VendorController.Info` are not, which is exactly the trap a
name-based application of the marker would fall into — so a parameterised test asserts
`Customer.Info` is still matchable.

#### The critical constraint: the `Html.Action` bridge still sees these actions — verified, not reasoned

Task 7.3's `Nop.Web/Extensions/ChildActionExtensions.cs` resolves child actions through
`IActionDescriptorCollectionProvider` and invokes them by reflection; it never touches the matcher.
So suppressing *matching* should be invisible to it. "Should" is not evidence, and the downside of
being wrong is severe — the home page's ~15 child actions would stop rendering, trading a minor
information exposure for a broken storefront. A new `/__smoke/action` probe therefore reports, for
one `Controller.Action`, **both** the endpoint table and the descriptor collection, and a
parameterised test asserts both. Measured for `Common.Footer`:

```
endpointCount=1
endpoint pattern={controller=Home}/{action=Index}/{id?} suppressMatching=True
matchableEndpointCount=0
actionDescriptorCount=1
visibleToChildActionBridge=True
```

and for `Widget.WidgetsByZone`, which has an explicit route as well as the `Default` one:

```
endpointCount=2   (widgetsbyzone/ and {controller=Home}/{action=Index}/{id?})
matchableEndpointCount=0
visibleToChildActionBridge=True
```

Two properties fall out of that and are worth stating for task 8.3:

- **Suppression is scoped to the ACTION, not to one route.** Every endpoint MVC builds for a marked
  action is suppressed, including ones from an explicit `MapControllerRoute`. That is correct: a
  `[ChildActionOnly]` action was unreachable by URL in 3.90 no matter which route reached it.
- **The endpoint still exists in the table** (the endpoint count did not change: 398 default-route
  endpoints before and after), it is simply not a match candidate. Nothing else is affected —
  `ISuppressLinkGenerationMetadata` is a different marker, and neither touches
  `ActionDescriptor`s.

#### One in-tree behavioural note, and a doc correction

`WidgetController.WidgetsByZone` is one of the 48 **and** is the target of `RouteProvider`'s named
`widgetsbyzone/` route. Task 7.3's remark on `WidgetViewComponent` claimed that URL endpoint "is
part of the public surface and a plugin or script may call it". **That claim was wrong about 3.90:**
MVC 5's `[ChildActionOnly]` threw `InvalidOperationException` for a non-child invocation, so
`GET /widgetsbyzone/` answered **500** in 3.90 and the route was only ever usable for URL
*generation*. Marking the action is therefore the faithful choice; the URL now answers 404 instead
of 500. Verified that nothing in the tree requests that path (grep across views, scripts and
controllers). The `WidgetViewComponent` remark has been corrected in place rather than left to
mislead task 8.x.

#### What is deliberately NOT restored

MVC 5 threw on a direct URL request; this yields a **404**. 404 is the better answer — it does not
disclose that the action exists but may not be called that way — and it is what the endpoint-routing
mechanism gives. The observable difference from 3.90 is 500 → 404 on a request that was already
refused.

### 45.2 Deferral 7.7-1 — RESOLVED

**New:** `src/Presentation/Nop.Web.Framework/Infrastructure/NopStatusCodePagesExtensions.cs`.
`Program.cs`'s `app.UseStatusCodePagesWithReExecute("/page-not-found")` became
`app.UseNopStatusCodePages()`, at the same pipeline position.

`UseNopStatusCodePages` registers the **stock** re-execute middleware — re-execution has to preserve
`PathBase`, the original path and query through `IStatusCodeReExecuteFeature`, and clear the matched
endpoint and route values, and re-deriving that by hand is pure risk — and then, **immediately
inside it**, `NopStatusCodePagesScopeMiddleware`, which after `_next` sets
`IStatusCodePagesFeature.Enabled = false` for any status that is not 404. Because the filter is
inside, it runs first on the way back out and takes the decision with the final status code in hand;
`StatusCodePagesMiddleware` then reads the feature and does nothing. That feature is the
framework's sanctioned per-response opt-out, which is why it was preferred over the alternative
that also works — writing a placeholder body so the stock middleware skips the response — since that
one changes what is sent on the wire in order to influence control flow.

**Verified by execution, in install mode, with no database.** `InstallController.RestartInstall` is
`[HttpPost]`-only, so `GET /install/restartinstall` matches the route pattern but no HTTP method and
routing produces a **bodiless 405** — the same shape as `PublicAntiForgeryAttribute`'s
`BadRequestResult`, and reachable without a store because `InstallUrlMiddleware` lets `/install/*`
through. Measured, same binary, before and after:

| Request | Before | After |
|---|---|---|
| `GET /install/restartinstall` (bodiless 405) | **302 → /install** (405 swallowed, `/page-not-found` re-executed, `InstallUrlMiddleware` redirected that) | **405**, no body, no `Location` |
| `GET /does-not-exist` (genuine 404) | 302 → /install | **302 → /install** — unchanged, i.e. 404 still re-executes |
| `GET /install` | 200 | 200 |

Both halves matter and both are asserted: a bodiless non-404 keeps its status, and a genuine 404
still re-executes to nopCommerce's `PageNotFound` view at the original URL. On an installed store
the antiforgery case is asserted directly — `POST /register` with no token must be **400** with no
`html-not-found-page` marker.

**Not fixed, and out of scope:** the 400 itself. MVC 5 let `HttpAntiForgeryException` propagate to a
500, which `Application_Error` logged; ASP.NET Core's convention is a 400, which task 6.2 adopted
(§12, "Anti-forgery failure status") and which `NopErrorLoggingMiddleware` does not log. That is a
pre-existing recorded difference, not part of 7.7-1.

### 45.3 Test coverage added, and proof that it can fail

`src/Tests/Nop.Web.SmokeTests` gains 16 always-run assertions plus one installed-store one, and one
new canary. Placement is deliberate: the endpoint-table and descriptor-collection assertions live in
`HostAndContainerTests` because they need **no database**, which is what lets them protect the
`Html.Action` bridge in install mode too — the state in which no rendered page could reveal that the
bridge had broken.

| Test | Fixture | Runs without a database |
|---|---|---|
| `Deferral_7_3_4_a_marked_child_action_has_no_matchable_endpoint` × 7 | HostAndContainer | yes |
| `Deferral_7_3_4_a_marked_child_action_is_STILL_visible_to_the_Html_Action_bridge` × 4 | HostAndContainer | yes |
| `Deferral_7_3_4_an_UNMARKED_action_is_still_matchable` × 4 | HostAndContainer | yes |
| `Deferral_7_7_1_a_bodiless_non_404_keeps_its_status_code` | HostAndContainer | yes |
| `Deferral_7_3_4_a_former_child_action_is_no_longer_reachable_by_URL` (rewritten from the `KNOWN_GAP` test) | InstalledStore | no |
| `Deferral_7_3_4_an_UNMARKED_action_is_still_reachable_by_URL` | InstalledStore | no |
| `Deferral_7_7_1_a_refused_XSRF_post_keeps_its_400_status` (rewritten from the `KNOWN_GAP` test) | InstalledStore | no |
| `Deferral_7_3_4_a_former_child_action_URL_does_not_return_the_partial` (rewritten) | InstallMode | yes |

The install-mode test was rewritten honestly rather than made to look stronger than it is: install
mode **cannot** distinguish the fix from the bug at the HTTP level. Before the fix the endpoint
matched and `InstallUrlMiddleware` redirected (302); after the fix nothing matches, the 404
re-executes `/page-not-found`, and `InstallUrlMiddleware` redirects *that* (302 again). It therefore
asserts only the invariant install mode can show — the bare partial is never returned — and points
at the tests that carry the real proof.

**Proof the new assertions can fail.** Two things were done, because a green suite is only evidence
if it has been shown to go red.

1. **A permanent fourth canary.** `HarnessCanaryTests.CANARY_action_probe_assertions_can_fail`
   asserts `visibleToChildActionBridge=True` for `Common.NoSuchActionExists`, which the probe
   reports as `False` — guarding specifically against the failure mode that would make the
   substring-based 7.3-4 assertions vacuous. Run explicitly: **4 failed / 0 passed** (was 3/0).
2. **A temporary regression, then reverted.** With the convention registration commented out **and**
   `Program.cs` reverted to the stock `UseStatusCodePagesWithReExecute`, the suite reported
   **8 failed / 40 passed / 18 skipped** — precisely the 7 `has_no_matchable_endpoint` cases plus
   `a_bodiless_non_404_keeps_its_status_code`. The bridge-visibility and unmarked-action tests
   **still passed** in that run, which is the desired result: they are independent of the fix rather
   than tautologically coupled to it. Both files were then restored and the full suite re-run green.

### 45.4 Not verified by execution

Stated so a green run is not over-read.

- **The three installed-store tests above did not run** — no database is installed in this
  environment, and they `Assert.Ignore` with the reason rather than passing weakly. What was
  verified for 7.7-1 is the *mechanism*, through a bodiless **405** rather than a bodiless **400**;
  the two travel the identical code path (`StatusCodePagesMiddleware` does not inspect *which*
  4xx it is), so the inference is narrow, but it is an inference. The next run against a real store
  should confirm `POST /register` with no token → **400**, and `GET /Common/Footer` → **404** with
  the `PageNotFound` view.
- **Only 7 of the 48 marked actions** are covered by the parameterised endpoint assertion, chosen to
  span the interesting shapes: two on the same controller, one with an explicit route as well as the
  `Default` route (`Widget.WidgetsByZone`), one whose name collides with an unmarked action
  (`Profile.Info` vs `Customer.Info`), and three plain ones. That the other 41 are marked is
  verified by the set-comparison script, not by an HTTP assertion each.
- **No new deferral was opened by this work**, and nothing was added to the `Administration/` tree.



---

# Nop.Admin — SDK-style project conversion and package migration (task 8.1)

Task 8.1 is project-file plumbing only: no `.cs`, no `.cshtml`, no `AdminAreaRegistration.cs`,
no `Web.config` and no `sitemap.config` was edited. As with 7.1, **its success criterion was
never a clean compile** — it is a clean restore, a project MSBuild can evaluate with correct
resolved item sets, and a CS\*/RZ\* error inventory for tasks 8.2–8.7 to consume.

| Measurement | Value |
|---|---|
| restore | **clean** — all 5 projects, **0** `NU*` diagnostics of any severity |
| project load / MSBuild evaluation | **clean** — **0** `MSB*`, **0** `NETSDK*` |
| upstream projects, re-gated `--no-incremental` after `rm -rf obj bin` | `Nop.Core` **0**/3 · `Nop.Data` **0**/3 · `Nop.Services` **0**/10 · `Nop.Web.Framework` **0**/10 · `Nop.Web` **0**/15 — every one matching its recorded baseline exactly, **no warning added**. Adding Nop.Admin to the build perturbs **nothing**: they are siblings with no `ProjectReference` in either direction |
| `Nop.Tests` | **4 passed / 0 failed** — unchanged |
| `Nop.Web.SmokeTests` | **48 passed / 0 failed / 18 skipped** — unchanged. `HarnessCanaryTests` (`[Explicit]`) still **4 failed / 0 passed**, as required |
| `Nop.Admin` errors | **2959 unique** `(file, line, col, code)` diagnostics across **673 files**, **100% `CS*`/`RZ*`**. Zero errors of any other category |
| warnings | **10**, and **0 of them originate in Nop.Admin** — all ten are the pre-existing upstream `SYSLIB0014`/`SYSLIB0021`/`SYSLIB0023`/`SYSLIB0045`/`SYSLIB0051` obsolescence notices in `Nop.Core`/`Nop.Services`, unchanged since 6.5 |
| swallowed-diagnostics check (`-v:normal`) | `"converted to a warning"` **0** · `ContinueOnError` **0** · `NU1901`–`NU1904` **0** · Six Labors licence lines **0** (see §46.4) |
| new central package pins added | **0** — all six `PackageReference`s already had a `PackageVersion` entry |
| packages removed | **9 of 15** |

## 46. Task 8.1 — what changed

### 46.1 The resolved item sets — measured, with the delta fully explained

The structural trap runs both ways. Task 7.1 had to add `Administration\**` to
`Nop.Web.csproj`'s `DefaultItemExcludes` so Nop.Web's globs would not swallow this tree. The
inverse needs **no** exclusion, and that was established by evaluating the resolved item lists
(`dotnet msbuild -getItem:Compile,Content,None,EmbeddedResource`), not by reading globs:
MSBuild roots the default globs at the directory holding the project file, so Nop.Admin's
globs cannot reach *up* into Nop.Web, and nothing is nested inside `Administration\`.

| Item | Count | Composition | Leakage outside `Administration/` |
|---|---|---|---|
| `Compile` | **277** | Models 156, Validators 57, Controllers 55, Infrastructure 3, Extensions 2, Helpers 2, `AdminAreaRegistration.cs`, `Properties/AssemblyInfo.cs` | **0** |
| `Content` | **340** | 325 `.cshtml` + 12 `.json` + 3 `.config` | **0** |
| `None` | **1200** | 534 `.js`, 355 `.png`, 104 `.gif`, 87 `.css`, 71 `.map`, 11 `.jpg`, 6 `.ttf`, 6 `.woff`, 5 `.eot`, 5 `.svg`, 4 `.txt`, 3 `.html`, 2 `.woff2`, 1 `.otf`, 1 `.swf`, plus 2 extension-less | **0** |
| `EmbeddedResource` | 0 | — | **0** |

**277 is exactly the length of the legacy explicit `<Compile>` list and exactly the `.cs`
count on disk; 325 is exactly the `.cshtml` count on disk.** The globs reproduce the old
compilation set precisely.

The legacy explicit `<Content>` list had 1527 entries against 1540 resolved `Content`+`None`
items, and rather than accept a 13-item discrepancy the two sets were diffed against each
other. It resolves completely:

- **−1**: `packages.config`, deleted by this task.
- **+14**: `Content/tinymce/langs/{ar,de_AT,de_DE,es_ES,es_MX,fr_CH,fr_FR,it_IT,nl_NL,pt_BR,pt_PT,ru_RU,zh_CN,zh_TW}.js`.
  These exist in source control but were **never listed** in the legacy `<Content>` list, so
  **3.90 did not deploy them** — a deployed admin could not load a non-English TinyMCE
  language pack. (`Content/tinymce/langs/readme.md` *was* listed, which is how the omission
  went unnoticed.) Implicit globbing silently fixes a pre-existing 3.90 packaging omission.
  Recorded because it is a change relative to a 3.90 *deployment*, not because it is a
  problem: it is a benign improvement and the files are already in the repository.

### 46.2 Publish shaping — deferral 7.4-2 RESOLVED, and a defect found inside this task

Deferral 7.4-2 assigned two entries from 3.90's `Nop.Web.csproj` `ExcludeFilesFromDeployment`
list here, because `Administration\**` is in that project's `DefaultItemExcludes` and nothing
under this directory is an item of it. `Administration\bin\**` needs no entry (the SDK never
publishes intermediate output, and the flat shared `..\bin\` `OutputPath` is gone).
**`Administration\db_backups\*.bak` is the security-relevant one and is now handled.**

As on the storefront side (§31.4), the larger half of the work turned out to be **inclusions**,
not exclusions: the legacy project used ~1200 explicit `<Content Include>` entries to copy
`Content\` and `Scripts\` into the output, but the SDK classifies
`.js`/`.css`/`.png`/`.gif`/`.map`/`.woff`/`.svg`/`.eot`/`.ttf`/`.otf`/`.swf` as `None`, and
`None` defaults to `CopyToPublishDirectory=Never`. Without the added include rules a published
Nop.Admin would have had **no admin CSS, no admin JavaScript, no AdminLTE, no bootstrap, no
Kendo, no TinyMCE and no Roxy Fileman** — an unstyled, non-functional admin UI. Deferral 7.4-2
did not mention this half.

#### The defect: `db_backups\**` alone is wrong in BOTH directions

`tasks.md` step 8.1 and deferral 7.4-2 both prescribe
`<None Update="db_backups\**" CopyToPublishDirectory="Never" />`. That is wrong, and the task
brief's instruction to *verify rather than assume* is what caught it.

1. **Too broad.** It also withholds `db_backups\placeholder.txt`, and
   `MaintenanceService.GetAllBackupFiles()` opens with
   `if (!Directory.Exists(path)) throw new IOException("Backup directory not exists")` —
   so an absent directory makes the admin Maintenance page **throw** instead of showing "no
   backups". The legacy project shipped the placeholder via
   `<Content Include="db_backups\placeholder.txt" />` for exactly this reason. Same trap task
   7.4 hit and fixed for `Content\Images\Thumbs\placeholder.txt` (§31.3).
2. **Not sufficient either, and this was measured.** Narrowing to `*.bak` is necessary but
   does not by itself make the placeholder publish: `placeholder.txt` is a `.txt`, hence a
   `None` item, hence `CopyToPublishDirectory=Never` **by default**. The first computed
   publish set for this project confirmed it — placeholder **absent** with no exclusion
   touching it at all.

The final shape is therefore an ordered pair: `db_backups\**` → `PreserveNewest` in the
include group, then `db_backups\**\*.bak` → `Never` in the withheld group.

Note this also means testing the exclusion *alone* proves nothing — an absent `.bak` is
equally consistent with "my rule works" and with "`None` defaults to `Never` anyway". The
placeholder/`.bak` pair is the discriminating test, and it passes.

#### Verified publish set

`ComputeFilesToPublish` was evaluated with `-getItem:ResolvedFileToPublish`
(`-p:StaticWebAssetsEnabled=false`, which removes the `staticwebassets.build.json`
dependency so the computation runs without a successful compile), with a **real `.bak`
planted on disk** so the exclusions were tested against a file that actually existed.
1630 distinct published paths.

| Must be absent | Result |
|---|---|
| `db_backups/*.bak` — **the database backups** | ✅ withheld |
| any `*.bak` anywhere | ✅ withheld |
| `Web.config` (legacy, capital W) | ✅ withheld |
| `Views/Web.config` | ✅ withheld |
| any `.cs` | ✅ none |

| Must be present | Result |
|---|---|
| `db_backups/placeholder.txt` | ✅ |
| `sitemap.config` (read by `Views/Shared/Menu.cshtml` line 7 via `XmlSiteMap.LoadFrom`) | ✅ |
| `Content/Roxy_Fileman/conf.json` (read by `RoxyFilemanController`) | ✅ |
| `Content/**` · `Scripts/**` | ✅ 682 · 529 |
| `Nop.Admin.dll` | ✅ |

**One assertion is explicitly NOT trustworthy from this invocation and must be re-checked at
8.8 with a real publish:** `.cshtml` files appear in the computed set (325). A control run of
the identical partial invocation against **`Nop.Web`** — whose *real* publish task 7.5 verified
contains **zero** `.cshtml` — likewise reports 195, so the appearance is an artifact of
bypassing the Razor publish targets, not of this project file (which sets no metadata on
`.cshtml` at all). The same control confirmed `App_Data/Settings.txt` is correctly **absent**,
so the exclusion metadata *is* honoured by the invocation and the assertions above are sound.

#### The `.bak` download link is dead by design — 8.3/8.5 must not "fix" it the easy way

`Controllers/CommonController.cs` line 584 still builds
`_webHelper.GetStoreLocation(false) + "Administration/db_backups/" + p.Name`, i.e. 3.90 served
backups as **static files** — which is why `Nop.Web/Web.config` carried
`<mimeMap fileExtension=".bak" mimeType="application/octet-stream"/>` with the comment "Allow
database backup (.bak) file loading". Task 7.4 deliberately did not reproduce that mimeMap
(§33.2), and `NopStaticFileProvider` additionally denies the `.bak` extension *and* excludes
`Administration/` from its allow-list. So the link is refused three times over. **Task 8.3/8.5
must replace it with a controller action that streams the file under the existing admin
permission check** — which is the correct design regardless — and must **not** re-add the
mimeMap or widen the static-file allow-list to `db_backups`.

### 46.3 The error inventory handed to 8.2–8.7

**2959 unique errors across 673 files, and only three missing namespaces plus one Razor
family account for all of them.** Nothing is a packaging problem.

| Root cause | Sites | Owner |
|---|---|---|
| `System.Web.Mvc` — 330 `CS0234` sites, and the overwhelming majority of the 2687 `CS0246`: `ActionResult` (1594), `HttpPost`/`HttpPostAttribute` (980 each), `AllowHtml`/`AllowHtmlAttribute` (790 each), `SelectListItem` (366), `NonAction` (254), `ActionName` (140), `FormCollection` (58), `ValidateInput` (48), `ChildActionOnly` (32), `Bind` (32), `SelectList` (14), `HttpVerbs` (the 5 `CS0103`) | the bulk | **8.3** (controllers), **8.4** (views) |
| `System.Web.Routing` — 30 `CS0234` sites, `RouteValueDictionary` (16 `CS0246`) | — | **8.2** (`AdminAreaRegistration.cs`, and the new `IRouteProvider`), **8.3** |
| `System.Web.Configuration` — 2 `CS0234` sites | — | **8.7** — see §46.5 |
| Razor: **78 `RZ1002`** "the helper directive is not supported" in **26 view files**, plus **4 `RZ2005`** + **4 `RZ1011`** in exactly two files (`Views/Customer/_CustomerAttributes.cshtml`, `Views/Shared/_AddressAttributes.cshtml`) | 86 | **8.4** |
| cascade failures inside views — `ProductModel` (96), `OrderModel` (52), `DiscountModel` (24), `CategoryModel`/`CustomerModel`/`CustomerRoleModel`/`ManufacturerModel` (16 each), `TopicModel`/`StoreModel`/`VendorModel`/`WarehouseModel` (12 each) … | — | **8.4** — these are *not* independent defects; see below |

Distribution by directory (files carrying at least one error): `Views` **253 of 325**,
`obj` **252** (the Razor source generator's virtual `*_cshtml.g.cs`, i.e. views again),
`Models` **110 of 156**, `Controllers` **55 of 55**, `Helpers` 1, `Extensions` 1,
`AdminAreaRegistration.cs`.

**The `_ViewImports.cshtml` leverage point, and it is even larger here than on the
storefront.** The `*Model` CS0246 cluster is not 300-odd separate problems: those types exist
and compile fine in `Models/`, and they are unresolvable *only inside views*, because
`Views/Web.config`'s `pageBaseType` + `<namespaces>` no longer apply and nothing replaces
them yet. On `Nop.Web` the single `Views/_ViewImports.cshtml` file took the count from **1974
to 24** (§27). Nop.Admin has 325 views to Nop.Web's 193 and the identical dependency, so
**task 8.4 must create `Views/_ViewImports.cshtml` from `Views/Web.config`'s `pageBaseType`
and `<namespaces>` FIRST**, before any per-view work. Two constraints on its content:

- it must **not** carry `System.Web.Optimization` (bundling is dropped — design §8), and
- `Views/Web.config`'s `<namespaces>` is the source list, but the whole `System.Web.*` set in
  it maps to ASP.NET Core equivalents rather than transferring verbatim.

**Which trees need a `_ViewImports.cshtml`, per deferral 7.5-1.** Razor discovers
`_ViewImports.cshtml` by walking up from the view's **own** directory to the project root, so
it is per-tree, not per-project. Nop.Admin has exactly **one** view tree — `Views/` (with
`Views/Shared/`, `Views/Shared/EditorTemplates/`, `Views/Shared/DisplayTemplates/` beneath it,
all of which the single `Views/_ViewImports.cshtml` covers). There is **no** `Themes/` tree and
no second root under `Administration/`, so one file suffices. Note this differs from
`tasks.md` step 8.4's wording: the file to delete is **`Views/Web.config`**, not
`Areas/Admin/Views/web.config` — the admin views live at `Administration/Views/`, and there is
no `Areas/` directory anywhere in this project.

### 46.4 Two corrections to earlier recorded statements

- **§7.18 says the ImageSharp licence diagnostic "affects `Nop.Services` now and `Nop.Admin`
  at task 8.6". It does NOT affect `Nop.Admin`.** That sentence was written when the pin was
  4.1.1; the pin was subsequently moved to **2.1.13**, the last purely Apache-2.0 line, which
  ships **no `build/` targets at all** and therefore no `ValidateLicenseTask`. Measured: with
  `SixLabors.ImageSharp` referenced and restored, a `-v:normal` build log contains **zero**
  Six Labors licence lines and **zero** `"converted to a warning"` lines. The only
  `sixlabors` strings in the log are NuGet download URLs. `Directory.Packages.props` already
  documents this; §7.18 was not updated at the time.
- **`tasks.md` step 8.7's instruction to remove `Properties/AssemblyInfo.cs` is WRONG for this
  solution**, for exactly the reason task 7.5 measured for `Nop.Web` (§37.1):
  `Directory.Build.props` sets `GenerateAssemblyInfo=false` solution-wide (task 2.3), so the
  SDK emits **no** replacement attributes and deleting the file drops `AssemblyVersion` from
  `3.9.0.0` to `0.0.0.0` and loses `AssemblyTitle`/`AssemblyFileVersion`/`ComVisible`/`Guid`
  outright. The file is **KEPT**, consistent with all five migrated projects. Moving to
  SDK-generated assembly info is a solution-wide change (flip the property, add
  `Version`/`FileVersion`/`Title`, delete all six files together), not a per-project one.

### 46.5 `System.Configuration` here is a REMOVAL, not a configuration migration

`tasks.md` step 8.7 says "rewrite `ConfigurationManager.*` call-sites" to `IConfiguration`.
There is exactly **one** call site in this project and it cannot be rewritten that way:

`Controllers/CommonController.cs` line 458 —
`ConfigurationManager.GetSection("system.web/machineKey") as MachineKeySection` — in the admin
**System Info** page. `MachineKeySection` does not exist on .NET in any form; ASP.NET Core
replaced `<machineKey>` with **Data Protection**. So there is no setting to carry across, and
`appsettings.json` must not grow a `machineKey` key. 8.7 either deletes the block or reports
the Data Protection key-ring state instead (which is the genuinely useful equivalent, and
relates directly to deferral 7.13's multi-instance key-ring caveat).

The same file has a second System-Info item already recorded: §2 requires lines 205 and 217 to
report trust level `"Full"` unconditionally, because `CommonHelper.GetTrustLevel()` and
`AspNetHostingPermissionLevel` were deleted in task 2.4.

## 47. Case-sensitivity audits over `Administration/` — deferral 7.7-4 RESOLVED (findings), run early

Deferral 7.7-4 assigned these to 8.4/8.5. They were run **now**, at 8.1, because the finding
list shapes those tasks. Task 7.7's own scripts were throwaway and not committed, so both were
re-implemented to the same specification: extract the references, strip comments first
(`//`, `/* */`, `@* *@`, with string-literal tracking so a `//` inside a URL is not mistaken
for a comment), then resolve **segment by segment** against the real filesystem, reporting any
segment whose spelling differs in case from the entry on disk. All paths resolve against the
**host content root** `src/Presentation/Nop.Web`, because `CommonHelper.MapPath` resolves `~/`
against `CommonHelper.BaseDirectory` — which task 7.2 sets to the Nop.Web content root — and
`Url.Content("~/…")` resolves against `PathBase`.

**Fixing them is task 8.4's job** (both are in a view). They are recorded, not fixed, because
8.1's scope is the project file.

### Audit A — `MapPath("~/…")` literals in `Administration/**/*.cs`

**5 literals checked, 0 case mismatches.** Clean.

Two further `Server.MapPath` calls are **relative, not `~/`-rooted**, and are reported rather
than audited because relative `MapPath` has no ASP.NET Core equivalent at all —
`HttpServerUtility` is gone, and `IWebHostEnvironment.ContentRootPath` composition has no
notion of "relative to the current request's directory":

- `Controllers/RoxyFilemanController.cs:241` — `MapPath("../Uploads")`
- `Controllers/RoxyFilemanController.cs:505` — `MapPath("../tmp/")`

Both are **task 8.6's**, and 505 is also the subject of deferral 8.1-1 below.

### Audit B — filesystem-rooted `~/…` references in admin views, stylesheets and scripts

**116 references checked across `.cshtml` / `.css` / `.js`, 2 case mismatches — and both are
in `Views/Shared/_AdminLayout.cshtml`, the layout every single admin page uses.**

| Site | Written | On disk | Consequence on a case-sensitive filesystem |
|---|---|---|---|
| `Views/Shared/_AdminLayout.cshtml:43` | `Html.AppendScriptParts("~/Administration/scripts/admin.navigation.js")` | `Administration/Scripts/…` | the admin **navigation script 404s** on every admin page |
| `Views/Shared/_AdminLayout.cshtml:99` | `@Url.Content("~/administration/content/images/throbber-synchronizing.gif")` | `Administration/Content/images/…` | the "synchronizing" throbber image 404s |

Line 43 is the more serious of the two: it goes through `IPageHeadBuilder.AppendScriptParts`,
so it is also the path `IFileVersionProvider` fingerprints (§14.33) — a missing file gets **no
`?v=` suffix and no warning**, so the failure is doubly silent until the browser 404s.

33 further `~/…` literals are **MVC routes, not filesystem paths** (`~/Admin/Customer/Edit/`,
`~/admin`, …) and are correctly out of scope for a filesystem audit. They are, however, task
8.2's concern: they hard-code the `Admin` route prefix that `MapAreaControllerRoute` must
preserve.

### Supplementary audit — relative `url(...)` in admin stylesheets

Not part of 7.7's two audits, added because the class of defect is identical and the admin CSS
surface is large (87 stylesheets). Every relative `url(...)` reference was resolved
case-exactly against the stylesheet's own directory: **582 references checked, 0 case
mismatches**, 25 unresolved (referenced files genuinely absent — third-party font/image
references in vendored libraries, pre-existing in 3.90 and not a casing problem).

**Still not audited, and 8.4/8.5 should extend the scan:** the `~/Administration/Content/kendo/{0}/…`
family, whose `{0}` is the Kendo culture folder substituted at runtime, and any path composed
by string concatenation rather than written as a literal.

## 48. NEW deferrals opened by task 8.1

| # | Item | Owner task(s) | Severity |
|---|------|---------------|----------|
| 8.1-1 | `Content/Roxy_Fileman/tmp/` does not exist, so the file manager's "download folder as zip" throws | 8.5 / 8.6 | Low |
| 8.1-2 | Two case-sensitivity defects in `_AdminLayout.cshtml` | 8.4 | Medium |
| 8.1-3 | `Server.MapPath` with a **relative** path has no ASP.NET Core equivalent | 8.6 | Medium |
| 8.1-4 | The admin views' compiled Razor identifiers will be `/Views/…`, which `ThemeableViewLocationExpander` does **not** search | 8.2 / 8.4 | **High** |

### 8.1-1 `Content/Roxy_Fileman/tmp/` does not exist

The legacy project carried `<Folder Include="Content\Roxy_Fileman\tmp\" />`, a Visual Studio
empty-folder placeholder. **The directory is not in this checkout** — git does not track empty
directories and there is no placeholder file in it — and an SDK project has no `<Folder>`
equivalent, so the item was dropped.

`Controllers/RoxyFilemanController.cs` line 505 does
`MapPath("../tmp/" + dirName + ".zip")` and then `ZipFile.CreateFromDirectory(..., tmpZip, …)`,
which throws `DirectoryNotFoundException` when the parent is absent. So the file manager's
DOWNLOADDIR operation fails.

**This is PRE-EXISTING in 3.90**, not a regression: the `<Folder>` item created the directory
in a developer's working copy, but MSBuild's `_CopyWebApplication` does not deploy empty
directories either. It is recorded because dropping the `<Folder>` item is the moment the last
trace of the intent disappears from the build.

**Fix (8.5 or 8.6):** commit a placeholder file, as `db_backups/placeholder.txt` and
`Content/Images/Thumbs/placeholder.txt` already do — and then add a publish include for it,
since a `.txt` is a `None` item and defaults to `Never` (§46.2) — or `Directory.CreateDirectory`
before writing, which is the better fix and is a one-liner inside the rewrite 8.6 performs
anyway. Note `.gitignore` line 40's `*.tmp` does **not** match a directory named `tmp`.

### 8.1-2 Two case-sensitivity defects in `_AdminLayout.cshtml`

Full detail in §47. `Views/Shared/_AdminLayout.cshtml` lines 43 and 99. **Task 8.4.** They are
masked twice over today — the admin assets do not serve at all yet (deferral 7.4-2), so
widening `NopStaticFileProvider`'s allow-list at 8.5 without fixing these two will still 404,
which is exactly the interaction `tasks.md` step 8.5 warns about.

### 8.1-3 `Server.MapPath` with a relative path has no equivalent

`RoxyFilemanController.cs` lines 241 (`"../Uploads"`) and 505 (`"../tmp/"`). Every other
`MapPath` in the project is `~/`-rooted and maps cleanly onto `CommonHelper.MapPath`; these two
do not, because `HttpServerUtility.MapPath` resolved a relative path against the *current
request's* virtual directory and nothing in ASP.NET Core has that notion. **Task 8.6** must
resolve them to explicit content-root-relative paths — and note that
`~/Administration/Content/Roxy_Fileman/../Uploads` is `~/Administration/Content/Uploads`,
which does **not** exist on disk either, so 8.6 should establish what the intended target is
rather than mechanically translating the `..`.

### 8.1-4 Compiled admin view identifiers will not match the themeable view-location expander — HIGHEST IMPACT

**Measured, not inferred.** The Razor source generator names a view by its path **relative to
its own project directory**. Confirmed two ways:

- the generator's own output paths for this project are
  `obj/Debug/net10.0/…/RazorSourceGenerator/Views/ActivityLog/ListLogs_cshtml.g.cs` — i.e.
  `Views/…`, with no `Administration/` prefix;
- reading the **built `Nop.Web.dll`**, its 108 compiled view identifiers are `/Views/Blog/BlogPost.cshtml`
  and so on — relative to *that* project's root, which is why they match the expander's
  `/Views/{1}/{0}.cshtml` format.

So `Nop.Admin`'s views will compile with identifiers `/Views/Shared/_AdminLayout.cshtml`,
`/Views/Product/List.cshtml`, …

`Nop.Web.Framework`'s `ThemeableViewLocationExpander` (task 6.3, §16.1) emits, for the `admin`
area, `/Administration/Views/Shared/{0}.cshtml` and `/Administration/Views/{1}/{0}.cshtml`, and
for non-area lookups the same two paths as its last entries. Those formats faithfully preserve
3.90, where admin views were **physical files under the Nop.Web application root**. Under
ASP.NET Core they are **compiled into `Nop.Admin.dll`** and discovered as an application part
at their compiled identifiers, so `/Administration/Views/…` matches nothing.

**§14.30 flagged the opposite direction** — that if the expander is not registered, "the
`/Administration/Views/…` locations are not searched at all, so if task 8.x leaves the admin
views where 3.90 put them the whole admin UI 404s on view lookup". The finding here is sharper
and survives the expander being registered correctly: the mismatch is between the expander's
**location formats** and the views' **compiled identifiers**, not between formats and physical
file locations.

**Impact if unaddressed:** every admin view lookup fails. Loud (an
`InvalidOperationException` naming the searched locations), not silent — but it will look like
a routing or area bug rather than a view-location one, so it is worth knowing the cause before
8.2/8.4 start.

**Options for 8.2/8.4, none of them decided here:**

1. Prefix the compiled identifiers, so views compile as `/Administration/Views/…` and the
   existing expander formats work untouched. The Razor SDK exposes this per-item
   (`_RazorGenerateRelativePath`-style metadata on the `RazorGenerate`/`Content` items); this
   is the least-change option with respect to `Nop.Web.Framework`, which is committed and
   gated.
2. Change the expander's two admin formats to `/Views/…`. **Careful**: those entries are
   `/Views/Shared/{0}.cshtml` and `/Views/{1}/{0}.cshtml`, which are *already* the non-themed
   storefront fallbacks, so the admin and storefront view namespaces would collide — a
   same-named view could resolve to the wrong project's copy. This option needs the collision
   thought through, and it edits a gated project.
3. Contribute the admin views under an explicit area, i.e. move them to `Areas/Admin/Views/`
   and let ASP.NET Core's own area location formats find them. Most idiomatic, largest diff
   (325 file moves), and it makes the "little hack" in §16.1 that exists solely to serve
   `/Administration/` instead of `Areas/Admin/` redundant — which may be a feature.

Whichever is chosen, note that §16.1 preserved a **3.90 ordering quirk verbatim** in the admin
area formats: `…/Views/Shared/{0}.cshtml` is searched **before** `…/Views/{1}/{0}.cshtml`, so a
same-named Shared view shadows the controller-specific one. That quirk must survive the change,
or admin view resolution changes behaviour.

## 49. Deferrals explicitly NOT closed by 8.1, with the reason

| # | Item | Why not here |
|---|------|---|
| **7.4-2** (admin static assets half) | admin `Content/`/`Scripts/` do not **serve** | **8.5**. 8.1 fixed the *publish* half only (§46.2). The serving half is `NopStaticFileProvider`'s allow-list, and it must be widened with `Administration/Content/**` and `Administration/Scripts/**` specifically — **not** by widening the root, or `db_backups` and the admin `.cshtml` tree come with it |
| **7.7-4** | case-sensitivity audits over `Administration/` | **RUN HERE, findings recorded** (§47); the two fixes are **8.4**'s, and extending the scan to `{0}`-substituted and concatenated paths is 8.4/8.5's |
| **7.3-1** | the `Html.Action` bridge lives in `Nop.Web` and the admin views need it | **8.3**. Recommend promoting `Nop.Web/Extensions/ChildActionExtensions.cs` to `Nop.Web.Framework` |
| **7.3-4** | apply `[NopChildActionOnly]` to the admin actions | **8.3**. The mechanism is finished (§45.1); the 32 `ChildActionOnly` `CS0246` sites in this project's error inventory are the ones to mark, identified from the pre-migration tree per step 8.3, never by guessing |
| **18.4** | `CA1416` at the two `Nop.Admin` `CommonController` call sites | **8.3**. They are not visible yet — the file has 55 `CS0246`/`CS0234` errors, so the analyser has not run on it |
| **35** | minification gone, nothing replaces it | post-migration (design §8). Note the two now-inert bundling checkboxes in `Views/Setting/GeneralCommon.cshtml` are **8.4**'s to remove (design §8) |
| **7.2-3** | `TaskManager.Instance.Stop()` never called | none (3.90 parity) |
| **7.4-1** | a Kestrel-only deployment gets no response compression | deployment decision |
| **4.11** | `ExecuteSqlCommand` per-batch transaction during the Fast installer | needs a real database; unexercised (§44/7.7-5) |
| **4.10** | `GO`-batched `CreateDatabaseScript()` in four plugin contexts | 11.2, 13.1, 14.4, 15.1 |
| **9 / 4.9** | `NopObjectContext` needs a real connection string | 3.4 (`Nop.Data.Tests` fixtures only) |
| **7.7-2** | the smoke project is not in `NopCommerce.sln` | 18.1. `Nop.Admin` **is** already in the solution under the plain C# project GUID, so no solution edit was needed at 8.1 either |
| **7.7-3** | EF Core adds ~97 unrequested FK indexes | schema review (post-migration) |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted; no override exists anywhere |
| **7.3-2** · **7.3-3** · **7.3-5** · **7.3-6** | payment `CustomValues` JSON, request validation gone, browser detection gone, bridge skips filters | 12.1–12.5 / accepted |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision — **and it does not affect this project**, see §46.4 |
