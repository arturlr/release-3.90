# Ralph Discovery Log

Append-only log of cross-iteration findings. Never edit or remove previous entries.


## 2026-04-09 — Scaffold Iteration

### ATXDocumentation Gaps
- `ATXDocumentation/architecture/components.md` referenced in README but does not exist
- `ATXDocumentation/architecture/patterns.md` referenced in README but does not exist
- `ATXDocumentation/migration/component-order.md` referenced in README but does not exist
- `ATXDocumentation/technical-debt/remediation-plan.md` referenced in README but does not exist
- `ATXDocumentation/technical-debt/outdated-components.md` referenced but does not exist
- `ATXDocumentation/technical-debt/maintenance-burden.md` referenced but does not exist
- Available ATXDocumentation: 39 files across architecture (2), behavior (4), reference (3), analysis (4), migration (2), specialized (3), technical-debt (2), diagrams (4), plus README and project-overview and technical-debt-report

### Codebase Facts
- All 31 projects target .NET Framework 4.5.1 (old-style MSBuild ToolsVersion 12.0)
- 1731 C# files, ~252K LOC total
- 207 domain entity files in `Nop.Core/Domain/` across 26 subdirectories
- 117 service interfaces in `Nop.Services/`
- 27 public controllers (excluding BasePublicController)
- 54 admin controllers (excluding BaseAdminController)
- 20 plugin projects
- 5 test projects

### God Classes (>1000 LOC)
- FedEx SOAP Reference.cs: 16721 LOC (generated, will be replaced by REST API)
- CodeFirstInstallationService.cs: 12269 LOC (seed data — split into domain-specific seeders)
- Admin ProductController: 4857 LOC
- FedEx TrackService.cs: 4850 LOC (generated SOAP)
- Admin OrderController: 4379 LOC
- OrderProcessingService: 3167 LOC
- UPS SOAP Reference.cs: 2787 LOC (generated, will be replaced)
- Admin CustomerController: 2396 LOC
- Admin SettingController: 2339 LOC
- ProductService: 2142 LOC
- WorkflowMessageService: 1920 LOC

### Pattern Decisions
- Replace Autofac with built-in DI (Microsoft.Extensions.DependencyInjection)
- Replace Newtonsoft.Json with System.Text.Json
- Replace custom ICacheManager with IMemoryCache + IDistributedCache
- Replace Forms Authentication with ASP.NET Core Cookie Authentication
- Replace SOAP web references (FedEx, UPS) with REST API clients
- Replace SQL CE support with SQL Server only
- Keep IRepository<T> pattern, implement with EF Core
- Keep domain event pattern (EntityInserted/Updated/Deleted)
- Keep model factory pattern for view model construction
- Keep GenericAttribute extension property system

### Cross-Cutting Findings
- No GDPR service directory exists in legacy — GDPR features may be spread across CustomerService
- No dedicated observability — only ILogger (DB) and KeepAliveTask
- Authentication is custom FormsAuthenticationService, not ASP.NET Identity
- Plugin system uses shadow copying and dynamic assembly loading
- Kendo UI used extensively in admin grids — needs replacement evaluation

## 2026-04-09 — Refine Iteration 2

### Gap Analysis: 25 Missing Service Interfaces
Compared all 117 service interfaces in `src/Libraries/Nop.Services/` against spec files. Found 25 interfaces not explicitly listed in any spec. All were sub-services belonging to existing spec domains:

- **svc-common.md**: Added `IAddressAttributeFormatter`, `IAddressAttributeParser`
- **svc-catalog.md**: Added `IProductAttributeParser`, `IProductAttributeFormatter`, `ICompareProductsService`, `ICategoryTemplateService`, `IManufacturerTemplateService`, `IProductTemplateService`
- **svc-orders.md**: Added `ICheckoutAttributeParser`, `ICheckoutAttributeFormatter`, `IOrderReportService`, `IRewardPointService`, `ICustomNumberFormatter`
- **svc-customers.md**: Added `ICustomerAttributeParser`, `ICustomerAttributeFormatter`, `ICustomerReportService`
- **svc-directory.md**: Added `IGeoLookupService`
- **svc-tax.md**: Added `ITaxCategoryService`
- **svc-seo.md**: Added `ISitemapGenerator`
- **svc-messages.md**: Added `ITokenizer`, `IEmailSender`
- **svc-helpers.md**: Added `IUserAgentHelper`
- **svc-events.md**: Added `ISubscriptionService`
- **xcut-authentication.md**: Added `IOpenAuthenticationService`, `IClaimsTranslator`, `IExternalAuthorizer`, `IExternalProviderAuthorizer`
- **nop-core-infrastructure.md**: Added `IMachineNameProvider`

### Gap Analysis: 5 Missing Controllers
- **Admin**: `JbimagesController` (79 LOC) + `RoxyFilemanController` (765 LOC) — TinyMCE file managers. Added plan item [5.82] to replace with modern file manager.
- **Public**: `BackwardCompatibility1XController` (271 LOC) + `BackwardCompatibility2XController` (113 LOC) — legacy URL redirects. Added plan item [5.28] for redirect middleware.
- **Public**: `KeepAliveController` (14 LOC) — simple health check. Added plan item [5.27] mapping to observability spec.

### Plan Item Growth
- Plan items: 165 → 168 (+3 new items)
- All 25 service interfaces now explicitly listed in their parent specs
- All plan items enriched with complete sub-service lists

## 2026-04-09 — Refine Iteration 3

### Gap Analysis: EU VIES VAT Validation
- `TaxService.DoVatCheck()` calls `EuropaCheckVatService.checkVatService` — a SOAP web reference at `src/Libraries/Nop.Services/Web References/EuropaCheckVatService/`
- Endpoint: `http://ec.europa.eu/taxation_customs/vies/services/checkVatService`
- Not documented in any spec or plan item. Added to `svc-tax.md` external dependencies and acceptance criteria, plus new plan item [7.12]

### Gap Analysis: MaxMind GeoLite2
- `GeoLookupService` uses `MaxMind.GeoIP2` NuGet package with local `App_Data/GeoLite2-Country.mmdb` database file
- Used by `TaxService` (tax jurisdiction) and `OnlineCustomerController` (admin IP geolocation)
- Added to `svc-directory.md` external dependencies

### Gap Analysis: Testing Strategy
- Legacy has 5 test projects (Core, Data, Services, Tests shared, Web.MVC) using NUnit + Rhino Mocks
- ATXDocumentation/migration/test-specifications.md defines coverage targets and critical paths
- No ralph spec or plan item existed for testing. Created `specs/testing-strategy.md` and plan item [1.7]

