# Implementation Plan

Greenfield .NET 10 rewrite of nopCommerce (.NET Framework 4.5.1 → .NET 10).
Sequenced leaf-to-root, lowest risk first. Each item references its spec.

---

## Phase 1: Foundation

- [x] [1.1] Solution scaffold — .sln, Directory.Build.props, global.json, nuget.config
  - Spec: specs/solution-scaffold.md
  - Scope: New solution structure
  - Depends on: nothing
  - Done: 2026-04-09. .NET 8 SDK (8.0.413). 6 source + 5 test projects. src/New/ layout.

- [ ] [1.2] CI/CD pipeline — build, test, publish
  - Spec: specs/solution-scaffold.md
  - Scope: GitHub Actions or Azure DevOps
  - Depends on: 1.1

- [x] [1.3] Nop.Core.Domain project — all 207 entity types, enums, marker interfaces
  - Spec: specs/nop-core-domain.md
  - Scope: src/Core/Nop.Core.Domain (26 domain subdirectories)
  - Depends on: 1.1
  - Done: 2026-04-09. 167 .cs files (entities, enums, interfaces, DTOs, constants). Settings/Events/Extensions/TypeConverters excluded (belong in Nop.Core or Nop.Services). Nullable annotations throughout. No nav properties (EF Core config).

- [x] [1.4] Nop.Core.Infrastructure project — IRepository, IEngine, IWorkContext, IStoreContext, IWebHelper
  - Spec: specs/nop-core-infrastructure.md
  - Scope: src/Core/Nop.Core
  - Depends on: 1.3
  - Done: 2026-04-09. 24 files: IRepository<T>, IPagedList<T>/PagedList<T>, ISettings, IWorkContext, IStoreContext, IWebHelper, CacheKey, IStaticCacheManager, EntityInserted/Updated/Deleted, CommonHelper, NopException, NopVersion, MimeTypes, XmlHelper, Extensions, GenericListTypeConverter<T>, GenericDictionaryTypeConverter<K,V>, HtmlHelper, BBCodeHelper, ResolveLinksHelper, NopConfig. Modernized: GeneratedRegex, async caching, IOptions<NopConfig>, dropped service locator pattern.

- [x] [1.5] Nop.Data project — NopDbContext, EfRepository<T>, all entity configurations
  - Spec: specs/nop-data.md
  - Scope: src/Data/Nop.Data
  - Depends on: 1.3, 1.4
  - Done: 2026-04-09. NopDbContext (105 DbSets), EfRepository<T> (IRepository<T>), 17 configuration files covering all entities. EF Core 8.0.13. No nav properties → convention-based FKs. Enum properties Ignored. Decimal precision(18,4) money, (18,8) rates. Computed properties (NumReplies, FriendlyName) Ignored. Configs auto-discovered via ApplyConfigurationsFromAssembly.

- [x] [1.6] EF Core initial migration — create database from entity configurations
  - Spec: specs/nop-data.md
  - Scope: EF Core migration
  - Depends on: 1.5
  - Done: 2026-04-10. Added Microsoft.EntityFrameworkCore.Design 8.0.13 (PrivateAssets=all). Created NopDbContextFactory (IDesignTimeDbContextFactory) for dotnet-ef tooling. Fixed OverriddenPrice decimal precision (18,4) on ProductAttributeCombinationConfiguration. InitialCreate migration generates 113 tables matching all 105 DbSets + join entities. Pre-existing format issues in OrderProcessingService.*.cs fixed.

- [x] [1.7] Test project scaffold — xUnit projects per layer, shared test utilities
  - Spec: specs/testing-strategy.md
  - Scope: Test projects for Core, Data, Services, Web
  - Depends on: 1.1
  - Done: 2026-04-09. Added NSubstitute 5.x, FluentAssertions 6.x, EF Core InMemory 8.0.13 to test projects. Created FakeRepository<T> (in-memory IRepository) and FakeCacheManager (in-memory IStaticCacheManager) as shared test helpers in Nop.Tests namespace. 6 test files: CommonHelperTests (14 tests), PagedListTests (5 tests), CacheKeyTests (5 tests), EfRepositoryTests (7 tests with InMemory), EncryptionServiceTests (9 tests), SettingServiceTests (7 tests). All 65 tests pass.

---

## Phase 2: Cross-Cutting Concerns

- [x] [2.1] Caching — IMemoryCache + IDistributedCache + pattern invalidation + per-request cache
  - Spec: specs/xcut-caching.md
  - Scope: Caching infrastructure
  - Depends on: 1.4
  - Done: 2026-04-09. MemoryCacheManager (IMemoryCache wrapper, ConcurrentDictionary key tracking, PostEviction cleanup, prefix-based invalidation). NopRequestCache (scoped Dictionary). CachingDefaults (60-min default). Microsoft.Extensions.Caching.Memory 8.0.1. Redis IDistributedCache deferred to [7.2].

- [x] [2.2] Logging — INopLogger (DB), ICustomerActivityService, LoggingExtensions, NullLogger
  - Spec: specs/xcut-logging.md
  - Scope: Nop.Services/Logging
  - Depends on: 1.5
  - Done: 2026-04-09. INopLogger (renamed from ILogger to avoid Microsoft.Extensions.Logging conflict), DefaultLogger, ICustomerActivityService, CustomerActivityService, LoggingExtensions, NullLogger. Fixed ActivityLog entity (added IpAddress). Dropped CommonSettings.IgnoreLogWordlist (deferred to [3.1]). Dropped TRUNCATE optimization (uses repository delete-all). Microsoft.Extensions.Logging integration deferred to [2.8] Observability.

- [x] [2.3] Localization — ILocalizationService, ILanguageService, ILocalizedEntityService, string resources
  - Spec: specs/xcut-localization.md
  - Scope: Nop.Services/Localization + Nop.Web.Framework/Localization
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. ILanguageService/LanguageService (cached, event publishing), ILocalizationService/LocalizationService (dual-mode resource lookup, XML import/export), ILocalizedEntityService/LocalizedEntityService (dual-mode property lookup, expression-based save), LocalizationExtensions (entity/enum/setting localization). Dropped service locator pattern. Plugin extensions deferred to [2.10], permission extensions to [2.4]. IStoreMappingService filtering deferred to [3.2].

- [x] [2.4] Security — IPermissionService, IAclService, IEncryptionService
  - Spec: specs/xcut-security.md
  - Scope: Nop.Services/Security
  - Depends on: 1.5
  - Done: 2026-04-09. IEncryptionService/EncryptionService (AES encrypt/decrypt, SHA1/SHA256/MD5/SHA384/SHA512 hashing, RandomNumberGenerator salt). IAclService/AclService (entity-level ACL via customer roles, cached role ID lookup, CatalogSettings.IgnoreAcl bypass). IPermissionService/PermissionService (cached per-role permission check, install/uninstall with role mappings, localized permission names). StandardPermissionProvider (50 permissions, 5 default role mappings). IPermissionProvider interface. Created join entities CustomerCustomerRoleMapping and PermissionRecordRoleMapping with EF Core configs and DbSets. Permission localization extensions added to LocalizationExtensions.

- [x] [2.5] Authentication — cookie auth, sign-in/sign-out, impersonation
  - Spec: specs/xcut-authentication.md
  - Scope: Authentication middleware + IAuthenticationService
  - Depends on: 2.4
  - Done: 2026-04-09. IAuthenticationService (async: SignInAsync, SignOutAsync, GetAuthenticatedCustomerAsync). CookieAuthenticationService uses ASP.NET Core cookie auth with CustomerGuid claim (stable identifier). NopAuthenticationDefaults (scheme + claim type constants). No ICustomerService dependency — uses IRepository<Customer> + join tables directly to avoid circular dependency. Validates Active, !RequireReLogin, !Deleted, IsRegistered (via CustomerCustomerRoleMapping join). Impersonation remains in WebWorkContext (not auth service) via GenericAttribute ImpersonatedCustomerId — matching legacy pattern. External auth (IOpenAuthenticationService etc.) deferred to [6.17].

- [x] [2.6] Authorization — ASP.NET Core policies, permission-based authorization, admin area protection
  - Spec: specs/xcut-authorization.md
  - Scope: Authorization policies + IPermissionService integration
  - Depends on: 2.4, 2.5
  - Done: 2026-04-09. NopPermissionRequirement (IAuthorizationRequirement per permission system name), NopPermissionHandler (AuthorizationHandler delegates to IPermissionService.Authorize), NopAuthorizationPolicyProvider (dynamic IAuthorizationPolicyProvider creates policies per permission system name — any [Authorize(Policy = "ManageProducts")] auto-resolves). Replaces legacy AdminAuthorizeAttribute + service locator pattern. AdminVendorValidation deferred to [5.1] (presentation-layer filter). DI registration (services.AddSingleton<IAuthorizationPolicyProvider, NopAuthorizationPolicyProvider>() + services.AddScoped<IAuthorizationHandler, NopPermissionHandler>()) deferred to Nop.Web Program.cs.

- [x] [2.7] Error handling — global exception handler, ProblemDetails, custom error pages
  - Spec: specs/xcut-error-handling.md
  - Scope: Middleware + error views
  - Depends on: 2.2
  - Done: 2026-04-10. NopExceptionHandler (IExceptionHandler) logs to INopLogger (best-effort) and Microsoft.Extensions.Logging, returns ProblemDetails JSON for API requests, redirects to /error for browser requests. CommonController serves Error.cshtml (500) and PageNotFound.cshtml (404). Program.cs wired with AddExceptionHandler, AddProblemDetails, UseStatusCodePagesWithReExecute. INopLogger/IWorkContext resolved from RequestServices (optional) since full DI composition root not yet built.

- [x] [2.8] Observability — health checks, OpenTelemetry metrics, distributed tracing
  - Spec: specs/xcut-observability.md
  - Scope: Health check endpoints + OTel configuration
  - Depends on: 1.5, 2.1
  - Done: 2026-04-10. NopDbHealthCheck (Nop.Data, EF Core CanConnectAsync). Health endpoints: /health (all checks), /health/live (liveness — no checks, confirms app running), /health/ready (readiness — DB tagged "ready"). OpenTelemetry metrics (ASP.NET Core + HttpClient instrumentation, Prometheus exporter at /metrics). OpenTelemetry tracing (ASP.NET Core + HttpClient instrumentation). Redis health check deferred to [7.2], SMTP health check deferred to [7.1]. OTel packages: OpenTelemetry.Extensions.Hosting 1.10.0, Instrumentation.AspNetCore 1.10.1, Instrumentation.Http 1.10.0, Exporter.Prometheus.AspNetCore 1.9.0-beta.2.

- [x] [2.9] Domain events — IEventPublisher, IConsumer<T>, cache event consumers
  - Spec: specs/svc-events.md
  - Scope: Nop.Services/Events
  - Depends on: 1.4, 2.1
  - Done: 2026-04-09. Async IConsumer<T>.HandleEventAsync, EventPublisher resolves via IServiceProvider.GetServices<T>(), ILogger<EventPublisher> for error logging. Dropped ISubscriptionService indirection and plugin check (deferred to [2.10]). EventPublisherExtensions with EntityInsertedAsync/UpdatedAsync/DeletedAsync.

- [ ] [2.10] Plugin system — assembly loading, plugin discovery, IPlugin lifecycle
  - Spec: specs/nop-core-infrastructure.md
  - Scope: Plugin infrastructure
  - Depends on: 1.4

---

## Phase 3: Service Layer — Simple Services

- [x] [3.1] Configuration services — ISettingService, settings load/save
  - Spec: specs/svc-configuration.md
  - Scope: Nop.Services/Configuration
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. ISettingService (async-first), SettingService (cached dictionary, TypeDescriptor serialization, prefix-based invalidation, event publishing). SettingExtensions (key from expression). 29 Settings POCOs in Nop.Core/Domain/ (BlogSettings, CatalogSettings, ProductEditorSettings, WidgetSettings, AddressSettings, AdminAreaSettings, CommonSettings, DisplayDefaultMenuItemSettings, PdfSettings, CustomerSettings, ExternalAuthenticationSettings, RewardPointsSettings, CurrencySettings, MeasureSettings, ForumSettings, LocalizationSettings, MediaSettings, EmailAccountSettings, MessageTemplatesSettings, NewsSettings, OrderSettings, ShoppingCartSettings, PaymentSettings, SecuritySettings, SeoSettings, ShippingSettings, StoreInformationSettings, TaxSettings, VendorSettings).

