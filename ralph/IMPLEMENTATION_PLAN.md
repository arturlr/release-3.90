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

- [ ] [1.6] EF Core initial migration — create database from entity configurations
  - Spec: specs/nop-data.md
  - Scope: EF Core migration
  - Depends on: 1.5

- [ ] [1.7] Test project scaffold — xUnit projects per layer, shared test utilities
  - Spec: specs/testing-strategy.md
  - Scope: Test projects for Core, Data, Services, Web
  - Depends on: 1.1

---

## Phase 2: Cross-Cutting Concerns

- [x] [2.1] Caching — IMemoryCache + IDistributedCache + pattern invalidation + per-request cache
  - Spec: specs/xcut-caching.md
  - Scope: Caching infrastructure
  - Depends on: 1.4
  - Done: 2026-04-09. MemoryCacheManager (IMemoryCache wrapper, ConcurrentDictionary key tracking, PostEviction cleanup, prefix-based invalidation). NopRequestCache (scoped Dictionary). CachingDefaults (60-min default). Microsoft.Extensions.Caching.Memory 8.0.1. Redis IDistributedCache deferred to [7.2].

- [ ] [2.2] Logging — ILogger (DB), ICustomerActivityService, Microsoft.Extensions.Logging integration
  - Spec: specs/xcut-logging.md
  - Scope: Nop.Services/Logging
  - Depends on: 1.5

- [ ] [2.3] Localization — ILocalizationService, ILanguageService, ILocalizedEntityService, string resources
  - Spec: specs/xcut-localization.md
  - Scope: Nop.Services/Localization + Nop.Web.Framework/Localization
  - Depends on: 1.5, 2.1

- [ ] [2.4] Security — IPermissionService, IAclService, IEncryptionService
  - Spec: specs/xcut-security.md
  - Scope: Nop.Services/Security
  - Depends on: 1.5

- [ ] [2.5] Authentication — cookie auth, sign-in/sign-out, impersonation
  - Spec: specs/xcut-authentication.md
  - Scope: Authentication middleware + IAuthenticationService
  - Depends on: 2.4

- [ ] [2.6] Authorization — ASP.NET Core policies, permission-based authorization, admin area protection
  - Spec: specs/xcut-authorization.md
  - Scope: Authorization policies + IPermissionService integration
  - Depends on: 2.4, 2.5

- [ ] [2.7] Error handling — global exception handler, ProblemDetails, custom error pages
  - Spec: specs/xcut-error-handling.md
  - Scope: Middleware + error views
  - Depends on: 2.2

- [ ] [2.8] Observability — health checks, OpenTelemetry metrics, distributed tracing
  - Spec: specs/xcut-observability.md
  - Scope: Health check endpoints + OTel configuration
  - Depends on: 1.5, 2.1

- [ ] [2.9] Domain events — IEventPublisher, IConsumer<T>, cache event consumers
  - Spec: specs/svc-events.md
  - Scope: Nop.Services/Events
  - Depends on: 1.4, 2.1

- [ ] [2.10] Plugin system — assembly loading, plugin discovery, IPlugin lifecycle
  - Spec: specs/nop-core-infrastructure.md
  - Scope: Plugin infrastructure
  - Depends on: 1.4

---

## Phase 3: Service Layer — Simple Services

- [ ] [3.1] Configuration services — ISettingService, settings load/save
  - Spec: specs/svc-configuration.md
  - Scope: Nop.Services/Configuration
  - Depends on: 1.5, 2.1

- [ ] [3.2] Store services — IStoreService, IStoreMappingService, IStoreContext
  - Spec: specs/svc-stores.md
  - Scope: Nop.Services/Stores
  - Depends on: 1.5, 2.1

- [ ] [3.3] Directory services — ICountryService, IStateProvinceService, ICurrencyService, IMeasureService, IGeoLookupService
  - Spec: specs/svc-directory.md
  - Scope: Nop.Services/Directory
  - Depends on: 1.5, 2.1, 3.1