### OfficialFeedManager — Intentionally Not Migrated
- `IOfficialFeedManager` / `OfficialFeedManager` makes HTTP calls to `nopcommerce.com/extensionsxml.aspx` for plugin marketplace feed
- Used only by admin `PluginController` to show available plugins from nopCommerce marketplace
- Decision: Do NOT migrate — this is nopCommerce marketplace-specific functionality. The new system will have its own plugin management without external marketplace dependency. Covered implicitly by plan item [5.39] (Admin PluginController)

### Plan Item Growth
- Specs: 66 → 67 (+1: testing-strategy.md)
- Plan items: 168 → 170 (+2: [1.7] test scaffold, [7.12] VIES VAT integration)
- Integration items: 11 → 12

## 2026-04-09 — Refine Iteration 4

### Gap Analysis: Azure Blob Storage in Media
- `AzurePictureService` (167 LOC) extends `PictureService` for Azure Blob container storage
- Uses `Microsoft.WindowsAzure.Storage` (CloudStorageAccount, CloudBlobClient, CloudBlobContainer)
- Configured via `NopConfig.AzureBlobStorageConnectionString`, `AzureBlobStorageContainerName`, `AzureBlobStorageEndPoint`
- Was listed as "consider" in svc-media.md — upgraded to explicit external dependency with acceptance criterion
- Target: `Azure.Storage.Blobs` SDK

### Gap Analysis: reCAPTCHA Integration
- Google reCAPTCHA is a significant cross-cutting security feature not previously documented in any spec
- 10 files in `Nop.Web.Framework/Security/Captcha/` — `GReCaptchaValidator` calls `https://www.google.com/recaptcha/api/siteverify`
- Used by 7 controllers (Blog, Common, Customer, News, Product, ShoppingCart, Vendor) and 7 model factories
- Supports reCAPTCHA v2 and v3 via `ReCaptchaVersion` enum
- Settings: `CaptchaSettings` (Enabled, PublicKey, PrivateKey, Version, ShowOnLoginPage, ShowOnRegistrationPage, etc.)
- Added to `xcut-security.md` and new plan item [7.13]

### Gap Analysis: Honeypot Anti-Spam
- `HoneypotValidatorAttribute` in `Nop.Web.Framework/Security/Honeypot/` — hidden form field anti-spam
- Configured via `SecuritySettings.HoneypotEnabled`
- Used on customer registration
- Added to `xcut-security.md`

### Gap Analysis: Shipment Tracking
- `IShipmentTracker` and `GeneralShipmentTracker` in `Nop.Services/Shipping/Tracking/` were not explicitly listed in svc-shipping.md
- `GeneralShipmentTracker` aggregates tracking from all active shipping plugins
- Added to spec

### Enrichments (no new items)
- `nop-web-framework.md`: Added explicit mentions of Kendo UI classes (DataSourceRequest, DataSourceResult, Filter, Sort), admin menu system (IAdminMenuPlugin, SiteMapNode, XmlSiteMap), IPageHeadBuilder, CaptchaValidatorAttribute, HoneypotValidatorAttribute
- `xcut-security.md`: Expanded from 4 to 6 acceptance criteria; added Captcha, Honeypot, HTTPS, anti-forgery, IP validation to legacy source and migration notes

### Plan Item Growth
- Plan items: 170 → 171 (+1: [7.13] Google reCAPTCHA integration)
- Integration items: 12 → 13

## 2026-04-09 — Refine Iteration 5

### Gap Analysis: Nop.Web.Framework Missing Detail
- `nop-web-framework.md` was missing explicit listing of 8 root-level action filter attributes: `CheckAffiliateAttribute`, `CustomerLastActivityAttribute`, `LanguageSeoCodeAttribute`, `PublicStoreAllowNavigationAttribute`, `StoreClosedAttribute`, `StoreIpAddressAttribute`, `StoreLastVisitedPageAttribute`, `ValidatePasswordAttribute`
- Also missing: `WebWorkContext`, `WebStoreContext` (IWorkContext/IStoreContext implementations), `RemotePost` (payment gateway form POST helper), `NopResourceDisplayName` (localized display name attribute), routing infrastructure (`IRouteProvider`, `IRoutePublisher`, `GenericPathRoute`), custom model binders (`NopModelBinder`, `CommaSeparatedModelBinder`), custom action results (`RssActionResult`, `NullJsonResult`, `XmlDownloadResult`, `ConverterJsonResult`), base models (`BaseNopModel`, `BasePageableModel`), FluentValidation classes (`BaseNopValidator<T>`, `CreditCardPropertyValidator`, `DecimalPropertyValidator`), localization helpers (`ILocalizedModel`, `LocalizedRoute`), admin events (`AdminTabStripCreated`, `ProductSearchEvent`)
- All added to Key Entities, Migration Notes, and Acceptance Criteria

### Gap Analysis: Nop.Core Root-Level Utilities
- `nop-core-infrastructure.md` was missing root-level Nop.Core utility classes: `CommonHelper`, `NopException`, `NopVersion`, `MimeTypes`, `XmlHelper`, `IPagedList<T>`/`PagedList<T>`, `Extensions`
- Also missing: `Html/` subdirectory (`HtmlHelper`, `BBCodeHelper`, `ResolveLinksHelper`, `CodeFormatter/`), `ComponentModel/` (`GenericDictionaryTypeConverter<T>`, `GenericListTypeConverter<T>`)
- `Fakes/` directory noted as drop-target — ASP.NET Core provides `WebApplicationFactory` for testing
- All added to Key Entities and Migration Notes

### Gap Analysis: ModelCacheEventConsumer
- Public `ModelCacheEventConsumer` (1335 LOC) and Admin `ModelCacheEventConsumer` (144 LOC) — presentation-layer cache invalidation event handlers
- Were not explicitly mentioned in `nop-web-public.md` or `nop-admin.md` Legacy Source sections
- Added to both specs

### Gap Analysis: Public Model Factories
- 21 model factory interfaces in `src/Presentation/Nop.Web/Factories/` were mentioned generically but not enumerated
- Full list now in `nop-web-public.md`: Address, Blog, Catalog, Checkout, Common, Country, Customer, ExternalAuthentication, Forum, Newsletter, News, Order, Poll, PrivateMessages, Product, Profile, ReturnRequest, ShoppingCart, Topic, Vendor, Widget

### ATXDocumentation Inaccuracy
- `IInventoryService` mentioned in `ATXDocumentation/architecture/system-overview.md` but does not exist in codebase — inventory management is part of `IProductService`

### No New Specs or Plan Items
- All gaps were enrichment of existing specs (more detail in Key Entities, Migration Notes, Acceptance Criteria)
- No new components discovered — 67 specs and 171 plan items remain stable
- Admin area confirmed to NOT use model factories (inline model construction in controllers)

## 2026-04-09 — Refine Iteration 6