- [x] [3.2] Store services — IStoreService, IStoreMappingService, IStoreContext
  - Spec: specs/svc-stores.md
  - Scope: Nop.Services/Stores
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. IStoreService/StoreService (cached CRUD, prefix invalidation, event publishing). IStoreMappingService/StoreMappingService (entity-store mapping, cached GetStoreIdsWithAccess, Authorize with CatalogSettings.IgnoreStoreLimitations bypass). WebStoreContext in Nop.Web.Framework (IHttpContextAccessor host resolution, per-request caching). StoreExtensions.ContainsHostValue replaced with private static ContainsHost method. FrameworkReference added to Web.Framework csproj.

- [x] [3.3] Directory services — ICountryService, IStateProvinceService, ICurrencyService, IMeasureService, IGeoLookupService
  - Spec: specs/svc-directory.md
  - Scope: Nop.Services/Directory
  - Depends on: 1.5, 2.1, 3.1
  - Done: 2026-04-09. CountryService (cached, store mapping filter via join query), StateProvinceService (cached by country+language+showHidden), CurrencyService (cached, store mapping post-filter, conversion via exchange rates), MeasureService (dimension+weight CRUD, ratio-based conversions), NullGeoLookupService (MaxMind deferred to [7.15]). Exchange rate provider methods (IPluginFinder) deferred to [2.10].

- [x] [3.4] SEO services — IUrlRecordService, ISitemapGenerator, slug generation
  - Spec: specs/svc-seo.md
  - Scope: Nop.Services/Seo
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. IUrlRecordService/UrlRecordService (async-first, cached slug lookup, dual-mode LoadAllUrlRecordsOnStartup). ISitemapGenerator interface only (implementation deferred — depends on Phase 4 services). SeoExtensions (FrozenDictionary 1028-entry transliteration table, GetSeName slug generator, ValidateSeNameAsync uniqueness enforcement). No service locator — all dependencies passed as parameters.

- [x] [3.5] Helpers — IDateTimeHelper, IUserAgentHelper
  - Spec: specs/svc-helpers.md
  - Scope: Nop.Services/Helpers
  - Depends on: 1.4
  - Done: 2026-04-09. DateTimeSettings (Nop.Core/Domain/Common). IDateTimeHelper/DateTimeHelper (UTC↔user timezone conversion, customer timezone via IRepository<GenericAttribute>, DefaultStoreTimeZone falls back to UTC). IUserAgentHelper/UserAgentHelper (FrozenSet<string> 25 crawler tokens, IHttpContextAccessor). FrameworkReference added to Nop.Services.csproj. Legacy BrowscapXmlHelper replaced. Property setters removed from interface (mutation belongs in controllers).

- [x] [3.6] Scheduled tasks — IScheduleTaskService, IHostedService integration
  - Spec: specs/svc-tasks.md
  - Scope: Nop.Services/Tasks
  - Depends on: 1.5
  - Done: 2026-04-09. IScheduleTaskService/ScheduleTaskService (async-first CRUD, no caching, no events — matching legacy). ITask (async ExecuteAsync). TaskSchedulerHostedService (BackgroundService: 30s startup delay, 30s poll interval, scoped DI per execution, Type.GetType resolution, timestamp recording, StopOnError handling). Dropped web farm leasing (DB leasing + Redis lock — add later if needed). Dropped catch-up thread (polling loop handles overdue tasks naturally via IsDue).

- [x] [3.7] Media services — IPictureService, IDownloadService
  - Spec: specs/svc-media.md
  - Scope: Nop.Services/Media
  - Depends on: 1.5, 3.1
  - Done: 2026-04-09. IDownloadService/DownloadService (async CRUD, event publishing). IPictureService/PictureService (file system + DB dual storage, SixLabors.ImageSharp 3.1.12 for thumbnail generation replacing ImageResizer, IWebHostEnvironment for path resolution replacing CommonHelper.MapPath, StoreInDb property migrates pictures between storage modes). GetPicturesHash dropped (SQL Server HASHBYTES specific). IsDownloadAllowed/IsLicenseDownloadAllowed deferred to [4.9] (depend on Order/Product nav properties). IODirectory alias for System.IO.Directory namespace conflict.

- [x] [3.8] Common services — IAddressService, IAddressAttributeService, IAddressAttributeParser, IAddressAttributeFormatter, IGenericAttributeService, ISearchTermService, IFulltextService, IPdfService
  - Spec: specs/svc-common.md
  - Scope: Nop.Services/Common
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. GenericAttributeService (cached by entity+keyGroup, prefix invalidation, event publishing). GenericAttributeExtensions (async, no service locator). AddressAttributeService (dual-prefix cache for attributes+values). AddressAttributeParser (XML format preserved, async attribute resolution, validation warnings). AddressAttributeFormatter (WebUtility.HtmlEncode, async). AddressService (cached by-id, configurable validation via AddressSettings, depends on ICountryService/IStateProvinceService). SearchTermService (no caching, GroupBy stats). FulltextService (SQL Server stored procedures via EF Core). IPdfService interface only (implementation depends on Phase 4 + PDF library).

- [x] [3.9] Affiliate services — IAffiliateService
  - Spec: specs/svc-affiliates.md
  - Scope: Nop.Services/Affiliates
  - Depends on: 1.5
  - Done: 2026-04-09. IAffiliateService (async-first), AffiliateService (IRepository<Address> join for firstName/lastName filtering — no nav properties, async event publishing, ArgumentNullException.ThrowIfNull, TableNoTracking for read-only joins). AffiliateExtensions: GetFullName(Address), GenerateUrl(IWebHelper), ValidateFriendlyUrlNameAsync(IAffiliateService) — no service locator.

- [x] [3.10] Vendor services — IVendorService
  - Spec: specs/svc-vendors.md
  - Scope: Nop.Services/Vendors
  - Depends on: 1.5, 3.4
  - Done: 2026-04-09. IVendorService/VendorService (async-first, no caching, soft delete for vendors, hard delete for vendor notes, event publishing). Follows AffiliateService pattern.

- [x] [3.11] Topic services — ITopicService, ITopicTemplateService
  - Spec: specs/svc-topics.md
  - Scope: Nop.Services/Topics
  - Depends on: 1.5, 2.1, 2.3, 3.4
  - Done: 2026-04-09. ITopicService/TopicService (cached GetAllTopics with inline ACL+store mapping joins, cached GetById, prefix invalidation, event publishing). ITopicTemplateService/TopicTemplateService (simple CRUD, no caching, event publishing). GetTopicBySystemNameAsync uses post-query AuthorizeAsync store filtering. Follows CountryService pattern for cached+filtered queries.