- [ ] [3.4] SEO services — IUrlRecordService, ISitemapGenerator, slug generation
  - Spec: specs/svc-seo.md
  - Scope: Nop.Services/Seo
  - Depends on: 1.5, 2.1

- [ ] [3.5] Helpers — IDateTimeHelper, IUserAgentHelper
  - Spec: specs/svc-helpers.md
  - Scope: Nop.Services/Helpers
  - Depends on: 1.4

- [ ] [3.6] Scheduled tasks — IScheduleTaskService, IHostedService integration
  - Spec: specs/svc-tasks.md
  - Scope: Nop.Services/Tasks
  - Depends on: 1.5

- [ ] [3.7] Media services — IPictureService, IDownloadService
  - Spec: specs/svc-media.md
  - Scope: Nop.Services/Media
  - Depends on: 1.5, 3.1

- [ ] [3.8] Common services — IAddressService, IAddressAttributeService, IAddressAttributeParser, IAddressAttributeFormatter, IGenericAttributeService, ISearchTermService, IFulltextService, IPdfService
  - Spec: specs/svc-common.md
  - Scope: Nop.Services/Common
  - Depends on: 1.5, 2.1

- [ ] [3.9] Affiliate services — IAffiliateService
  - Spec: specs/svc-affiliates.md
  - Scope: Nop.Services/Affiliates
  - Depends on: 1.5

- [ ] [3.10] Vendor services — IVendorService
  - Spec: specs/svc-vendors.md
  - Scope: Nop.Services/Vendors
  - Depends on: 1.5, 3.4

- [ ] [3.11] Topic services — ITopicService, ITopicTemplateService
  - Spec: specs/svc-topics.md
  - Scope: Nop.Services/Topics
  - Depends on: 1.5, 2.1, 2.3, 3.4

- [ ] [3.12] Poll services — IPollService
  - Spec: specs/svc-polls.md
  - Scope: Nop.Services/Polls
  - Depends on: 1.5, 2.1

- [ ] [3.13] Blog services — IBlogService
  - Spec: specs/svc-blogs.md
  - Scope: Nop.Services/Blogs
  - Depends on: 1.5, 2.1, 2.3

- [ ] [3.14] News services — INewsService
  - Spec: specs/svc-news.md
  - Scope: Nop.Services/News
  - Depends on: 1.5, 2.1, 2.3

- [ ] [3.15] CMS/Widget services — IWidgetService
  - Spec: specs/svc-cms.md
  - Scope: Nop.Services/Cms
  - Depends on: 1.5, 2.10

- [ ] [3.16] GDPR services — data export, anonymization, consent
  - Spec: specs/svc-gdpr.md
  - Scope: Nop.Services/Gdpr
  - Depends on: 1.5

---

## Phase 4: Service Layer — Complex Services

- [ ] [4.1] Customer services — ICustomerService, ICustomerRegistrationService, ICustomerAttributeService, ICustomerAttributeParser, ICustomerAttributeFormatter, ICustomerActivityService, ICustomerReportService
  - Spec: specs/svc-customers.md
  - Scope: Nop.Services/Customers
  - Depends on: 1.5, 2.1, 2.4, 2.5, 3.1

- [ ] [4.2] Message services — IWorkflowMessageService, IMessageTemplateService, IQueuedEmailService, IEmailAccountService, IEmailSender, INewsLetterSubscriptionService, IMessageTokenProvider, ITokenizer, ICampaignService
  - Spec: specs/svc-messages.md
  - Scope: Nop.Services/Messages
  - Depends on: 1.5, 2.1, 2.3, 3.1, 3.6

- [ ] [4.3] Forum services — IForumService
  - Spec: specs/svc-forums.md
  - Scope: Nop.Services/Forums
  - Depends on: 1.5, 2.1, 4.1, 4.2

