# nopCommerce 3.9 to .NET 8 Migration Plan

**Created:** January 21, 2026  
**Timeline:** 4-6 months (Balanced approach)  
**Strategy:** Incremental migration with .NET Standard intermediate step

---

## Problem Statement

Migrate nopCommerce 3.9 from .NET Framework 4.5.1 (ASP.NET MVC 5) to .NET 8 (ASP.NET Core), involving 31 projects, 106 entity mappings, 20 plugins, and a complex plugin architecture with shadow copying. The migration must maintain database schema compatibility while modernizing the technology stack.

---

## Requirements

### Migration Strategy
- Incremental approach using .NET Standard as intermediate step
- Migrate libraries first, then web projects
- Modernize plugin architecture using .NET 8 features (AssemblyLoadContext)
- Maintain backward compatibility with existing database schema

### Technical Constraints
- Direct migration from Entity Framework 6 to EF Core
- Database schema must remain unchanged
- Balanced timeline: 4-6 months with thorough testing
- 106 entity type configurations need conversion
- Plugin system requires complete redesign for .NET 8

### Success Criteria
- All 31 projects compile and run on .NET 8
- Existing database works without schema changes
- All 20 plugins functional with new architecture
- Performance equal or better than current system
- All tests passing

---

## Background

### Current Architecture
- .NET Framework 4.5.1 with ASP.NET MVC 5
- Entity Framework 6 with 106 entity mappings
- Custom plugin system using shadow copying and BuildManager
- Autofac for dependency injection
- 4 library projects, 3 presentation projects, 20 plugins, 5 test projects
- Uses packages.config for NuGet dependencies

### Key Migration Challenges
1. **Plugin System**: Current implementation relies on `BuildManager`, `PreApplicationStartMethod`, and shadow copying - all unavailable in .NET Core
2. **EF6 to EF Core**: 106 entity configurations using `EntityTypeConfiguration` need conversion to `IEntityTypeConfiguration`
3. **ASP.NET MVC to Core**: Controllers, filters, routing, and middleware need conversion
4. **System.Web Dependencies**: `HttpContext`, `HttpRequest`, fake implementations need replacement
5. **Configuration**: Web.config to appsettings.json migration

---

## Proposed Solution

**Phase 1: Foundation** - Create .NET Standard 2.0 libraries for domain and data access  
**Phase 2: Data Layer** - Migrate to EF Core with schema compatibility  
**Phase 3: Core Libraries** - Migrate services and infrastructure to .NET Standard 2.1/.NET 8  
**Phase 4: Plugin Architecture** - Implement modern plugin system using AssemblyLoadContext  
**Phase 5: Web Layer** - Migrate to ASP.NET Core MVC  
**Phase 6: Testing & Validation** - Comprehensive testing and performance validation

---

## Task Breakdown

### Task 1: Create .NET Standard 2.0 Foundation Projects

**Objective:** Establish .NET Standard 2.0 versions of Nop.Core domain models to enable incremental migration.

**Implementation:**
- Create new Nop.Core.csproj targeting `netstandard2.0`
- Migrate domain entities (BaseEntity and all domain classes in Domain folder)
- Migrate interfaces (IWorkContext, IStoreContext, IWebHelper, IRepository, etc.)
- Remove System.Web dependencies, create abstractions for HTTP context
- Update to PackageReference format
- Keep original .NET Framework project alongside for now

**Key Changes:**
- Replace `System.Web.HttpContext` with custom `IHttpContextAccessor` abstraction
- Remove Fakes folder (FakeHttpContext, etc.) - will recreate for testing later
- Keep domain models unchanged to ensure database compatibility

**Demo:** Nop.Core.csproj compiles successfully on .NET Standard 2.0, can be referenced by both .NET Framework and .NET Core projects.

---

### Task 2: Create EF Core Data Layer Foundation

**Objective:** Create new Nop.Data project targeting .NET Standard 2.1 with EF Core, maintaining exact database schema compatibility.

**Implementation:**
- Create new Nop.Data.csproj targeting `netstandard2.1`
- Install EF Core packages (Microsoft.EntityFrameworkCore.SqlServer)
- Create new `NopDbContext` inheriting from `DbContext` (EF Core)
- Migrate `IDbContext` interface to be EF Core compatible
- Create `EfCoreRepository<T>` implementing `IRepository<T>`
- Keep original EF6 project for reference

**Key Changes:**
- `NopObjectContext` → `NopDbContext`
- Update `IDbContext` to expose EF Core `DbSet<T>` properties
- Repository pattern remains same interface, different implementation

**Demo:** NopDbContext can be instantiated and connects to existing database without errors.

---

### Task 3: Migrate Entity Type Configurations to EF Core