- [x] [3.12] Poll services — IPollService
  - Spec: specs/svc-polls.md
  - Scope: Nop.Services/Polls
  - Depends on: 1.5, 2.1
  - Done: 2026-04-09. IPollService/PollService (async-first, no caching, event publishing). AlreadyVotedAsync uses join between PollAnswer and PollVotingRecord with TableNoTracking. Follows VendorService pattern. No store mapping filtering (legacy didn't filter polls by store in service layer).

- [x] [3.13] Blog services — IBlogService
  - Spec: specs/svc-blogs.md
  - Scope: Nop.Services/Blogs
  - Depends on: 1.5, 2.1, 2.3
  - Done: 2026-04-09. IBlogService/BlogService (async-first, no caching, store mapping join for GetAllBlogPosts, tag-based filtering loads all then filters in-memory, event publishing). BlogExtensions.ParseTags (static method, StringSplitOptions.TrimEntries). Follows PollService pattern.

- [x] [3.14] News services — INewsService
  - Spec: specs/svc-news.md
  - Scope: Nop.Services/News
  - Depends on: 1.5, 2.1, 2.3
  - Done: 2026-04-09. INewsService/NewsService (async-first, no caching, store mapping join for GetAllNews, comment filtering with multiple optional params, event publishing). Follows BlogService pattern exactly. No tags (unlike Blog).

- [ ] [3.15] CMS/Widget services — IWidgetService
  - Spec: specs/svc-cms.md
  - Scope: Nop.Services/Cms
  - Depends on: 1.5, 2.10

- [x] [3.16] GDPR services — data export, anonymization, consent
  - Spec: specs/svc-gdpr.md
  - Scope: Nop.Services/Gdpr
  - Depends on: 1.5
  - Done: 2026-04-09. GdprRequestType enum (ConsentAgree/Disagree, ExportData, DeleteCustomer), GdprLog entity, GdprLogConfiguration (EF Core), IGdprService/GdprService (log CRUD, PermanentDeleteCustomerAsync with full anonymization: deletes forum content, blog/news comments, product reviews, activity/system logs, shopping cart, back-in-stock subscriptions, private messages, forum subscriptions, generic attributes, passwords, role mappings; anonymizes order addresses; marks customer as deleted with anonymized email). No caching (low-volume admin operations). No legacy code — greenfield feature.

---

## Phase 4: Service Layer — Complex Services

- [x] [4.1] Customer services — ICustomerService, ICustomerRegistrationService, ICustomerAttributeService, ICustomerAttributeParser, ICustomerAttributeFormatter, ICustomerReportService
  - Spec: specs/svc-customers.md
  - Scope: Nop.Services/Customers
  - Depends on: 1.5, 2.1, 2.4, 2.5, 3.1
  - Done: 2026-04-09. 17 files: ICustomerAttributeService/CustomerAttributeService (cached CRUD), ICustomerAttributeParser/CustomerAttributeParser (XML format), ICustomerAttributeFormatter/CustomerAttributeFormatter, ICustomerService/CustomerService (customer CRUD, role management via CustomerCustomerRoleMapping, password management, guest lifecycle, checkout reset, guest cleanup), ICustomerRegistrationService/CustomerRegistrationService (validate, register, change password, set email/username), ICustomerReportService/CustomerReportService (best customers, registered count), CustomerPasswordChangedEvent, ChangePasswordRequest/Result, CustomerRegistrationRequest/Result. Deferred: INewsLetterSubscriptionService (newsletter on email change), IRewardPointService (points for registration), IWorkflowMessageService (email revalidation). ICustomerActivityService already implemented in [2.2]. DeleteGuestsTask deferred to [3.6].

- [x] [4.2] Message services — IWorkflowMessageService, IMessageTemplateService, IQueuedEmailService, IEmailAccountService, IEmailSender, INewsLetterSubscriptionService, IMessageTokenProvider, ITokenizer, ICampaignService
  - Spec: specs/svc-messages.md
  - Scope: Nop.Services/Messages
  - Depends on: 1.5, 2.1, 2.3, 3.1, 3.6
  - Done: 2026-04-09. 9 interfaces + implementations: EmailAccountService (simple CRUD, event publishing), QueuedEmailService (CRUD + search with priority ordering), NewsLetterSubscriptionService (CRUD with subscribe/unsubscribe events, customer role filtering via join), MessageTemplateService (cached with store mapping, copy with localization), Tokenizer (%key% replacement with conditional statements, replaces System.Linq.Dynamic with simple evaluator), MessageTokenProvider (token builders for all entity types — Store/Customer/Vendor/Newsletter fully implemented, Order/Shipment minimal until Phase 4 services built), EmailSender (MailKit 4.3.0 replacing System.Net.Mail), WorkflowMessageService (30+ methods all following get-template/get-account/build-tokens/queue-email pattern), CampaignService (CRUD + bulk email to newsletter subscribers). Plus Token, TokenGroupNames, events, QueuedMessagesSendTask, MessageTemplateExtensions.

- [x] [4.3] Forum services — IForumService
  - Spec: specs/svc-forums.md
  - Scope: Nop.Services/Forums
  - Depends on: 1.5, 2.1, 4.1, 4.2
  - Done: 2026-04-10. IForumService/ForumService (single service matching legacy — private messages NOT separate). 5 files: IForumService.cs (interface), ForumService.cs (constructor + utilities), ForumService.Groups.cs (ForumGroup + Forum CRUD with caching), ForumService.Topics.cs (ForumTopic + ForumPost CRUD with denormalized stats + notifications), ForumService.Messaging.cs (PrivateMessage + Subscription + Permission checks + PostVote). Replaced customer.IsGuest()/IsForumModerator() with ICustomerService role checks. Removed IWorkContext dependency from MoveTopic (permission check is caller's responsibility). PM keyword search uses OR (subject OR text) instead of legacy AND.

- [x] [4.4] Catalog services — IProductService, ICategoryService, IManufacturerService, IProductAttributeService, IProductAttributeParser, IProductAttributeFormatter, IPriceCalculationService, IPriceFormatter, IProductTagService, ISpecificationAttributeService, ICopyProductService, IBackInStockSubscriptionService, IRecentlyViewedProductsService, ICompareProductsService, ICategoryTemplateService, IManufacturerTemplateService, IProductTemplateService
  - Spec: specs/svc-catalog.md
  - Scope: Nop.Services/Catalog
  - Depends on: 1.5, 2.1, 2.3, 2.9, 3.1, 3.2, 3.4, 3.7, 4.1
  - Done: 2026-04-09. 17 interfaces + 16 implementations: CategoryTemplateService, ManufacturerTemplateService, ProductTemplateService (simple CRUD), CategoryService (cached, ACL+store mapping joins, parent-child hierarchy), ManufacturerService (cached, ACL+store mapping joins), ProductTagService (ProductProductTagMapping join entity, cached product counts), SpecificationAttributeService (CRUD for spec attrs/options/product-spec mappings), ProductAttributeService (CRUD for attrs/mappings/values/predefined/combinations, cached), ProductAttributeParser (XML format preserved, gift card attributes), ProductAttributeFormatter (HTML-encoded attribute display), ProductService (SearchProducts with 20+ params, CRUD, related/cross-sell, reviews, pictures, inventory, stock history), PriceCalculationService (tier pricing, attribute adjustments, rental periods — discounts deferred to [4.5]), PriceFormatter (currency formatting), BackInStockSubscriptionService, CompareProductsService (cookie-based), RecentlyViewedProductsService (cookie-based). ICopyProductService interface only (implementation deferred — depends on many services).

- [x] [4.5] Discount services — IDiscountService
  - Spec: specs/svc-discounts.md
  - Scope: Nop.Services/Discounts
  - Depends on: 1.5, 2.1, 4.1, 4.4
  - Done: 2026-04-09. IDiscountService/DiscountService (async-first, cached GetAllDiscounts with prefix invalidation, no DiscountForCaching — cache Discount entities directly). DiscountCategoryMapping/DiscountManufacturerMapping/DiscountProductMapping join entities with EF Core configs and DbSets. DiscountExtensions (GetDiscountAmount with proper decimal math, GetPreferredDiscount with cumulative support, ContainsDiscount). Hierarchical AND/OR requirement evaluation (leaf rules pass by default until plugin system [2.10]). Usage history with Order join for customer filtering. DiscountValidationResult, DiscountRequirementValidationRequest/Result, IDiscountRequirementRule (no IPlugin dependency). Plugin-dependent methods (LoadDiscountRequirementRuleBySystemName, LoadAllDiscountRequirementRules) deferred to [2.10].

- [x] [4.6] Tax services — ITaxService, ITaxCategoryService
  - Spec: specs/svc-tax.md
  - Scope: Nop.Services/Tax
  - Depends on: 1.5, 2.1, 3.1, 3.3, 4.1
  - Done: 2026-04-09. ITaxCategoryService/TaxCategoryService (cached CRUD), ITaxService/TaxService (async-first, tuples instead of out params, address-based tax basis, EU VAT consumer detection, tax exemptions via customer role join). Added BillingAddressId/ShippingAddressId to Customer entity. CheckoutAttribute passed explicitly (no nav properties). ITaxProvider interface (no IPlugin dependency — deferred to [2.10]). CalculateTaxRequest/CalculateTaxResult DTOs. VIES VAT check deferred to [7.12]. Tax provider resolution returns 0% until [2.10] plugin system.

- [x] [4.7] Shipping services — IShippingService, IShipmentService, IDateRangeService
  - Spec: specs/svc-shipping.md
  - Scope: Nop.Services/Shipping
  - Depends on: 1.5, 2.1, 3.1, 3.3, 4.4
  - Done: 2026-04-09. IDateRangeService/DateRangeService (simple CRUD for DeliveryDate and ProductAvailabilityRange, no caching). IShipmentService/ShipmentService (CRUD for Shipment/ShipmentItem, GetAllShipmentsAsync with Order→Address joins for address filtering, vendor/warehouse filtering via ShipmentItem→OrderItem→Product joins, GetQuantityInShipmentsAsync with multi-table join). IShippingService/ShippingService (shipping method CRUD with country restriction filtering via ShippingMethodCountryMapping join entity table ShippingMethodRestrictions, warehouse CRUD with prefix-based cache invalidation, GetNearestWarehouseAsync). DTOs: GetShippingOptionResponse, GetPickupPointsResponse, ShipmentStatusEvent, ShippingRateComputationMethodType. Plugin-dependent methods (LoadActive*, GetShippingOptions, GetPickupPoints) deferred to [2.10]. Workflow methods (GetShoppingCartItemWeight, GetTotalWeight, GetDimensions, CreateShippingOptionRequests) deferred — depend on Product nav properties on ShoppingCartItem + IProductAttributeParser + ICheckoutAttributeParser.

- [x] [4.8] Payment services — IPaymentService, IPaymentMethod, payment DTOs
  - Spec: specs/svc-payments.md
  - Scope: Nop.Services/Payments
  - Depends on: 1.5, 2.1, 3.1, 4.1
  - Done: 2026-04-09. 17 files: IPaymentMethod (async-first, no IPlugin dependency — SystemName replaces PluginDescriptor), IPaymentService (async-first, restriction methods take systemName string), PaymentService (resolves IPaymentMethod via IEnumerable<IPaymentMethod> from DI), PaymentExtensions (IsPaymentMethodActive, XML SerializeCustomValues/DeserializeCustomValues preserving legacy format). 2 enums (PaymentMethodType, RecurringPaymentType). 10 DTOs (ProcessPaymentRequest/Result, CapturePaymentRequest/Result, RefundPaymentRequest/Result, VoidPaymentRequest/Result, CancelRecurringPaymentRequest/Result, PostProcessPaymentRequest). Plugin-dependent methods (LoadActivePaymentMethods, LoadAllPaymentMethods, LoadPaymentMethodBySystemName) deferred to [2.10]. CalculateAdditionalFee (percentage mode) deferred — depends on IOrderTotalCalculationService [4.9]. RoundingHelper replaced with Math.Round(value, 2). No EF Core configs needed.

- [x] [4.9] Order services — IOrderService, IOrderProcessingService, IShoppingCartService, ICheckoutAttributeService, ICheckoutAttributeParser, ICheckoutAttributeFormatter, IGiftCardService, IOrderTotalCalculationService, IReturnRequestService, IOrderReportService, IRewardPointService, ICustomNumberFormatter
  - Spec: specs/svc-orders.md
  - Scope: Nop.Services/Orders
  - Depends on: 1.5, 2.1, 2.9, 3.1, 4.1, 4.2, 4.4, 4.5, 4.6, 4.7, 4.8
  - Done: 2026-04-09. All 12 services implemented across [4.9], [4.9a], [4.9b], [4.9c].

- [x] [4.9a] IShoppingCartService — cart add/update/remove/migrate/validate
  - Spec: specs/svc-orders.md
  - Scope: Nop.Services/Orders/ShoppingCartService
  - Depends on: 4.9
  - Done: 2026-04-09. IShoppingCartService (async-first, 15 methods) + ShoppingCartService (3 partial class files: core + validation + operations). GetShoppingCartAsync replaces Customer.ShoppingCartItems nav property. All legacy extension methods inlined (ParseRequiredProductIds, ParseAllowedQuantities, GetTotalStockQuantity, IsSearchEngineAccount, RequiresShipping, LimitPerStore, GetRecurringCycleInfo). Product loaded via IProductService.GetProductByIdAsync (no nav properties). MigrateShoppingCart copies discount/gift card coupon codes via IGenericAttributeService. 20 constructor dependencies.

- [x] [4.9b] IOrderTotalCalculationService — subtotal, tax, shipping, discount, total
  - Spec: specs/svc-orders.md
  - Scope: Nop.Services/Orders/OrderTotalCalculationService
  - Depends on: 4.9, 4.9a
  - Done: 2026-04-09. IOrderTotalCalculationService (async-first, tuples instead of out params) + OrderTotalCalculationService (16 dependencies via primary constructor). ShoppingCartTotal result class and AppliedGiftCard DTO. Key methods: GetShoppingCartSubTotalAsync (with tax rates and checkout attributes), AdjustShippingRateAsync, GetShoppingCartAdditionalShippingChargeAsync, IsFreeShippingAsync, GetShoppingCartShippingTotalAsync (simplified — only uses selected shipping option from GenericAttribute, no fixed-rate fallback since that requires plugin system [2.10]), GetTaxTotalAsync, GetShoppingCartTotalAsync (with gift cards + reward points). UpdateOrderTotals deferred (depends on plugin-dependent shipping methods). CalculateRewardPointsAsync checks guest role via ICustomerService. Product loaded via IProductService (no nav properties).

- [x] [4.9c] IOrderProcessingService — PlaceOrder, status transitions, payment operations
  - Spec: specs/svc-orders.md
  - Scope: Nop.Services/Orders/OrderProcessingService
  - Depends on: 4.9, 4.9a, 4.9b
  - Done: 2026-04-09. IOrderProcessingService (async-first, 25 methods) + OrderProcessingService (4 partial class files: main, PlaceOrder, Status, Payment, Shipping). PlaceOrderAsync with full workflow (validate → payment → create order → inventory → cart clear → notifications → events). Status transitions (CheckOrderStatus, SetOrderStatus, ProcessOrderPaid, reward points, gift cards). Payment operations (authorize, capture, mark as paid, refund, partial refund, void — online and offline). Shipping (ship, deliver). Recurring payments (process next, cancel, can-cancel, can-retry). Reorder, return request validation, min order validation, payment workflow check. Also added InsertOrderItemAsync to IOrderService/OrderService, and public GetRecurringCycleInfoAsync to IShoppingCartService/ShoppingCartService. UpdateOrderTotals deferred (depends on plugin-dependent shipping methods [2.10]).

- [x] [4.10] Export/Import services — IExportManager, IImportManager
  - Spec: specs/svc-export-import.md
  - Scope: Nop.Services/ExportImport
  - Depends on: 4.4, 4.9
  - Done: 2026-04-10. ClosedXML 0.104.2 replaces EPPlus (MIT license vs commercial). Dropped PropertyManager<T>/PropertyByName<T> helper pattern — ClosedXML API is simpler, direct cell writes. Dropped dropdown list features, vendor filtering, advanced-mode property ignore (presentation-layer concerns). Dropped hash-based picture import optimization (GetPicturesHash already dropped in [3.7]). Added GetProductTagsByProductIdAsync to IProductTagService. Product import uses SKU-based upsert, category/manufacturer import uses name-based upsert. Product attribute export/import deferred — complex feature, can be added when admin controllers need it.

- [ ] [4.11] Installation services — IInstallationService, seed data
  - Spec: specs/svc-installation.md
  - Scope: Nop.Services/Installation
  - Depends on: all Phase 3 + Phase 4 services

---

## Phase 5: Presentation Layer

- [x] [5.1] Nop.Web.Framework — base controllers, tag helpers, theme engine, FluentValidation, security filters
  - Spec: specs/nop-web-framework.md
  - Scope: src/Presentation/Nop.Web.Framework
  - Depends on: 2.3, 2.6, 2.7
  - Done: 2026-04-10. 12 files: BaseNopModel/BaseNopEntityModel/BasePageableModel (Mvc/), ActionConfirmationModel/DeleteConfirmationModel (Mvc/), BaseController (notifications, Kendo grid error, AddLocales), BasePublicController, BaseAdminController ([Area("Admin")] + [Authorize(Policy="AccessAdminPanel")]), WebWorkContext (IWorkContext: customer/language/currency/tax resolution, impersonation, guest cookie), NotifyType enum (UI/), 8 action filters as IAsyncActionFilter (Filters/: CustomerLastActivity, StoreClosed, StoreIpAddress, StoreLastVisitedPage, LanguageSeoCode placeholder, PublicStoreAllowNavigation, ValidatePassword, CheckAffiliate), BaseNopValidator<T> (FluentValidation 11.3.0), DataSourceRequest/DataSourceResult (Kendoui/), NopResourceDisplayName + ILocalizedModel<T> + ILocalizedModelLocal (Localization/). Deferred: theme engine, GenericPathRoute SEO routing, localized URL routing, IPageHeadBuilder, Captcha/Honeypot, RemotePost, custom model binders, custom action results.

- [x] [5.2] Public: HomeController + Home views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/HomeController, Views/Home
  - Depends on: 5.1
  - Done: 2026-04-10. HomeController extends BasePublicController with Index() action. Views/Home/Index.cshtml with Razor comments documenting each legacy child action/widget zone and which plan item will implement it. Program.cs updated: replaced MapGet("/") + MapControllers() with MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}") enabling conventional routing for all controllers.

- [ ] [5.3] Public: CommonController + Common views (header, footer, widgets, language/currency selectors)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CommonController, Views/Common
  - Depends on: 5.1, 3.2, 3.3, 3.15

- [x] [5.4] Public: CatalogController + Catalog views (category list, manufacturer list, search, filtering)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CatalogController, Views/Catalog
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. CatalogController as 2 partial class files (main + helpers) with all 16 actions: Category (with breadcrumb, sub-categories, ACL+store mapping checks, template resolution), CategoryNavigation, TopMenu, HomepageCategories, Manufacturer (with ACL+store mapping checks, template resolution), ManufacturerAll, ManufacturerNavigation, Vendor, VendorAll, VendorNavigation, PopularProductTags, ProductsByTag, ProductTagsAll, Search (with advanced search, category/manufacturer/vendor dropdowns, search term tracking), SearchBox, SearchTermAutoComplete. 7 view models in Models/Catalog/. 7 Razor views including _ProductBox shared partial. ProductTag uses SeoExtensions.GetSeName(string) since it doesn't implement ISlugSupported. Inline model construction (no model factory). Legacy child actions (CategoryNavigation, TopMenu, HomepageCategories, ManufacturerNavigation, VendorNavigation, PopularProductTags, SearchBox) deferred to ViewComponents. Localized entity names deferred. Featured products deferred (requires separate SearchProducts call with featuredProducts:true).

- [x] [5.5] Public: ProductController + Product views (product detail, reviews)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ProductController, Views/Product
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. ProductController as 2 partial class files (main + helpers) with all non-ChildAction actions: ProductDetails (with availability check, visibility redirect, recently viewed tracking, activity log, template resolution), ProductReviews (GET with purchase-required check), ProductReviewsAdd (POST with validation), SetProductReviewHelpfulness (AJAX), RecentlyViewedProducts, NewProducts, AddProductToCompareList (AJAX), RemoveProductFromCompareList, CompareProducts, ClearCompareList. 3 view models (ProductDetailsModel, ProductReviewsModel, CompareProductsModel). 5 Razor views. Added InsertProductReviewAsync and SetProductReviewHelpfulnessAsync to IProductService/ProductService. IsAvailable as static helper in controller (no extension method — domain entities have no extensions). ChildAction methods (RelatedProducts, ProductsAlsoPurchased, CrossSellProducts, HomepageBestSellers, HomepageProducts, RecentlyViewedProductsBlock) deferred to ViewComponents. RSS, Captcha, EmailAFriend, CustomerProductReviews deferred.

- [x] [5.6] Public: CustomerController + Customer views (register, login, account, addresses, orders)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CustomerController, Views/Customer
  - Depends on: 5.1, 4.1, 2.5
  - Done: 2026-04-10. CustomerController as 4 partial class files (constructor, Login, Register, Account, Addresses). 10 view models (LoginModel, RegisterModel, RegisterResultModel, PasswordRecoveryModel, PasswordRecoveryConfirmModel, ChangePasswordModel, AccountActivationModel, EmailRevalidationModel, CustomerAvatarModel, AddressModels). 13 Razor views. CustomerAddressMapping join entity + EF config + DbSet for customer-address many-to-many (replaces legacy Customer.Addresses nav property). CustomerEvents (CustomerLoggedinEvent, CustomerLoggedOutEvent, CustomerRegisteredEvent). Deferred: DownloadableProducts/UserAgreement (depend on order items), CustomerNavigation (ViewComponent), RemoveExternalAssociation (IOpenAuthenticationService [6.17]), Info action (complex model factory — add later), Captcha/Honeypot ([7.13]).

- [x] [5.7] Public: ShoppingCartController + ShoppingCart views (cart, wishlist, mini-cart)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ShoppingCartController, Views/ShoppingCart
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. ShoppingCartController as 4 partial class files (main, Ajax, Wishlist, Helpers) with 16 actions: Cart (GET), UpdateCart (POST), ContinueShopping (POST), StartCheckout (POST), ApplyDiscountCoupon (POST), RemoveDiscountCoupon (POST), ApplyGiftCard (POST), RemoveGiftCardCode (POST), AddProductToCart_Catalog (AJAX), AddProductToCart_Details (AJAX), ProductDetails_AttributeChange (AJAX), CheckoutAttributeChange (AJAX), UploadFileProductAttribute (AJAX), UploadFileCheckoutAttribute (AJAX), Wishlist (GET), UpdateWishlist (POST), AddItemsToCartFromWishlist (POST). 4 view models (ShoppingCartModel, WishlistModel, MiniShoppingCartModel, OrderTotalsModel). 2 Razor views (Cart.cshtml, Wishlist.cshtml). Deferred: EmailWishlist (Captcha [7.13]), EstimateShipping (plugin-dependent [2.10]), OrderSummary/OrderTotals/FlyoutShoppingCart (ViewComponents for [5.26]).

- [x] [5.8] Public: CheckoutController + Checkout views (address, shipping, payment, confirm)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CheckoutController, Views/Checkout
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. CheckoutController as 2 partial class files (main + Steps) with multi-step checkout flow: Index (cart validation, checkout reset), BillingAddress (existing address selection + new address form), SelectBillingAddress (POST), NewBillingAddress (POST), ShippingAddress (existing + new), SelectShippingAddress (POST), NewShippingAddress (POST), ShippingMethod (auto-skips when no plugins), SelectShippingMethod (POST), PaymentMethod (auto-skips when no plugins), SelectPaymentMethod (POST with reward points), PaymentInfo (skips to confirm when no plugins), EnterPaymentInfo (POST), Confirm (GET), ConfirmOrder (POST with PlaceOrderAsync + PostProcessPaymentAsync), Completed. 7 view models in CheckoutModels.cs. 6 Razor views. Address deduplication via FindOrCreateAddressAsync using CustomerAddressMapping. Deferred: OPC (depends on RenderPartialViewToString), CheckoutProgress (ViewComponent for [5.26]), pickup points (plugin-dependent [2.10]).

- [x] [5.9] Public: OrderController + Order views (order history, order details)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/OrderController, Views/Order
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. OrderController as 2 partial class files (main + helpers) with 10 actions: CustomerOrders (order list + recurring payments), CancelRecurringPayment (POST), RetryLastRecurringPayment (POST), CustomerRewardPoints (paged history), Details (full order detail with items/totals/shipping/notes), PrintOrderDetails (print mode), GetPdfInvoice (PDF download via IPdfService), ReOrder (re-add items to cart), RePostPayment (POST payment retry), ShipmentDetails (shipment items). 4 view models (CustomerOrderListModel, OrderDetailsModel, ShipmentDetailsModel, CustomerRewardPointsModel) in OrderModels.cs. 4 Razor views. FormValueRequired eliminated — separate action endpoints. IPdfService injected as nullable (no implementation yet). RecurringPayment.NextPaymentDate computed from history (no nav property). Gift cards queried via IGiftCardService.GetAllGiftCardsAsync(usedWithOrderId). Deferred: address display on order details (requires address model infrastructure), localized order status names, downloadable product links.

- [x] [5.10] Public: BlogController + Blog views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BlogController, Views/Blog
  - Depends on: 5.1, 3.13
  - Done: 2026-04-10. BlogController with List, BlogByTag, BlogByMonth, BlogPost (detail), BlogCommentAdd (POST). 8 view models (BlogPostModel, BlogPostListModel, BlogPagingFilteringModel, BlogCommentModel, AddBlogCommentModel, BlogPostTagModel, BlogPostTagListModel, BlogPostYearModel/BlogPostMonthModel). 2 Razor views (List.cshtml, BlogPost.cshtml). Added InsertBlogCommentAsync to IBlogService/BlogService. Inline model construction (no model factory). Legacy child actions (BlogTags, BlogMonths, RssHeaderLink) deferred to ViewComponents. ListRss deferred (needs RssActionResult). Captcha deferred to [7.13].

- [x] [5.11] Public: NewsController + News views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/NewsController, Views/News
  - Depends on: 5.1, 3.14
  - Done: 2026-04-10. NewsController with List, NewsItem (detail), NewsCommentAdd (POST). 5 view models (NewsItemModel, NewsItemListModel, NewsCommentModel, AddNewsCommentModel, NewsPagingFilteringModel). 2 Razor views (List.cshtml, NewsItem.cshtml). Added InsertNewsCommentAsync to INewsService/NewsService. News differs from Blog: has CommentTitle field in form and comment display, no tags, no month/tag filtering, simpler paging model. Inline model construction (no model factory). Legacy child actions (HomePageNews, RssHeaderLink) deferred to ViewComponents. ListRss deferred (needs RssActionResult). Captcha deferred to [7.13].

- [x] [5.12] Public: BoardsController + Boards views (forums)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BoardsController, Views/Boards
  - Depends on: 5.1, 4.3
  - Done: 2026-04-10. BoardsController as 3 partial class files (main, Topics, Posts) with 17 actions: Index, ActiveDiscussions, ForumGroup, Forum, ForumWatch (AJAX), TopicWatch (AJAX), PostVote (AJAX), Topic, TopicCreate (GET+POST), TopicEdit (GET+POST), TopicDelete (AJAX), TopicMove (GET+POST), PostCreate (GET+POST), PostEdit (GET+POST), PostDelete (AJAX), Search, CustomerForumSubscriptions (GET+POST). 10 view models in BoardsModels.cs. 12 Razor views. Deferred: RSS actions (ActiveDiscussionsRss, ForumRss — need RssActionResult), ChildAction methods (LastPost, ForumBreadcrumb, ActiveDiscussionsSmall → ViewComponents). CustomerName/Avatar not populated in post models (requires ICustomerService per-post lookup). FormatPostText uses simple HtmlEncode + newline→br (legacy ForumExtensions.FormatPostText used BBCode/service locator). CustomerForumSubscriptionsDelete uses IEnumerable<int> subscriptionIds instead of legacy FormCollection parsing.

- [x] [5.13] Public: TopicController + Topic views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/TopicController, Views/Topic
  - Depends on: 5.1, 3.11
  - Done: 2026-04-10. TopicController with 4 actions: TopicDetails (by ID), TopicDetailsPopup (by systemName), TopicBlock (partial by systemName), Authenticate (POST password check). TopicModel view model. TopicDetails.cshtml (full page with password protection via fetch API), TopicBlock.cshtml (partial with Model.Id-suffixed element IDs). Inline model construction (no model factory). Replaced jQuery AJAX with vanilla JS fetch. Localized title/body deferred (requires ILocalizedEntityService parameter passing). TopicTemplate view path resolution via ITopicTemplateService.

- [x] [5.14] Public: PollController + Poll views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/PollController, Views/Poll
  - Depends on: 5.1, 3.12
  - Done: 2026-04-10. PollController with 3 actions: PollBlock (by systemKeyword), Vote (POST AJAX with anti-forgery), HomePagePolls (homepage polls list). Added GetPollAnswersByPollIdAsync, UpdatePollAnswerAsync, InsertPollVotingRecordAsync to IPollService/PollService (replaces nav property access). PollModel/PollAnswerModel view models. 3 Razor views (_Poll.cshtml, PollBlock.cshtml, HomePagePolls.cshtml). Vote returns JSON data (client-side DOM update) instead of rendered HTML (legacy RenderPartialViewToString). Vanilla JS fetch replaces jQuery AJAX. No model factory — inline model construction. No caching (legacy cached via ModelCacheEventConsumer — deferred). PollBlock and HomePagePolls are regular actions (ViewComponent conversion deferred to [5.26]).

- [x] [5.15] Public: NewsletterController + Newsletter views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/NewsletterController, Views/Newsletter
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. NewsletterController with 3 actions: NewsletterBox (sync, returns PartialView with AllowToUnsubscribe from CustomerSettings), SubscribeNewsletter (async POST AJAX, returns JSON with Success/Result), SubscriptionActivation (async GET with Guid token, activates or deletes subscription). 2 view models (NewsletterBoxModel, SubscriptionActivationModel). 2 Razor views (NewsletterBox.cshtml with vanilla JS fetch replacing jQuery AJAX, SubscriptionActivation.cshtml). No model factory — inline construction. No anti-forgery on SubscribeNewsletter (matching legacy). NewsletterBox is a regular action (ViewComponent conversion deferred to [5.26]).

- [x] [5.16] Public: ReturnRequestController + ReturnRequest views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ReturnRequestController, Views/ReturnRequest
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. ReturnRequestController with 4 actions: CustomerReturnRequests (list with product/download lookup), ReturnRequest (GET form with returnable items, reasons, actions), ReturnRequestSubmit (POST with per-item quantity parsing from form, return request creation, custom number generation, notifications), UploadFileReturnRequest (AJAX file upload with size validation). 2 view models (CustomerReturnRequestsModel, SubmitReturnRequestModel with nested OrderItemModel/ReturnRequestReasonModel/ReturnRequestActionModel). 2 Razor views. Vanilla JS fetch for file upload (replaces legacy jQuery fineUploader). FormValueRequired eliminated — separate ReturnRequestSubmit endpoint. Inline model construction (no model factory). Legacy `customer.ReturnRequests.Add()` nav property replaced with `IReturnRequestService.InsertReturnRequestAsync`. Localized reason/action names deferred (uses entity Name directly).

- [x] [5.17] Public: PrivateMessagesController + PrivateMessages views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/PrivateMessagesController, Views/PrivateMessages
  - Depends on: 5.1, 4.3
  - Done: 2026-04-10. PrivateMessagesController with 8 actions: Index (with inlined Inbox/SentItems tabs — legacy child actions eliminated), DeleteInboxPM (POST), MarkUnread (POST), DeleteSentPM (POST), SendPM (GET+POST), ViewPM, DeletePM. 3 view models (PrivateMessageIndexModel with embedded PrivateMessageListModel for both tabs, PrivateMessageModel, SendPrivateMessageModel). 3 Razor views. FormValueRequired eliminated — separate action endpoints (same as BoardsController [5.12], ShoppingCartController [5.7]). FormCollection parsing replaced with IEnumerable<int> model binding (same as BoardsController.CustomerForumSubscriptions). Inline model construction (no model factory). CustomerName not populated in message lists (requires per-message ICustomerService lookup). Vanilla JS tab switching replaces jQuery UI tabs.

- [x] [5.18] Public: ProfileController + Profile views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ProfileController, Views/Profile
  - Depends on: 5.1, 4.1
  - Done: 2026-04-10. ProfileController with Index action (inline Info/Posts model preparation — legacy child actions eliminated). 4 view models (ProfileIndexModel with embedded ProfileInfoModel + ProfilePostsModel, PostModel) in ProfileModels.cs. 3 Razor views (Index.cshtml with vanilla JS tab switching replacing jQuery UI tabs, _Info.cshtml partial with avatar/stats/PM link, _Posts.cshtml partial with post list and inline pager). Guest check via ICustomerService role lookup (no nav properties). FormatUserName replaced with customer.Email. FormatPostText replaced with WebUtility.HtmlEncode + newline→br. RelativeFormat replaced with simple FormatRelativeDate helper. IPermissionService removed (legacy used it for admin edit link via DisplayEditLink — not available in new codebase). Widget zones dropped (depend on [3.15]). Localized strings replaced with plain text. No model factory — inline construction.

- [x] [5.19] Public: VendorController + Vendor views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/VendorController, Views/Vendor
  - Depends on: 5.1, 3.10
  - Done: 2026-04-10. VendorController with 5 actions: ApplyVendor (GET), ApplyVendorSubmit (POST), Info (GET), InfoSave (POST), RemovePicture (POST). 2 view models (ApplyVendorModel, VendorInfoModel). 2 Razor views. FormValueRequired eliminated — separate endpoints (InfoSave instead of Info POST, ApplyVendorSubmit instead of ApplyVendor POST). IFormFile replaces HttpPostedFileBase. Inline model construction (no model factory). Captcha deferred to [7.13].

- [ ] [5.20] Public: ExternalAuthenticationController + ExternalAuthentication views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ExternalAuthenticationController, Views/ExternalAuthentication
  - Depends on: 5.1, 2.5

- [x] [5.21] Public: DownloadController
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/DownloadController
  - Depends on: 5.1, 3.7
  - Done: 2026-04-10. DownloadController with 5 actions: Sample (product sample download), GetDownload (purchased product download with IsDownloadAllowed check, user agreement redirect, max download limit, download count increment), GetLicense (license file download with IsLicenseDownloadAllowed check), GetFileUpload (GUID-based file upload retrieval), GetOrderNoteFile (order note attachment with customer ownership check). Added UpdateOrderItemAsync to IOrderService/OrderService for download count increment. IsDownloadAllowed/IsLicenseDownloadAllowed implemented as private static method in controller (deferred from [3.7] service layer — logic checks order status, payment status, activation type, expiration). No views needed (file download endpoints only). No nav properties — Order/Product loaded via service calls from OrderItem FK IDs.

- [x] [5.22] Public: CountryController (AJAX)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CountryController
  - Depends on: 5.1, 3.3
  - Done: 2026-04-10. Single GetStatesByCountryId action. No model factory — inline construction. No PublicStoreAllowNavigationFilter (always accessible for AJAX). Returns JSON array of {id, name} with placeholder items matching legacy logic.

- [ ] [5.23] Public: WidgetController + Widget views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/WidgetController, Views/Widget
  - Depends on: 5.1, 3.15

- [x] [5.24] Public: BackInStockSubscriptionController + views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BackInStockSubscriptionController, Views/BackInStockSubscription
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. BackInStockSubscriptionController with 4 actions: SubscribePopup (GET popup with subscription status), SubscribePopupPOST (toggle subscribe/unsubscribe via AJAX), CustomerSubscriptions (GET paged list), DeleteSelected (POST batch delete). 2 view models (BackInStockSubscribeModel, CustomerBackInStockSubscriptionsModel with nested BackInStockSubscriptionModel). 2 Razor views. FormCollection parsing replaced with IEnumerable<int> model binding (same as BoardsController [5.12], PrivateMessagesController [5.17]). FormValueRequired eliminated — separate POST endpoints. jQuery AJAX replaced with vanilla JS fetch. Inline model construction (no model factory). Product.StockQuantity used directly (simplified from legacy GetTotalStockQuantity which aggregated multi-warehouse stock).

- [ ] [5.25] Public: InstallController + Install views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/InstallController, Views/Install
  - Depends on: 4.11

- [x] [5.26] Public: Shared views (layout, partial views, _ViewImports)
  - Spec: specs/nop-web-public.md
  - Scope: Views/Shared
  - Depends on: 5.1
  - Done: 2026-04-10. Created 6 shared view files: _ViewImports.cshtml (public: tag helpers + Nop.Web/Nop.Web.Models/Nop.Web.Framework.Mvc namespaces), _ViewStart.cshtml (public: default _Layout), _Layout.cshtml (minimal HTML5 shell with header/body/footer, Breadcrumb + Scripts sections), admin _ViewImports.cshtml (tag helpers + Nop.Web.Framework.Mvc), admin _ViewStart.cshtml (default _AdminLayout), _AdminLayout.cshtml (minimal admin shell with Scripts section). Updated 56 existing views: removed Layout = null, stripped DOCTYPE/html/head/body wrappers from 19 views, removed Layout = null line from 37 multi-line blocks, cleaned 13 empty @{ } blocks. Added ViewData["Title"] to Home/Index.cshtml. Removed Layout = null from admin Home/Index.cshtml. Error.cshtml and PageNotFound.cshtml kept standalone (Layout = null preserved). Deferred: IPageHeadBuilder (meta tags), theme engine, _Pager shared partial (inline paging already in views), ViewComponents for sidebar/footer content.

- [ ] [5.27] Public: KeepAliveController → health check endpoint
  - Spec: specs/xcut-observability.md
  - Scope: Controllers/KeepAliveController → /health endpoint
  - Depends on: 2.8

- [ ] [5.28] Public: Legacy URL redirect middleware (BackwardCompatibility1X + 2X)
  - Spec: specs/nop-web-public.md
  - Scope: Middleware replacing BackwardCompatibility1XController + BackwardCompatibility2XController
  - Depends on: 5.1, 3.4

---

## Phase 5B: Admin Area

- [x] [5.30] Admin: HomeController + Home views (dashboard)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/HomeController, Admin/Views/Home
  - Depends on: 5.1
  - Done: 2026-04-10. Admin HomeController with Index action (DashboardModel with IsLoggedInAsVendor). CommonStatisticsViewComponent replaces legacy child action (order/customer/return-request/low-stock counts). NopCommerceNews intentionally not migrated (marketplace-specific). Area route registered in Program.cs ({area:exists}/{controller=Home}/{action=Index}/{id?}) before default route.

- [ ] [5.31] Admin: ProductController + Product views (CRUD, attributes, pictures, inventory)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ProductController (4857 LOC), Admin/Views/Product
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. ProductController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with category/manufacturer/vendor/type/published filters), ProductList (AJAX grid returning ProductGridModel), Create (GET+POST), Edit (GET+POST), Delete (POST), DeleteSelected (POST batch), GoToSku (SKU-based redirect), ExportExcelAll (filtered export), ImportExcel (file upload). 3 view models: ProductListModel (search filters + dropdowns), ProductModel (full product fields + dropdowns), ProductGridModel (grid display). 3 Razor views: List.cshtml (search form + vanilla JS AJAX grid), Create.cshtml (product form), Edit.cshtml (product form + delete). Inline model construction (no model factory). Vendor access restriction on all actions. Sub-entity management deferred: pictures, product attributes, spec attributes, tier prices, related/cross-sell products, purchased-with-orders, bulk editing, stock quantity history, product tags, attribute combinations, editor settings, copy product, low stock reports.