- [ ] [4.4] Catalog services — IProductService, ICategoryService, IManufacturerService, IProductAttributeService, IProductAttributeParser, IProductAttributeFormatter, IPriceCalculationService, IPriceFormatter, IProductTagService, ISpecificationAttributeService, ICopyProductService, IBackInStockSubscriptionService, IRecentlyViewedProductsService, ICompareProductsService, ICategoryTemplateService, IManufacturerTemplateService, IProductTemplateService
  - Spec: specs/svc-catalog.md
  - Scope: Nop.Services/Catalog
  - Depends on: 1.5, 2.1, 2.3, 2.9, 3.1, 3.2, 3.4, 3.7, 4.1

- [ ] [4.5] Discount services — IDiscountService
  - Spec: specs/svc-discounts.md
  - Scope: Nop.Services/Discounts
  - Depends on: 1.5, 2.1, 4.1, 4.4

- [ ] [4.6] Tax services — ITaxService, ITaxCategoryService
  - Spec: specs/svc-tax.md
  - Scope: Nop.Services/Tax
  - Depends on: 1.5, 2.1, 3.1, 3.3, 4.1

- [ ] [4.7] Shipping services — IShippingService, IShipmentService, IDateRangeService
  - Spec: specs/svc-shipping.md
  - Scope: Nop.Services/Shipping
  - Depends on: 1.5, 2.1, 3.1, 3.3, 4.4

- [ ] [4.8] Payment services — IPaymentService
  - Spec: specs/svc-payments.md
  - Scope: Nop.Services/Payments
  - Depends on: 1.5, 2.1, 3.1, 4.1

- [ ] [4.9] Order services — IOrderService, IOrderProcessingService, IShoppingCartService, ICheckoutAttributeService, ICheckoutAttributeParser, ICheckoutAttributeFormatter, IGiftCardService, IOrderTotalCalculationService, IReturnRequestService, IOrderReportService, IRewardPointService, ICustomNumberFormatter
  - Spec: specs/svc-orders.md
  - Scope: Nop.Services/Orders
  - Depends on: 1.5, 2.1, 2.9, 3.1, 4.1, 4.2, 4.4, 4.5, 4.6, 4.7, 4.8

- [ ] [4.10] Export/Import services — IExportManager, IImportManager
  - Spec: specs/svc-export-import.md
  - Scope: Nop.Services/ExportImport
  - Depends on: 4.4, 4.9

- [ ] [4.11] Installation services — IInstallationService, seed data
  - Spec: specs/svc-installation.md
  - Scope: Nop.Services/Installation
  - Depends on: all Phase 3 + Phase 4 services

---

## Phase 5: Presentation Layer

- [ ] [5.1] Nop.Web.Framework — base controllers, tag helpers, theme engine, FluentValidation, security filters
  - Spec: specs/nop-web-framework.md
  - Scope: src/Presentation/Nop.Web.Framework
  - Depends on: 2.3, 2.6, 2.7

- [ ] [5.2] Public: HomeController + Home views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/HomeController, Views/Home
  - Depends on: 5.1

- [ ] [5.3] Public: CommonController + Common views (header, footer, widgets, language/currency selectors)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CommonController, Views/Common
  - Depends on: 5.1, 3.2, 3.3, 3.15

- [ ] [5.4] Public: CatalogController + Catalog views (category list, manufacturer list, search, filtering)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CatalogController, Views/Catalog
  - Depends on: 5.1, 4.4

- [ ] [5.5] Public: ProductController + Product views (product detail, reviews)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ProductController, Views/Product
  - Depends on: 5.1, 4.4

- [ ] [5.6] Public: CustomerController + Customer views (register, login, account, addresses, orders)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CustomerController, Views/Customer
  - Depends on: 5.1, 4.1, 2.5

- [ ] [5.7] Public: ShoppingCartController + ShoppingCart views (cart, wishlist, mini-cart)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ShoppingCartController, Views/ShoppingCart
  - Depends on: 5.1, 4.9

