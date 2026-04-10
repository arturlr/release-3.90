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

## 2026-04-09 — [3.3] Directory Services / Implementation

### Exchange Rate Provider Methods Deferred
- Legacy `ICurrencyService` included `GetCurrencyLiveRates`, `LoadActiveExchangeRateProvider`, `LoadExchangeRateProviderBySystemName`, `LoadAllExchangeRateProviders` — all depend on `IPluginFinder` (plugin system [2.10])
- These methods are NOT included in the new `ICurrencyService` interface. They will be added when the plugin system is built
- `IExchangeRateProvider` interface (extends `IPlugin`) also deferred to [2.10]
- `UpdateExchangeRateTask` (scheduled task) deferred to [3.6]

### CountryService Store Mapping Pattern
- Legacy used inline LINQ join between `Country` and `StoreMapping` tables for store filtering
- New code preserves this pattern using `IRepository<StoreMapping>` join query (not `IStoreMappingService.AuthorizeAsync`) because the filtering happens inside a cached query — calling an async service per-entity inside a LINQ query would be inefficient
- `CurrencyService` uses a different pattern: post-cache filter via `IStoreMappingService.AuthorizeAsync` (matching legacy which also filtered after cache retrieval)

### Sync Conversion Methods
- `ConvertCurrency`, `ConvertDimension`, `ConvertWeight` and their primary-conversion variants are sync methods (matching legacy signatures)
- They call `GetCurrencyByIdAsync`/`GetMeasureDimensionByIdAsync`/`GetMeasureWeightByIdAsync` via `.GetAwaiter().GetResult()` — safe because these resolve from `IStaticCacheManager` which is in-memory
- Future: if callers are made async, these can be converted to async variants

### Localized Sorting Deferred
- Legacy `CountryService.GetAllCountries` and `StateProvinceService.GetStateProvincesByCountryId` sorted by localized names when `languageId > 0` using `entity.GetLocalized(x => x.Name, languageId)`
- New code does NOT implement localized sorting — `GetLocalized` extension requires `ILocalizedEntityService` and `ILanguageService` parameters (per [2.3] discovery about service locator elimination)
- Localized sorting will be added when a helper method or the calling layer can pass the required services
- The `languageId` parameter is kept in the interface for API compatibility

### Impact on Future Items
- [2.10] Plugin system: add `IExchangeRateProvider`, exchange rate provider methods to `ICurrencyService`
- [3.6] Scheduled tasks: implement `UpdateExchangeRateTask`
- [4.6] Tax services: can now use `ICountryService`, `IStateProvinceService` for tax jurisdiction lookups
- [4.7] Shipping services: can now use `ICountryService`, `IMeasureService` for shipping calculations
- [7.15] MaxMind GeoIP2: replace `NullGeoLookupService` with full implementation

## 2026-04-09 — [3.4] SEO Services / Implementation

### Service Locator Elimination in SeoExtensions
- Legacy `SeoExtensions` used `EngineContext.Current.Resolve<T>()` extensively for `IUrlRecordService`, `IWorkContext`, `ILanguageService`, `SeoSettings`
- New `SeoExtensions` takes all dependencies as explicit parameters — no service locator calls
- `GetSeNameAsync<T>` takes `IUrlRecordService` and optional `ILanguageService` as parameters
- `ValidateSeNameAsync` takes `IUrlRecordService` and `SeoSettings` as parameters
- `GetSeName(string)` takes `convertNonWesternChars` and `allowUnicodeCharsInUrls` booleans directly
- Impact: all callers (controllers, model factories, services) must pass dependencies explicitly

### Character Transliteration Table Modernization
- Legacy used `Dictionary<string, string>` with lazy initialization + lock pattern (1029 entries, keyed by string)
- New code uses `FrozenDictionary<char, string>` (1028 entries, keyed by char) — immutable, thread-safe, zero-allocation lookups
- `01BE` (LATIN LETTER INVERTED GLOTTAL STOP WITH STROKE) was commented out in legacy — excluded from new table
- `ToUnichar()` helper eliminated — char literals used directly (`'\u00C0'` instead of `ToUnichar("00C0")`)

### UrlRecordForCaching DTO Eliminated
- Legacy `UrlRecordService` used nested `UrlRecordForCaching` class to cache URL records separately from EF entities
- New code caches `UrlRecord` entities directly from `TableNoTracking` — consistent with pattern established in [2.2] Logging and [2.3] Localization
- Safe because cached entities are read-only (not tracked by EF Core)

### ISitemapGenerator Implementation Deferred
- `ISitemapGenerator` interface created but implementation deferred — depends on `ICategoryService`, `IProductService`, `IManufacturerService`, `ITopicService` (Phase 4)
- Legacy `SitemapGenerator` also depends on `IStoreContext`, `IWebHelper`, `CommonSettings`, `BlogSettings`, `NewsSettings`, `ForumSettings`, `SecuritySettings`
- Implementation will be created when Phase 4 services are available
- Legacy `UpdateFrequency` enum not migrated — will be created with the implementation

### Entity-Specific GetSeName Extensions Dropped
- Legacy had entity-specific `GetSeName()` extension methods for `ProductTag`, `ForumGroup`, `Forum`, `ForumTopic` that generated slugs from entity names directly (not from URL records)
- These used service locator to resolve `IWorkContext` and `ILocalizationService`
- New code only provides `GetSeNameAsync<T>` (URL record lookup) and `GetSeName(string)` (slug generation from raw text)
- Entity-specific slug generation will be handled by calling `GetSeName(entity.Name, ...)` directly in services/controllers

### Impact on Future Items
- [3.10] Vendors, [3.11] Topics, [4.4] Catalog: can now use `IUrlRecordService.SaveSlugAsync` and `SeoExtensions.ValidateSeNameAsync`
- [5.x] Controllers: must pass `IUrlRecordService` and `SeoSettings` to `ValidateSeNameAsync` calls
- ISitemapGenerator implementation: add as sub-item when Phase 4 services are complete

## 2026-04-09 — [3.5] Helpers / Implementation

### BrowscapXmlHelper Replaced with FrozenSet Crawler Detection
- Legacy `UserAgentHelper` used `BrowscapXmlHelper` — parsed large XML files from Browser Capabilities Project, stored regex patterns in a `List<string>`, matched via `Regex.IsMatch` per pattern per request
- Heavy: XML parsing on first request, Singleton pattern with double-checked locking, regex compilation per match
- New approach: `FrozenSet<string>` of 25 well-known crawler tokens (googlebot, bingbot, etc.) with case-insensitive `string.Contains` matching
- Trade-off: less comprehensive than full browscap database but covers all major search engines. Can be extended by adding tokens
- Impact: `NopConfig.UserAgentStringsPath` and `NopConfig.CrawlerOnlyUserAgentStringsPath` config properties are no longer needed

### IDateTimeHelper Property Setters Removed
- Legacy `IDateTimeHelper.DefaultStoreTimeZone { get; set; }` setter called `ISettingService.SaveSetting(DateTimeSettings)` — mutating DB state from a property setter is a side-effect anti-pattern
- Legacy `IDateTimeHelper.CurrentTimeZone { get; set; }` setter called `IGenericAttributeService.SaveAttribute` — same issue
- New interface has read-only properties. Timezone mutation will be handled by admin controllers calling `ISettingService`/`IGenericAttributeService` directly
- Impact: admin SettingController [5.38] must save `DateTimeSettings.DefaultStoreTimeZoneId` directly via `ISettingService`

### DateTimeHelper Uses IRepository<GenericAttribute> Directly
- `IGenericAttributeService` not built yet ([3.8])
- Instead of deferring customer timezone lookup, `DateTimeHelper` queries `IRepository<GenericAttribute>` directly for `TimeZoneId` attribute
- When [3.8] is built, can optionally refactor to use `IGenericAttributeService` — but direct repository access is simpler and avoids an unnecessary abstraction layer for a single query