- [x] [5.32] Admin: CategoryController + Category views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CategoryController, Admin/Views/Category
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. CategoryController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with name/store filters), CategoryList (AJAX grid returning CategoryGridModel with breadcrumb), Create (GET+POST), Edit (GET+POST), Delete (POST), DeleteSelected (POST batch), ExportExcel, ImportExcel. 3 view models: CategoryListModel (search filters + store dropdown), CategoryModel (all category fields + parent category dropdown + template dropdown), CategoryGridModel (grid display with breadcrumb). 3 Razor views: List.cshtml (search form + vanilla JS AJAX grid), Create.cshtml (category form), Edit.cshtml (category form + delete). Inline model construction (no model factory). Breadcrumb computed via static helper walking parent chain. Sub-entity management deferred: products (ProductList/Update/Delete/AddPopup), discounts, ACL (customer roles), store mapping, picture.

- [x] [5.33] Admin: ManufacturerController + Manufacturer views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ManufacturerController, Admin/Views/Manufacturer
  - Depends on: 5.1, 4.4
  - Done: 2026-04-10. ManufacturerController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with name/store filters), ManufacturerList (AJAX grid returning ManufacturerGridModel), Create (GET+POST), Edit (GET+POST), Delete (POST), DeleteSelected (POST batch), ExportExcel, ImportExcel. 3 view models: ManufacturerListModel (search filters + store dropdown), ManufacturerModel (all manufacturer fields + template dropdown), ManufacturerGridModel (grid display). 3 Razor views: List.cshtml (search form + vanilla JS AJAX grid), Create.cshtml (manufacturer form), Edit.cshtml (manufacturer form + delete). Simpler than Category: no ParentCategoryId (no parent dropdown, no breadcrumb), no ShowOnHomePage, no IncludeInTopMenu. Has PriceRanges field. ExportManufacturersToXlsx is sync (not async). Permission is ManageManufacturers. Sub-entity management deferred: products, discounts, ACL, store mapping, picture.

