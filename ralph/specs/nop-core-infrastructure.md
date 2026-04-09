# Nop.Core Infrastructure

## Bounded Context
Core infrastructure — DI engine, type finding, caching abstractions, event system, plugin system, and common utilities that all layers consume.

## Legacy Source
- `src/Libraries/Nop.Core/Infrastructure/` — IEngine, NopEngine, EngineContext, ITypeFinder, WebAppTypeFinder, IStartupTask, IMachineNameProvider
- `src/Libraries/Nop.Core/Caching/` — ICacheManager, MemoryCacheManager, RedisCacheManager, PerRequestCacheManager, IRedisConnectionWrapper
- `src/Libraries/Nop.Core/Events/` — EntityInserted, EntityUpdated, EntityDeleted
- `src/Libraries/Nop.Core/Plugins/` — IPlugin, BasePlugin, IPluginFinder, PluginManager, PluginDescriptor (14 files)
- `src/Libraries/Nop.Core/Data/` — IRepository<T>, DataSettings, IDataProvider
- `src/Libraries/Nop.Core/IWorkContext.cs`, `IStoreContext.cs`, `IWebHelper.cs`
- `src/Libraries/Nop.Core/ComponentModel/`, `Html/`, `Fakes/`

## Key Entities
- `IEngine`, `NopEngine`, `EngineContext` — IoC bootstrap
- `ITypeFinder`, `WebAppTypeFinder` — assembly scanning
- `ICacheManager` — caching contract
- `IRepository<T>` — generic repository interface
- `IPlugin`, `BasePlugin`, `PluginDescriptor`, `IPluginFinder` — plugin system
- `IWorkContext`, `IStoreContext`, `IWebHelper` — request context
- `IStartupTask` — startup hooks
- `EntityInserted<T>`, `EntityUpdated<T>`, `EntityDeleted<T>` — domain events
- `CommonHelper` — utility methods (email validation, random digits, object comparison, etc.)
- `NopException` — custom exception base class
- `NopVersion` — application version constants
- `MimeTypes` — MIME type lookup by file extension
- `XmlHelper` — XML serialization/deserialization helpers
- `IPagedList<T>`, `PagedList<T>` — paged collection abstraction used by all list queries
- `Extensions` — extension methods on common types
- `HtmlHelper`, `BBCodeHelper`, `ResolveLinksHelper` — HTML sanitization and formatting utilities
- `GenericDictionaryTypeConverter<T>`, `GenericListTypeConverter<T>` — custom type converters for comma-separated values

## External Dependencies
- Autofac 4.4.0 → replace with `Microsoft.Extensions.DependencyInjection` (built-in .NET 10)
- StackExchange.Redis.StrongName 1.2.1 → `StackExchange.Redis` 2.x or `Microsoft.Extensions.Caching.StackExchangeRedis`
- RedLock.net.StrongName 1.7.4 → `RedLock.net` 3.x or `Medallion.Threading.Redis`
- AutoMapper 5.2.0 → `Mapster` or `AutoMapper` 13.x
- Newtonsoft.Json 9.0.1 → `System.Text.Json` (built-in)

## Migration Notes
- **Decision**: Rewrite
- Replace Autofac with built-in DI; `IDependencyRegistrar` → `IServiceCollection` extension methods
- `NopEngine` / `EngineContext` service locator pattern → eliminate; use constructor injection everywhere
- `ICacheManager` → `IDistributedCache` + `IMemoryCache` from Microsoft.Extensions.Caching
- `IRepository<T>` → keep interface, implement with EF Core
- Plugin system → redesign around `AssemblyLoadContext` for .NET 10
- Domain events → `MediatR` INotification or custom `IEventPublisher`
- `IWorkContext` / `IStoreContext` → keep as scoped services, implement via `IHttpContextAccessor`
- `Fakes/` directory → drop entirely; ASP.NET Core provides `WebApplicationFactory` and `TestServer` for integration testing
- `Html/CodeFormatter/` → evaluate if still needed; consider Markdig or similar for rich text processing
- `ComponentModel/` type converters → port as-is, used by settings system for comma-separated list storage

## Acceptance Criteria
- [ ] DI container bootstraps using `Microsoft.Extensions.DependencyInjection` with no Autofac dependency
- [ ] `IRepository<T>` interface defined with `GetById`, `Table`, `TableNoTracking`, `Insert`, `Update`, `Delete`
- [ ] `ICacheManager` (or replacement) supports in-memory and distributed (Redis) caching with pattern-based invalidation
- [ ] Domain event types (`EntityInserted<T>`, `EntityUpdated<T>`, `EntityDeleted<T>`) are defined and publishable
- [ ] Plugin discovery loads assemblies from a configured directory and registers services
- [ ] `IWorkContext` and `IStoreContext` interfaces defined with same properties as legacy
- [ ] `CommonHelper`, `NopException`, `MimeTypes`, `XmlHelper` utility classes ported with equivalent functionality
- [ ] `IPagedList<T>` / `PagedList<T>` paged collection abstraction available for all list queries