### Gap Analysis: IPermissionProvider + StandardPermissionProvider
- `IPermissionProvider` (interface) and `StandardPermissionProvider` (40+ permission constants) in `Nop.Services/Security/` were not mentioned in any spec
- `StandardPermissionProvider` defines all admin permissions (ManageProducts, ManageOrders, AccessAdminPanel, etc.) and public permissions (DisplayPrices, EnableShoppingCart, EnableWishlist, PublicStoreAllowNavigation)
- `IPermissionProvider` is the extension point for plugins to register custom permissions
- `PermissionService.InstallPermissions()` discovers all `IPermissionProvider` implementations to seed permission records
- Added to `xcut-authorization.md`: Legacy Source, Key Entities, Migration Notes, and new acceptance criterion

### Gap Analysis: NopConfig (XML App Config)
- `NopConfig` in `Nop.Core/Configuration/` implements `IConfigurationSectionHandler` — legacy XML config section from web.config
- Contains app-level settings: Redis connection, Azure Blob storage, web farm mode, user agent DB paths, installation flags
- Not mentioned in any spec. Added to `nop-core-infrastructure.md`: Legacy Source, Key Entities, Migration Notes
- Migration: replace with `appsettings.json` + `IOptions<NopConfig>` pattern

### Gap Analysis: ISettings Marker Interface
- `ISettings` in `Nop.Core/Configuration/` is the marker interface for all DB-backed settings classes (CatalogSettings, OrderSettings, etc.)
- Was mentioned in `svc-configuration.md` but not in `nop-core-infrastructure.md` where it physically lives
- Added to `nop-core-infrastructure.md` Key Entities and Migration Notes

### Comprehensive Cross-Check Results
- All 117 service interfaces verified against specs — only `IPermissionProvider` was missing (now fixed)
- All 55 admin controllers and 28 public controllers confirmed in plan
- All 67 spec files referenced in plan items
- All domain subdirectories (26) and service subdirectories (30+) verified covered
- Google Shopping plugin confirmed as feed generator (no external API calls) — no missing integration item
- `Nop.Services/Extensions.cs` (enum-to-SelectList helper) — minor utility, covered implicitly by web framework migration

### No New Specs or Plan Items
- All gaps were enrichment of existing specs (3 specs updated: xcut-authorization.md, nop-core-infrastructure.md)
- 67 specs and 171 plan items remain stable

## 2026-04-09 — Refine Iteration 7

### Gap Analysis: Missing Integration Plan Items
- **Azure Blob Storage**: `AzurePictureService` (167 LOC) uses `Microsoft.WindowsAzure.Storage` (CloudStorageAccount, CloudBlobClient, CloudBlobContainer). Was documented in `svc-media.md` External Dependencies and Acceptance Criteria since iteration 4, but had no dedicated Phase 7 integration item. Added [7.14].
- **MaxMind GeoIP2**: `GeoLookupService` uses `MaxMind.GeoIP2` NuGet with local `App_Data/GeoLite2-Country.mmdb` database. Was documented in `svc-directory.md` External Dependencies since iteration 4, but had no dedicated Phase 7 integration item. Added [7.15].

### Comprehensive Verification
- All 117 service interfaces confirmed covered in specs
- All 26 domain subdirectories confirmed covered
- All controllers (28 public + 55 admin) confirmed in plan
- All 67 specs referenced by at least one plan item
- All 173 plan items reference a valid spec file
- All specs have ≥3 acceptance criteria
- All external NuGet packages verified: DotNetOpenAuth (xcut-authentication), EPPlus (svc-export-import), ImageResizer (svc-media), iTextSharp (svc-common), MiniProfiler (nop-web-public), RedLock (xcut-caching), PayPal (plugin specs), StackExchange.Redis (xcut-caching), WindowsAzure.Storage (svc-media)
- SOAP web references verified: EuropaCheckVatService ([7.12]), FedEx RateService ([7.5]), UPS TrackService ([7.4])

### Plan Item Growth
- Plan items: 171 → 173 (+2: [7.14] Azure Blob Storage, [7.15] MaxMind GeoIP2)
- Integration items: 13 → 15
- Specs: 67 (unchanged)

## 2026-04-09 — Refine Iteration 8

### Gap Analysis: Scheduled Task Infrastructure
- `TaskManager` (singleton scheduler) and `TaskThread` (per-interval timer thread) in `Nop.Services/Tasks/` were not mentioned in `svc-tasks.md`
- 6 concrete `ITask` implementations were not enumerated: `UpdateExchangeRateTask` (Directory), `DeleteGuestsTask` (Customers), `QueuedMessagesSendTask` (Messages), `ClearLogTask` (Logging), `ClearCacheTask` (Caching), `KeepAliveTask` (Common)
- `TaskManager.Initialize()` has catch-up logic: tasks not run for >30 minutes are executed immediately on startup
- All added to `svc-tasks.md` Legacy Source, Key Entities, and Migration Notes

### Gap Analysis: Service-Layer Cache Event Consumers
- 3 service-layer cache event consumers were not mentioned in their parent specs:
  - `PriceCacheEventConsumer` (203 LOC) in `Nop.Services/Catalog/Cache/` — invalidates price caches on changes to Category, Manufacturer, ProductCategory, ProductManufacturer, Setting, Product, TierPrice, Order. Added to `svc-catalog.md`
  - `CustomerCacheEventConsumer` (51 LOC) in `Nop.Services/Customers/Cache/` — invalidates customer password lifetime cache on `CustomerPasswordChangedEvent`. Added to `svc-customers.md`
  - `DiscountEventConsumer` (161 LOC) in `Nop.Services/Discounts/Cache/` — invalidates discount caches on changes to Discount, DiscountRequirement, Category, Manufacturer, Setting. Added to `svc-discounts.md`

### Gap Analysis: Startup Tasks
- `EfStartUpTask` (Nop.Data) and `TypeConverterRegistrationStartUpTask` (Nop.Core) — concrete `IStartupTask` implementations not mentioned in specs
- Added to `nop-core-infrastructure.md` and `nop-data.md`

### Gap Analysis: Web Framework Completeness
- `GuidConstraint` (custom `IRouteConstraint` for GUID route parameters) not in `nop-web-framework.md`. Added to routing infrastructure
- 8 MVC filter attributes not explicitly enumerated: `AdminAntiForgeryAttribute`, `AdminValidateIpAddressAttribute`, `FormValueRequiredAttribute`, `NopHttpsRequirementAttribute`, `NoTrimAttribute`, `ParameterBasedOnFormNameAttribute`, `PublicAntiForgeryAttribute`, `WwwRequirementAttribute`. All now explicitly listed
- HTML helper/extension classes not enumerated: `HtmlExtensions` (697 LOC), `LayoutExtensions` (366 LOC), `DataListExtensions`, `UrlHelperExtensions`, `LocalizedRouteExtensions`, `ModelStateExtensions`, `QueryableExtensions`, `FilePermissionHelper` (186 LOC). All now listed