- [x] [5.34] Admin: OrderController + Order views (list, detail, status, refunds, shipments)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/OrderController (4379 LOC), Admin/Views/Order
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. OrderController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with date/status/store/vendor/country/email filters), OrderList (AJAX grid returning OrderGridModel with customer email lookup), Edit (full order detail with billing/shipping addresses, totals, items, payment operations, order notes, shipments grid), Delete, GoToOrderNumber (custom order number or ID lookup), ExportExcelAll (filtered export). Payment operations: CancelOrder, CaptureOrder, MarkOrderAsPaid, RefundOrder, RefundOrderOffline, VoidOrder, VoidOrderOffline, ChangeOrderStatus. Order notes: OrderNotesSelect (AJAX grid), OrderNoteAdd, OrderNoteDelete. Shipments: ShipmentsByOrder (AJAX grid). 7 view models: OrderListModel (search filters + dropdowns), OrderGridModel (grid display), OrderModel (full order detail + items + notes + payment flags), OrderItemModel, OrderNoteModel, ShipmentGridModel. 2 Razor views: List.cshtml (search form + vanilla JS AJAX grid), Edit.cshtml (order detail + payment operations + notes + shipments). Added GetShipmentsByOrderIdAsync to IShipmentService/ShipmentService (replaces order.Shipments nav property). Inline model construction (no model factory). Sub-entity management deferred: edit order items, add products to order, address editing, shipment creation/management, PDF invoices, partial refund popup, credit card info editing, order totals editing, reports (bestsellers, never sold, country, statistics).

