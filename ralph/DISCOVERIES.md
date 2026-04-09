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