### Gap Analysis: Core Interfaces
- `ILocalizedEnum` (marker interface for localized enums) in `Nop.Core.Domain.Localization` — not in any spec. Added to `nop-core-domain.md`
- `IMapperConfiguration` / `AutoMapperConfiguration` (mapper registration infrastructure) in `Nop.Core.Infrastructure.Mapper` — not in any spec. Added to `nop-core-infrastructure.md`
- `IOfficialFeedManager` — already documented as intentionally not migrated (iteration 3)

### No New Specs or Plan Items
- All gaps were enrichment of existing specs (8 specs updated)
- 67 specs and 173 plan items remain stable
- All Nop.Core interfaces now verified covered (except IOfficialFeedManager — intentionally excluded)
- All Nop.Services interfaces verified covered (0 missing)

## 2026-04-09 — Refine Iteration 9

### Gap Analysis: SeoExtensions (1386 LOC)
- `SeoExtensions` in `Nop.Services/Seo/` is a static class with entity-specific `GetSeName()` extension methods and a character transliteration table (`_seoCharacterTable`) with lazy initialization
- Not mentioned in `svc-seo.md` Legacy Source or Key Entities. Added with full method inventory and migration notes (consider `Lazy<T>` or `FrozenDictionary` for transliteration table)

### Gap Analysis: Admin MappingExtensions (1195 LOC) + AdminMapperConfiguration (1031 LOC)
- `MappingExtensions` in `Administration/Extensions/` defines all entity↔model mapping extension methods for admin area
- `AdminMapperConfiguration` in `Administration/Infrastructure/Mapper/` defines all AutoMapper profiles for admin
- Neither was explicitly mentioned in `nop-admin.md`. Added to Legacy Source and Migration Notes
- Combined 2226 LOC defines the complete admin mapping surface — critical for migration

### Gap Analysis: Admin Validators (57 files) + Public Validators (20 files)
- `Administration/Validators/` contains 57 FluentValidation validator classes
- `Nop.Web/Validators/` contains 20 FluentValidation validator classes
- Neither directory was mentioned in `nop-admin.md` or `nop-web-public.md`. Added to both specs

### Gap Analysis: Public Web Extensions Directory
- `Nop.Web/Extensions/` contains `MappingExtensions` (61 LOC), `HtmlExtensions` (254 LOC), `AttributeParserHelper` (96 LOC)
- Not mentioned in `nop-web-public.md`. Added to Legacy Source

### Gap Analysis: Installation Localization Infrastructure
- `Nop.Web/Infrastructure/Installation/` contains `IInstallationLocalizationService`, `InstallationLocalizationService`, `InstallationLanguage`
- Separate localization system for the install wizard (reads from `App_Data/Localization/Installation/*.xml`)
- Not mentioned in `svc-installation.md` or `nop-web-public.md`. Added to both specs

### Gap Analysis: ImageResizer NuGet Package
- `ImageResizer` 4.0.5 used by `PictureService` for image resizing (`ImageBuilder.Current.Build()`)
- Not mentioned in `svc-media.md` External Dependencies. Added with migration target: `SixLabors.ImageSharp` or `SkiaSharp`

### Gap Analysis: System.Linq.Dynamic NuGet Package
- Used by `Tokenizer` (svc-messages), `QueryableExtensions` (nop-web-framework), `BaseNopValidator<T>` (nop-web-framework)
- Not mentioned in any spec. Added to `svc-messages.md` and `nop-web-framework.md` External Dependencies
- Migration target: `System.Linq.Dynamic.Core` NuGet or inline LINQ expressions

### Gap Analysis: MiniProfiler NuGet Package
- `MiniProfiler` 3.2 used in `Global.asax.cs` for conditional performance profiling in public store
- Controlled by `StoreInformationSettings.DisplayMiniProfilerInPublicStore`
- Not mentioned in any spec. Added to `nop-web-framework.md` External Dependencies
- Migration target: `MiniProfiler.AspNetCore.Mvc` or OpenTelemetry tracing

### Verified Non-Gaps
- `DateTimeConsumer` — test fixture only (Nop.Web.MVC.Tests), not production code
- `Microsoft.Web.RedisSessionStateProvider` — commented out in web.config, not an active dependency
- Google OAuth — mentioned in ATXDocumentation system overview but no plugin exists in codebase (only Facebook)
- All 117 service interfaces confirmed covered
- All 28 public + 55 admin controllers confirmed in plan

### No New Specs or Plan Items
- All gaps were enrichment of existing specs (6 specs updated: svc-seo.md, nop-web-public.md, nop-admin.md, svc-installation.md, svc-media.md, svc-messages.md, nop-web-framework.md)
- 67 specs and 173 plan items remain stable

## 2026-04-09 — Refine Iteration 10

### Comprehensive Gap Analysis: No New Gaps Found
Performed exhaustive verification across all dimensions:

- **117/117 service interfaces** in `Nop.Services/` confirmed covered in specs (0 missing)
- **All Nop.Core interfaces** confirmed covered (only `IOfficialFeedManager` excluded — intentional, iteration 3)
- **54 admin controllers + 27 public controllers** all mapped to plan items (accounting for combined items [5.78], [5.82], [5.28])
- **All 26 domain subdirectories** confirmed covered
- **All Nop.Web.Framework subdirectories** (Controllers, Events, Kendoui, Localization, Menu, Mvc, Security, Seo, Themes, UI, Validators, ViewEngines) confirmed in spec
- **All Nop.Core subdirectories** (Caching, ComponentModel, Configuration, Data, Domain, Events, Fakes, Html, Infrastructure, Plugins) confirmed in specs
- **12 stored procedures/functions + 61 indexes** in `App_Data/Install/` confirmed covered by `data-migration-sqlserver.md` and `nop-data.md`
- **All 67 spec files** referenced by at least one plan item
- **All 173 plan items** reference a valid spec file
- **All specs** have ≥3 acceptance criteria
- **No large files (>500 LOC)** found uncovered — model factories, plugin processors, admin models all covered by parent specs

### NuGet Package Audit
- 43 unique NuGet packages across all `packages.config` files
- All meaningful packages confirmed covered in specs or discoveries
- Remaining unmentioned packages are framework-level (.NET Framework, ASP.NET MVC, OWIN, System.* BCL) that are replaced wholesale by .NET 10 — no individual spec needed
- `Microsoft.Azure.KeyVault.Core` is a transitive dependency from Azure Storage SDK (binding redirect only, no code usage)

### Plan Convergence
- Specs: 67 (stable since iteration 3)
- Plan items: 173 (stable since iteration 7)
- No new components, interfaces, controllers, or integrations discovered
- Plan has reached convergence — all codebase artifacts are mapped to specs and plan items