- [x] [5.35] Admin: CustomerController + Customer views (list, detail, roles, addresses)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerController (2396 LOC), Admin/Views/Customer
  - Depends on: 5.1, 4.1
  - Done: 2026-04-10. CustomerController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with email/username/name/DOB/company/phone/zip/IP/role filters), CustomerList (AJAX grid returning CustomerGridModel with role names and GenericAttribute lookups), Create (GET+POST), Edit (GET+POST), Delete (POST), DeleteSelected (POST batch), ExportExcelAll (filtered export). 3 view models: CustomerListModel (search filters + role checkboxes), CustomerModel (full customer fields + settings-driven visibility + dropdowns), CustomerGridModel (grid display with role names). 3 Razor views: List.cshtml (search form + vanilla JS AJAX grid), Create.cshtml (customer form with settings-driven field visibility), Edit.cshtml (customer form + read-only info + delete). 14 constructor dependencies via primary constructor (reduced from legacy 44 — IWorkContext removed as unused). GenericAttributes for form fields (FirstName, LastName, Gender, DOB, Company, Address, Phone, Fax). Customer roles managed via CustomerCustomerRoleMapping join entity. ValidateCustomerRoles ensures mutual exclusivity of Guests/Registered. SecondAdminAccountExistsAsync prevents last admin deactivation/deletion. Inline model construction (no model factory). Sub-entity management deferred: orders, addresses, shopping cart, activity log, back-in-stock subscriptions, reward points, newsletter subscriptions, send email/PM.

- [x] [5.36] Admin: CustomerRoleController + CustomerRole views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerRoleController, Admin/Views/CustomerRole
  - Depends on: 5.1, 4.1
  - Done: 2026-04-10. CustomerRoleController with core CRUD actions: List (AJAX grid), Create (GET+POST), Edit (GET+POST with system role protection), Delete (POST with system role guard). CustomerRoleModel view model. 3 Razor views. System role validation: can't deactivate system roles, can't change system name of system roles, can't set PurchasedWithProductId on Registered role. AssociateProductToCustomerRolePopup deferred (complex popup with product search — enter product ID manually for now). Follows ManufacturerController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [ ] [5.37] Admin: CustomerAttributeController + CustomerAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerAttributeController, Admin/Views/CustomerAttribute
  - Depends on: 5.1, 4.1

- [x] [5.38] Admin: SettingController + Setting views (all configuration sections)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/SettingController (2339 LOC), Admin/Views/Setting
  - Depends on: 5.1, 3.1
  - Done: 2026-04-10. SettingController as 5 partial class files (main + BlogVendorForumNews + Catalog + OrderCartMedia + CustomerAllSettings). 11 settings sections implemented: Blog, Vendor, Forum, News, Catalog, Order, ShoppingCart, Media, CustomerUser (CustomerSettings + AddressSettings + DateTimeSettings + ExternalAuthenticationSettings), RewardPoints, AllSettings (raw CRUD grid with search/add/update/delete). Store-scoped overrides via SaveSettingOverridablePerStoreAsync for all per-store settings. GetActiveStoreScopeAsync reads AdminAreaStoreScopeConfiguration GenericAttribute. ChangeStoreScopeConfiguration action for store scope switching. 20 view models in SettingModels.cs. Deferred: GeneralCommon (complex: SEO, security, PDF, localization, fulltext, encryption key), Shipping (depends on plugin system [2.10]), Tax (complex: tax categories, EU VAT), ReturnRequestReason/Action CRUD (sub-entity management), ChangePictureStorage (storage mode migration), Mode/StoreScopeConfiguration child actions (ViewComponents).

- [ ] [5.39] Admin: PluginController + Plugin views (install, uninstall, configure)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PluginController, Admin/Views/Plugin
  - Depends on: 5.1, 2.10

- [x] [5.40] Admin: DiscountController + Discount views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/DiscountController, Admin/Views/Discount
  - Depends on: 5.1, 4.5
  - Done: 2026-04-10. DiscountController as 2 partial class files (main + helpers) with core CRUD actions: List (search form with name/coupon/type filters), DiscountList (AJAX grid returning DiscountGridModel), Create (GET+POST), Edit (GET+POST with discount type change cleanup), Delete (POST). Applied-to entity management: ProductList/ProductDelete/ProductAdd, CategoryList/CategoryDelete/CategoryAdd, ManufacturerList/ManufacturerDelete/ManufacturerAdd (all AJAX, using join entity mapping CRUD methods). Usage history: UsageHistoryList/UsageHistoryDelete (AJAX). 7 view models in DiscountModels.cs. 3 Razor views (List, Create, Edit with inline AJAX grids). Added 9 entity mapping CRUD methods to IDiscountService/DiscountService (Insert/Delete/Get for DiscountCategoryMapping, DiscountManufacturerMapping, DiscountProductMapping). Discount requirements management deferred (plugin-dependent [2.10]). Follows ManufacturerController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [x] [5.41] Admin: ShippingController + Shipping views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ShippingController, Admin/Views/Shipping
  - Depends on: 5.1, 4.7
  - Done: 2026-04-10. ShippingController as 2 partial class files (main + helpers) with 4 regions: shipping methods CRUD (Methods, MethodList AJAX grid, CreateMethod, EditMethod, DeleteMethod), dates/ranges CRUD (DatesAndRanges, DeliveryDateList/Create/Edit/Delete, ProductAvailabilityRangeList/Create/Edit/Delete), warehouses CRUD with address (Warehouses, WarehouseList AJAX grid, CreateWarehouse, EditWarehouse, DeleteWarehouse), restrictions matrix (Restrictions GET + RestrictionSave POST). 7 view models in ShippingModels.cs. 12 Razor views. Added 3 country restriction methods to IShippingService/ShippingService (GetAllShippingMethodCountryMappingsAsync, InsertShippingMethodCountryMappingAsync, DeleteShippingMethodCountryMappingAsync). Plugin-dependent sections (Providers, PickupPointProviders) deferred to [2.10]. Localization deferred (consistent with all other admin controllers). Follows ManufacturerController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [ ] [5.42] Admin: PaymentController + Payment views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PaymentController, Admin/Views/Payment
  - Depends on: 5.1, 4.8

- [ ] [5.43] Admin: TaxController + Tax views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/TaxController, Admin/Views/Tax
  - Depends on: 5.1, 4.6

- [x] [5.44] Admin: BlogController + Blog views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/BlogController, Admin/Views/Blog
  - Depends on: 5.1, 3.13
  - Done: 2026-04-10. BlogController as 2 partial class files (main + helpers) with blog post CRUD (List, BlogPostList AJAX grid, Create, Edit, Delete) and comment management (Comments, CommentList AJAX grid, CommentUpdate, CommentDelete, DeleteSelectedComments, ApproveSelected, DisapproveSelected). 6 view models (BlogPostListModel, BlogPostModel, BlogPostGridModel, BlogCommentListModel, BlogCommentModel). 4 Razor views (List, Create, Edit, Comments). Added UpdateBlogCommentAsync to IBlogService/BlogService (replaces legacy nav property pattern). BlogPost implements ISlugSupported — uses ValidateSeNameAsync + SaveSlugAsync with LanguageId. Follows ManufacturerController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Store mapping management deferred (consistent with other admin controllers).

- [x] [5.45] Admin: NewsController + News views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/NewsController, Admin/Views/News
  - Depends on: 5.1, 3.14
  - Done: 2026-04-10. NewsController as 2 partial class files (main + helpers) with news item CRUD (List, NewsItemList AJAX grid, Create, Edit, Delete) and comment management (Comments, CommentList AJAX grid, CommentUpdate, CommentDelete, DeleteSelectedComments, ApproveSelected, DisapproveSelected). 5 view models (NewsItemListModel, NewsItemModel, NewsItemGridModel, NewsCommentListModel, NewsCommentModel). 4 Razor views (List, Create, Edit, Comments). Added UpdateNewsCommentAsync to INewsService/NewsService (replaces legacy nav property pattern). NewsItem implements ISlugSupported — uses ValidateSeNameAsync + SaveSlugAsync with LanguageId. Key differences from BlogController: NewsItem has Published field, Short/Full instead of BodyOverview/Body, no Tags field; NewsComment has CommentTitle column in grid. Follows BlogController [5.44] pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Store mapping management deferred (consistent with other admin controllers).

- [x] [5.46] Admin: ForumController + Forum views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ForumController, Admin/Views/Forum
  - Depends on: 5.1, 4.3
  - Done: 2026-04-10. ForumController with forum group CRUD (List, ForumGroupList AJAX grid, CreateForumGroup, EditForumGroup, DeleteForumGroup) and forum CRUD (ForumList AJAX grid by group, CreateForum, EditForum, DeleteForum). 2 view models (ForumGroupModel, ForumModel with AvailableForumGroups dropdown). 5 Razor views (List with nested group→forum display, CreateForumGroup, EditForumGroup, CreateForum, EditForum). PrepareForumGroupDropdownAsync helper for forum group selection. Follows ManufacturerController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. No ILocalizationService dependency (consistent with simplified admin controllers). Localization deferred. Topic/post management not in scope (admin manages structure only — topics/posts managed via public BoardsController [5.12]).

- [x] [5.47] Admin: PollController + Poll views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PollController, Admin/Views/Poll
  - Depends on: 5.1, 3.12
  - Done: 2026-04-10. PollController with poll CRUD (List, PollList AJAX grid, Create, Edit, Delete) and answer management (PollAnswerList, PollAnswerAdd, PollAnswerUpdate, PollAnswerDelete). 3 view models (PollModel, PollGridModel, PollAnswerModel). 3 Razor views (List, Create, Edit with inline answer grid). Added InsertPollAnswerAsync to IPollService/PollService (replaces legacy nav property pattern poll.PollAnswers.Add()). Language dropdown via ILanguageService. Follows ForumController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. No ILocalizationService dependency (consistent with simplified admin controllers). Localization deferred.