- [ ] [5.8] Public: CheckoutController + Checkout views (address, shipping, payment, confirm)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CheckoutController, Views/Checkout
  - Depends on: 5.1, 4.9

- [ ] [5.9] Public: OrderController + Order views (order history, order details)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/OrderController, Views/Order
  - Depends on: 5.1, 4.9

- [ ] [5.10] Public: BlogController + Blog views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BlogController, Views/Blog
  - Depends on: 5.1, 3.13

- [ ] [5.11] Public: NewsController + News views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/NewsController, Views/News
  - Depends on: 5.1, 3.14

- [ ] [5.12] Public: BoardsController + Boards views (forums)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BoardsController, Views/Boards
  - Depends on: 5.1, 4.3

- [ ] [5.13] Public: TopicController + Topic views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/TopicController, Views/Topic
  - Depends on: 5.1, 3.11

- [ ] [5.14] Public: PollController + Poll views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/PollController, Views/Poll
  - Depends on: 5.1, 3.12

- [ ] [5.15] Public: NewsletterController + Newsletter views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/NewsletterController, Views/Newsletter
  - Depends on: 5.1, 4.2

- [ ] [5.16] Public: ReturnRequestController + ReturnRequest views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ReturnRequestController, Views/ReturnRequest
  - Depends on: 5.1, 4.9

- [ ] [5.17] Public: PrivateMessagesController + PrivateMessages views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/PrivateMessagesController, Views/PrivateMessages
  - Depends on: 5.1, 4.3

- [ ] [5.18] Public: ProfileController + Profile views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ProfileController, Views/Profile
  - Depends on: 5.1, 4.1

- [ ] [5.19] Public: VendorController + Vendor views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/VendorController, Views/Vendor
  - Depends on: 5.1, 3.10

- [ ] [5.20] Public: ExternalAuthenticationController + ExternalAuthentication views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/ExternalAuthenticationController, Views/ExternalAuthentication
  - Depends on: 5.1, 2.5

- [ ] [5.21] Public: DownloadController
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/DownloadController
  - Depends on: 5.1, 3.7

- [ ] [5.22] Public: CountryController (AJAX)
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/CountryController
  - Depends on: 5.1, 3.3

- [ ] [5.23] Public: WidgetController + Widget views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/WidgetController, Views/Widget
  - Depends on: 5.1, 3.15

- [ ] [5.24] Public: BackInStockSubscriptionController + views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/BackInStockSubscriptionController, Views/BackInStockSubscription
  - Depends on: 5.1, 4.4

- [ ] [5.25] Public: InstallController + Install views
  - Spec: specs/nop-web-public.md
  - Scope: Controllers/InstallController, Views/Install
  - Depends on: 4.11

- [ ] [5.26] Public: Shared views (layout, partial views, _ViewImports)
  - Spec: specs/nop-web-public.md
  - Scope: Views/Shared
  - Depends on: 5.1

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

- [ ] [5.30] Admin: HomeController + Home views (dashboard)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/HomeController, Admin/Views/Home
  - Depends on: 5.1

- [ ] [5.31] Admin: ProductController + Product views (CRUD, attributes, pictures, inventory)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ProductController (4857 LOC), Admin/Views/Product
  - Depends on: 5.1, 4.4

- [ ] [5.32] Admin: CategoryController + Category views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CategoryController, Admin/Views/Category
  - Depends on: 5.1, 4.4

- [ ] [5.33] Admin: ManufacturerController + Manufacturer views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ManufacturerController, Admin/Views/Manufacturer
  - Depends on: 5.1, 4.4

- [ ] [5.34] Admin: OrderController + Order views (list, detail, status, refunds, shipments)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/OrderController (4379 LOC), Admin/Views/Order
  - Depends on: 5.1, 4.9