## 2026-04-09 — [1.1] Solution Scaffold / Implementation

### SDK Version
- Plan targets .NET 10 but installed SDK is .NET 8.0.413
- Pinned `global.json` to 8.0.413 with `rollForward: latestFeature` so upgrading to .NET 10 later only requires updating global.json
- All .csproj files inherit `net8.0` from `Directory.Build.props` — single-point TFM change when upgrading

### New Code Layout
- New source code lives under `src/New/` to coexist with legacy `src/Libraries/`, `src/Presentation/`, `src/Plugins/`
- Test projects live under `tests/` (top-level, separate from legacy `src/Tests/`)
- Solution file: `src/NopCommerce.New.sln`

### Project Structure
- 6 source projects: `Nop.Core.Domain` → `Nop.Core` → `Nop.Data`, `Nop.Services` → `Nop.Web.Framework` → `Nop.Web`
- 5 test projects: one per source project except Web.Framework (tested via Nop.Web.Tests)
- Separated `Nop.Core.Domain` from `Nop.Core` — domain entities have zero dependencies, infrastructure (IRepository, IEngine, etc.) lives in Nop.Core

### Build Properties (Directory.Build.props)
- `Nullable: enable` — all new code uses nullable reference types
- `ImplicitUsings: enable` — reduces boilerplate
- `TreatWarningsAsErrors: true` — enforces clean code from day one
- `LangVersion: latest` — access to newest C# features

### Impact on Future Items
- [1.3] Nop.Core.Domain: add entity files to `src/New/Core/Nop.Core.Domain/`
- [1.4] Nop.Core.Infrastructure: add to `src/New/Core/Nop.Core/`
- [1.5] Nop.Data: add to `src/New/Data/Nop.Data/`
- [1.7] Test scaffold: test projects already created with xunit — just add test files
- TFM upgrade to .NET 10: change one line in `Directory.Build.props`

## 2026-04-09 — [1.3] Nop.Core.Domain / Implementation

### File Classification
- 207 legacy .cs files classified: 136 entities/enums/interfaces/DTOs/constants kept, 29 Settings classes excluded, 5 Extension classes excluded, 7 Events files excluded = 167 new files created
- Settings classes (ISettings) excluded — they depend on `Nop.Core.Configuration.ISettings` which lives in Nop.Core, not Nop.Core.Domain
- Extension classes excluded — business logic moves to service layer
- Events classes excluded — domain events move to service layer
- TypeConverter classes (PickupPointTypeConverter, ShippingOptionTypeConverter, ShippingOptionListTypeConverter) excluded — infrastructure concern

### Key Decisions
- **Navigation properties removed**: All `virtual ICollection<T>` and `virtual Entity` properties stripped. EF Core will configure relationships in `Nop.Data` entity configurations. Domain entities are pure data carriers.
- **Computed properties depending on nav properties removed**: `ShoppingCartItem.IsFreeShipping`, `ShoppingCartItem.IsShipEnabled`, `ShoppingCartItem.AdditionalShippingCharge`, `ShoppingCartItem.IsTaxExempt` — these accessed `Product` nav property. Will be service methods.
- **RecurringPayment.NextPaymentDate and CyclesRemaining removed**: Depended on `RecurringPaymentHistory` collection nav property. Will be service methods.
- **Order.ParseTaxRates/TaxRatesDictionary removed**: Business logic using `CultureInfo`, `Debug`, `SortedDictionary`. Will be a service method.
- **Address.Clone simplified**: Removed references to `Country` and `StateProvince` nav properties. Clone copies only scalar/FK properties.
- **ForumTopic.NumReplies removed**: Computed from `NumPosts` nav-independent but was a derived property — trivial to compute in service.
- **Nullable strings**: All `string` properties made `string?` since they're DB-mapped and nullable annotations are enabled with TreatWarningsAsErrors.
- **byte[] properties**: Made `byte[]?` (Picture.PictureBinary, Download.DownloadBinary).
- **SystemCustomerAttributeNames/SystemCustomerNames/SystemCustomerRoleNames**: Modernized from `static string` properties to `const string` fields.

### Impact on Future Items
- [1.4] Nop.Core.Infrastructure: Must define `ISettings` marker interface so Settings classes can be created
- [1.5] Nop.Data: Entity configurations will define all navigation properties and relationships
- [2.9] Domain events: Events classes will be recreated in Nop.Services/Events
- [3.x-4.x] Services: Extension methods (CustomerExtensions, GiftCardExtensions, etc.) and computed properties will become service methods
- [4.9] Order services: `ParseTaxRates` logic moves to order service

## 2026-04-09 — [1.4] Nop.Core.Infrastructure / Implementation

### Key Modernization Decisions
- **ICacheManager → IStaticCacheManager**: Replaced sync-only `ICacheManager` with async-first `IStaticCacheManager`. Uses `CacheKey` with prefix-based invalidation instead of regex pattern matching. Implementations will wrap `IMemoryCache` + `IDistributedCache`.
- **IWebHelper modernized**: Dropped `HttpRequest` parameter from `IsStaticResource()` (ASP.NET Core uses `IHttpContextAccessor`), dropped `ServerVariables()` (no equivalent in ASP.NET Core), dropped `RestartAppDomain()` (ASP.NET Framework specific).
- **BBCodeHelper decoupled**: Removed `EngineContext.Current.Resolve<CommonSettings>()` service locator call. `openLinksInNewWindow` is now a parameter. Dropped `replaceCode` / `CodeFormatHelper` — complex code formatter not needed.
- **CommonHelper modernized**: Uses `GeneratedRegex` for email validation, `RandomNumberGenerator` instead of `RNGCryptoServiceProvider`, `stackalloc` for digit code generation. Dropped `GetTrustLevel()` (CAS), `MapPath()` (use `IWebHostEnvironment.ContentRootPath`), `SetTelerikCulture()` (Kendo UI hack).
- **NopException simplified**: Dropped `[Serializable]` attribute and `SerializationInfo` constructor (obsolete in .NET 8+).
- **NopConfig**: Replaced `IConfigurationSectionHandler` XML parsing with simple POCO for `IOptions<NopConfig>` binding from `appsettings.json`.
- **XmlHelper**: Uses `XmlWriter.Create()` instead of deprecated `XmlTextWriter`.
- **IEventPublisher**: Lives in `Nop.Services` (plan item [2.9]), not `Nop.Core`. Domain event types (`EntityInserted<T>`, etc.) are in `Nop.Core/Events/`.