- [x] [5.48] Admin: TopicController + Topic views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/TopicController, Admin/Views/Topic
  - Depends on: 5.1, 3.11
  - Done: 2026-04-10. TopicController with topic CRUD (List, TopicList AJAX grid, Create, Edit, Delete). 3 view models (TopicListModel, TopicModel, TopicGridModel). 3 Razor views (List with store filter + AJAX grid, Create, Edit with delete). Topic implements ISlugSupported — uses ValidateSeNameAsync + SaveSlugAsync with languageId=0. TopicTemplate dropdown via ITopicTemplateService. Follows PollController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. ACL/store mapping management deferred (consistent with other admin controllers). Localization deferred.

- [x] [5.49] Admin: LanguageController + Language views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/LanguageController, Admin/Views/Language
  - Depends on: 5.1, 2.3
  - Done: 2026-04-10. LanguageController with language CRUD (List, LanguageList AJAX grid, Create, Edit, Delete) and string resource management (Resources AJAX grid with name/value search + paging, ResourceUpdate, ResourceAdd, ResourceDelete) and XML export/import (ExportXml, ImportXml). 3 view models (LanguageModel with currency dropdown, LanguageGridModel, LanguageResourceModel). 3 Razor views (List, Create, Edit with resource grid + export/import). Currency dropdown via ICurrencyService. Last-published-language guard on Edit and Delete. GetAvailableFlagFileNames dropped (legacy scanned server filesystem for PNG files — user enters filename directly). Store mapping management deferred (consistent with other admin controllers). Follows PollController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [x] [5.50] Admin: CurrencyController + Currency views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CurrencyController, Admin/Views/Currency
  - Depends on: 5.1, 3.3
  - Done: 2026-04-10. CurrencyController with core CRUD (List, CurrencyList AJAX grid, Create, Edit, Delete) and currency management (ApplyRate, MarkAsPrimaryExchangeRateCurrency, MarkAsPrimaryStoreCurrency). 2 view models (CurrencyModel, CurrencyGridModel). 3 Razor views (List with mark-as-primary AJAX, Create, Edit with delete). ISettingService.SaveSettingAsync for primary currency settings. Live rates and exchange rate provider selection deferred (plugin-dependent [2.10]). Localization and store mapping deferred (consistent with all other admin controllers). Follows PollController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [x] [5.51] Admin: CountryController + Country views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CountryController, Admin/Views/Country
  - Depends on: 5.1, 3.3
  - Done: 2026-04-10. CountryController with country CRUD (List, CountryList AJAX grid, Create, Edit, Delete, DeleteSelected) and state province management (StateProvinceList, StateProvinceAdd, StateProvinceUpdate, StateProvinceDelete) and export/import (ExportCsv, ImportCsv). 3 view models (CountryListModel, CountryModel with search, CountryGridModel, StateProvinceModel). 3 Razor views (List with search + AJAX grid + batch delete + export/import CSV, Create, Edit with inline state province AJAX grid). Address-in-use guard on country and state delete. Legacy popup pattern for state create/edit replaced with inline AJAX grid (matching PollController answer management pattern). Follows PollController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Store mapping and localization deferred (consistent with all other admin controllers).

- [x] [5.52] Admin: MeasureController + Measure views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/MeasureController, Admin/Views/Measure
  - Depends on: 5.1, 3.3
  - Done: 2026-04-10. MeasureController with dimension CRUD (List, DimensionList AJAX grid, CreateDimension, EditDimension, DeleteDimension, MarkAsPrimaryDimension) and weight CRUD (WeightList AJAX grid, CreateWeight, EditWeight, DeleteWeight, MarkAsPrimaryWeight). 2 view models (MeasureDimensionModel, MeasureWeightModel). 1 Razor view (List.cshtml with two inline AJAX grids for dimensions and weights, each with add/edit/delete/mark-as-primary). ISettingService.SaveSettingAsync for primary dimension/weight settings. Follows CurrencyController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. No separate Create/Edit views — all CRUD in inline grid. Localization deferred (consistent with all other admin controllers).

- [x] [5.53] Admin: EmailAccountController + EmailAccount views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/EmailAccountController, Admin/Views/EmailAccount
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. EmailAccountController with core CRUD (List, EmailAccountList AJAX grid, Create, Edit, Delete), MarkAsDefaultEmail (saves EmailAccountSettings.DefaultEmailAccountId via ISettingService), ChangePassword (separate POST endpoint — legacy used FormValueRequired), SendTestEmail (IEmailSender.SendEmailAsync with store name subject). 1 view model (EmailAccountModel). 3 Razor views (List with mark-as-default AJAX, Create, Edit with change password + send test email + delete). Follows CurrencyController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Localization deferred (consistent with all other admin controllers).

- [x] [5.54] Admin: MessageTemplateController + MessageTemplate views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/MessageTemplateController, Admin/Views/MessageTemplate
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. MessageTemplateController with core CRUD (List with store filter, MessageTemplateList AJAX grid, Edit GET+POST, Delete), CopyTemplate (separate POST endpoint), TestTemplate (GET with token inputs), SendTestTemplate (POST with form-based token parsing). 4 view models (MessageTemplateListModel, MessageTemplateModel, MessageTemplateGridModel, TestMessageTemplateModel). 3 Razor views (List, Edit, TestTemplate). FormValueRequired eliminated — separate endpoints for CopyTemplate and SendTestTemplate. Allowed tokens displayed from IMessageTokenProvider.GetListOfAllowedTokens. Email account dropdown via IEmailAccountService. Delay settings (SendImmediately toggle, DelayBeforeSend, DelayPeriod). Attached download ID field. Follows EmailAccountController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Localization deferred. Store mapping management deferred (consistent with all other admin controllers).

- [x] [5.55] Admin: QueuedEmailController + QueuedEmail views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/QueuedEmailController, Admin/Views/QueuedEmail
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. QueuedEmailController with core CRUD (List with search form, QueuedEmailList AJAX grid, Edit GET+POST, Delete), batch operations (DeleteSelected, DeleteAll), navigation (GoToEmailByNumber), and Requeue (creates new queued email copy). 3 view models (QueuedEmailListModel, QueuedEmailModel, QueuedEmailGridModel). 2 Razor views (List with search filters + AJAX grid + select-all/delete-selected/delete-all + go-to-number, Edit with email detail form + requeue/delete). FormValueRequired eliminated — separate endpoints for GoToEmailByNumber, Requeue, DeleteAll. Legacy ParameterBasedOnFormName replaced with bool continueEditing parameter. PrepareQueuedEmailModelAsync helper resolves email account name via IEmailAccountService. 4 constructor dependencies (IQueuedEmailService, IEmailAccountService, IDateTimeHelper, IPermissionService). Permission: ManageMessageQueue. Follows EmailAccountController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [x] [5.56] Admin: CampaignController + Campaign views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CampaignController, Admin/Views/Campaign
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. CampaignController with core CRUD (List with store filter, CampaignList AJAX grid, Create, Edit, Delete) and email operations (SendTestEmail, SendMassEmail). 3 view models (CampaignListModel, CampaignModel, CampaignGridModel). 3 Razor views (List, Create, Edit with send test email + send mass email + delete). FormValueRequired eliminated — separate endpoints for SendTestEmail(id, testEmail, emailAccountId) and SendMassEmail(id, customerRoleId, emailAccountId). Follows QueuedEmailController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Localization deferred (consistent with all other admin controllers).

- [x] [5.57] Admin: NewsLetterSubscriptionController + views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/NewsLetterSubscriptionController, Admin/Views/NewsLetterSubscription
  - Depends on: 5.1, 4.2
  - Done: 2026-04-10. NewsLetterSubscriptionController with 7 actions: List (search form with email/date/store/active/role filters), SubscriptionList (AJAX grid), SubscriptionUpdate (inline edit), SubscriptionDelete, ExportCsv (separate POST endpoint — FormValueRequired eliminated), ImportCsv (IFormFile replaces FormCollection). 2 view models (NewsLetterSubscriptionListModel, NewsLetterSubscriptionModel). 1 Razor view (List.cshtml with search form + inline AJAX grid + export/import). No ILocalizationService dependency (consistent with simplified admin controllers). Follows CampaignController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping. Admin messaging area complete: EmailAccount [5.53] ✓, MessageTemplate [5.54] ✓, QueuedEmail [5.55] ✓, Campaign [5.56] ✓, NewsLetterSubscription [5.57] ✓.

- [x] [5.58] Admin: GiftCardController + GiftCard views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/GiftCardController, Admin/Views/GiftCard
  - Depends on: 5.1, 4.9
  - Done: 2026-04-10. GiftCardController with core CRUD (List with activated/coupon/recipient filters, GiftCardList AJAX grid, Create, Edit, Delete), GenerateCouponCode (AJAX), NotifyRecipient (separate POST endpoint — FormValueRequired eliminated), UsageHistoryList (AJAX grid), UsageHistoryDelete. 4 view models (GiftCardListModel, GiftCardModel, GiftCardGridModel, GiftCardUsageHistoryModel). 3 Razor views (List, Create, Edit with usage history grid + notify recipient + delete). Added DeleteGiftCardUsageHistoryAsync to IGiftCardService/GiftCardService. Legacy nav properties replaced: giftCard.PurchasedWithOrderItem → IOrderService.GetOrderItemByIdAsync + GetOrderByIdAsync; giftCard.GiftCardUsageHistory → IGiftCardService.GetGiftCardUsageHistoryAsync; x.UsedWithOrder.CustomOrderNumber → IOrderService.GetOrderByIdAsync. ParameterBasedOnFormName eliminated — continueEditing is regular form parameter. Follows PollController pattern: primary constructor, Forbid(), DataSourceResult, inline model mapping.

- [ ] [5.59] Admin: RecurringPaymentController + RecurringPayment views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/RecurringPaymentController, Admin/Views/RecurringPayment
  - Depends on: 5.1, 4.9

- [ ] [5.60] Admin: ReturnRequestController + ReturnRequest views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ReturnRequestController, Admin/Views/ReturnRequest
  - Depends on: 5.1, 4.9

- [ ] [5.61] Admin: ShoppingCartController + ShoppingCart views (abandoned carts)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ShoppingCartController, Admin/Views/ShoppingCart
  - Depends on: 5.1, 4.9

- [ ] [5.62] Admin: ProductAttributeController + ProductAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ProductAttributeController, Admin/Views/ProductAttribute
  - Depends on: 5.1, 4.4

- [ ] [5.63] Admin: SpecificationAttributeController + SpecificationAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/SpecificationAttributeController, Admin/Views/SpecificationAttribute
  - Depends on: 5.1, 4.4

- [ ] [5.64] Admin: ProductReviewController + ProductReview views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ProductReviewController, Admin/Views/ProductReview
  - Depends on: 5.1, 4.4

- [ ] [5.65] Admin: CheckoutAttributeController + CheckoutAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CheckoutAttributeController, Admin/Views/CheckoutAttribute
  - Depends on: 5.1, 4.9

- [ ] [5.66] Admin: AffiliateController + Affiliate views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/AffiliateController, Admin/Views/Affiliate
  - Depends on: 5.1, 3.9

- [ ] [5.67] Admin: VendorController + Vendor views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/VendorController, Admin/Views/Vendor
  - Depends on: 5.1, 3.10

- [ ] [5.68] Admin: StoreController + Store views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/StoreController, Admin/Views/Store
  - Depends on: 5.1, 3.2

- [ ] [5.69] Admin: ActivityLogController + ActivityLog views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ActivityLogController, Admin/Views/ActivityLog
  - Depends on: 5.1, 2.2

- [ ] [5.70] Admin: LogController + Log views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/LogController, Admin/Views/Log
  - Depends on: 5.1, 2.2