- [ ] [5.35] Admin: CustomerController + Customer views (list, detail, roles, addresses)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerController (2396 LOC), Admin/Views/Customer
  - Depends on: 5.1, 4.1

- [ ] [5.36] Admin: CustomerRoleController + CustomerRole views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerRoleController, Admin/Views/CustomerRole
  - Depends on: 5.1, 4.1

- [ ] [5.37] Admin: CustomerAttributeController + CustomerAttribute views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CustomerAttributeController, Admin/Views/CustomerAttribute
  - Depends on: 5.1, 4.1

- [ ] [5.38] Admin: SettingController + Setting views (all configuration sections)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/SettingController (2339 LOC), Admin/Views/Setting
  - Depends on: 5.1, 3.1

- [ ] [5.39] Admin: PluginController + Plugin views (install, uninstall, configure)
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PluginController, Admin/Views/Plugin
  - Depends on: 5.1, 2.10

- [ ] [5.40] Admin: DiscountController + Discount views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/DiscountController, Admin/Views/Discount
  - Depends on: 5.1, 4.5

- [ ] [5.41] Admin: ShippingController + Shipping views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ShippingController, Admin/Views/Shipping
  - Depends on: 5.1, 4.7

- [ ] [5.42] Admin: PaymentController + Payment views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PaymentController, Admin/Views/Payment
  - Depends on: 5.1, 4.8

- [ ] [5.43] Admin: TaxController + Tax views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/TaxController, Admin/Views/Tax
  - Depends on: 5.1, 4.6

- [ ] [5.44] Admin: BlogController + Blog views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/BlogController, Admin/Views/Blog
  - Depends on: 5.1, 3.13

- [ ] [5.45] Admin: NewsController + News views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/NewsController, Admin/Views/News
  - Depends on: 5.1, 3.14

- [ ] [5.46] Admin: ForumController + Forum views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/ForumController, Admin/Views/Forum
  - Depends on: 5.1, 4.3

- [ ] [5.47] Admin: PollController + Poll views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/PollController, Admin/Views/Poll
  - Depends on: 5.1, 3.12

- [ ] [5.48] Admin: TopicController + Topic views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/TopicController, Admin/Views/Topic
  - Depends on: 5.1, 3.11

- [ ] [5.49] Admin: LanguageController + Language views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/LanguageController, Admin/Views/Language
  - Depends on: 5.1, 2.3

- [ ] [5.50] Admin: CurrencyController + Currency views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CurrencyController, Admin/Views/Currency
  - Depends on: 5.1, 3.3

- [ ] [5.51] Admin: CountryController + Country views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CountryController, Admin/Views/Country
  - Depends on: 5.1, 3.3

- [ ] [5.52] Admin: MeasureController + Measure views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/MeasureController, Admin/Views/Measure
  - Depends on: 5.1, 3.3

- [ ] [5.53] Admin: EmailAccountController + EmailAccount views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/EmailAccountController, Admin/Views/EmailAccount
  - Depends on: 5.1, 4.2

- [ ] [5.54] Admin: MessageTemplateController + MessageTemplate views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/MessageTemplateController, Admin/Views/MessageTemplate
  - Depends on: 5.1, 4.2

- [ ] [5.55] Admin: QueuedEmailController + QueuedEmail views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/QueuedEmailController, Admin/Views/QueuedEmail
  - Depends on: 5.1, 4.2

- [ ] [5.56] Admin: CampaignController + Campaign views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/CampaignController, Admin/Views/Campaign
  - Depends on: 5.1, 4.2

- [ ] [5.57] Admin: NewsLetterSubscriptionController + views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/NewsLetterSubscriptionController, Admin/Views/NewsLetterSubscription
  - Depends on: 5.1, 4.2

- [ ] [5.58] Admin: GiftCardController + GiftCard views
  - Spec: specs/nop-admin.md
  - Scope: Admin/Controllers/GiftCardController, Admin/Views/GiftCard
  - Depends on: 5.1, 4.9

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