**Objective:** Convert all 106 EF6 entity mappings to EF Core `IEntityTypeConfiguration<T>` while preserving exact database schema.

**Implementation:**
- Convert `NopEntityTypeConfiguration<T>` base class to implement `IEntityTypeConfiguration<T>`
- Migrate all 106 mapping files in Mapping folder:
  - Replace `EntityTypeConfiguration<T>` with `IEntityTypeConfiguration<T>`
  - Convert `HasRequired/HasOptional` to `HasOne/WithMany`
  - Convert `HasMany` relationship configurations
  - Update `ToTable`, `HasKey`, `Property` configurations to EF Core syntax
- Register all configurations in `NopDbContext.OnModelCreating`
- Test each mapping against existing database schema

**Key Conversions:**
- `HasRequired(x => x.Parent).WithMany()` → `HasOne(x => x.Parent).WithMany()`
- `Property(x => x.Name).HasMaxLength(100)` → same syntax (compatible)
- `ToTable("TableName")` → same syntax (compatible)

**Demo:** All entities can be queried from existing database, relationships load correctly, no schema changes detected.

---

### Task 4: Migrate Nop.Services to .NET Standard 2.1

**Objective:** Convert service layer to .NET Standard 2.1, updating dependencies to use new data layer.

**Implementation:**
- Create new Nop.Services.csproj targeting `netstandard2.1`
- Update all service classes to reference new Nop.Core and Nop.Data
- Replace EF6-specific code (e.g., `Include` statements work same way)
- Update caching implementations (MemoryCacheManager for .NET Core)
- Migrate event publishing system
- Update all NuGet packages to .NET Standard compatible versions

**Key Updates:**
- `System.Runtime.Caching` → `Microsoft.Extensions.Caching.Memory`
- Keep service interfaces unchanged for compatibility
- Update Autofac registrations for .NET Core

**Demo:** Service layer compiles, unit tests pass using in-memory database.

---

### Task 5: Design Modern Plugin Architecture

**Objective:** Design new plugin system using .NET 8 AssemblyLoadContext, replacing shadow copying mechanism.

**Implementation:**
- Create `PluginLoadContext : AssemblyLoadContext` for plugin isolation
- Design new plugin discovery mechanism (scan Plugins folder)
- Create plugin manifest system (plugin.json replacing Description.txt)
- Implement plugin dependency resolution
- Design plugin lifecycle management (Load/Unload/Install/Uninstall)
- Create plugin interface compatibility layer for migration

**Architecture:**
```
PluginManager (static initialization removed)
  ↓
IPluginLoader (service-based)
  ↓
PluginLoadContext (per plugin)
  ↓
Plugin assemblies loaded in isolation
```

**Demo:** Design document and interfaces defined, can load a simple test plugin in isolated context.

---

### Task 6: Implement Plugin Loading Infrastructure

**Objective:** Implement the plugin loading system with AssemblyLoadContext.

**Implementation:**
- Implement `PluginLoadContext` with dependency resolution
- Implement `PluginLoader` service
- Create `PluginDescriptor` for .NET 8 (metadata, version, dependencies)
- Implement plugin discovery (scan Plugins folder for plugin.json)
- Implement plugin assembly loading with isolation
- Create plugin reference copying mechanism (no shadow copy needed)
- Handle plugin unloading and reloading

**Key Features:**
- Each plugin loads in separate AssemblyLoadContext
- Shared dependencies resolved from main context
- Plugin-specific dependencies isolated
- Support hot reload during development

**Demo:** Can discover and load multiple test plugins, each in isolated context, can access plugin types and instantiate them.

---

### Task 7: Migrate Nop.Web.Framework to .NET 8

**Objective:** Convert web framework library to ASP.NET Core, creating abstractions for MVC features.

**Implementation:**
- Create new Nop.Web.Framework.csproj targeting `net8.0`
- Migrate base controllers to ASP.NET Core
- Convert filters to ASP.NET Core filter attributes
- Migrate model binders and value providers
- Convert HTML helpers to Tag Helpers where appropriate
- Update routing infrastructure
- Migrate FluentValidation integration
- Update Autofac integration for ASP.NET Core

**Key Conversions:**
- `System.Web.Mvc.Controller` → `Microsoft.AspNetCore.Mvc.Controller`
- `ActionFilterAttribute` → ASP.NET Core filters
- `RouteAttribute` → ASP.NET Core routing
- Remove `System.Web` dependencies completely

**Demo:** Framework library compiles, basic controller can be instantiated and action methods discovered.

---

### Task 8: Create ASP.NET Core Web Application Shell

**Objective:** Create new Nop.Web project as ASP.NET Core application with basic infrastructure.