### Files Created (24 files in Nop.Core)
- `Data/IRepository.cs`, `IPagedList.cs`, `PagedList.cs`, `Configuration/ISettings.cs`
- `IWorkContext.cs`, `IStoreContext.cs`, `IWebHelper.cs`
- `Caching/CacheKey.cs`, `Caching/IStaticCacheManager.cs`
- `Events/EntityInserted.cs`, `Events/EntityUpdated.cs`, `Events/EntityDeleted.cs`
- `CommonHelper.cs`, `NopException.cs`, `NopVersion.cs`, `MimeTypes.cs`, `XmlHelper.cs`, `Extensions.cs`
- `ComponentModel/GenericListTypeConverter.cs`, `ComponentModel/GenericDictionaryTypeConverter.cs`
- `Html/HtmlHelper.cs`, `Html/BBCodeHelper.cs`, `Html/ResolveLinksHelper.cs`
- `Configuration/NopConfig.cs`

### Impact on Future Items
- [1.5] Nop.Data: Can now implement `EfRepository<T>` against `IRepository<T>`
- [2.1] Caching: Implement `IStaticCacheManager` with `MemoryCache` + optional Redis
- [2.9] Domain events: Implement `IEventPublisher` in Nop.Services consuming `EntityInserted<T>` etc.
- [3.1] Configuration: `ISettingService` uses `ISettings` marker to identify settings classes
- [5.1] Web.Framework: Implement `WebWorkContext`, `WebStoreContext`, `WebHelper` against interfaces

## 2026-04-09 — [1.5] Nop.Data / Implementation

### EF Core Configuration Approach
- No navigation properties in domain entities (stripped in [1.3]), so no FK relationship configurations needed
- EF Core convention-based FK discovery works via property names ending in `Id` (e.g., `CustomerId` → FK to Customer table)
- Legacy many-to-many relationships (Product_ProductTag_Mapping, Customer_CustomerRole_Mapping, Discount_AppliedToCategories, etc.) used EF6 `.HasMany().WithMany().Map()` — these will need explicit join entity configurations when services need them, but are NOT configured now since domain entities have no nav properties
- `ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())` replaces legacy reflection-based config discovery

### Enum Property Pattern
- All enum properties use backing `int` Id fields (e.g., `ProductTypeId` + `ProductType` computed property)
- EF Core Ignore on the enum property, store the int Id — same pattern as legacy EF6
- Nullable enum: `DiscountRequirement.InteractionType` uses `int?` backing field — Ignore works the same

### Computed Read-Only Properties
- `ForumTopic.NumReplies` — computed from `NumPosts`, no setter → must be Ignored
- `EmailAccount.FriendlyName` — computed from `Email` + `DisplayName`, no setter → must be Ignored
- `Setting.ToString()` — override of `object.ToString()`, EF Core doesn't try to map it

### Decimal Precision
- Money fields: `HasPrecision(18, 4)` — matches legacy exactly
- Currency rates: `HasPrecision(18, 8)` — `Currency.Rate`
- Measure ratios: `HasPrecision(18, 8)` — `MeasureDimension.Ratio`, `MeasureWeight.Ratio`

### EfRepository<T> Design
- Simplified from legacy: removed `IDbContext` abstraction, takes `NopDbContext` directly
- Uses `ArgumentNullException.ThrowIfNull()` instead of manual null checks
- Uses `AddRange`/`RemoveRange` for batch operations (EF Core native, more efficient than legacy per-item loop)
- `SaveChanges()` called per operation (same as legacy) — future optimization: unit of work pattern

### Impact on Future Items
- [1.6] EF Core migration: Can now generate initial migration from these configurations
- [2.1] Caching: `EfRepository<T>` is the injection point for cache-aside pattern
- [3.x-4.x] Services: All services inject `IRepository<T>`, resolved to `EfRepository<T>` via DI
- Many-to-many join tables (Product_ProductTag_Mapping, Customer_CustomerRole_Mapping, etc.) will need explicit join entities when services require cross-entity queries

## 2026-04-09 — [2.1] Caching / Implementation

### Pattern: Prefix-Based Invalidation vs Regex
- Legacy used regex-based `RemoveByPattern()` — created a new `Regex` per call, iterated all keys
- New approach uses prefix-based invalidation (`RemoveByPrefixAsync(string prefix)`) with `ConcurrentDictionary<string, byte>` key tracking
- `PostEvictionCallback` on each cache entry auto-cleans the key set when entries expire or are evicted
- Prefix matching is simpler, faster, and sufficient — all legacy cache key patterns were prefix-based in practice (e.g., `Nop.product.id-{0}` → prefix `Nop.product.`)

### DI Registration Pattern
- `MemoryCacheManager` should be registered as Singleton (wraps singleton `IMemoryCache`)
- `NopRequestCache` should be registered as Scoped (per-HTTP-request lifetime)
- DI registration deferred — no central DI composition root exists yet. Will be wired in Nop.Web `Program.cs` or a dedicated `ServiceCollectionExtensions` when services are implemented

### Redis Deferred
- `IDistributedCache` (Redis) implementation deferred to plan item [7.2]
- `MemoryCacheManager` is sufficient for single-instance deployment
- `NopConfig.RedisCachingEnabled` flag already exists for future conditional registration

### Impact on Future Items
- All Phase 3/4 services will inject `IStaticCacheManager` and `NopRequestCache`
- Cache key constants will be defined per service (e.g., `ProductCacheKeys`, `CategoryCacheKeys`)
- [2.9] Domain events: cache event consumers will call `RemoveByPrefixAsync` on entity changes

## 2026-04-09 — [2.2] Logging / Implementation

### ILogger → INopLogger Rename
- Legacy `ILogger` conflicts with `Microsoft.Extensions.Logging.ILogger` — renamed to `INopLogger`
- All downstream consumers (controllers, services) must use `INopLogger` for DB logging
- `Microsoft.Extensions.Logging.ILogger` remains available for structured logging to external sinks (deferred to [2.8])

### ActivityLog Entity Fix
- New `ActivityLog` entity was missing `IpAddress` property — legacy `CustomerActivityService.InsertActivity` sets it
- Added `IpAddress` to entity and EF Core configuration (`HasMaxLength(200)`)

### Deferred Dependencies
- `CommonSettings.IgnoreLogWordlist` — log message filtering deferred until [3.1] Configuration services provides `ISettingService`
- `TRUNCATE TABLE` optimization for `ClearLog()`/`ClearAllActivities()` — legacy used `IDbContext.ExecuteSqlCommand`. New code uses repository delete-all. Can optimize with `NopDbContext.Database.ExecuteSqlRaw()` when performance requires it
- `Microsoft.Extensions.Logging` integration — deferred to [2.8] Observability

### Pattern: Sync Cache API with Async Cache Manager
- `IStaticCacheManager` is async-first but `CustomerActivityService` methods are sync (matching legacy contract)
- Used `.GetAwaiter().GetResult()` for `RemoveByPrefixAsync` calls in sync methods
- `MemoryCacheManager` operations are effectively synchronous (in-memory), so no deadlock risk
- Future: consider adding sync overloads to `IStaticCacheManager` or making service methods async