- [ ] [5.71] Admin: OnlineCustomerController + OnlineCustomer views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/OnlineCustomerController, Admin/Views/OnlineCustomer
  - Depends on: 5.1, 4.1

- [ ] [5.72] Admin: SecurityController + Security views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/SecurityController, Admin/Views/Security
  - Depends on: 5.1, 2.4

- [ ] [5.73] Admin: ScheduleTaskController + ScheduleTask views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ScheduleTaskController, Admin/Views/ScheduleTask
  - Depends on: 5.1, 3.6

- [ ] [5.74] Admin: TemplateController + Template views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/TemplateController, Admin/Views/Template
  - Depends on: 5.1, 3.1

- [ ] [5.75] Admin: WidgetController + Widget views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/WidgetController, Admin/Views/Widget
  - Depends on: 5.1, 3.15

- [ ] [5.76] Admin: ExternalAuthenticationController + views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ExternalAuthenticationController, Admin/Views/ExternalAuthentication
  - Depends on: 5.1, 2.5

- [ ] [5.77] Admin: AddressAttributeController + AddressAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/AddressAttributeController, Admin/Views/AddressAttribute
  - Depends on: 5.1, 3.8

- [ ] [5.78] Admin: PictureController + DownloadController
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PictureController, Admin/Controllers/DownloadController
  - Depends on: 5.1, 3.7

- [ ] [5.79] Admin: CommonController + Common views (system info, warnings, maintenance)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CommonController, Admin/Views/Common
  - Depends on: 5.1

- [ ] [5.80] Admin: PreferencesController
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PreferencesController
  - Depends on: 5.1

- [ ] [5.81] Admin: Shared views (admin layout, partial views, Kendo grid templates)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Views/Shared
  - Depends on: 5.1

- [ ] [5.82] Admin: JbimagesController + RoxyFilemanController → modern file manager
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/JbimagesController (79 LOC) + RoxyFilemanController (765 LOC) → unified file manager
  - Depends on: 5.1, 3.7

---

## Phase 6: Plugins

- [ ] [6.1] Plugin: Payments.CheckMoneyOrder
  - Spec: specs/plugin-payments-check.md
  - Scope: src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder
  - Depends on: 4.8

- [ ] [6.2] Plugin: Payments.Manual
  - Spec: specs/plugin-payments-manual.md
  - Scope: src/Plugins/Nop.Plugin.Payments.Manual
  - Depends on: 4.8

- [ ] [6.3] Plugin: Payments.PurchaseOrder
  - Spec: specs/plugin-payments-purchase-order.md
  - Scope: src/Plugins/Nop.Plugin.Payments.PurchaseOrder
  - Depends on: 4.8

- [ ] [6.4] Plugin: Payments.PayPalStandard
  - Spec: specs/plugin-payments-paypal-standard.md
  - Scope: src/Plugins/Nop.Plugin.Payments.PayPalStandard
  - Depends on: 4.8

- [ ] [6.5] Plugin: Payments.PayPalDirect
  - Spec: specs/plugin-payments-paypal-direct.md
  - Scope: src/Plugins/Nop.Plugin.Payments.PayPalDirect
  - Depends on: 4.8

- [ ] [6.6] Plugin: Shipping.FixedOrByWeight
  - Spec: specs/plugin-shipping-fixed-weight.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.FixedOrByWeight
  - Depends on: 4.7

- [ ] [6.7] Plugin: Shipping.UPS
  - Spec: specs/plugin-shipping-ups.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.UPS
  - Depends on: 4.7

- [ ] [6.8] Plugin: Shipping.USPS
  - Spec: specs/plugin-shipping-usps.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.USPS
  - Depends on: 4.7

- [ ] [6.9] Plugin: Shipping.Fedex
  - Spec: specs/plugin-shipping-fedex.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.Fedex
  - Depends on: 4.7

- [ ] [6.10] Plugin: Shipping.CanadaPost
  - Spec: specs/plugin-shipping-canadapost.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.CanadaPost
  - Depends on: 4.7

- [ ] [6.11] Plugin: Shipping.AustraliaPost
  - Spec: specs/plugin-shipping-australiapost.md
  - Scope: src/Plugins/Nop.Plugin.Shipping.AustraliaPost
  - Depends on: 4.7

- [ ] [6.12] Plugin: Pickup.PickupInStore
  - Spec: specs/plugin-pickup-instore.md
  - Scope: src/Plugins/Nop.Plugin.Pickup.PickupInStore
  - Depends on: 4.7

- [ ] [6.13] Plugin: Tax.FixedOrByCountryStateZip
  - Spec: specs/plugin-tax-fixed-csz.md
  - Scope: src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip
  - Depends on: 4.6

- [ ] [6.14] Plugin: DiscountRules.CustomerRoles
  - Spec: specs/plugin-discount-customer-roles.md
  - Scope: src/Plugins/Nop.Plugin.DiscountRules.CustomerRoles
  - Depends on: 4.5

- [ ] [6.15] Plugin: DiscountRules.HasOneProduct
  - Spec: specs/plugin-discount-has-product.md
  - Scope: src/Plugins/Nop.Plugin.DiscountRules.HasOneProduct
  - Depends on: 4.5

- [ ] [6.16] Plugin: ExchangeRate.EcbExchange
  - Spec: specs/plugin-exchange-ecb.md
  - Scope: src/Plugins/Nop.Plugin.ExchangeRate.EcbExchange
  - Depends on: 3.3

- [ ] [6.17] Plugin: ExternalAuth.Facebook
  - Spec: specs/plugin-externalauth-facebook.md
  - Scope: src/Plugins/Nop.Plugin.ExternalAuth.Facebook
  - Depends on: 2.5

- [ ] [6.18] Plugin: Feed.GoogleShopping
  - Spec: specs/plugin-feed-google.md
  - Scope: src/Plugins/Nop.Plugin.Feed.GoogleShopping
  - Depends on: 4.4

- [ ] [6.19] Plugin: Widgets.GoogleAnalytics
  - Spec: specs/plugin-widgets-google-analytics.md
  - Scope: src/Plugins/Nop.Plugin.Widgets.GoogleAnalytics
  - Depends on: 3.15

- [ ] [6.20] Plugin: Widgets.NivoSlider
  - Spec: specs/plugin-widgets-nivo-slider.md
  - Scope: src/Plugins/Nop.Plugin.Widgets.NivoSlider
  - Depends on: 3.15

---

## Phase 7: External Integrations

- [ ] [7.1] SMTP email integration — MailKit for email sending
  - Spec: specs/svc-messages.md
  - Scope: Email sending infrastructure
  - Depends on: 4.2

- [ ] [7.2] Redis integration — distributed cache and distributed locking
  - Spec: specs/xcut-caching.md
  - Scope: Redis connection and cache provider
  - Depends on: 2.1

- [ ] [7.3] PayPal API integration — PayPal Checkout v2 SDK
  - Spec: specs/plugin-payments-paypal-standard.md
  - Scope: PayPal plugin HTTP client
  - Depends on: 6.4, 6.5

- [ ] [7.4] UPS REST API integration
  - Spec: specs/plugin-shipping-ups.md
  - Scope: UPS plugin HTTP client
  - Depends on: 6.7

- [ ] [7.5] FedEx REST API integration
  - Spec: specs/plugin-shipping-fedex.md
  - Scope: FedEx plugin HTTP client
  - Depends on: 6.9

- [ ] [7.6] USPS API integration
  - Spec: specs/plugin-shipping-usps.md
  - Scope: USPS plugin HTTP client
  - Depends on: 6.8

- [ ] [7.7] Canada Post API integration
  - Spec: specs/plugin-shipping-canadapost.md
  - Scope: Canada Post plugin HTTP client
  - Depends on: 6.10

- [ ] [7.8] Australia Post API integration
  - Spec: specs/plugin-shipping-australiapost.md
  - Scope: Australia Post plugin HTTP client
  - Depends on: 6.11

- [ ] [7.9] ECB exchange rate feed integration
  - Spec: specs/plugin-exchange-ecb.md
  - Scope: ECB plugin HTTP client
  - Depends on: 6.16

- [ ] [7.10] Facebook OAuth integration
  - Spec: specs/plugin-externalauth-facebook.md
  - Scope: Facebook auth middleware
  - Depends on: 6.17

- [ ] [7.11] Google Analytics integration
  - Spec: specs/plugin-widgets-google-analytics.md
  - Scope: GA tracking script injection
  - Depends on: 6.19

- [ ] [7.12] EU VIES VAT validation integration — replace SOAP web reference with HTTP client
  - Spec: specs/svc-tax.md
  - Scope: TaxService.DoVatCheck → HttpClient calling VIES endpoint
  - Depends on: 4.6

- [ ] [7.13] Google reCAPTCHA integration — validate captcha responses via Google API
  - Spec: specs/xcut-security.md
  - Scope: CaptchaValidatorAttribute + GReCaptchaValidator → HttpClient calling Google reCAPTCHA siteverify
  - Depends on: 2.4, 5.1

- [ ] [7.14] Azure Blob Storage integration — picture storage via Azure.Storage.Blobs SDK
  - Spec: specs/svc-media.md
  - Scope: AzurePictureService → Azure.Storage.Blobs (CloudStorageAccount, CloudBlobClient → BlobServiceClient)
  - Depends on: 3.7

- [ ] [7.15] MaxMind GeoIP2 integration — IP-to-country geolocation via local GeoLite2 database
  - Spec: specs/svc-directory.md
  - Scope: GeoLookupService → MaxMind.GeoIP2 NuGet + GeoLite2-Country.mmdb database file
  - Depends on: 3.3

---

## Phase 8: Data Migration

- [ ] [8.1] SQL Server schema migration — EF Core migrations for new schema
  - Spec: specs/data-migration-sqlserver.md
  - Scope: Database schema
  - Depends on: 1.6

- [ ] [8.2] SQL Server data migration — transfer all records from legacy to new schema
  - Spec: specs/data-migration-sqlserver.md
  - Scope: Data migration scripts
  - Depends on: 8.1

- [ ] [8.3] AttributesXml → JSON conversion
  - Spec: specs/data-migration-sqlserver.md
  - Scope: XML to JSON data transformation
  - Depends on: 8.2

- [ ] [8.4] Redis cache flush and rebuild
  - Spec: specs/data-migration-redis.md
  - Scope: Redis cache
  - Depends on: 7.2

- [ ] [8.5] File system migration — images, themes, downloads
  - Spec: specs/data-migration-filesystem.md
  - Scope: File copy and path updates
  - Depends on: 8.2

---

## Phase 9: Cutover

- [ ] [9.1] Strangler facade setup — reverse proxy routing between legacy and new
  - Spec: specs/solution-scaffold.md
  - Scope: Reverse proxy configuration
  - Depends on: Phase 5 complete

- [ ] [9.2] Strangler facade flip — route all traffic to new system
  - Spec: specs/solution-scaffold.md
  - Scope: Proxy rule update
  - Depends on: 9.1, Phase 8 complete

- [ ] [9.3] Smoke tests — automated verification of critical paths
  - Spec: specs/solution-scaffold.md
  - Scope: E2E test suite
  - Depends on: 9.2

- [ ] [9.4] Go/no-go criteria evaluation
  - Spec: specs/solution-scaffold.md
  - Scope: Checklist verification
  - Depends on: 9.3

- [ ] [9.5] Rollback plan — documented procedure to revert to legacy
  - Spec: specs/solution-scaffold.md
  - Scope: Rollback documentation and scripts
  - Depends on: 9.1

- [ ] [9.6] DNS/routing cutover — update DNS and load balancer to new system
  - Spec: specs/solution-scaffold.md
  - Scope: DNS and routing configuration
  - Depends on: 9.4

- [ ] [9.7] Monitoring setup — dashboards, alerts, and SLOs
  - Spec: specs/xcut-observability.md
  - Scope: Monitoring infrastructure
  - Depends on: 2.8

- [ ] [9.8] Legacy decommission — shut down legacy system after stabilization
  - Spec: specs/solution-scaffold.md
  - Scope: Legacy shutdown procedure
  - Depends on: 9.6