**Implementation:**
- Create new Nop.Web.csproj targeting `net8.0`
- Set up Program.cs with WebApplicationBuilder
- Configure services (DI, EF Core, Autofac, MVC)
- Set up middleware pipeline
- Migrate configuration from Web.config to appsettings.json
- Implement NopConfig section provider
- Set up static files and wwwroot
- Configure Kestrel/IIS hosting

**Configuration Migration:**
- Web.config → appsettings.json
- Custom NopConfig section → strongly-typed options
- Connection strings → appsettings.json
- App settings → configuration providers

**Demo:** Application starts, serves static files, basic health check endpoint responds.

---

### Task 9: Migrate Controllers and Views (Phase 1 - Public Store)

**Objective:** Migrate public-facing controllers and Razor views to ASP.NET Core.

**Implementation:**
- Migrate HomeController, CatalogController, ProductController
- Convert Razor views (minimal syntax changes needed)
- Update view models and model binding
- Migrate routing configuration
- Update form handling and validation
- Convert partial views and view components
- Update client-side validation

**Key Changes:**
- `@Html.Action` → View Components
- Form helpers mostly compatible
- Update `_ViewStart.cshtml` and `_Layout.cshtml`

**Demo:** Can browse home page, view product catalog, view product details with working navigation.

---

### Task 10: Migrate Controllers and Views (Phase 2 - Admin Area)

**Objective:** Migrate admin area controllers and views to ASP.NET Core.

**Implementation:**
- Migrate Nop.Admin project to `net8.0`
- Convert admin controllers
- Migrate admin views and layouts
- Update admin routing (area configuration)
- Migrate grid components and AJAX handlers
- Update file upload handling
- Migrate admin authentication and authorization

**Key Changes:**
- Area registration for admin
- Update Telerik/Kendo UI integration if used
- File upload using `IFormFile`

**Demo:** Can log into admin panel, view dashboard, manage products, settings pages load correctly.

---

### Task 11: Migrate Plugin Projects to .NET 8

**Objective:** Convert all 20 plugin projects to .NET 8 with new plugin architecture.

**Implementation:**
- Update each plugin .csproj to target `net8.0`
- Update plugin descriptors (Description.txt → plugin.json)
- Update plugin base classes to new architecture
- Remove shadow copy dependencies
- Update plugin controllers for ASP.NET Core
- Update plugin views and static content
- Test plugin installation/uninstallation
- Migrate plugin-specific dependencies

**Plugins to Migrate:**
- Payment plugins (5): PayPal, Manual, CheckMoneyOrder, PurchaseOrder, PayPalDirect
- Shipping plugins (6): UPS, FedEx, USPS, CanadaPost, AustraliaPost, FixedOrByWeight
- Tax plugins (1): FixedOrByCountryStateZip
- Widget plugins (2): NivoSlider, GoogleAnalytics
- Other plugins (6): Facebook auth, discount rules, pickup, feed, exchange rate

**Demo:** All plugins load successfully, can be installed/uninstalled through admin, plugin functionality works (e.g., can configure PayPal, shipping methods appear).

---

### Task 12: Migrate Authentication and Authorization

**Objective:** Convert Forms Authentication to ASP.NET Core Identity/Cookie Authentication.

**Implementation:**
- Configure ASP.NET Core Cookie Authentication
- Migrate authentication middleware
- Update login/logout logic
- Migrate password hashing (ensure compatibility with existing hashes)
- Update authorization policies
- Migrate external authentication (Facebook plugin)
- Update anti-forgery token handling

**Key Changes:**
- Forms Authentication → Cookie Authentication middleware
- `[Authorize]` attribute mostly compatible
- Update authentication ticket creation
- Ensure existing password hashes still validate

**Demo:** Can log in with existing user credentials, authentication persists across requests, authorization rules enforced, can log out.

---

### Task 13: Migrate Test Projects to .NET 8

**Objective:** Convert all test projects to .NET 8 and update to latest NUnit.

**Implementation:**
- Update test project .csproj files to target `net8.0`
- Update NUnit to latest version (3.x → 4.x if needed)
- Update test infrastructure and helpers
- Replace RhinoMocks with NSubstitute or Moq
- Update EF Core in-memory database for tests
- Fix broken tests due to API changes
- Ensure all tests pass

**Test Projects:**
- Nop.Core.Tests
- Nop.Data.Tests
- Nop.Services.Tests
- Nop.Web.MVC.Tests
- Nop.Tests

**Demo:** All test projects compile, test runner discovers all tests, all tests pass (or documented failures with migration plan).

---

### Task 14: Performance Optimization and Caching

**Objective:** Optimize performance and implement distributed caching for .NET 8.