### Pattern: Direct Entity Caching
- Legacy used `ActivityLogTypeForCaching` nested DTO class to cache activity log types
- New code caches `ActivityLogType` entities directly — simpler, no mapping overhead
- Safe because cached entities are read-only lookups (not tracked by EF Core after `ToList()`)

### ClearLogTask Not Implemented
- `ClearLogTask` (legacy `ITask` implementation) belongs to [3.6] Scheduled Tasks — not part of [2.2]
- Will be implemented when `IScheduleTaskService` / `IHostedService` infrastructure is built

## 2026-04-09 — [2.9] Domain Events / Implementation

### Architecture Decisions
- **Dropped ISubscriptionService**: Legacy used `ISubscriptionService` → `EngineContext.Current.ResolveAll<IConsumer<T>>()` (service locator). New code uses `IServiceProvider.GetServices<IConsumer<T>>()` directly in `EventPublisher` — simpler, no indirection layer needed with built-in DI.
- **Dropped plugin installed check**: Legacy `EventPublisher.PublishToConsumer` checked `PluginManager.ReferencedPlugins` to skip consumers from uninstalled plugins. Plugin system [2.10] not built yet — will add filtering when plugin infrastructure exists.
- **Async-first**: `IConsumer<T>.HandleEventAsync` replaces sync `HandleEvent`. `IEventPublisher.PublishAsync<T>` replaces sync `Publish<T>`. Extension methods return `Task` (`EntityInsertedAsync`, `EntityUpdatedAsync`, `EntityDeletedAsync`).
- **Error isolation preserved**: Each consumer invocation is try/caught individually — one failing consumer doesn't block others. Uses `ILogger<EventPublisher>` (Microsoft.Extensions.Logging) instead of legacy `ILogger` (DB logger) to avoid circular dependency (DB logger itself may publish events).

### No New Packages Required
- `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions` are transitively available through EF Core → no explicit PackageReference additions needed in Nop.Services.csproj.

### Impact on Future Items
- All services that publish entity events will call `await _eventPublisher.EntityInsertedAsync(entity)` etc.
- Cache event consumers (PriceCacheEventConsumer, CustomerCacheEventConsumer, DiscountEventConsumer, ModelCacheEventConsumer) will implement `IConsumer<EntityInserted<T>>` etc. — built when their parent services are implemented.
- DI registration: `services.AddScoped<IEventPublisher, EventPublisher>()` + `services.AddScoped<IConsumer<T>, ConcreteConsumer>()` for each consumer — wired when DI composition root is built.

## 2026-04-09 — [3.1] Configuration Services / Implementation

### Settings Classes Placement
- Settings classes depend on `ISettings` (Nop.Core) and domain enums (Nop.Core.Domain)
- Cannot live in `Nop.Core.Domain` (zero dependencies) — placed in `Nop.Core/Domain/{subdirectory}/` instead
- Namespace remains `Nop.Core.Domain.X.XSettings` matching legacy, since Nop.Core's RootNamespace is `Nop.Core`
- This was the reason Settings were excluded from [1.3] — now resolved

### Async Pattern for Sync Repository
- `IRepository<T>` methods are synchronous but `ISettingService` is async-first
- Methods that only call repository (GetSettingByIdAsync, GetAllSettingsAsync) use `Task.FromResult` to avoid CS1998
- `GetAllSettingsCachedAsync` lambda passed to `IStaticCacheManager.GetAsync` also uses `Task.FromResult` since repository calls are sync
- When EF Core async methods are added to IRepository, these can be converted to true async

### SetSetting Dynamic Type Handling
- Legacy used `dynamic` keyword for property values in SaveSetting — replaced with `object` cast and `SetSettingAsync(key, value ?? string.Empty, ...)` pattern
- `TypeDescriptor.GetConverter(typeof(T)).ConvertToInvariantString(value)` handles the serialization regardless of runtime type

### CaptchaSettings Not in Domain
- `CaptchaSettings` lives in `Nop.Web.Framework/Security/Captcha/` (presentation layer), not in domain
- Will be created when [5.1] Nop.Web.Framework is implemented
- Only 29 Settings classes are domain-level (matching legacy `Nop.Core.Domain` layout)

### Impact on Future Items
- All Phase 3/4 services can now inject `ISettingService` and call `LoadSettingAsync<T>()` for their settings
- [3.2] Store services: can use `ISettingService` for store-scoped settings
- [5.38] Admin SettingController: will use `SaveSettingAsync`, `SaveSettingOverridablePerStoreAsync`

## 2026-04-09 — [2.3] Localization / Implementation

### Stored Procedure Elimination
- Legacy `ImportResourcesFromXml` used `[LanguagePackImport]` stored procedure for bulk XML import via `IDbContext.ExecuteSqlCommand`
- New implementation uses EF Core repository bulk insert (`IRepository<T>.Insert(IEnumerable<T>)`) with in-memory dictionary lookup for existing resources
- No stored procedure dependency — simpler, portable, sufficient for import volumes

### Service Locator Elimination in Extensions
- Legacy `LocalizationExtensions` used `EngineContext.Current.Resolve<T>()` (service locator) extensively for `GetLocalized`, `GetLocalizedSetting`, `SaveLocalizedSetting`, plugin extensions
- New `LocalizationExtensions` takes all dependencies as explicit parameters — no service locator calls
- This changes the call-site signature: callers must pass `ILocalizedEntityService`, `ILanguageService`, etc. explicitly
- Impact: all controllers and model factories that call `entity.GetLocalized(x => x.Name)` will need to pass services

### IStoreMappingService Deferred
- Legacy `LanguageService.GetAllLanguages(storeId)` filtered by `IStoreMappingService.Authorize(language, storeId)`
- `IStoreMappingService` not built yet (plan item [3.2])
- `storeId` parameter kept in interface for API compatibility but not filtered — all languages returned regardless of store
- When [3.2] is implemented, add store mapping filter to `GetAllLanguagesAsync`

### LocalizedPropertyForCaching DTO Eliminated
- Legacy `LocalizedEntityService` used a nested `LocalizedPropertyForCaching` DTO class to cache localized properties
- New implementation caches `LocalizedProperty` entities directly from `TableNoTracking` — simpler, no mapping overhead
- Safe because cached entities are read-only (not tracked by EF Core)

### Plugin and Permission Extensions Deferred
- Plugin extensions (`DeletePluginLocaleResource`, `AddOrUpdatePluginLocaleResource`, `GetLocalizedFriendlyName`, `SaveLocalizedFriendlyName`) depend on `IPlugin`/`BasePlugin` — deferred to [2.10]
- Permission extensions (`GetLocalizedPermissionName`, `SaveLocalizedPermissionName`, `DeleteLocalizedPermissionName`) depend on `PermissionRecord` — deferred to [2.4]

