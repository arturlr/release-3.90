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
| 4 | Configuration source unset — all `NopConfig` settings at defaults | 7.4 | ✅ **seam RESOLVED by 7.2** (§24); 7.4 still owns authoring `appsettings.json` — see new deferral 7.2-4 |
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
| 8 | ~~Schema initializer is never invoked — a fresh install creates no tables~~ | — | ✅ **RESOLVED by 7.2** (§24) — `Program.InitializeDatabaseSchema()`; see new deferral 7.2-1 |
| 9 | `NopObjectContext` now needs a real connection string, not a database name | 3.4, ~~6.4~~ | Medium — **6.4 verified clean**, see §17.1 |
| 10 | `CreateDatabaseScript()` output is `GO`-batched — 4 plugin contexts will fail | 11.2, 13.1, 14.4, 15.1 | Medium |
| 11 | `ExecuteSqlCommand(doNotEnsureTransaction: false)` now opens a real transaction | ~~4.2~~, 7.7 | Medium — **reviewed at 7.2, no change made** (§24.2); needs a real database |
| 12 | Many-to-many join **column** names follow EF Core conventions, not 3.90's | 4.2 / schema review | Medium |

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
| 15 | ~~Session state is not configured — external authentication round-trip fails closed~~ | — | ✅ **RESOLVED by 6.4 + 7.2** (§24) — `ISessionStore` verified; **distributed store still 7.4's**, see deferral 36 |
| 16 | EU VAT service endpoint is a compiled-in constant, not configuration | 7.4 | Medium |
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
| 32 | No `Widget` view component exists — `@Html.Widget(...)` throws | 7.3 | High |
| 33 | Cache busting silently no-ops for assets outside the web root | 7.3 / 7.4 / 8.x | Medium — `UseStaticFiles()` is registered by 7.2, but the asset trees have not moved (§26) |
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
| **4** | open, owners 7.2 + 7.4 | **narrowed.** The *assignment* is now part of the same one line. 7.4 still owns authoring the `NopConfig` and `appSettings` sections in `appsettings.json` — without them the seam is set but there is nothing to read. |
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

### 18.5 Noted, not changed — `FilePermissionHelper`'s file/directory lists are stale

`GetFilesWrite()` still asks for write permission on `~/Global.asax` and `~/web.config`, and
`GetDirectoriesWrite()` builds its paths with hard-coded `\\` separators. Both are pre-existing and
**neither was touched**, because changing them alters what the installer and the admin System Info
page report — a behaviour change outside 6.5's scope.

It is **harmless today**: `CheckPermissions` wraps the ACL read in `try { … } catch { return true; }`,
so a path that does not exist is reported as "permission OK". But it is dead weight once task 7.5
deletes `Global.asax` and task 7.4 reduces `web.config` to an optional ANCM shim.

**Recommendation for task 7.5** (which owns removing obsolete `Nop.Web` files): drop `Global.asax`
from `GetFilesWrite()`, and decide whether `web.config` stays (keep it only if the ANCM shim is
kept). The `\\` separators should become `Path.Combine` segments if any non-Windows deployment is
ever in scope — but that is coupled to the `CA1416` guard decision in §18.4 and belongs with it.


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
| 36 | Redis session state provider dropped with no successor | 7.4 (decision) | Medium |
| 37 | ~~MiniProfiler packages removed — two source sites still reference them~~ | 7.3 | ✅ `Global.asax.cs` half **RESOLVED by 7.2** (file deleted, §20); `Views/Shared/_Root.Head.cshtml` lines 10–11 / 54–56 still open |
| 38 | ~~No `launchSettings.json` — the IIS Express / dev-server settings were discarded~~ | — | ✅ **RESOLVED by 7.2** (§24) — `Properties/launchSettings.json` |
| 39 | `ExcludeFilesFromDeployment` publish shaping lost with the Web Application Project targets | 7.4 | **Medium — security-relevant** |
| 40 | Static asset trees still at their 3.90 locations, outside `wwwroot` | 7.3 / 7.4 | High — `UseStaticFiles()` registered by 7.2 but `WebRootFileProvider` is empty (§26) |
| 41 | `WebGrease.Css.Extensions.ForEach` is load-bearing at two call sites | 7.3 | Low but **will not compile** |

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
| **1.4** | Configuration source unset | the `UseNopHostingEnvironment(builder.Configuration)` argument. Narrowed only — 7.4 still owns authoring `appsettings.json` |
| **1.5** | `CommonHelper.MapPath` resolves under `bin/` | same call, first statement. **Verified**: `MapPath("~/App_Data/x")` lands under the content root |
| **1.6** | `WebHelper.RestartAppDomain` throws | `UseServiceProviderFactory` + `Populate`. **Verified**: `IHostApplicationLifetime` resolvable from the nopCommerce container |
| **4.8** | Schema initializer never invoked | `Program.InitializeDatabaseSchema()` — `SqlServerDataProvider.DatabaseInitializer.InitializeDatabase(context)`, called **after** `RunStartupTasks` because `EfStartUpTask` (Order −1000) is what publishes the initializer. Resolved through an explicitly disposed `ILifetimeScope` (`IDbContext` is `InstancePerLifetimeScope` and there is no request scope at startup) |
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
| 7.2-2 | `IRouteProvider` implementations still unported, so `UseNopEndpoints()` cannot run | 7.3 | **High — blocks any request** |
| 7.2-3 | `TaskManager.Instance.Stop()` is never called on shutdown | none (3.90 parity) | Low |
| 7.2-4 | `appsettings.json` does not exist, so every bindable setting is at its default | 7.4 | Medium |

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
| **36 / 7.1-1** | Redis session state provider dropped | 7.4 — and 7.4 must harvest the connection string from `Web.config`'s commented-out `<sessionState>` **before** reducing that file, because it exists nowhere else |
| **39 / 7.1-4** | `ExcludeFilesFromDeployment` publish shaping lost (**security-relevant**) | 7.4 |
| **41** | `WebGrease.Css.Extensions.ForEach` load-bearing in `CustomerModelFactory` | 7.3 — still 1 error in the residual count |
| **9 / 4.9** | `NopObjectContext` needs a real connection string | 3.4 (`Nop.Data.Tests` fixtures only) |
| **4.10** | `GO`-batched `CreateDatabaseScript()` in four plugin contexts | 11.2, 13.1, 14.4, 15.1 |
| **11.27** | `BaseNopModel.BindModel` no longer invoked | accepted, no override exists anywhere |
| **18 / 7.18** | ImageSharp licence diagnostic | business decision |