**Implementation:**
- Benchmark current vs migrated performance
- Optimize EF Core queries (add AsNoTracking where appropriate)
- Implement response caching middleware
- Update Redis caching implementation for .NET 8
- Optimize static file serving
- Implement output caching for product pages
- Profile and optimize hot paths
- Update MiniProfiler integration

**Key Optimizations:**
- Use compiled queries where beneficial
- Implement response compression
- Optimize middleware pipeline order
- Use `Span<T>` and `Memory<T>` for performance-critical code

**Demo:** Performance benchmarks show equal or better performance, caching works correctly, MiniProfiler shows optimized query counts.

---

### Task 15: Database Migration Tooling

**Objective:** Create tools to help users migrate from .NET Framework to .NET 8 version.

**Implementation:**
- Create database compatibility checker
- Create configuration migration tool (Web.config → appsettings.json)
- Create plugin compatibility checker
- Document breaking changes
- Create migration guide
- Create rollback procedures
- Test migration on sample databases

**Tools:**
- `nop-migrate-config` - converts Web.config to appsettings.json
- `nop-check-db` - validates database compatibility
- `nop-check-plugins` - identifies incompatible plugins

**Demo:** Migration tools successfully convert sample configuration, identify issues, provide clear guidance for manual steps.

---

### Task 16: Integration Testing and Validation

**Objective:** Comprehensive end-to-end testing of migrated application.

**Implementation:**
- Create integration test suite
- Test complete customer journey (browse, add to cart, checkout)
- Test admin workflows (product management, order processing)
- Test all 20 plugins functionality
- Test with existing production-like database
- Load testing and stress testing
- Cross-browser testing
- Mobile responsiveness testing

**Test Scenarios:**
- Customer registration and login
- Product search and filtering
- Shopping cart operations
- Checkout process with various payment methods
- Order management
- Admin CRUD operations
- Plugin installation/configuration
- Multi-store functionality
- Localization and currencies

**Demo:** All integration tests pass, application handles production-like load, no data corruption, all features functional.

---

### Task 17: Documentation and Deployment

**Objective:** Create comprehensive documentation and deployment procedures.

**Implementation:**
- Update developer documentation
- Create deployment guide for .NET 8
- Document hosting requirements (IIS, Kestrel, Docker)
- Create Docker containerization option
- Document breaking changes and migration path
- Update plugin development guide
- Create troubleshooting guide
- Document performance tuning options

**Documentation:**
- Migration guide from 3.9 to .NET 8
- New plugin development guide
- Deployment guide (IIS, Linux, Docker)
- Configuration reference
- Breaking changes document
- Performance tuning guide

**Demo:** Documentation is complete, can successfully deploy to test environment following deployment guide, Docker container runs successfully.

---

## Migration Phases Timeline

### Phase 1: Foundation (Weeks 1-4)
- Tasks 1-2: .NET Standard foundation and EF Core setup

### Phase 2: Data Layer (Weeks 5-8)
- Task 3: Entity configuration migration
- Task 4: Services migration

### Phase 3: Plugin Architecture (Weeks 9-12)
- Tasks 5-6: New plugin system design and implementation

### Phase 4: Web Layer (Weeks 13-18)
- Tasks 7-10: Web framework and application migration

### Phase 5: Plugins & Auth (Weeks 19-22)
- Tasks 11-12: Plugin migration and authentication

### Phase 6: Testing & Deployment (Weeks 23-26)
- Tasks 13-17: Testing, optimization, and documentation

---

## Risk Mitigation

### High-Risk Areas
1. **Entity Framework Migration**: 106 mappings must preserve exact schema
   - Mitigation: Automated schema comparison, extensive testing
   
2. **Plugin System**: Complete architectural change
   - Mitigation: Incremental rollout, compatibility layer, thorough testing

3. **Authentication**: Password hash compatibility critical
   - Mitigation: Test with production data copy, ensure hash validation works

4. **Performance**: Must match or exceed current performance
   - Mitigation: Continuous benchmarking, profiling, optimization

### Rollback Strategy
- Keep .NET Framework version in separate branch
- Database schema unchanged allows easy rollback
- Document rollback procedures
- Test rollback process

---

## Success Metrics

- ✅ All 31 projects compile on .NET 8
- ✅ All 106 entity mappings preserve database schema
- ✅ All 20 plugins functional
- ✅ All tests passing (unit + integration)
- ✅ Performance within 10% of current (preferably better)
- ✅ Zero data loss or corruption
- ✅ Existing user credentials work
- ✅ Documentation complete

---

## Notes

- This plan assumes minimal custom modifications to the base nopCommerce 3.9
- Third-party plugins not included in the 20 core plugins will need separate migration
- Consider creating a migration branch to preserve original code
- Regular backups essential during migration process
- Recommend staging environment for thorough testing before production deployment