### Localized Routes Not Implemented
- `Nop.Web.Framework/Localization/` contains `LocalizedRoute`, `LocalizedRouteExtensions`, `ILocalizedModel<T>` — presentation-layer localization
- These belong to [5.1] Nop.Web.Framework, not [2.3] service layer
- Acceptance criterion "Localized URL routing resolves language from URL prefix" will be addressed in [5.1]

### Impact on Future Items
- [2.4] Security: add `GetLocalizedPermissionNameAsync`, `SaveLocalizedPermissionNameAsync`, `DeleteLocalizedPermissionNameAsync` to extensions
- [2.10] Plugin system: add plugin locale resource extensions
- [3.2] Store services: add `IStoreMappingService` filtering to `LanguageService.GetAllLanguagesAsync`
- [5.1] Web.Framework: implement localized routes, `ILocalizedModel<T>`, `NopResourceDisplayName`
- All Phase 3/4 services that need localized entity properties will use `GetLocalizedAsync` extension

## 2026-04-09 — [2.4] Security / Implementation

### Join Entities for Many-to-Many Relationships
- Legacy `Customer.CustomerRoles` and `CustomerRole.PermissionRecords` nav properties were stripped in [1.3]
- Created `CustomerCustomerRoleMapping` (table: `Customer_CustomerRole_Mapping`) and `PermissionRecordRoleMapping` (table: `PermissionRecord_Role_Mapping`) as explicit join entities
- All services that need customer→role or role→permission lookups must query via these join entity repositories
- EF Core configurations and DbSets added for both

### Encryption Modernization
- **TripleDES → AES**: Legacy used `TripleDESCryptoServiceProvider` (obsolete, weak). New code uses `Aes.Create()` with `EncryptCbc`/`DecryptCbc`
- **Key derivation changed**: Legacy used 16-char key (first 16 bytes for key, bytes 8-16 for 8-byte IV). New code uses 24-char key (first 16 bytes for AES-128 key, bytes 8-24 for 16-byte IV). This is a **breaking change** — existing encrypted data cannot be decrypted with the new service
- **Data migration impact**: Plan item [8.3] or a dedicated migration step must re-encrypt any TripleDES-encrypted data (credit card numbers, etc.) during migration
- **Hash modernization**: `HashAlgorithm.Create()` → static `SHA1.HashData()`, `SHA256.HashData()`, etc. `BitConverter.ToString().Replace("-","")` → `Convert.ToHexString()`. `RNGCryptoServiceProvider` → `RandomNumberGenerator.GetBytes()`
- Legacy hash algorithms (SHA1, MD5) kept for migration compatibility — existing password hashes must still verify

### Permission Service Pattern: No ICustomerService Dependency
- Legacy `PermissionService` depended on `ICustomerService` for `GetCustomerRoleBySystemName()` during `InstallPermissions()`
- New code queries `IRepository<CustomerRole>` directly — avoids circular dependency risk (ICustomerService depends on IPermissionService in some patterns)
- `InstallPermissions` creates missing roles directly via repository

### Sync-over-Async Pattern in Security Services
- `IStaticCacheManager` and `IEventPublisher` are async-first, but security service methods match legacy sync signatures
- Used `.GetAwaiter().GetResult()` for cache invalidation and event publishing in sync methods
- `MemoryCacheManager` operations are effectively synchronous (in-memory), so no deadlock risk in non-ASP.NET-Core-request contexts
- Future: consider making security service methods async when downstream consumers are updated

### Permission Localization Extensions
- Added `SaveLocalizedPermissionName` and `DeleteLocalizedPermissionName` extension methods to `LocalizationExtensions.cs`
- These were deferred from [2.3] to [2.4] per DISCOVERIES.md
- Both are sync wrappers over async `AddOrUpdateLocaleResourceAsync`/`DeleteLocaleResourceAsync`

### Impact on Future Items
- [2.5] Authentication: can now use `IPermissionService` for permission checks during sign-in
- [2.6] Authorization: can now build ASP.NET Core authorization policies backed by `IPermissionService`
- [4.1] Customer services: `CustomerCustomerRoleMapping` repository available for role management
- [8.x] Data migration: must handle TripleDES→AES re-encryption of sensitive data
- Any service needing customer roles must inject `IRepository<CustomerCustomerRoleMapping>` — this is a cross-cutting pattern

## 2026-04-09 — [3.2] Store Services / Implementation

### StoreExtensions Eliminated
- Legacy `StoreExtensions.ParseHostValues()` and `ContainsHostValue()` were extension methods on `Store` entity in `Nop.Core.Domain.Stores`
- New code uses a private static `ContainsHost(Store, string)` method inside `WebStoreContext` — no extension method needed since host matching is only used during store resolution
- Uses `StringSplitOptions.TrimEntries` (modern .NET) instead of manual `.Trim()` loop

### WebStoreContext Sync-over-Async Pattern
- `IStoreContext.CurrentStore` is a sync property (matching legacy contract used by `StoreMappingService.Authorize` and many other consumers)
- `IStoreService.GetAllStoresAsync()` is async — `WebStoreContext` uses `.GetAwaiter().GetResult()` to bridge
- Safe because `MemoryCacheManager` operations are effectively synchronous (in-memory) and the store list is cached after first call
- Per-request caching via `_cachedStore` field prevents repeated calls — `WebStoreContext` should be registered as Scoped

### StoreMappingService CatalogSettings Dependency
- Legacy injected `CatalogSettings` directly (resolved by Autofac from `ISettingService`)
- New code also takes `CatalogSettings` as constructor parameter — requires DI registration to resolve via `ISettingService.LoadSettingAsync<CatalogSettings>()`
- This is a pattern that will repeat for all services depending on Settings POCOs — DI composition root must register each Settings class

### FrameworkReference in Nop.Web.Framework
- Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `Nop.Web.Framework.csproj` for `IHttpContextAccessor`
- This is the standard pattern for class libraries that need ASP.NET Core types without being a web project themselves
- All future Web.Framework components (filters, middleware, tag helpers) will benefit from this reference

### Impact on Future Items
- [2.3] Localization: `LanguageService.GetAllLanguagesAsync` can now add `IStoreMappingService` filtering (was deferred)
- [3.3-3.14] Services: `IStoreMappingService` available for entity visibility filtering
- [4.4] Catalog: `IStoreMappingService.AuthorizeAsync` used for product/category/manufacturer store filtering
- DI composition root: must register `IStoreService → StoreService` (Scoped), `IStoreMappingService → StoreMappingService` (Scoped), `IStoreContext → WebStoreContext` (Scoped), `CatalogSettings` (from ISettingService)