### DefaultStoreTimeZone Falls Back to UTC
- Legacy fell back to `TimeZoneInfo.Local` (server's local timezone) — this is server-dependent and causes inconsistent behavior across deployments
- New code falls back to `TimeZoneInfo.Utc` — deterministic, server-agnostic
- Impact: stores that relied on server timezone matching their business timezone must now explicitly set `DateTimeSettings.DefaultStoreTimeZoneId`

### FrameworkReference Added to Nop.Services.csproj
- `UserAgentHelper` needs `IHttpContextAccessor` from `Microsoft.AspNetCore.Http.Abstractions`
- Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `Nop.Services.csproj`
- This is the same pattern used by `Nop.Web.Framework.csproj`
- Impact: future services in Nop.Services can now use ASP.NET Core types (IHttpContextAccessor, etc.) without additional references

## 2026-04-09 — [3.8] Common Services / Implementation

### GenericAttribute Service Locator Elimination
- Legacy `GenericAttributeExtensions.GetAttribute<T>()` used `EngineContext.Current.Resolve<IGenericAttributeService>()` (service locator)
- New `GenericAttributeExtensions.GetAttributeAsync<T>()` takes `IGenericAttributeService` as explicit parameter
- Impact: all callers (controllers, services) must pass `IGenericAttributeService` explicitly
- Legacy `GetUnproxiedEntityType()` replaced with `GetType()` — EF Core doesn't use proxy types for no-tracking queries

### Address Attribute XML Format Preserved
- Legacy address attributes stored as XML (`<Attributes><AddressAttribute ID="1"><AddressAttributeValue><Value>...</Value></AddressAttributeValue></AddressAttribute></Attributes>`)
- New code preserves this XML format for backward compatibility with data migration
- Plan item [8.3] (AttributesXml → JSON conversion) will handle format migration if desired

### AddressAttributeFormatter/Parser Localization Simplified
- Legacy used `attribute.GetLocalized(a => a.Name, _workContext.WorkingLanguage.Id)` for localized attribute names
- New code uses `attribute.Name` directly — localized name resolution requires `ILocalizedEntityService` + `ILanguageService` parameters (per [2.3] discovery about service locator elimination)
- When presentation layer is built, controllers/model factories can pass localized names if needed
- `ShouldHaveValues` logic duplicated as private static method in both Parser and Formatter — could extract to shared utility

### AddressService Validation Pattern
- Legacy `IsAddressValid` had a quirk: if ANY required custom attributes exist, address is always invalid (regardless of whether they're filled in)
- This is because the method doesn't receive the custom attributes XML — it only checks the `Address` entity properties
- New code preserves this behavior with a comment explaining the limitation
- Full custom attribute validation requires `IAddressAttributeParser.GetAttributeWarningsAsync(attributesXml)` which is called separately by controllers

### SearchTermService — No Caching, No Events
- Search terms are write-heavy analytics data (incremented on every search)
- Legacy didn't cache search terms either — only read operations are GetByKeyword (single lookup) and GetStats (reporting)
- Event publishing kept for CRUD operations (matching legacy pattern) but no cache consumers needed

### FulltextService — Direct NopDbContext Dependency
- Legacy used `IDataProvider` + `IDbContext` abstractions for stored procedure calls
- New code uses `NopDbContext.Database.SqlQueryRaw<int>()` and `ExecuteSqlRawAsync()` directly
- Simpler, no abstraction layer needed since we only support SQL Server
- `IsFullTextSupportedAsync` has try/catch fallback — returns false if stored procedures don't exist

### IPdfService — Interface Only
- Legacy `PdfService` is 74K LOC using iTextSharp (AGPL licensed)
- Implementation depends on: Order, Product, Shipment entities (exist), IOrderService, IProductService, ILocalizationService, IWorkContext, IPictureService, IStoreService, IStoreContext, ISettingService, IAddressService, ICountryService, IStateProvinceService, ICurrencyService, IMeasureService, IPaymentService, IDateTimeHelper (many not yet built)
- PDF library choice deferred: QuestPDF (MIT), iText7 (AGPL/commercial), or SkiaSharp-based
- `PrintProductsToPdf` dropped from interface — legacy feature rarely used, can be added later

### Impact on Future Items
- [3.5] Helpers: `DateTimeHelper` already uses `IRepository<GenericAttribute>` directly — can optionally refactor to use `IGenericAttributeService` now that it exists
- [4.1] Customer services: `IGenericAttributeService` available for customer attribute storage (timezone, language, currency preferences)
- [4.9] Order services: `IPdfService` interface available for order invoice generation
- [5.x] Controllers: must pass `IGenericAttributeService` to `GetAttributeAsync` extension method calls

## 2026-04-09 — [3.9] Affiliate Services / Implementation

### No-Nav-Property Join Pattern for Name Filtering
- Legacy `GetAllAffiliates` filtered by `a.Address.FirstName.Contains(firstName)` using EF6 nav property
- New code joins `IRepository<Address>` explicitly: `from a in query join addr in addresses on a.AddressId equals addr.Id`
- This is the first service to need cross-entity filtering without nav properties — establishes the pattern for any future service that needs to filter by related entity properties
- Alternative considered: load Address separately per affiliate — rejected because it would be N+1 queries

### AffiliateExtensions.GetFullName Signature Change
- Legacy: `affiliate.GetFullName()` — accessed `affiliate.Address.FirstName` via nav property
- New: `affiliate.GetFullName(address)` — Address must be passed explicitly
- Impact: all callers (admin AffiliateController [5.66]) must load the Address separately and pass it

### ValidateFriendlyUrlName Now Async
- Legacy was sync, used `EngineContext.Current.Resolve<IAffiliateService>()` (service locator)
- New `ValidateFriendlyUrlNameAsync` takes `IAffiliateService` as parameter, returns `Task<string>`
- Impact: callers must await the result

### No Caching Needed
- Affiliate service has no caching (matching legacy) — affiliates are low-volume admin-managed entities
- No cache event consumers needed

## 2026-04-09 — [3.10] Vendor Services / Implementation

### Simple Leaf Service — No Surprises
- VendorService is a straightforward CRUD service with no caching, no cross-entity joins, no complex filtering
- Follows AffiliateService pattern exactly: async-first, `Task.FromResult` for sync repo calls, `ArgumentNullException.ThrowIfNull`, `await` event publishing
- Soft delete for vendors (sets `Deleted=true` then calls `UpdateVendor`), hard delete for vendor notes (repository `Delete`)
- Legacy `DeleteVendor` published `EntityDeleted` event (not `EntityUpdated` despite calling `UpdateVendor`) — new code preserves this: `DeleteVendorAsync` calls `_vendorRepository.Update` then `EntityDeletedAsync`
- No `InsertVendorNote` or `UpdateVendorNote` methods in legacy — vendor notes are created/managed via admin controller directly through repository. Only `GetVendorNoteById` and `DeleteVendorNote` exist in the service interface

### Impact on Future Items
- [5.19] Public VendorController: can now use `IVendorService` for vendor listing/detail pages
- [5.67] Admin VendorController: can now use `IVendorService` for vendor CRUD + notes management
- Admin controller will need to handle vendor note insert/update via `IRepository<VendorNote>` directly (matching legacy pattern) or add methods to `IVendorService`

## 2026-04-09 — [3.7] Media Services / Implementation

### ImageResizer → SixLabors.ImageSharp
- Legacy used `ImageResizer` 4.0.5 NuGet (`ImageBuilder.Current.Build()`) and `System.Drawing.Bitmap` for image loading/resizing
- New code uses `SixLabors.ImageSharp` 3.1.12 — cross-platform, no GDI+ dependency, Apache 2.0 license
- ImageSharp 3.x API breaking change: `Image.Load(byte[], out IImageFormat)` no longer exists for byte[] overloads. Must use `Image.DetectFormat(byte[])` separately
- Initial version 3.1.7 had a known moderate vulnerability (GHSA-rxmq-m78w-7wmc) — upgraded to 3.1.12

### System.IO.Directory Namespace Conflict
- `System.IO.Directory` conflicts with `Nop.Services.Directory` namespace when implicit usings are enabled
- Solution: `using IODirectory = System.IO.Directory;` alias at top of PictureService.cs
- This is the same pattern used by legacy code (`System.IO.Directory` vs `Nop.Core.Domain.Directory`)
- Impact: any future service in `Nop.Services` that uses `System.IO.Directory` must use the alias

### CommonHelper.MapPath → IWebHostEnvironment
- Legacy used `CommonHelper.MapPath("~/content/images/")` for file system paths
- New code uses `IWebHostEnvironment.WebRootPath` + `Path.Combine` — standard ASP.NET Core pattern
- `IWebHostEnvironment` injected via constructor (available because Nop.Services has `<FrameworkReference Include="Microsoft.AspNetCore.App" />`)

### Mutex Replaced with File.Exists Check
- Legacy used `new Mutex(false, thumbFileName)` for thread-safe thumbnail generation — heavyweight OS-level synchronization
- New code uses simple `File.Exists(thumbFilePath)` check — ImageSharp is thread-safe for independent operations
- Worst case: two threads generate the same thumbnail simultaneously, one overwrites the other with identical content — no data corruption risk
- If contention becomes an issue, can add `ConcurrentDictionary<string, SemaphoreSlim>` per-file locking

### IsDownloadAllowed/IsLicenseDownloadAllowed Deferred
- Legacy `DownloadService.IsDownloadAllowed(OrderItem)` accessed `orderItem.Order` and `orderItem.Product` nav properties
- Nav properties were stripped in [1.3] — these methods need Order and Product passed as parameters
- These are really order-domain logic (check order status, payment status, activation, expiration)
- Deferred to [4.9] Order services where Order/Product entities are readily available

### GetPicturesHash Dropped
- Legacy used SQL Server `HASHBYTES('sha1', ...)` via raw SQL for picture binary hashing
- Only used by `ImportManager` for `MediaSettings.ImportProductImagesUsingHash` optimization
- Dropped from interface — can be re-added in [4.10] Export/Import if needed, using C# `SHA1.HashData()` instead of SQL

### Extensions.cs (Media) Not Migrated
- Legacy `Extensions.cs` had `GetDownloadBits(HttpPostedFileBase)`, `GetPictureBits(HttpPostedFileBase)` — ASP.NET MVC 5 specific
- ASP.NET Core uses `IFormFile.OpenReadStream()` — these extension methods are obsolete
- `GetProductPicture(Product, string, IPictureService, IProductAttributeParser)` depends on `IProductAttributeParser` ([4.4]) — deferred

### Impact on Future Items
- [4.4] Catalog: can now use `IPictureService` for product/category/manufacturer pictures
- [4.9] Order services: must implement `IsDownloadAllowed`/`IsLicenseDownloadAllowed` logic
- [4.10] Export/Import: may need `GetPicturesHash` equivalent using C# SHA1
- [5.21] Public DownloadController: can now use `IDownloadService`
- [5.78] Admin PictureController/DownloadController: can now use both services
- [7.14] Azure Blob Storage: `AzurePictureService` extends `PictureService` — virtual methods preserved for override

## 2026-04-09 — [3.11] Topic Services / Implementation

### IStoreMappingService API is Async-Only
- Legacy `IStoreMappingService.Authorize(entity, storeId)` was sync
- New `IStoreMappingService.AuthorizeAsync(entity, storeId)` is async — no sync overload exists
- `GetTopicBySystemNameAsync` uses a foreach loop with `await AuthorizeAsync` for post-query store filtering (cannot use LINQ `.Where` with async predicate)
- `GetAllTopicsAsync` avoids this by using inline join query against `StoreMapping` repository (same pattern as CountryService) — more efficient for cached queries

### TopicService Follows CountryService Pattern for ACL+Store Mapping
- `GetAllTopicsAsync` uses inline LINQ joins against `AclRecord` and `StoreMapping` repositories with left outer join + group-by deduplication
- This is the same pattern used by `CountryService.GetAllCountriesAsync` — inline joins inside cached query are more efficient than per-entity `AuthorizeAsync` calls
- Customer role IDs obtained from `CustomerCustomerRoleMapping` repository (no nav properties)

### TopicTemplateService — No Caching (Matching Legacy)
- Legacy `TopicTemplateService` had no caching — topic templates are low-volume admin-managed entities
- New code preserves this: simple CRUD with event publishing, no `IStaticCacheManager` dependency
- Follows VendorService pattern exactly

### Impact on Future Items
- [5.13] Public TopicController: can now use `ITopicService` for topic display
- [5.48] Admin TopicController: can now use both `ITopicService` and `ITopicTemplateService` for topic CRUD
- [4.4] Catalog: `ICategoryTemplateService`, `IManufacturerTemplateService`, `IProductTemplateService` can follow the same `TopicTemplateService` pattern (simple CRUD, no caching)

## 2026-04-09 — [3.12] Poll Services / Implementation

### Simple Leaf Service — No Surprises
- PollService is a straightforward CRUD service with no caching, no store mapping filtering, no complex dependencies
- Follows VendorService pattern exactly: async-first, `Task.FromResult` for sync repo calls, `ArgumentNullException.ThrowIfNull`, event publishing
- Legacy `PollService` had no caching — polls are low-volume entities, no cache event consumers needed

### AlreadyVotedAsync Join Pattern
- Legacy `AlreadyVoted` joined `PollAnswer` and `PollVotingRecord` tables to check if a customer voted on any answer belonging to a poll
- New code preserves this join pattern using `TableNoTracking` (read-only query, no change tracking overhead)
- This is the same cross-entity join pattern established in [3.9] AffiliateService

### No Store Mapping in Service Layer
- Legacy `PollService` did NOT filter polls by store mapping — store filtering was done at the presentation layer (PollController/model factory)
- Spec mentions "store mapping filtering" in acceptance criteria but legacy service didn't implement it
- Store mapping filtering will be handled by controllers/model factories when [5.14] and [5.47] are built

### Impact on Future Items
- [5.14] Public PollController: can now use `IPollService` for poll display and voting
- [5.47] Admin PollController: can now use `IPollService` for poll CRUD
- No InsertPollAnswer/UpdatePollAnswer in service interface (matching legacy) — admin controller manages answers via `IRepository<PollAnswer>` directly or these can be added to the interface when needed

## 2026-04-09 — [3.13] Blog Services / Implementation

### ParseTags Moved to Service Layer
- Legacy `BlogExtensions.ParseTags()` was an extension method on `BlogPost` in `Nop.Core.Domain.Blogs`
- Extension methods were excluded from domain entities in [1.3] — `ParseTags` recreated as a static method in `Nop.Services.Blogs.BlogExtensions`
- Modernized: `StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries` replaces manual `.Trim()` loop; collection expression `[]` for empty return
- Not an extension method (no `this` parameter) — called as `BlogExtensions.ParseTags(blogPost)` since it's service-layer logic, not a domain concern

### InvariantCultureIgnoreCase → OrdinalIgnoreCase
- Legacy used `StringComparison.InvariantCultureIgnoreCase` for tag matching
- New code uses `StringComparison.OrdinalIgnoreCase` — faster, no culture-dependent behavior, appropriate for tag string matching
- Tags are user-entered comma-separated strings, not locale-sensitive data

### Simple Leaf Service — No Surprises
- BlogService follows PollService pattern exactly: async-first, `Task.FromResult` for sync repo calls, `ArgumentNullException.ThrowIfNull`, event publishing
- No caching (matching legacy) — blog posts are low-to-medium volume entities
- Store mapping uses inline LINQ join (same pattern as CountryService, TopicService) — efficient for filtered queries
- `GetAllBlogPostsByTagAsync` and `GetAllBlogPostTagsAsync` load all posts then filter in-memory (matching legacy) — acceptable for blog-scale data volumes

### Impact on Future Items
- [5.10] Public BlogController: can now use `IBlogService` for blog display
- [5.44] Admin BlogController: can now use `IBlogService` for blog CRUD
- [3.14] News services: `INewsService` follows the same pattern — nearly identical structure to BlogService

## 2026-04-09 — [3.14] News Services / Implementation

### Simple Leaf Service — No Surprises
- NewsService is structurally identical to BlogService: CRUD for NewsItem + NewsComment, store mapping join for GetAllNews, comment filtering with multiple optional params, GetNewsCommentsCount, batch delete
- No caching (matching legacy) — news items are low-to-medium volume entities
- No tags (unlike Blog) — simpler interface, no ParseTags/GetAllByTag/GetAllTags methods
- Store mapping uses inline LINQ join (same pattern as CountryService, TopicService, BlogService) — efficient for filtered queries
- Comment text search includes both CommentText and CommentTitle (matching legacy)

### Impact on Future Items
- [5.11] Public NewsController: can now use `INewsService` for news display
- [5.45] Admin NewsController: can now use `INewsService` for news CRUD

## 2026-04-09 — [2.5] Authentication / Implementation

### CustomerGuid as Claim Instead of Email/Username
- Legacy stored email or username in `FormsAuthenticationTicket.UserData`, then resolved customer via `ICustomerService.GetCustomerByEmail/Username` (depending on `CustomerSettings.UsernamesEnabled`)
- New code stores `CustomerGuid` (GUID) as the claim value — stable identifier that never changes even if email/username is updated
- Eliminates dependency on `CustomerSettings` at authentication time
- Customer lookup uses `IRepository<Customer>.Table.FirstOrDefault(c => c.CustomerGuid == guid)` — single indexed query

### No ICustomerService Dependency — Circular Dependency Avoidance
- Legacy `FormsAuthenticationService` depended on `ICustomerService` for customer lookup
- `ICustomerService` ([4.1]) depends on `IAuthenticationService` ([2.5]) in some patterns (e.g., registration flow calls SignIn)
- New `CookieAuthenticationService` uses `IRepository<Customer>`, `IRepository<CustomerCustomerRoleMapping>`, `IRepository<CustomerRole>` directly
- IsRegistered check uses join query: `CustomerCustomerRoleMapping` → `CustomerRole` where `SystemName == "Registered"` and `Active == true`

### Impersonation Not in Auth Service
- Legacy impersonation was NOT in `FormsAuthenticationService` — it was in `WebWorkContext` (presentation layer)
- `WebWorkContext.CurrentCustomer` getter checks `GenericAttribute` for `ImpersonatedCustomerId`, loads that customer, sets `OriginalCustomerIfImpersonated`
- This pattern is preserved: auth service handles cookie-based identity only, impersonation is a presentation-layer concern
- Impact: [5.1] Nop.Web.Framework `WebWorkContext` implementation must handle impersonation

### Cookie Authentication Middleware Configuration Deferred
- `CookieAuthenticationService` uses `HttpContext.SignInAsync/SignOutAsync` with `NopAuthenticationDefaults.AuthenticationScheme`
- The actual cookie middleware registration (`services.AddAuthentication().AddCookie(NopAuthenticationDefaults.AuthenticationScheme, ...)`) belongs in `Nop.Web/Program.cs` or a DI extension method
- Cookie options (expiration, path, domain, HttpOnly, Secure) will be configured there
- This is intentional: the service layer defines the scheme name, the web layer configures the middleware

### External Authentication Deferred
- Legacy `External/` directory (IOpenAuthenticationService, IClaimsTranslator, IExternalAuthorizer, IExternalProviderAuthorizer) is DotNetOpenAuth-based
- ASP.NET Core replaces all of this with built-in OAuth/OpenID Connect middleware
- External auth will be implemented with [6.17] Plugin: ExternalAuth.Facebook using `Microsoft.AspNetCore.Authentication.Facebook`

### Impact on Future Items
- [2.6] Authorization: can now build ASP.NET Core authorization policies that check `NopAuthenticationDefaults.AuthenticationScheme`
- [4.1] Customer services: `ICustomerRegistrationService.RegisterCustomer` can call `IAuthenticationService.SignInAsync` after registration
- [5.1] Web.Framework: `WebWorkContext` must call `IAuthenticationService.GetAuthenticatedCustomerAsync()` then check impersonation via GenericAttribute
- [5.6] Public CustomerController: login/logout actions use `IAuthenticationService.SignInAsync/SignOutAsync`
- DI registration: `services.AddScoped<IAuthenticationService, CookieAuthenticationService>()`

## 2026-04-09 — [2.6] Authorization / Implementation

### Dynamic Policy Provider Pattern
- Legacy used custom `AdminAuthorizeAttribute` (IAuthorizationFilter) with service locator (`EngineContext.Current.Resolve<IPermissionService>()`) to check `AccessAdminPanel` permission
- New approach uses ASP.NET Core's built-in authorization pipeline: `IAuthorizationPolicyProvider` + `IAuthorizationHandler`
- `NopAuthorizationPolicyProvider.GetPolicyAsync(policyName)` creates a policy with `NopPermissionRequirement(policyName)` for ANY policy name — no need to pre-register policies
- `NopPermissionHandler` resolves `IPermissionService` via constructor injection (no service locator) and calls `Authorize(systemName)`
- Usage: `[Authorize(Policy = "ManageProducts")]` on controllers/actions — the policy name IS the permission system name

### AdminVendorValidation Deferred to [5.1]
- Legacy `AdminVendorValidation` is a presentation-layer filter that checks `IWorkContext.CurrentCustomer.IsVendor()` and validates `IWorkContext.CurrentVendor != null`
- This is NOT an authorization concern — it's a vendor account validation filter
- Will be implemented as an ASP.NET Core action filter (IAsyncActionFilter) in [5.1] Nop.Web.Framework, not as an authorization handler
- Reason: authorization handlers should only check permissions, not business rules about vendor account status

### DI Registration Pattern
- `NopAuthorizationPolicyProvider` must be registered as Singleton (replaces the default `DefaultAuthorizationPolicyProvider`)
- `NopPermissionHandler` must be registered as Scoped (matches `IPermissionService` lifetime — it depends on `IWorkContext` which is scoped)
- Registration: `services.AddSingleton<IAuthorizationPolicyProvider, NopAuthorizationPolicyProvider>()` + `services.AddScoped<IAuthorizationHandler, NopPermissionHandler>()`
- Must be registered AFTER `services.AddAuthorization()` in the DI pipeline

### Impact on Future Items
- [5.1] Web.Framework: implement `AdminVendorValidation` as IAsyncActionFilter
- [5.30-5.82] Admin controllers: use `[Authorize(Policy = "AccessAdminPanel")]` on base admin controller, individual permission checks via `[Authorize(Policy = "ManageProducts")]` on actions or inline `IPermissionService.Authorize()` calls
- [5.2-5.28] Public controllers: use `[Authorize(Policy = "PublicStoreAllowNavigation")]` where needed
- Nop.Web Program.cs: must register `NopAuthorizationPolicyProvider` and `NopPermissionHandler` in DI

## 2026-04-09 — [4.1] Customer Services / Implementation

### Role Management via Join Entity (No Nav Properties)
- Legacy `CustomerService` used `customer.CustomerRoles` nav property for role checks, `InsertGuestCustomer` used `customer.CustomerRoles.Add(guestRole)`, and `RegisterCustomer` used `request.Customer.CustomerRoles.Add(registeredRole)` / `.Remove(guestRole)`
- New code uses `IRepository<CustomerCustomerRoleMapping>` for all role operations: `AddCustomerRoleMappingAsync`, `RemoveCustomerRoleMappingAsync`, `GetCustomerRoleIdsAsync`
- Added these three methods to `ICustomerService` interface — they don't exist in legacy (legacy used nav properties directly)
- Impact: all downstream consumers that check customer roles must use `ICustomerService.GetCustomerRoleIdsAsync()` instead of `customer.CustomerRoles`

### Shopping Cart Filter via Join
- Legacy `GetAllCustomers(loadOnlyWithShoppingCart: true)` used `c.ShoppingCartItems.Any()` nav property
- New code joins `IRepository<ShoppingCartItem>` explicitly
- Same pattern as AffiliateService [3.9] for cross-entity filtering without nav properties

### DeleteGuestCustomers — LINQ Only, No Stored Procedure
- Legacy used `[DeleteGuests]` stored procedure when `CommonSettings.UseStoredProceduresIfSupported` was true
- New code uses LINQ-only approach (matching legacy's fallback path)
- Also deletes `CustomerCustomerRoleMapping` records for each guest (legacy didn't need this because cascade delete handled it via nav properties)
- Dropped `IDataProvider`, `IDbContext`, `CommonSettings` dependencies

### CustomerRegistrationService — Deferred Dependencies
- Legacy depended on `INewsLetterSubscriptionService`, `IRewardPointService`, `IWorkflowMessageService`, `IStoreService`, `IGenericAttributeService`, `IWorkContext`
- New code only depends on `ICustomerService`, `IEncryptionService`, `ILocalizationService`, `IEventPublisher`, `CustomerSettings`
- Deferred functionality:
  - Newsletter subscription update on email change → [4.2] when `INewsLetterSubscriptionService` is built
  - Reward points for registration → [4.9] when `IRewardPointService` is built
  - Email revalidation message → [4.2] when `IWorkflowMessageService` is built
  - Email revalidation token (GenericAttribute) → deferred with email revalidation

### CustomerReportService — Simplified Dependencies
- Legacy depended on `ICustomerService` (for `GetCustomerRoleBySystemName`) and `IDateTimeHelper` (for timezone conversion)
- New code queries `IRepository<CustomerRole>` directly and uses `DateTime.UtcNow.AddDays(-days)` instead of timezone conversion
- Legacy converted to user time then subtracted days — for UTC-based date filtering, subtracting days from UtcNow is equivalent and simpler

### CustomerAttributeFormatter — Dropped IWorkContext
- Legacy used `_workContext.WorkingLanguage.Id` for localized attribute/value names via `GetLocalized()` extension
- New code uses `attribute.Name` / `attributeValue.Name` directly (no localization)
- Localized names can be passed by presentation layer if needed — consistent with AddressAttributeFormatter pattern

### CustomerExtensions Not Migrated
- Legacy `CustomerExtensions` (18K LOC) used service locator extensively (`EngineContext.Current.Resolve<T>()`)
- Methods like `GetFullName`, `FormatUserName`, `ParseAppliedDiscountCouponCodes`, `ApplyDiscountCouponCode`, `IsPasswordRecoveryTokenValid`, `PasswordIsExpired`, `GetCustomerRoleIds` all depend on service locator
- These will be implemented as service methods or controller-level logic when needed by presentation layer
- `GetCustomerRoleIds` is already available as `ICustomerService.GetCustomerRoleIdsAsync`

### ICustomerActivityService Already Implemented
- Plan item [4.1] listed `ICustomerActivityService` but it was already implemented in [2.2] Logging
- Removed from [4.1] scope — no duplicate implementation needed

### Impact on Future Items
- [4.2] Messages: add newsletter subscription update to `SetEmailAsync`, email revalidation to `SetEmailAsync(requireValidation: true)`
- [4.3] Forums: can now use `ICustomerService` for customer lookups
- [4.4] Catalog: can now use `ICustomerService` for customer role checks in price calculations
- [4.9] Orders: add reward points for registration to `RegisterCustomerAsync`, implement `DeleteGuestsTask`
- [5.6] Public CustomerController: can now use `ICustomerRegistrationService` for register/login/password flows
- [5.35] Admin CustomerController: can now use `ICustomerService` for customer CRUD

## 2026-04-09 — [3.6] Scheduled Tasks / Implementation

### Architecture: TaskManager/TaskThread/Task → Single BackgroundService
- Legacy used 3 classes: `TaskManager` (singleton, groups tasks by interval into `TaskThread` instances), `TaskThread` (per-interval `System.Threading.Timer`), `Task` (execution wrapper with Autofac scope creation, web farm leasing, error logging)
- New code uses a single `TaskSchedulerHostedService` (extends `BackgroundService`) with a 30-second polling loop
- Simpler: no grouping by interval, no per-interval timers — just poll all enabled tasks every 30s and run any that are due
- Trade-off: tasks may run up to 30s late vs legacy's exact-interval timers. Acceptable for background tasks (cache clearing, guest cleanup, email sending)

### Web Farm Leasing Dropped
- Legacy had two web farm coordination mechanisms:
  1. DB leasing: `ScheduleTask.LeasedByMachineName` + `LeasedUntilUtc` (30-min lease, checked before execution)
  2. Redis lock: `IRedisConnectionWrapper.PerformActionWithLock` (distributed lock with TTL)
- Both depend on `NopConfig.MultipleInstancesEnabled` and `IMachineNameProvider`
- New code drops both — single-instance deployment assumed initially
- `ScheduleTask` entity still has `LeasedByMachineName` and `LeasedUntilUtc` properties for future use
- When web farm support is needed, add distributed locking via `IDistributedLock` (e.g., `Medallion.Threading.Redis`)

### Catch-Up Logic Simplified
- Legacy `TaskManager.Initialize()` created a special `RunOnlyOnce` `TaskThread` for tasks with `Seconds >= 1800` (30 min) that hadn't run recently, scheduled to execute 5 minutes after startup
- New code doesn't need this: the polling loop's `IsDue()` check returns `true` for tasks that have never run (`LastStartUtc == null`) or are overdue (`LastStartUtc + Seconds < UtcNow`)
- Overdue tasks execute on the first poll cycle (30s after startup) — slightly faster than legacy's 5-minute delay

### No Caching, No Events (Matching Legacy)
- Legacy `ScheduleTaskService` had no caching and no event publishing — pure CRUD
- New code preserves this: `ScheduleTaskService` is the simplest service in the codebase
- No cache event consumers needed

### DI Registration Pattern
- `IScheduleTaskService → ScheduleTaskService` (Scoped)
- `TaskSchedulerHostedService` registered via `services.AddHostedService<TaskSchedulerHostedService>()`
- Each concrete `ITask` implementation registered as its concrete type (Scoped) so `IServiceProvider.GetService(taskType)` resolves it
- DI registration deferred to Nop.Web `Program.cs` or DI composition root

### Impact on Future Items
- [4.2] Messages: `QueuedMessagesSendTask` can now implement `ITask` and be scheduled
- [4.1] Customers: `DeleteGuestsTask` can now implement `ITask`
- [2.2] Logging: `ClearLogTask` can now implement `ITask`
- [2.1] Caching: `ClearCacheTask` can now implement `ITask`
- [3.3] Directory: `UpdateExchangeRateTask` can now implement `ITask`
- [5.73] Admin ScheduleTaskController: can now use `IScheduleTaskService` for task CRUD

## 2026-04-09 — [4.2] Message Services / Implementation

### System.Net.Mail → MailKit 4.3.0
- Legacy `EmailSender` used `System.Net.Mail.SmtpClient` (obsolete in .NET Core, no async support)
- New code uses `MailKit.Net.Smtp.SmtpClient` with `MimeKit.MimeMessage` — full async, cross-platform, actively maintained
- `SecureSocketOptions.SslOnConnect` when `EnableSsl=true`, `StartTlsWhenAvailable` otherwise
- `UseDefaultCredentials` handled by skipping `AuthenticateAsync` call (MailKit doesn't have a `DefaultNetworkCredentials` equivalent — if no credentials needed, just don't authenticate)
- Download attachments use `BodyBuilder.Attachments.Add(fileName, bytes)` instead of `System.Net.Mail.Attachment(MemoryStream)`

### System.Linq.Dynamic → Simple Condition Evaluator
- Legacy `Tokenizer.ReplaceConditionalStatements` used `System.Linq.Dynamic` `.Where(conditionString)` for evaluating conditional expressions in templates
- `System.Linq.Dynamic` is a heavy dependency for a simple feature (conditional token display)
- New code uses a simple evaluator: supports `==`, `!=` comparisons and truthy/falsy checks (non-empty, non-"false", non-"0")
- Trade-off: less expressive than full LINQ dynamic queries, but covers all practical template conditions
- If complex conditions are needed, can add `System.Linq.Dynamic.Core` NuGet later

### MessageTokenProvider — Minimal Order/Shipment Tokens
- Legacy `MessageTokenProvider` had 20+ dependencies including `IOrderService`, `IPriceFormatter`, `ICurrencyService`, `IPaymentService`, `IProductAttributeParser`, `IAddressAttributeFormatter`, `IShippingService`
- Most of these services don't exist yet (Phase 4)
- New code implements Store, Customer, Vendor, Newsletter, Forum, Product (basic), GiftCard tokens fully
- Order, Shipment, ReturnRequest tokens are minimal (ID, basic fields) — will be enriched when [4.9] Order services is built
- This is intentional: the interfaces are complete, the token values will improve as dependencies become available

### NewsLetterSubscriptionService — No IDbContext.LoadOriginalCopy
- Legacy used `IDbContext.LoadOriginalCopy(entity)` to snapshot original values before update (for subscribe/unsubscribe event logic)
- EF Core doesn't have this method. New code queries `TableNoTracking` by ID to get the original state
- This adds one extra DB query per update, but newsletter updates are low-frequency operations

### MessageTemplateService — Cache Nullable Pattern
- `IStaticCacheManager.GetAsync<T>` returns `T?` but the acquire function expects `Func<Task<T>>`
- For `GetMessageTemplateByNameAsync` which can legitimately return null (template not found), used null-forgiving operator `!` on `FirstOrDefault()` since the cache itself returns `T?`
- For `GetAllMessageTemplatesAsync` which should never return null, used `?? []` null-coalescing

### EventPublisher Extensions for Messages
- Legacy had `PublishNewsletterSubscribe`, `PublishNewsletterUnsubscribe`, `EntityTokensAdded`, `MessageTokensAdded` extension methods
- New code creates dedicated event classes: `EmailSubscribedEvent`, `EmailUnsubscribedEvent`, `EntityTokensAddedEvent<T,U>`, `MessageTokensAddedEvent<U>`
- Events published via `IEventPublisher.PublishAsync` — consistent with async-first pattern

### Impact on Future Items
- [4.3] Forums: can now use `IWorkflowMessageService` for forum notification emails
- [4.9] Orders: can now use `IWorkflowMessageService` for order notification emails; must enrich `MessageTokenProvider.AddOrderTokensAsync` with full order details
- [5.15] Public NewsletterController: can now use `INewsLetterSubscriptionService`
- [5.53-5.57] Admin email/template/campaign controllers: can now use all message services
- [7.1] SMTP integration: already implemented via MailKit in `EmailSender`

## 2026-04-09 — [3.16] GDPR Services / Implementation

### Greenfield Feature — No Legacy Code
- No `Gdpr` directory exists in legacy `Nop.Services/` or `Nop.Core.Domain/`
- DISCOVERIES.md iteration 1 noted "No GDPR service directory exists in legacy — GDPR features may be spread across CustomerService" — confirmed: no GDPR-specific code exists anywhere in legacy
- Created entirely new: `GdprRequestType` enum, `GdprLog` entity, EF Core config, `IGdprService`/`GdprService`

### PermanentDeleteCustomerAsync — Comprehensive Cleanup
- Deletes 12 categories of customer-related data: forum posts/topics/subscriptions, blog comments, news comments, product reviews (+ helpfulness records), activity logs, system logs, shopping cart items, back-in-stock subscriptions, private messages, generic attributes, passwords, customer-role mappings
- Anonymizes order addresses (preserves order history integrity): FirstName→"Deleted", LastName→"Customer", nulls PII fields
- Anonymizes customer record: email→`deleted-{guid}@anonymized.invalid`, nulls username/admin comment/IP, sets Active=false, Deleted=true
- Logs the deletion via `InsertLogAsync` after anonymization

### Data Export Not Implemented in Service Layer
- Spec mentions "customer data export produces complete data package" — this is a presentation-layer concern (controller assembles data from multiple services into export format)
- `IGdprService` provides the consent logging and deletion infrastructure; data export will be assembled by the admin GDPR controller when Phase 5 is built
- No separate `ExportCustomerDataAsync` method needed — the controller will query each service directly

### No Caching Needed
- GDPR operations are low-volume admin actions (consent logging, customer deletion)
- No cache event consumers needed — consistent with other low-volume services (VendorService, AffiliateService, PollService)

### Impact on Future Items
- [5.x] Admin GDPR controller: will use `IGdprService` for consent log display and customer deletion
- [4.9] Order services: order history preserved after customer deletion (addresses anonymized, not deleted)
- [8.x] Data migration: `GdprLog` table is new — no migration from legacy needed

## 2026-04-09 — [4.4] Catalog Services / Implementation

### ProductProductTagMapping Join Entity Created
- Legacy `Product.ProductTags` nav property was stripped in [1.3] — many-to-many relationship between Product and ProductTag had no join entity
- Created `ProductProductTagMapping` (table: `Product_ProductTag_Mapping`) with `ProductId` and `ProductTagId` properties
- EF Core configuration and DbSet added to NopDbContext
- `ProductTagService.UpdateProductTagsAsync` and `GetProductCountAsync` use this join entity for all product-tag queries

### Product Entity Does Not Have SpecialPrice
- Legacy `Product` entity does NOT have `SpecialPrice`, `SpecialPriceStartDateTimeUtc`, `SpecialPriceEndDateTimeUtc` properties
- The new `Product` entity also lacks these — they were never part of the nopCommerce 3.x domain model
- `PriceCalculationService` was initially coded with SpecialPrice logic — removed during build fix
- Price calculation uses: base Price → tier pricing → attribute adjustments → rental periods → (discounts deferred to [4.5])

### ProductAttributeMapping.IsNonCombinable Is Not a Property
- Legacy `IsNonCombinable()` was an extension method in `ProductAttributeExtensions` that returned `!ShouldHaveValues(mapping)`
- `ShouldHaveValues` checks `AttributeControlType` — TextBox, MultilineTextbox, Datepicker, FileUpload are non-combinable
- New code uses `ShouldHaveValues(mapping.AttributeControlTypeId)` directly in `ProductAttributeParser` instead of an extension method
- Impact: any future code checking combinability should use the same `ShouldHaveValues` logic

### ICopyProductService — Interface Only
- `CopyProductService` (33K LOC legacy) depends on: IProductService, ICategoryService, IManufacturerService, IProductAttributeService, IProductAttributeParser, ISpecificationAttributeService, IPictureService, IUrlRecordService, ILocalizedEntityService, ILanguageService, IStoreMappingService, IAclService, IDownloadService, IProductTagService
- All dependencies now exist, but implementation is complex and low-priority (admin-only feature)
- Interface created for DI registration; implementation can be added when admin controllers need it

### SearchProducts — LINQ Only, No Stored Procedure
- Legacy `SearchProducts` had two paths: stored procedure `[ProductLoadAllPaged]` (when `CommonSettings.UseStoredProceduresIfSupported`) and LINQ fallback
- New code uses LINQ-only approach — simpler, portable, no stored procedure dependency
- Performance: acceptable for typical catalog sizes; can add stored procedure optimization later if needed
- `filterableSpecificationAttributeOptionIds` computed from all matching products (not just current page) — matches legacy behavior

### Cookie-Based Services Pattern
- `CompareProductsService` and `RecentlyViewedProductsService` use `IHttpContextAccessor` for cookie-based state
- Cookie format: comma-separated product IDs (e.g., "42,17,8")
- Cookie names: `.Nop.CompareProducts`, `.Nop.RecentlyViewedProducts`
- HttpOnly cookies with 10-day expiration
- Max items controlled by `CatalogSettings.CompareProductsNumber` and `CatalogSettings.RecentlyViewedProductsNumber`

### PriceCalculationService — Discounts Deferred
- Legacy `PriceCalculationService` depended on `IDiscountService` for `GetAllowedDiscounts`, `GetPreferredDiscount`, `DiscountForCaching`
- `IDiscountService` is plan item [4.5] — not yet built
- New `PriceCalculationService` implements: base price, tier pricing (with customer role filtering), attribute price adjustments, rental period multiplication
- Discount application will be added when [4.5] is implemented
- `GetFinalPriceWithDiscountAsync` returns `discountAmount = 0m` until then

### Impact on Future Items
- [4.5] Discounts: can now use `IProductService`, `ICategoryService`, `IManufacturerService` for discount-entity associations; must add discount logic to `PriceCalculationService`
- [4.6] Tax: can now use `IPriceCalculationService` for pre-tax price calculation
- [4.7] Shipping: can now use `IProductService` for product weight/dimensions
- [4.9] Orders: can now use all catalog services for order processing
- [5.4] Public CatalogController: can now use `ICategoryService`, `IManufacturerService`, `IProductService.SearchProducts`
- [5.5] Public ProductController: can now use `IProductService`, `IPriceCalculationService`, `IPriceFormatter`
- [5.31-5.33] Admin Product/Category/Manufacturer controllers: can now use all catalog services

## 2026-04-09 — [4.5] Discount Services / Implementation

### DiscountForCaching DTO Eliminated
- Legacy used `DiscountForCaching` (separate DTO class) to cache discounts separately from EF entities
- New code caches `Discount` entities directly from `TableNoTracking` — consistent with pattern established in [2.2] Logging, [2.3] Localization, [3.4] SEO
- `MapDiscount()` extension method eliminated — no mapping overhead
- All extension methods (`GetDiscountAmount`, `GetPreferredDiscount`, `ContainsDiscount`) operate on `Discount` directly

### Float-Cast Percentage Bug Fixed
- Legacy `GetDiscountAmount` used `(decimal)((((float)amount) * ((float)discount.DiscountPercentage)) / 100f)` — casts to float (32-bit) losing precision for large amounts
- New code uses `amount * discount.DiscountPercentage / 100m` — pure decimal arithmetic, no precision loss
- Impact: discount amounts may differ by fractions of a cent for large order totals — acceptable improvement

### Plugin-Dependent Methods Deferred
- Legacy `LoadDiscountRequirementRuleBySystemName` and `LoadAllDiscountRequirementRules` depend on `IPluginFinder` (plugin system [2.10])
- These methods are NOT included in the new `IDiscountService` interface — will be added when plugin system is built
- Leaf discount requirements pass by default in `EvaluateRequirementsAsync` until plugin system provides `IDiscountRequirementRule` implementations

### Gift Card Validation Deferred
- Legacy `ValidateDiscount` checked `customer.ShoppingCartItems.Any(x => x.Product.IsGiftCard)` to prevent discounts on gift card purchases
- This requires `ShoppingCartItem` nav property on Customer and `Product` nav property on ShoppingCartItem — both stripped in [1.3]
- Gift card validation will be added when [4.9] Order services provides `IShoppingCartService` with cart item queries
- Impact: until [4.9], order-total and order-subtotal discounts can be applied to carts containing gift cards

### Join Entities for Discount-Entity Mappings
- Created `DiscountCategoryMapping` (table: `Discount_AppliedToCategories`), `DiscountManufacturerMapping` (table: `Discount_AppliedToManufacturers`), `DiscountProductMapping` (table: `Discount_AppliedToProducts`)
- Legacy used EF6 `HasMany().WithMany().Map()` for these many-to-many relationships via nav properties
- New code uses explicit join entities with `IRepository<T>` queries — consistent with `CustomerCustomerRoleMapping`, `PermissionRecordRoleMapping`, `ProductProductTagMapping` patterns

### Validation Uses ICustomerService for Registered Check
- Legacy `ValidateDiscount` called `customer.IsRegistered()` extension method (service locator)
- New code uses `ICustomerService.GetCustomerRoleIdsAsync` + `GetCustomerRoleBySystemNameAsync` to check if customer has Registered role
- More explicit, no service locator dependency

### Impact on Future Items
- [4.6] Tax: no direct dependency on discounts
- [4.9] Orders: can now use `IDiscountService` for order total/subtotal discount application; must add gift card validation to `ValidateDiscountAsync`
- [4.4] Catalog: `PriceCalculationService` can now integrate discount logic (currently returns `discountAmount = 0m`)
- [5.40] Admin DiscountController: can now use `IDiscountService` for discount CRUD
- [6.14] Plugin: DiscountRules.CustomerRoles: will implement `IDiscountRequirementRule`
- [6.15] Plugin: DiscountRules.HasOneProduct: will implement `IDiscountRequirementRule`

## 2026-04-09 — [4.6] Tax Services / Implementation

### Customer Entity: BillingAddressId/ShippingAddressId Added
- Legacy `Customer` had `BillingAddress` and `ShippingAddress` nav properties (stripped in [1.3])
- Legacy EF6 config: `HasOptional(c => c.BillingAddress)` / `HasOptional(c => c.ShippingAddress)` — these created `BillingAddress_Id` and `ShippingAddress_Id` FK columns in the DB
- New code adds `int? BillingAddressId` and `int? ShippingAddressId` as explicit FK properties on `Customer` entity
- Impact: [1.5] Nop.Data EF Core config for Customer may need updating if convention-based FK discovery doesn't pick these up (convention expects `AddressId` not `BillingAddressId`). [8.2] Data migration must map `BillingAddress_Id` → `BillingAddressId` column rename
- Any service that previously accessed `customer.BillingAddress` or `customer.ShippingAddress` must now use `IAddressService.GetAddressByIdAsync(customer.BillingAddressId.Value)`

### Async-First with Tuples Instead of Out Parameters
- Legacy `ITaxService` used `out decimal taxRate` parameters extensively (7 methods with out params)
- C# async methods cannot have `out` parameters — replaced all with `Task<(decimal price, decimal taxRate)>` tuple returns
- Legacy `GetVatNumberStatus` used `out string name, out string address` — replaced with `Task<(VatNumberStatus status, string name, string address)>`
- Legacy `DoVatCheck` used `out string name, out string address, out Exception exception` — replaced with `Task<(VatNumberStatus status, string name, string address, Exception? exception)>`
- Impact: all downstream consumers (OrderTotalCalculationService, ShoppingCartService, etc.) must destructure tuples instead of using out params

### CheckoutAttribute Passed Explicitly (No Nav Properties)
- Legacy `GetCheckoutAttributePrice(CheckoutAttributeValue cav)` accessed `cav.CheckoutAttribute` nav property for `IsTaxExempt` and `TaxCategoryId`
- Nav properties stripped in [1.3] — new signature: `GetCheckoutAttributePriceAsync(CheckoutAttributeValue cav, CheckoutAttribute checkoutAttribute, ...)`
- Impact: [4.9] Order services must load `CheckoutAttribute` separately via `IRepository<CheckoutAttribute>` and pass it to tax calculation

### Tax Provider Resolution Deferred to [2.10]
- Legacy `LoadActiveTaxProvider`, `LoadTaxProviderBySystemName`, `LoadAllTaxProviders` all depend on `IPluginFinder` (plugin system)
- These methods are NOT in the new `ITaxService` interface — will be added when plugin system is built
- `GetTaxRateAsync` currently returns `(0m, true)` (0% tax rate, taxable) when no provider is available
- Impact: until [2.10] + [6.13] (Tax.FixedOrByCountryStateZip plugin), all products are taxed at 0%

### Customer Role Tax Exemption via Repository Join
- Legacy used `customer.CustomerRoles.Where(cr => cr.Active).Any(cr => cr.TaxExempt)` nav property
- New code uses LINQ join: `CustomerCustomerRoleMapping` → `CustomerRole` where `Active && TaxExempt`
- Same pattern as [2.4] Security (PermissionService) and [2.5] Authentication (CookieAuthenticationService)

### VIES VAT Check Deferred to [7.12]
- `DoVatCheckAsync` returns `(VatNumberStatus.Unknown, "", "", null)` until VIES HTTP client is implemented
- Legacy used SOAP web reference `EuropaCheckVatService.checkVatService` — will be replaced with `HttpClient` calling VIES REST endpoint
- Impact: EU VAT number validation always returns Unknown status until [7.12]

### VAT Number Regex Modernized
- Legacy created `new Regex(@"^(\w{2})(.*)")` per call — allocates regex object each time
- New code uses `[GeneratedRegex]` source generator — compiled at build time, zero allocation
- `TaxService` is `partial class` to support `GeneratedRegex`

### Impact on Future Items
- [4.7] Shipping: can now use `ITaxService.GetShippingPriceAsync` for shipping tax
- [4.8] Payment: can now use `ITaxService.GetPaymentMethodAdditionalFeeAsync` for payment fee tax
- [4.9] Orders: can now use all `ITaxService` methods for order total calculation; must pass `CheckoutAttribute` explicitly to `GetCheckoutAttributePriceAsync`
- [5.43] Admin TaxController: can now use `ITaxCategoryService` for tax category CRUD
- [6.13] Plugin: Tax.FixedOrByCountryStateZip: will implement `ITaxProvider` interface
- [7.12] VIES VAT: must implement `DoVatCheckAsync` with HTTP client
- [8.2] Data migration: must handle `BillingAddress_Id` → `BillingAddressId` column mapping

## 2026-04-09 — [4.7] Shipping Services / Implementation

### ShippingMethodCountryMapping Join Entity Created
- Legacy `ShippingMethod.RestrictedCountries` nav property was stripped in [1.3] — many-to-many relationship between ShippingMethod and Country had no join entity
- Created `ShippingMethodCountryMapping` (table: `ShippingMethodRestrictions`) with `ShippingMethodId` and `CountryId` properties
- EF Core configuration and DbSet added to NopDbContext
- `GetAllShippingMethodsAsync(filterByCountryId)` queries this join entity to exclude restricted shipping methods — matching legacy behavior where restricted countries mean the method is NOT available in that country

### ShipmentService — Multi-Table Joins Replace Nav Properties
- Legacy `GetAllShipments` used `s.Order.ShippingAddress.CountryId` (3-level nav property chain: Shipment→Order→Address)
- New code uses explicit joins: `Shipment` → `Order` (on OrderId) → `Address` (on ShippingAddressId)
- Legacy vendor filtering used `orderItem.Product.VendorId` and `s.ShipmentItems.Select(si => si.OrderItemId)` — both nav property chains
- New code uses: `ShipmentItem` → `OrderItem` (on OrderItemId) → `Product` (on ProductId) for vendor filtering
- Legacy `GetQuantityInShipments` used `si.Shipment.Order.Deleted` and `si.Shipment.Order.OrderStatusId` — nav property chains
- New code joins: `ShipmentItem` → `Shipment` → `Order` with explicit join conditions

### GetShipmentItemsByShipmentIdAsync — New Method
- Legacy `IShipmentService` did not have a method to get shipment items by shipment ID — it relied on `shipment.ShipmentItems` nav property
- Nav properties stripped in [1.3] — added `GetShipmentItemsByShipmentIdAsync(int shipmentId)` to the interface
- Impact: all code that previously accessed `shipment.ShipmentItems` must now call this method

### Plugin-Dependent Methods Deferred to [2.10]
- Legacy `IShippingService` had 9 plugin-dependent methods: `LoadActiveShippingRateComputationMethods`, `LoadShippingRateComputationMethodBySystemName`, `LoadAllShippingRateComputationMethods`, `LoadActivePickupPointProviders`, `LoadPickupPointProviderBySystemName`, `LoadAllPickupPointProviders`, `GetShippingOptions`, `GetPickupPoints`
- All depend on `IPluginFinder` (plugin system [2.10]) — NOT included in new `IShippingService` interface
- `IShippingRateComputationMethod` and `IPickupPointProvider` interfaces also deferred (extend `IPlugin`)
- `ShippingExtensions` (IsShippingRateComputationMethodActive, IsPickupPointProviderActive, CountryRestrictionExists) also deferred

### Workflow Methods Deferred
- `GetShoppingCartItemWeight`, `GetTotalWeight`, `GetDimensions`, `GetAssociatedProductDimensions`, `CreateShippingOptionRequests` all depend on:
  - `ShoppingCartItem.Product` nav property (stripped in [1.3])
  - `IProductAttributeParser` (for associated product weight/dimensions)
  - `ICheckoutAttributeParser` (for checkout attribute weight)
  - `IGenericAttributeService` (for customer checkout attributes)
  - `IProductService` (for associated product lookup)
- These will be added when [4.9] Order services is built (which provides `IShoppingCartService` with cart item queries)
- `GetShippingOptionRequest` DTO not recreated in services — it's a domain DTO in `Nop.Core.Domain.Shipping` already. The service-layer version with `PackageItem` nested class will be created when workflow methods are implemented

### Impact on Future Items
- [4.8] Payment: no direct dependency on shipping services
- [4.9] Orders: can now use `IShipmentService` for shipment management, `IShippingService` for warehouse/method lookups; must implement workflow methods (weight, dimensions, package creation)
- [5.41] Admin ShippingController: can now use all three shipping services for CRUD
- [6.6-6.12] Shipping plugins: will implement `IShippingRateComputationMethod` / `IPickupPointProvider` interfaces

## 2026-04-09 — [4.8] Payment Services / Implementation

### IPaymentMethod: No IPlugin Dependency
- Legacy `IPaymentMethod` extended `IPlugin` (plugin infrastructure) — new interface is standalone
- `PluginDescriptor.SystemName` replaced with `IPaymentMethod.SystemName` property — simpler, no plugin metadata needed
- Legacy `GetConfigurationRoute`/`GetPaymentInfoRoute`/`GetControllerType` (ASP.NET MVC 5 routing) dropped — ASP.NET Core uses attribute routing and DI-based configuration
- When plugin system [2.10] is built, `IPaymentMethod` may extend a new `IPlugin` interface, or plugins register `IPaymentMethod` implementations directly via DI

### PaymentService: IEnumerable<IPaymentMethod> Resolution
- Legacy used `IPluginFinder.GetPlugins<IPaymentMethod>()` to discover payment methods
- New code resolves `IEnumerable<IPaymentMethod>` from DI constructor injection — standard .NET DI pattern
- `LoadPaymentMethodBySystemName` iterates the injected collection with `OrdinalIgnoreCase` comparison
- Plugin-dependent methods (LoadActivePaymentMethods, LoadAllPaymentMethods, LoadPaymentMethodBySystemName as public API) deferred to [2.10]
- DI registration: each `IPaymentMethod` implementation registered as `services.AddScoped<IPaymentMethod, ConcretePaymentMethod>()`

### Restriction Methods: systemName String Instead of IPaymentMethod
- Legacy `GetRestictedCountryIds(IPaymentMethod)` and `SaveRestictedCountryIds(IPaymentMethod, List<int>)` took `IPaymentMethod` parameter and accessed `paymentMethod.PluginDescriptor.SystemName`
- New methods take `string paymentMethodSystemName` directly — decouples from plugin infrastructure
- Legacy setting key format preserved: `PaymentMethodRestictions.{systemName}` (note: legacy typo "Restictions" preserved for data migration compatibility)

### RoundingHelper Replaced
- Legacy `RoundingHelper.RoundPrice(result)` used service locator (`EngineContext.Current.Resolve<IWorkContext>()`) to get working currency's `RoundingType`
- New code uses `Math.Round(result, 2)` — simple 2-decimal rounding without currency-specific rounding rules
- Currency-specific rounding (cash rounding for Swiss Franc, Hungarian Forint, etc.) can be added when `IWorkContext` is available in the service layer or passed as parameter
- Impact: prices may differ by fractions of a cent for currencies with non-standard rounding rules

### CalculateAdditionalFee Percentage Mode Deferred
- Legacy `PaymentExtensions.CalculateAdditionalFee` with `usePercentage=true` called `IOrderTotalCalculationService.GetShoppingCartTotal(cart, usePaymentMethodAdditionalFee: false)`
- `IOrderTotalCalculationService` is plan item [4.9] — not yet built
- `CalculateAdditionalFee` not included in new `PaymentExtensions` — will be added when [4.9] provides the dependency
- Fixed-fee mode is handled directly by `PaymentService.GetAdditionalHandlingFeeAsync` delegating to `IPaymentMethod.GetAdditionalHandlingFeeAsync`

### PaymentExtensions: XML Serialization Preserved
- Legacy `SerializeCustomValues`/`DeserializeCustomValues` used `XmlSerializer` with custom `DictionarySerializer : IXmlSerializable`
- New code uses `XmlWriter`/`XmlReader` directly — simpler, no `IXmlSerializable` class needed
- XML format preserved: `<CustomValues><item><key>...</key><value>...</value></item></CustomValues>`
- Values are always serialized as strings (via `ToString()`) — matching legacy behavior
- Data migration compatibility: existing `Order.CustomValuesXml` values can be deserialized by new code

### Impact on Future Items
- [4.9] Orders: can now use `IPaymentService` for payment processing, capture, refund, void; must add `CalculateAdditionalFee` percentage mode when `IOrderTotalCalculationService` is built
- [5.42] Admin PaymentController: can now use `IPaymentService` for payment method management
- [6.1-6.5] Payment plugins: will implement `IPaymentMethod` interface (CheckMoneyOrder, Manual, PurchaseOrder, PayPalStandard, PayPalDirect)
- DI registration: `services.AddScoped<IPaymentService, PaymentService>()` + each `IPaymentMethod` implementation

## 2026-04-09 — [4.9] Order Services (Partial) / Implementation

### Services Implemented (9 of 12)
- **ICustomNumberFormatter/CustomNumberFormatter**: Mask-based formatting with `{ID}`, `{YYYY}`, `{YY}`, `{MM}`, `{DD}` tokens. Primary constructor pattern.
- **IRewardPointService/RewardPointService**: Points balance with deferred activation via `ActivatePendingPoints`. `RewardPointsHistory` entity has no `UsedWithOrder`/`Customer` nav properties — uses FK IDs. `AddRewardPointsHistoryEntryAsync` takes `int? usedWithOrderId` instead of `Order usedWithOrder`.
- **IReturnRequestService/ReturnRequestService**: CRUD for return requests + actions + reasons. No caching (matching legacy).
- **ICheckoutAttributeService/CheckoutAttributeService**: Cached CRUD with store mapping via inline LINQ join (same pattern as CountryService). Dual-prefix cache invalidation.
- **ICheckoutAttributeParser/CheckoutAttributeParser**: XML format preserved (`<Attributes><CheckoutAttribute ID="1"><CheckoutAttributeValue><Value>...</Value></CheckoutAttributeValue></CheckoutAttribute></Attributes>`). `EnsureOnlyActiveAttributesAsync` conservatively keeps all attributes — checking `Product.IsShipEnabled` per cart item requires `IProductService` which would create circular dependency.
- **ICheckoutAttributeFormatter/CheckoutAttributeFormatter**: HTML display with tax-adjusted prices via `ITaxService.GetCheckoutAttributePriceAsync`. Removed `ICurrencyService` dependency (not needed for formatting).
- **IGiftCardService/GiftCardService**: CRUD, GUID-based 13-char coupon code generation, usage history via `IRepository<GiftCardUsageHistory>`. `GetGiftCardRemainingAmountAsync` replaces nav-property-based `GiftCardExtensions.GetGiftCardRemainingAmount`. `GetActiveGiftCardsAppliedByCustomerAsync` parses XML coupon codes from `GenericAttribute` (uses `IGenericAttributeService.GetAttributesForEntityAsync` directly, not extension method). No caching (matching legacy).
- **IOrderService/OrderService**: Full CRUD for orders, order items, order notes, recurring payments. `SearchOrdersAsync` with billing address join, product filter join, order notes join. Soft delete for orders and recurring payments. Added `GetOrderItemsByOrderIdAsync`, `GetOrderNotesByOrderIdAsync`, `GetRecurringPaymentHistoryAsync`, `InsertOrderNoteAsync`, `InsertRecurringPaymentHistoryAsync` — methods that replace nav property access. Vendor filtering deferred (needs `IRepository<Product>` which would add dependency on Catalog domain).
- **IOrderReportService/OrderReportService**: Country report, average report, bestsellers, also-purchased, never-sold, profit report. All use LINQ joins replacing nav properties. `IRepository<Product>` injected for `ProductsNeverSoldAsync`.

### Plan Split
- [4.9] was too large for a single iteration — split into [4.9] (done: 9 services), [4.9a] (IShoppingCartService), [4.9b] (IOrderTotalCalculationService), [4.9c] (IOrderProcessingService)
- Each sub-item depends on the previous — ShoppingCartService needed by OrderTotalCalculationService, both needed by OrderProcessingService

### Key Patterns
- **No nav properties**: All cross-entity queries use explicit LINQ joins via `IRepository<T>`. This is consistent with all previous services.
- **Async-first**: All interfaces use `Task<T>` return types. Sync repository calls wrapped in `Task.FromResult`.
- **Primary constructors**: All services use C# 12 primary constructor syntax (matching established pattern).
- **No caching for order services**: Orders, gift cards, return requests, reward points — all low-to-medium volume entities where caching adds complexity without benefit. Only checkout attributes are cached (matching legacy).

### Impact on Future Items
- [4.9a] IShoppingCartService: can now use ICheckoutAttributeParser, ICheckoutAttributeService for checkout attribute validation
- [4.9b] IOrderTotalCalculationService: can now use IRewardPointService, IGiftCardService, ICheckoutAttributeParser
- [4.9c] IOrderProcessingService: can now use IOrderService, ICustomNumberFormatter, IGiftCardService
- [4.10] Export/Import: can now use IOrderService, IOrderReportService
- [5.34] Admin OrderController: can now use IOrderService, IOrderReportService

## 2026-04-09 — [4.9a] IShoppingCartService / Implementation

### Nav Property Elimination — Heaviest Service So Far
- Legacy `ShoppingCartService` was the most nav-property-dependent service: `customer.ShoppingCartItems`, `sci.Product`, `sci.Customer`, `sci.Product.IsGiftCard`, `attribute.Product`, `attributeValue.ProductAttributeMapping.ProductAttribute`
- New code uses `IRepository<ShoppingCartItem>` queries by `CustomerId` for cart retrieval, `IProductService.GetProductByIdAsync` for product loading, `IProductAttributeService` for attribute/value lookups
- Added `GetShoppingCartAsync(Customer, ShoppingCartType?, storeId)` as a new method — replaces `customer.ShoppingCartItems.Where(...).LimitPerStore(storeId)` pattern used throughout legacy

### Extension Methods Inlined as Private Helpers
- `Product.ParseRequiredProductIds()` → `ParseRequiredProductIds(Product)` — comma-separated string parsing
- `Product.ParseAllowedQuantities()` → `ParseAllowedQuantities(Product)` — comma-separated string parsing
- `Product.GetTotalStockQuantity()` → `product.StockQuantity` — legacy method was complex (multi-warehouse aggregation) but simplified since `UseMultipleWarehouses` support is deferred
- `ProductAttributeMapping.IsNonCombinable()` → `ShouldHaveValues(int attributeControlTypeId)` — checks control type
- `customer.IsSearchEngineAccount()` → `customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.SearchEngine)` comparison
- `shoppingCart.RequiresShipping()` → inline foreach checking `product.IsShipEnabled` per cart item
- `shoppingCart.LimitPerStore(storeId)` → `storeId` parameter in `GetShoppingCartAsync` LINQ query
- `shoppingCart.GetRecurringCycleInfo()` → `GetRecurringCycleInfoAsync` private method

### Coupon Code Migration in MigrateShoppingCart
- Legacy used `customer.ParseAppliedDiscountCouponCodes()` and `customer.ApplyDiscountCouponCode()` extension methods — these read/write `GenericAttribute` with key `DiscountCouponCode`
- Legacy used `customer.ParseAppliedGiftCardCouponCodes()` and `customer.ApplyGiftCardCouponCode()` — these read/write `GenericAttribute` with key `GiftCardCouponCodes` (XML format)
- New code reads/writes via `IGenericAttributeService` directly
- Discount codes: comma-separated string, merged by concatenation
- Gift card codes: XML format, copied as-is (merging XML is complex — skipped if target already has codes)

### Mixed Sync/Async API Surface
- `IPermissionService.Authorize` is sync, `IAclService.Authorize` is sync — called directly in async methods
- `IStoreMappingService.AuthorizeAsync` is async — awaited
- `ILocalizationService.GetResourceAsync` is async — all localization calls awaited
- `IPriceFormatter.FormatPriceAsync` is async — awaited in `GetStandardWarningsAsync`
- `ICurrencyService.ConvertFromPrimaryStoreCurrency` is sync — called directly
- `IRepository<T>` methods are sync — wrapped in `Task.FromResult` where needed

### GetTotalStockQuantity Simplified
- Legacy `GetTotalStockQuantity()` aggregated stock across multiple warehouses when `product.UseMultipleWarehouses` was true, querying `ProductWarehouseInventory` records
- New code uses `product.StockQuantity` directly — multi-warehouse stock aggregation deferred until warehouse management is needed
- Impact: products using multiple warehouses will show incorrect stock until multi-warehouse support is added

### Impact on Future Items
- [4.9b] IOrderTotalCalculationService: can now use `IShoppingCartService.GetShoppingCartAsync` for cart retrieval
- [4.9c] IOrderProcessingService: can now use `IShoppingCartService` for cart validation and clearing
- [5.7] Public ShoppingCartController: can now use `IShoppingCartService` for all cart operations
- [5.8] Public CheckoutController: can now use `IShoppingCartService.GetShoppingCartWarningsAsync` for checkout validation
- [5.61] Admin ShoppingCartController: can now use `IShoppingCartService.GetShoppingCartAsync` for abandoned cart viewing

## 2026-04-09 — [4.9b] IOrderTotalCalculationService / Implementation

### UpdateOrderTotals Deferred
- Legacy `UpdateOrderTotals` (200+ LOC) depends on `IShippingService.GetShippingOptions`, `IShippingService.GetPickupPoints`, `IShippingService.LoadActiveShippingRateComputationMethods` — all plugin-dependent methods deferred to [2.10]
- Also depends on `Order.OrderItems` nav property (stripped in [1.3]) and `GiftCard.GiftCardUsageHistory` nav property
- Will be implemented when plugin system [2.10] provides shipping rate computation methods, or as a separate plan item if needed before then
- Impact: admin order editing (recalculating totals after item changes) is not available until UpdateOrderTotals is implemented

### GetShoppingCartShippingTotal Simplified
- Legacy had two paths: (1) use selected shipping option from GenericAttribute, (2) fall back to fixed-rate computation from single active shipping rate computation method
- Path (2) depends on `IShippingService.LoadActiveShippingRateComputationMethods`, `IShippingService.CreateShippingOptionRequests`, `IShippingRateComputationMethod.GetFixedRate` — all plugin-dependent
- New code only implements path (1): reads `SelectedShippingOption` from customer GenericAttribute
- If no shipping option is selected and cart requires shipping, returns null (indicating total cannot be calculated)
- Impact: checkout flow must ensure shipping option is selected before calling GetShoppingCartTotalAsync

### Product Loading Pattern for Cart Items
- Legacy accessed `shoppingCartItem.Product` nav property for `IsShipEnabled`, `IsFreeShipping`, `AdditionalShippingCharge`, `IsRecurring`
- New code loads product via `IProductService.GetProductByIdAsync(sci.ProductId)` for each cart item
- This creates N+1 queries for N cart items — acceptable for typical cart sizes (5-20 items)
- Future optimization: batch load products for all cart items at the start of each method, or add a `GetProductsByIdsAsync` batch method to `IProductService`

### Checkout Attribute Tax Calculation
- Legacy accessed `checkoutAttributeValue.CheckoutAttribute` nav property for `TaxCategoryId` and `IsTaxExempt`
- New code parses checkout attributes from XML, then finds the parent `CheckoutAttribute` by matching `av.CheckoutAttributeId` against parsed attributes
- This means `ParseCheckoutAttributesAsync` is called once to get all attributes, then each value is matched to its parent

### CalculateRewardPointsAsync — Guest Detection
- Legacy used `customer.IsGuest()` extension method (service locator based)
- New code uses `ICustomerService.GetCustomerRoleIdsAsync` + `GetCustomerRoleBySystemNameAsync(Guests)` to check if customer has ONLY the Guest role
- More explicit, no service locator dependency

### Impact on Future Items
- [4.9c] IOrderProcessingService: can now use `IOrderTotalCalculationService` for order total calculation during PlaceOrder
- [4.8] Payment: `CalculateAdditionalFee` percentage mode can now be implemented using `GetShoppingCartTotalAsync(cart, usePaymentMethodAdditionalFee: false)`
- [5.7] Public ShoppingCartController: can now display order totals
- [5.8] Public CheckoutController: can now calculate and display order totals during checkout

## 2026-04-09 — [4.9c] IOrderProcessingService / Implementation

### Architecture: 4 Partial Class Files
- Legacy `OrderProcessingService` was 3167 LOC in a single file — the most complex service in the system
- New code split into 4 partial class files for manageability:
  - `OrderProcessingService.cs` — constructor (35 dependencies via primary constructor) + `PlaceOrderContainer` internal class
  - `OrderProcessingService.PlaceOrder.cs` — `PlaceOrderAsync`, `PreparePlaceOrderDetailsAsync`, `SaveOrderDetailsAsync`, `CreateOrderItemsAsync`, `SendNotificationsAndSaveNotesAsync`
  - `OrderProcessingService.Status.cs` — `CheckOrderStatusAsync`, `SetOrderStatusAsync`, `ProcessOrderPaidAsync`, reward points (award/reduce/return), gift card activation, cancel, delete
  - `OrderProcessingService.Payment.cs` — authorize, capture, mark as paid, refund (full/partial, online/offline), void (online/offline)
  - `OrderProcessingService.Shipping.cs` — ship, deliver, recurring payments, reorder, return request, validation

### Nav Property Elimination — Heaviest Orchestrator
- Legacy `OrderProcessingService` was the heaviest consumer of nav properties: `customer.ShoppingCartItems`, `order.OrderItems`, `order.Shipments`, `order.Customer`, `orderItem.Product`, `recurringPayment.InitialOrder`, `order.BillingAddress`, `order.ShippingAddress`, `order.RedeemedRewardPointsEntry`
- New code uses explicit service calls for all cross-entity access:
  - `IShoppingCartService.GetShoppingCartAsync` replaces `customer.ShoppingCartItems`
  - `IOrderService.GetOrderItemsByOrderIdAsync` replaces `order.OrderItems`
  - `IProductService.GetProductByIdAsync` replaces `orderItem.Product`
  - `ICustomerService.GetCustomerByIdAsync` replaces `order.Customer`
  - `IAddressService.GetAddressByIdAsync` replaces `order.BillingAddress`/`order.ShippingAddress`
  - `IRewardPointService.GetRewardPointsHistoryEntryByIdAsync` replaces `order.RedeemedRewardPointsEntry`

### CanCancelRecurringPayment / CanRetryLastRecurringPayment — Signature Change
- Legacy methods accessed `recurringPayment.InitialOrder` nav property internally
- New methods take `Order? initialOrder` as explicit parameter — caller must load it
- Impact: [5.34] Admin OrderController and [5.59] Admin RecurringPaymentController must load initial order before calling these methods

### UpdateOrderTotals Deferred
- Legacy `UpdateOrderTotals` (200+ LOC) depends on `IShippingService.GetShippingOptions`, `IShippingService.LoadActiveShippingRateComputationMethods` — all plugin-dependent methods deferred to [2.10]
- Also depends on `IOrderTotalCalculationService.UpdateOrderTotals` which was deferred in [4.9b]
- Will be implemented when plugin system [2.10] provides shipping rate computation methods

### Address Cloning Pattern
- Legacy used `(Address)customer.BillingAddress.Clone()` — nav property + Clone method
- New code uses `CloneAddress(Address)` private static method that creates a new Address entity with copied scalar properties, then `InsertAddressAsync` to persist it
- The cloned address gets its own ID — order references the clone, not the customer's current address
- This preserves the legacy behavior: order addresses are snapshots at order time

### Shipping Option Parsing
- Legacy stored `ShippingOption` as a serialized object in GenericAttribute (XML format via Autofac TypeConverter)
- New code attempts JSON parsing of the stored shipping option to extract `Name` and `ShippingRateComputationMethodSystemName`
- Falls back to treating the value as a plain text name if JSON parsing fails
- Impact: checkout flow must store shipping option as JSON in GenericAttribute

### IPriceCalculationService.GetSubTotalAsync Returns Decimal Only
- Legacy `GetSubTotal` returned discount amount and applied discounts via `out` parameters
- New `GetSubTotalAsync` returns only the final subtotal (decimal)
- Discount amount for order items computed as difference between no-discount and with-discount subtotals
- Impact: per-item discount tracking is approximate — exact discount attribution requires adding tuple return to `GetSubTotalAsync` in future

### Added InsertOrderItemAsync to IOrderService
- Legacy used `order.OrderItems.Add(orderItem)` + `UpdateOrder(order)` — nav property collection manipulation
- New code needs explicit `InsertOrderItemAsync(OrderItem)` since there are no nav properties
- Added to both `IOrderService` interface and `OrderService` implementation

### Added Public GetRecurringCycleInfoAsync to IShoppingCartService
- Legacy `GetRecurringCycleInfo` was an extension method on `IList<ShoppingCartItem>` using service locator
- New code had a private method in `ShoppingCartService.Validation.cs` — promoted to public interface method
- Returns `(string? Error, int CycleLength, RecurringProductCyclePeriod CyclePeriod, int TotalCycles)` tuple
- Private method renamed to `GetRecurringCycleInfoInternalAsync` for internal validation use

### Removed Unused Constructor Parameters
- `IPriceFormatter`, `ICountryService`, `IStateProvinceService` were in the legacy constructor but not needed in the new implementation
- `IPriceFormatter` — only used in legacy for min order amount error messages (new code uses localization resource directly)
- `ICountryService`/`IStateProvinceService` — only used in legacy for pickup point address creation (new code defers pickup address creation to checkout controller)
- Removed to satisfy `TreatWarningsAsErrors` (CS9113 unread primary constructor parameter)

### Impact on Future Items
- [4.10] Export/Import: can now use `IOrderProcessingService` for order-related operations
- [5.8] Public CheckoutController: can now use `PlaceOrderAsync` for order placement
- [5.34] Admin OrderController: can now use all payment/status operations
- [5.59] Admin RecurringPaymentController: can now use recurring payment operations
- Phase 4 is now COMPLETE — all service layer items implemented

## 2026-04-09 — [1.7] Test Project Scaffold / Implementation

### Test Infrastructure Choices
- **NSubstitute 5.x** over Moq — simpler syntax, no Castle.Core dependency, better async support
- **FluentAssertions 6.x** — readable assertion syntax, good xUnit integration
- **EF Core InMemory 8.0.13** — matches EF Core version in Nop.Data for integration tests

### Shared Test Helpers Pattern
- `FakeRepository<T>` in `Nop.Tests` namespace (lives in Nop.Core.Tests/Helpers/) — `List<T>`-backed `IRepository<T>` with auto-incrementing IDs. No EF Core dependency, fast, deterministic
- `FakeCacheManager` in `Nop.Tests` namespace — `ConcurrentDictionary`-backed `IStaticCacheManager`. Supports prefix-based invalidation matching production behavior
- Both placed in Nop.Core.Tests project; Services.Tests references Core.Tests project for access
- Namespace `Nop.Tests` (not `Nop.Core.Tests`) so any test project can use them without namespace confusion

### xUnit Implicit Usings Gap
- `ImplicitUsings: enable` in Directory.Build.props provides System/System.Linq/etc. but NOT `Xunit`
- All test files must include explicit `using Xunit;` — this is standard for xUnit projects
- Could add a `GlobalUsings.cs` to each test project but explicit imports are clearer for test files

### Pre-existing Format Issues
- `dotnet format --verify-no-changes` reports whitespace issues in `OrderProcessingService.Shipping.cs` and `OrderProcessingService.Status.cs` from previous iterations
- These are NOT from [1.7] changes — test files pass format check cleanly
- Should be fixed in a dedicated cleanup pass or when those files are next modified

### Test Coverage Summary
- Core layer: CommonHelper (email/IP validation, string utilities, type conversion), PagedList (all 3 constructors, pagination properties), CacheKey (constructor, prefixes, init property)
- Data layer: EfRepository CRUD with InMemory provider (insert, batch insert, update, delete, batch delete, null guard, no-tracking query)
- Services layer: EncryptionService (salt, hashing with 4 algorithms, AES encrypt/decrypt roundtrip), SettingService (set/get/overwrite/delete/load/save settings with FakeRepository + FakeCacheManager)

### Impact on Future Items
- All future service implementations can be tested using FakeRepository + FakeCacheManager + NSubstitute pattern
- EfRepository integration tests can be extended for any entity type using InMemory provider
- WebApplicationFactory-based E2E tests (acceptance criterion) deferred until [5.1] Nop.Web.Framework provides enough infrastructure

## 2026-04-10 — [4.3] Forum Services / Implementation

### Single Service — No Separate IPrivateMessageService
- Legacy `IForumService` handles ALL forum operations including private messages — no separate `IPrivateMessageService` exists
- New code preserves this: `IForumService` / `ForumService` is a single service with 5 partial class files for manageability
- ForumExtensions (FormatPostText, StripTopicSubject, FormatPrivateMessageText, GetFirstPost, GetLastPost) NOT migrated — these are presentation-layer concerns using service locator (`EngineContext.Current.Resolve<ForumSettings>()`) and will be handled by controllers/model factories

### customer.IsGuest() / customer.IsForumModerator() Replaced
- Legacy permission checks used `customer.IsGuest()` and `customer.IsForumModerator()` extension methods (service locator based, from `CustomerExtensions`)
- New code uses `ICustomerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Guests/ForumModerators)` + `GetCustomerRoleIdsAsync(customer)` + `Contains(roleId)`
- This adds 2 async calls per permission check — acceptable since permission checks are low-frequency (once per user action)
- Pattern: `IsGuestAsync(Customer)` and `IsForumModeratorAsync(Customer)` private helper methods in ForumService

### IWorkContext Removed from MoveTopic
- Legacy `MoveTopic` called `IsCustomerAllowedToMoveTopic(_workContext.CurrentCustomer, forumTopic)` internally
- New `MoveTopicAsync` does NOT check permissions — the caller (controller) is responsible for checking `IsCustomerAllowedToMoveTopicAsync` before calling `MoveTopicAsync`
- This removes the `IWorkContext` dependency from ForumService entirely — cleaner separation of concerns
- Impact: [5.12] Public BoardsController must call `IsCustomerAllowedToMoveTopicAsync` before `MoveTopicAsync`

### Private Message Keyword Search: OR Instead of AND
- Legacy `GetAllPrivateMessages` with keywords applied TWO separate `.Where()` clauses: `pm.Subject.Contains(keywords)` AND `pm.Text.Contains(keywords)` — this means BOTH subject AND text must contain the keyword (very restrictive, likely a bug)
- New code uses `pm.Subject.Contains(keywords) || pm.Text.Contains(keywords)` — matches if keyword appears in EITHER subject OR text (more intuitive)
- Impact: search results may return more messages than legacy — this is an improvement

### Notification Language ID
- Legacy used `_workContext.WorkingLanguage.Id` for notification language
- New code passes `0` (default language) since `IWorkContext` is not injected
- When presentation layer is built, controllers can pass the correct language ID to `InsertTopicAsync`/`InsertPostAsync` if needed, or the notification methods can resolve default language internally

### Impact on Future Items
- [5.12] Public BoardsController: can now use `IForumService` for all forum operations
- [5.17] Public PrivateMessagesController: can now use `IForumService` for PM operations
- [5.46] Admin ForumController: can now use `IForumService` for forum CRUD
- [3.16] GDPR: `PermanentDeleteCustomerAsync` already deletes forum posts/topics/subscriptions/private messages — no changes needed

## 2026-04-10 — [5.1] Nop.Web.Framework / Implementation

### Service Locator Elimination — Complete
- All 8 legacy action filter attributes used `EngineContext.Current.Resolve<T>()` (service locator pattern)
- New code uses `IAsyncActionFilter` with primary constructor DI injection — zero service locator calls
- `NopResourceDisplayName` is the only exception: uses static `IHttpContextAccessor` because `DisplayNameAttribute` is instantiated by the MVC metadata system, not DI. Requires `NopResourceDisplayName.Configure(httpContextAccessor)` call at startup

### WebWorkContext — Removed IStoreMappingService and LocalizationSettings Dependencies
- Legacy `WebWorkContext` used `IStoreMappingService` for language store filtering and `LocalizationSettings` for SEO URL language detection
- Language store filtering was already deferred in [2.3] discovery — `GetAllLanguagesAsync(storeId)` handles store filtering at the service level
- SEO URL language detection deferred to `LanguageSeoCodeFilter` (currently a no-op placeholder) — requires localized URL routing infrastructure
- Removing these dependencies simplifies the constructor from 15 to 12 parameters

### Action Filters — IAsyncActionFilter vs ActionFilterAttribute
- Legacy used `ActionFilterAttribute` (ASP.NET MVC 5) with `OnActionExecuting` override
- New code uses `IAsyncActionFilter` (ASP.NET Core) with `OnActionExecutionAsync` — supports async operations natively
- Tracking filters (CustomerLastActivity, StoreIpAddress, StoreLastVisitedPage, CheckAffiliate) run AFTER action execution (`await next()` first)
- Blocking filters (StoreClosed, PublicStoreAllowNavigation, ValidatePassword) run BEFORE action execution (set `context.Result` to short-circuit)
- `LanguageSeoCodeFilter` is a no-op placeholder — full implementation requires localized URL routing infrastructure (LocalizedRoute, LocalizedUrlExtensions)

### ValidatePassword — No PasswordIsExpired Extension Method
- Legacy used `customer.PasswordIsExpired()` extension method (service locator based, from `CustomerExtensions`)
- New code uses `ICustomerService.GetCurrentPasswordAsync(customer.Id)` + `CustomerSettings.PasswordLifetime` to check expiration directly
- Also checks if customer is registered via `ICustomerService.GetCustomerRoleIdsAsync` (no nav properties)

### BaseController — No Service Locator for Logging
- Legacy `LogException` used `EngineContext.Current.Resolve<IWorkContext>()` and `EngineContext.Current.Resolve<ILogger>()`
- New `ErrorNotification(Exception)` uses `HttpContext.RequestServices.GetService<ILogger<BaseController>>()` — scoped to the request, not a global service locator
- This is the only `RequestServices` usage in the entire Web.Framework — all other DI is via constructor injection

### FluentValidation — SetDatabaseValidationRules Dropped
- Legacy `BaseNopValidator<T>.SetDatabaseValidationRules` used `System.Linq.Dynamic` to dynamically create validation rules from DB column metadata
- This required `IDbContext.GetColumnsMaxLength()` and `IDbContext.GetDecimalMaxValue()` — EF Core doesn't expose these
- New approach: validators use explicit `RuleFor(x => x.Name).MaximumLength(400)` rules matching entity configuration
- Trade-off: validation rules must be manually kept in sync with EF Core configurations. This is standard practice in modern ASP.NET Core apps

### Kendo UI — Filter/Sort/QueryableExtensions Dropped
- Legacy `Filter`, `Sort`, `QueryableExtensions`, `ModelStateExtensions` all depended on `System.Linq.Dynamic`
- New code only provides `DataSourceRequest` (Page/PageSize) and `DataSourceResult` (Data/Total/Errors/ExtraData)
- Admin grids will use server-side paging: controller receives `DataSourceRequest`, queries with `.Skip((page-1)*pageSize).Take(pageSize)`, returns `DataSourceResult`
- No dynamic LINQ filtering/sorting — admin controllers will implement filtering explicitly in LINQ queries

### Deferred Components
- **Theme engine** (IThemeContext, IThemeProvider, ThemeableRazorViewEngine): requires view location expander infrastructure — deferred until views are built
- **GenericPathRoute SEO routing**: requires ASP.NET Core endpoint routing customization — deferred until public controllers need SEO-friendly URLs
- **Localized URL routing** (LocalizedRoute, LocalizedUrlExtensions): requires route constraint infrastructure — deferred
- **IPageHeadBuilder**: requires layout views — deferred until [5.26] Shared views
- **Captcha/Honeypot**: deferred to [7.13] Google reCAPTCHA integration
- **RemotePost**: deferred until payment plugins need form POST redirects
- **Custom model binders** (NopModelBinder, CommaSeparatedModelBinder): deferred until controllers need them
- **Custom action results** (RssActionResult, NullJsonResult, XmlDownloadResult): deferred until controllers need them

### Impact on Future Items
- [5.2-5.28] Public controllers: can now extend `BasePublicController`, use `IWorkContext` via DI
- [5.30-5.82] Admin controllers: can now extend `BaseAdminController`, use `[Authorize(Policy = "...")]` for permissions
- Action filters must be registered as global filters or per-controller in `Program.cs`: `services.AddScoped<CustomerLastActivityFilter>()` + `options.Filters.AddService<CustomerLastActivityFilter>()`
- `NopResourceDisplayName.Configure(httpContextAccessor)` must be called in `Program.cs` after DI container is built

## 2026-04-10 — [1.6] EF Core Initial Migration / Implementation

### dotnet-ef Tool Setup
- `dotnet-ef` global tool was not installed — installed version 8.0.25 matching SDK 8.0.413
- Requires `DOTNET_ROOT=/home/artrodri/.dotnet` environment variable for tool to find the runtime
- Tool path: `/home/artrodri/.dotnet/tools` must be on PATH

### NopDbContextFactory Design-Time Factory
- EF Core migrations tooling (`dotnet ef`) needs `IDesignTimeDbContextFactory<NopDbContext>` to instantiate the context without a running application
- Connection string in factory is design-time only (used for migration generation, not runtime) — points to a dummy `NopCommerce_Design` database
- Factory lives in `Nop.Data/NopDbContextFactory.cs` alongside the context

### OverriddenPrice Precision Fix
- `ProductAttributeCombination.OverriddenPrice` (decimal?) had no precision configured in `ProductAttributeCombinationConfiguration`
- EF Core warned: "No store type was specified for the decimal property 'OverriddenPrice'" — values would be silently truncated
- Fixed by adding `builder.Property(pac => pac.OverriddenPrice).HasPrecision(18, 4)` — matching money field convention
- All other decimal properties already had precision configured in their respective entity configurations

### Migration Statistics
- 113 `CreateTable` calls in the migration — covers all 105 DbSets plus join entities (CustomerCustomerRoleMapping, PermissionRecordRoleMapping, ProductProductTagMapping, DiscountCategoryMapping, DiscountManufacturerMapping, DiscountProductMapping, ShippingMethodCountryMapping) and additional entities
- Migration file sizes: ~144K (Up/Down), ~159K (Designer), ~159K (Snapshot)
- No manual edits to generated migration files — all schema derived from entity configurations

### Pre-existing Format Issues Fixed
- `OrderProcessingService.Payment.cs`, `OrderProcessingService.PlaceOrder.cs`, `OrderProcessingService.Shipping.cs`, `OrderProcessingService.Status.cs` had whitespace formatting issues from previous iterations
- Fixed via `dotnet format` — these were noted in [1.7] discoveries but not resolved until now

### Impact on Future Items
- [8.1] SQL Server schema migration: InitialCreate migration provides the baseline schema for data migration
- [4.11] Installation services: can use `context.Database.MigrateAsync()` to create database from scratch
- Future migrations: use `dotnet ef migrations add <Name>` from `src/New/Data/Nop.Data/` directory with `DOTNET_ROOT` set

## 2026-04-10 — [2.7] Error Handling / Implementation

### IExceptionHandler Pattern (ASP.NET Core 8+)
- Used `IExceptionHandler` (new in .NET 8) instead of legacy `HandleErrorAttribute` or custom middleware
- `NopExceptionHandler` registered via `builder.Services.AddExceptionHandler<NopExceptionHandler>()` + `app.UseExceptionHandler()` in non-dev environments
- In development, the default developer exception page is used (no `UseExceptionHandler` call)

### Optional DI Dependencies via RequestServices
- `NopExceptionHandler` constructor only takes `IHostEnvironment` and `ILogger<T>` (always available)
- `INopLogger` and `IWorkContext` resolved from `httpContext.RequestServices.GetService<T>()` at runtime — returns null if not registered
- This pattern is necessary because the full DI composition root isn't built yet (services registered incrementally as plan items are completed)
- When DI composition root is complete, these could be promoted to constructor injection, but the RequestServices pattern is more resilient

### IWorkContext.CurrentCustomer Is Sync Property
- `IWorkContext.CurrentCustomer` is a sync property (not `GetCurrentCustomerAsync()`)
- This matches the legacy pattern where `WebWorkContext` resolves the customer synchronously from cookie auth
- The exception handler accesses it synchronously — no async bridging needed

### Program.cs Now Has MVC Infrastructure
- `AddControllersWithViews()` and `MapControllers()` added to Program.cs for the first time
- This enables all future controllers (Phase 5) to work without additional Program.cs changes
- `UseStatusCodePagesWithReExecute("/page-not-found")` handles 404s by re-executing the request to CommonController.PageNotFound

### API vs Browser Detection
- `NopExceptionHandler` checks `Accept` header to distinguish API requests (JSON) from browser requests (HTML)
- API requests get `ProblemDetails` JSON with stack trace in development mode only
- Browser requests get a 302 redirect to `/error` which serves the Error.cshtml view
- This pattern supports both the MVC storefront and any future API endpoints

### Service Validation Pattern — Deferred
- Spec acceptance criterion "Service validation errors returned as structured results" is a cross-cutting concern affecting all services
- Existing services already use `IList<string>` error return pattern (e.g., `ShoppingCartService.GetShoppingCartWarningsAsync`)
- A formal `Result<T>` pattern (FluentResults, OneOf) would require refactoring all service interfaces — not worth the disruption
- Decision: keep existing `IList<string>` pattern for validation errors. Global exception handler covers unhandled exceptions only.

### Impact on Future Items
- [5.2-5.28] Public controllers: error pages available, controllers can throw and exceptions are caught
- [5.30-5.82] Admin controllers: same error handling applies
- [5.26] Shared views: Error.cshtml and PageNotFound.cshtml use standalone Layout=null — will integrate with shared layout when built
- Program.cs: future DI registrations go before `var app = builder.Build()`, middleware goes after

## 2026-04-10 — [2.8] Observability / Implementation

### Health Check Architecture
- `NopDbHealthCheck` lives in `Nop.Data` (closest to the DB dependency) — uses `NopDbContext.Database.CanConnectAsync()` for lightweight connectivity check
- Health check registered with `"ready"` tag — readiness probe at `/health/ready` includes DB check, liveness probe at `/health/live` includes no checks (just confirms app process is running)
- `/health` endpoint runs all registered checks (superset of ready)
- Redis health check deferred to [7.2] — will add `AddCheck<RedisHealthCheck>("redis", tags: ["ready"])` when Redis integration is built
- SMTP health check deferred to [7.1] — will add when MailKit integration has a health check wrapper

### OpenTelemetry Package Versions
- `OpenTelemetry.Extensions.Hosting` 1.10.0 — core hosting integration
- `OpenTelemetry.Instrumentation.AspNetCore` 1.10.1 — auto-instruments HTTP request metrics and traces
- `OpenTelemetry.Instrumentation.Http` 1.10.0 — auto-instruments outbound HttpClient calls (payment, shipping APIs when built)
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` 1.9.0-beta.2 — exposes `/metrics` endpoint for Prometheus scraping. Beta because the stable Prometheus exporter hasn't shipped yet for this version line
- No OTLP exporter added — can be added later for pushing to Jaeger/Tempo/Grafana Cloud

### Prometheus Exporter Is Beta
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` 1.9.0-beta.2 is the latest available version
- The Prometheus exporter for ASP.NET Core has been in beta for several releases — stable enough for production use
- If stability is a concern, can switch to OTLP exporter + Prometheus remote write adapter

### Custom Business Metrics Deferred
- Spec mentions "order count, cache hit/miss ratio, queue depth" — these require custom `Meter` and `Counter<T>` instruments
- Will add custom metrics when the services that produce them are wired into DI (e.g., order count from OrderProcessingService, cache metrics from MemoryCacheManager)
- ASP.NET Core instrumentation already provides: request count, request duration, error rates, active requests

### Impact on Future Items
- [5.27] KeepAliveController: can be replaced by `/health/live` endpoint — no controller needed
- [7.2] Redis: add Redis health check to health check builder
- [7.1] SMTP: add SMTP health check to health check builder
- [9.7] Monitoring: dashboards can scrape `/metrics` for Prometheus data, `/health` for uptime monitoring
- Custom `ActivitySource` for nopCommerce-specific spans can be added when payment/shipping API calls are implemented

## 2026-04-10 — [5.2] Public HomeController + Home Views / Implementation

### Conventional Routing Established
- Replaced `app.MapGet("/", ...)` + `app.MapControllers()` with `app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")`
- This enables conventional routing so HomeController.Index serves `/` and all other controllers work via `{controller}/{action}/{id?}` pattern
- `CommonController` uses `[Route]` attribute routing (explicit `/error` and `/page-not-found` routes) — attribute routing and conventional routing coexist in ASP.NET Core
- Impact: all future public and admin controllers can use conventional routing without additional Program.cs changes. Admin area controllers will need `[Area("Admin")]` attribute + area route registration when Phase 5B begins

### Legacy Child Actions → View Components (Future)
- Legacy `Index.cshtml` used `@Html.Action("HomepageCategories", "Catalog")` etc. — ASP.NET MVC 5 child actions
- ASP.NET Core replaces child actions with View Components (`@await Component.InvokeAsync("HomepageCategories")`)
- Current view uses Razor comments documenting each placeholder and which plan item will implement it
- When [5.4] Catalog, [5.5] Product, [5.11] News, [5.14] Poll, [5.13] Topic are implemented, they should register view components for homepage sections

### Impact on Future Items
- [5.3-5.28] Public controllers: conventional routing is now active — controllers just need to follow `{controller}/{action}` naming convention
- [5.26] Shared views: `_Layout.cshtml` will be needed when views need a shared layout (currently Index.cshtml has no layout)
- [5.30] Admin HomeController: will need area route registration: `app.MapControllerRoute("admin", "Admin/{controller=Home}/{action=Index}/{id?}")`
