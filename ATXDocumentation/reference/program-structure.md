# nopCommerce Program Structure

## Table of Contents
- [Overview](#overview)
- [Namespace Organization](#namespace-organization)
- [Core Domain Entities](#core-domain-entities)
- [Service Layer](#service-layer)
- [Data Access Layer](#data-access-layer)
- [Presentation Layer](#presentation-layer)
- [Plugin System](#plugin-system)
- [Infrastructure Components](#infrastructure-components)

## Overview

The nopCommerce codebase is organized following a layered architecture with clear separation of concerns. The program structure comprises 1,731 C# files organized into 31 projects across 4 main categories:

- **Libraries** (734 files): Core business logic and data access
- **Presentation** (612 files): Web application and framework
- **Plugins** (181 files): Extensible modules
- **Tests** (204 files): Unit and integration tests

## Namespace Organization

### Nop.Core Namespace
The foundation namespace containing domain models, infrastructure, and common utilities.

**Primary Sub-namespaces:**
```
Nop.Core
├── Domain
│   ├── Affiliates
│   ├── Blogs
│   ├── Catalog (Product, Category, Manufacturer)
│   ├── Cms
│   ├── Common (Address, GenericAttribute)
│   ├── Configuration
│   ├── Customers (Customer, CustomerRole)
│   ├── Directory (Country, Currency, StateProvince)
│   ├── Discounts
│   ├── Forums
│   ├── Localization
│   ├── Logging
│   ├── Media (Picture, Download)
│   ├── Messages (EmailAccount, MessageTemplate)
│   ├── News
│   ├── Orders (Order, OrderItem, ShoppingCartItem)
│   ├── Payments
│   ├── Polls
│   ├── Security (PermissionRecord, AclRecord)
│   ├── Seo (UrlRecord)
│   ├── Shipping (Shipment, ShippingMethod)
│   ├── Stores (Store, StoreMapping)
│   ├── Tasks (ScheduleTask)
│   ├── Tax
│   ├── Topics
│   └── Vendors
├── Infrastructure (DI, Engine, Type Finding)
├── Plugins (Plugin System)
├── Caching (ICacheManager, RedisCacheManager)
├── Data (IRepository, DataSettings)
├── Events (Domain Events)
└── Html (Helpers and Utilities)
```

### Nop.Data Namespace
Entity Framework implementation and data access.

```
Nop.Data
├── Mapping (Fluent API Configuration)
│   ├── Affiliates
│   ├── Blogs
│   ├── Catalog (ProductMap, CategoryMap, etc.)
│   ├── Customers (CustomerMap, CustomerRoleMap)
│   ├── Orders (OrderMap, OrderItemMap)
│   └── [other domain mappings]
├── Initializers (Database initialization)
├── NopObjectContext (DbContext)
├── EfRepository<T> (Repository implementation)
└── DataProviders (SQL Server, SQL CE)
```

### Nop.Services Namespace
Business logic and service implementations.

```
Nop.Services
├── Affiliates
├── Authentication
├── Blogs
├── Catalog (ProductService, CategoryService, etc.)
├── Cms
├── Common
├── Configuration
├── Customers (CustomerService, CustomerRegistrationService)
├── Directory
├── Discounts
├── Events
├── ExportImport
├── Forums
├── Helpers
├── Installation
├── Localization
├── Logging
├── Media
├── Messages (MessageService, EmailAccountService)
├── News
├── Orders (OrderService, ShoppingCartService)
├── Payments
├── Polls
├── Security (PermissionService, AclService)
├── Seo
├── Shipping
├── Stores
├── Tasks
├── Tax
├── Topics
└── Vendors
```

### Nop.Web Namespace
Web application including controllers and views.

```
Nop.Web
├── Controllers
│   ├── CatalogController
│   ├── ProductController
│   ├── CustomerController
│   ├── CheckoutController
│   ├── OrderController
│   ├── ShoppingCartController
│   └── [28+ controllers]
├── Models (View Models)
├── Views (Razor Templates)
├── Themes
├── Administration (Admin Area)
└── Infrastructure
```

### Nop.Web.Framework Namespace
Shared web infrastructure.

```
Nop.Web.Framework
├── Controllers (Base Controllers)
├── Filters (MVC Filters)
├── Localization
├── Mvc (Custom MVC Components)
├── Security
├── Themes
├── UI (HTML Helpers, Paging)
└── Validators (FluentValidation)
```

## Core Domain Entities

### Base Entity
All domain entities inherit from `BaseEntity`:

**Location**: `Nop.Core.BaseEntity`

```csharp
public abstract partial class BaseEntity
{
    public int Id { get; set; }
    // Equals/GetHashCode implementation
    // Entity comparison operators
}
```

**Key Characteristics:**
- Provides common `Id` property
- Implements proper entity equality based on ID
- Handles transient entities (not yet persisted)
- Type-safe equality comparison

### Primary Domain Entities

#### Product Entity
**Location**: `Nop.Core.Domain.Catalog.Product`

**Interfaces Implemented:**
- `ILocalizedEntity` - Localization support
- `ISlugSupported` - SEO-friendly URLs
- `IAclSupported` - Access control lists
- `IStoreMappingSupported` - Multi-store support

**Key Properties:**
- Product information (Name, SKU, Description, Price)
- Inventory management (StockQuantity, ManageInventoryMethod)
- Shipping details (Weight, Dimensions, ShippingCharge)
- Pricing (Price, OldPrice, TierPrices, Discounts)
- SEO metadata (MetaKeywords, MetaDescription, MetaTitle)
- Product type (Simple, Grouped, Downloadable, Recurring, Rental)
- Gift card support
- Rental and recurring configuration
- Dates (Created, Updated, Available dates)

**Navigation Properties:**
- `ProductCategories` - Category associations
- `ProductManufacturers` - Manufacturer associations
- `ProductPictures` - Product images
- `ProductReviews` - Customer reviews
- `ProductSpecificationAttributes` - Specifications
- `ProductTags` - Product tags
- `ProductAttributeMappings` - Variant attributes
- `ProductAttributeCombinations` - Variant combinations
- `TierPrices` - Quantity-based pricing
- `AppliedDiscounts` - Discount associations
- `ProductWarehouseInventory` - Multi-warehouse inventory

**Total Properties**: 100+ properties covering all e-commerce scenarios

#### Customer Entity
**Location**: `Nop.Core.Domain.Customers.Customer`

**Key Properties:**
- Authentication (Username, Email, CustomerGuid)
- Security (FailedLoginAttempts, CannotLoginUntilDateUtc)
- Status flags (Active, Deleted, IsSystemAccount)
- Tracking (LastLoginDateUtc, LastActivityDateUtc, LastIpAddress)
- Business associations (AffiliateId, VendorId)
- Tax settings (IsTaxExempt)
- Performance optimization flags (HasShoppingCartItems)

**Navigation Properties:**
- `CustomerRoles` - Role assignments
- `ShoppingCartItems` - Cart and wishlist items
- `ExternalAuthenticationRecords` - OAuth logins
- `ReturnRequests` - Return requests
- `Addresses` - Saved addresses
- `BillingAddress` - Default billing address
- `ShippingAddress` - Default shipping address

#### Order Entity
**Location**: `Nop.Core.Domain.Orders.Order`

**Key Properties:**
- Order identification (OrderGuid, CustomOrderNumber)
- Customer information (CustomerId, BillingAddress, ShippingAddress)
- Financial details (OrderTotal, OrderSubtotal, Tax, Shipping, Discount)
- Payment information (PaymentMethodSystemName, PaymentStatus)
- Shipping information (ShippingMethod, ShippingStatus, TrackingNumber)
- Status (OrderStatus: Pending, Processing, Complete, Cancelled)
- Dates (CreatedOnUtc, PaidDateUtc, ShippedDateUtc)

**Navigation Properties:**
- `OrderItems` - Line items
- `OrderNotes` - Order notes
- `Shipments` - Shipment records
- `GiftCardUsageHistory` - Gift card usage

#### Category Entity
**Location**: `Nop.Core.Domain.Catalog.Category`

**Key Properties:**
- Name, Description
- Parent category support (ParentCategoryId)
- Display settings (PictureId, ShowOnHomePage, IncludeInTopMenu)
- SEO support (MetaKeywords, MetaDescription, MetaTitle)
- Sorting (DisplayOrder, PageSize)

#### Other Key Entities
- **Manufacturer**: Product brands/manufacturers
- **ShoppingCartItem**: Cart and wishlist items
- **Address**: Billing/shipping addresses
- **CustomerRole**: Security roles (Admin, Registered, Guest)
- **Discount**: Promotional discounts
- **GiftCard**: Gift card products
- **Shipment**: Shipping records
- **ReturnRequest**: Product returns
- **NewsLetterSubscription**: Email subscriptions
- **Poll, BlogPost, NewsItem**: Content entities
- **ForumTopic, ForumPost**: Forum content
- **EmailAccount, MessageTemplate**: Email configuration
- **Store**: Multi-store support
- **Language**: Localization
- **Currency**: Multi-currency support
- **TaxCategory**: Tax configuration
- **Setting**: Configuration settings

## Service Layer

### Service Interface Pattern

All services follow a consistent interface pattern:

```csharp
public interface I[Entity]Service
{
    // Get operations
    [Entity] GetById(int id);
    IList<[Entity]> GetAll(...);
    IPagedList<[Entity]> Search(...);
    
    // Write operations
    void Insert([Entity] entity);
    void Update([Entity] entity);
    void Delete([Entity] entity);
    
    // Business-specific operations
    // ...
}
```

### Major Service Categories

#### Catalog Services
**Location**: `Nop.Services.Catalog`

**Services:**
- `IProductService` - Product management
- `ICategoryService` - Category operations
- `IManufacturerService` - Manufacturer management
- `IProductAttributeService` - Product attributes/variants
- `IPriceCalculationService` - Price calculations
- `IPriceFormatter` - Price formatting
- `IProductTagService` - Product tagging
- `IRecentlyViewedProductsService` - Recently viewed tracking
- `ISpecificationAttributeService` - Product specifications
- `ICopyProductService` - Product duplication
- `IBackInStockSubscriptionService` - Stock notifications

**Key Methods (IProductService example):**
- `GetProductById(int productId)`
- `GetProductsByIds(int[] productIds)`
- `SearchProducts(...)` - Complex product search
- `InsertProduct(Product product)`
- `UpdateProduct(Product product)`
- `DeleteProduct(Product product)`
- `GetProductsDisplayedOnHomePage()`
- `GetLowStockProducts()`
- `UpdateHasDiscountsApplied(Product product)`
- `UpdateProductReviewTotals(Product product)`

#### Customer Services
**Location**: `Nop.Services.Customers`

**Services:**
- `ICustomerService` - Customer CRUD operations
- `ICustomerRegistrationService` - Registration and authentication
- `ICustomerAttributeService` - Custom attributes
- `ICustomerActivityService` - Activity tracking

**Key Methods (ICustomerRegistrationService):**
- `ValidateCustomer(string username, string password)`
- `RegisterCustomer(CustomerRegistrationRequest request)`
- `ChangePassword(ChangePasswordRequest request)`
- `ValidateCustomer(Customer customer)`

#### Order Services
**Location**: `Nop.Services.Orders`

**Services:**
- `IOrderService` - Order management
- `IOrderProcessingService` - Order workflow
- `IShoppingCartService` - Cart operations
- `ICheckoutAttributeService` - Checkout attributes
- `IGiftCardService` - Gift card handling
- `IOrderTotalCalculationService` - Order total calculations
- `IReturnRequestService` - Return management

**Key Methods (IOrderProcessingService):**
- `PlaceOrder(ProcessPaymentRequest request)`
- `CanCancelOrder(Order order)`
- `CancelOrder(Order order, bool notifyCustomer)`
- `CompleteOrder(Order order)`
- `CanMarkOrderAsPaid(Order order)`
- `MarkOrderAsPaid(Order order)`
- `CanShip(Order order)`
- `Ship(Shipment shipment, bool notifyCustomer)`
- `CanDeliver(Order order)`
- `Deliver(Shipment shipment, bool notifyCustomer)`

#### Payment Services
**Location**: `Nop.Services.Payments`

**Services:**
- `IPaymentService` - Payment processing coordination
- Payment methods implemented as plugins

#### Shipping Services
**Location**: `Nop.Services.Shipping`

**Services:**
- `IShippingService` - Shipping coordination
- `IShipmentService` - Shipment tracking
- `IDateRangeService` - Delivery date ranges
- Shipping rate computation via plugins

#### Security Services
**Location**: `Nop.Services.Security`

**Services:**
- `IPermissionService` - Authorization
- `IAclService` - Access control lists
- `IEncryptionService` - Cryptography

#### Message Services
**Location**: `Nop.Services.Messages`

**Services:**
- `IMessageTokenProvider` - Email template tokens
- `IMessageTemplateService` - Email templates
- `IQueuedEmailService` - Email queue
- `IEmailAccountService` - SMTP configuration
- `INewsLetterSubscriptionService` - Newsletter management
- `IWorkflowMessageService` - Workflow email sending

#### Common Services
- `ILocalizationService` - Translations
- `ILanguageService` - Language management
- `ISettingService` - Configuration settings
- `IStoreService` - Multi-store management
- `IWorkContext` - Current user context
- `IStoreContext` - Current store context
- `ILogger` - Logging
- `IWebHelper` - Web utilities
- `IDateTimeHelper` - Date/time operations

### Service Implementation Pattern

Services typically follow this structure:

```csharp
public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<...> _otherRepositories;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICacheManager _cacheManager;
    
    public ProductService(
        IRepository<Product> productRepository,
        // ... other dependencies
    )
    {
        _productRepository = productRepository;
        // ... initialize dependencies
    }
    
    public virtual Product GetProductById(int productId)
    {
        if (productId == 0)
            return null;
            
        string key = string.Format(CACHE_KEY, productId);
        return _cacheManager.Get(key, () => 
        {
            return _productRepository.GetById(productId);
        });
    }
    
    public virtual void InsertProduct(Product product)
    {
        if (product == null)
            throw new ArgumentNullException("product");
            
        _productRepository.Insert(product);
        _cacheManager.RemoveByPattern(CACHE_PATTERN);
        _eventPublisher.EntityInserted(product);
    }
}
```

**Common Patterns:**
- Constructor injection of dependencies
- Repository pattern for data access
- Caching for read operations
- Domain event publishing for state changes
- Virtual methods for extensibility

## Data Access Layer

### Repository Pattern

**Interface**: `Nop.Core.Data.IRepository<T>`

```csharp
public interface IRepository<T> where T : BaseEntity
{
    T GetById(object id);
    void Insert(T entity);
    void Insert(IEnumerable<T> entities);
    void Update(T entity);
    void Update(IEnumerable<T> entities);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);
    IQueryable<T> Table { get; }
    IQueryable<T> TableNoTracking { get; }
}
```

**Implementation**: `Nop.Data.EfRepository<T>`

**Key Features:**
- Generic repository for all entities
- Lazy loading support
- No-tracking queries for read-only scenarios
- Bulk operations support

### Entity Framework DbContext

**Class**: `Nop.Data.NopObjectContext`

**Features:**
- Inherits from Entity Framework's `DbContext`
- Dynamic configuration loading from assembly
- Fluent API mappings
- SQL query execution support
- Stored procedure support

### Entity Mappings

**Base Class**: `Nop.Data.Mapping.NopEntityTypeConfiguration<T>`

**Example Mapping** (`ProductMap`):
```csharp
public class ProductMap : NopEntityTypeConfiguration<Product>
{
    public ProductMap()
    {
        this.ToTable("Product");
        this.HasKey(p => p.Id);
        this.Property(p => p.Name).IsRequired().HasMaxLength(400);
        this.Property(p => p.Price).HasPrecision(18, 4);
        // ... additional property configurations
        
        // Ignore computed properties
        this.Ignore(p => p.ProductType);
        
        // Many-to-many relationships
        this.HasMany(p => p.ProductTags)
            .WithMany(pt => pt.Products)
            .Map(m => m.ToTable("Product_ProductTag_Mapping"));
    }
}
```

**Mapping Categories:**
- Table and column mappings
- Primary key configuration
- Property constraints (Required, MaxLength, Precision)
- Relationship mappings (One-to-many, many-to-many)
- Computed property exclusions

**All Domain Areas Have Mappings:**
- Catalog, Customers, Orders, Shipping, Payments
- Blogs, News, Forums, Polls
- Security, Localization, Configuration
- And more...

## Presentation Layer

### Controller Structure

**Base Controllers:**
- `BasePublicController` - Customer-facing controllers
- `BaseAdminController` - Admin area controllers (in Nop.Admin)

### Public Controllers

**Location**: `Nop.Web.Controllers`

**Major Controllers:**
- `CatalogController` - Product catalog, categories, manufacturers, search
- `ProductController` - Product details, reviews
- `ShoppingCartController` - Cart operations, wishlist
- `CheckoutController` - Checkout process
- `CustomerController` - Account, registration, login
- `OrderController` - Order history, details
- `BlogController` - Blog functionality
- `NewsController` - News articles
- `BoardsController` - Forums
- `CommonController` - Common functions (header, footer, widgets)
- `HomeController` - Homepage
- `CountryController` - AJAX country/state selection
- `DownloadController` - Digital downloads
- `NewsletterController` - Newsletter subscription
- `PollController` - Polling
- `ReturnRequestController` - Product returns
- `TopicController` - Content pages
- `WidgetController` - Widget rendering
- `ExternalAuthenticationController` - OAuth/OpenID

### Controller Pattern

```csharp
public class ProductController : BasePublicController
{
    private readonly IProductService _productService;
    private readonly IWorkContext _workContext;
    private readonly ILocalizationService _localizationService;
    // ... other services
    
    public ProductController(
        IProductService productService,
        // ... dependencies
    )
    {
        _productService = productService;
        // ... initialize
    }
    
    [HttpGet]
    public ActionResult ProductDetails(int productId)
    {
        var product = _productService.GetProductById(productId);
        if (product == null || product.Deleted)
            return RedirectToRoute("HomePage");
            
        var model = PrepareProductDetailsModel(product);
        return View(model);
    }
    
    [HttpPost]
    public ActionResult AddProductToCart(int productId, FormCollection form)
    {
        // Business logic...
        return Json(new { success = true });
    }
}
```

### View Models

**Location**: `Nop.Web.Models`

**Categories:**
- `Catalog` - Product, category, manufacturer models
- `Customer` - Registration, login, account models
- `Checkout` - Checkout process models
- `Order` - Order history models
- `Blogs` - Blog models
- `News` - News models
- `Boards` - Forum models
- `Common` - Shared models (header, footer, currency, language)

**View Model Pattern:**
```csharp
public class ProductDetailsModel : BaseNopEntityModel
{
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string Price { get; set; }
    public bool AvailableForPreOrder { get; set; }
    // ... many more properties
    
    public IList<ProductPictureModel> Pictures { get; set; }
    public IList<ProductVariantAttributeModel> Attributes { get; set; }
}
```

### Views

**Location**: `Nop.Web.Views`

**View Engine**: Razor (`.cshtml`)

**Major View Directories:**
- `Catalog` - Product listings, details, search
- `Customer` - Account pages
- `Checkout` - Checkout steps
- `Order` - Order history
- `ShoppingCart` - Cart and wishlist
- `Blog`, `News`, `Boards` - Content views
- `Shared` - Layout, partial views, components

## Plugin System

### Plugin Architecture

**Core Interface**: `Nop.Core.Plugins.IPlugin`

```csharp
public interface IPlugin
{
    PluginDescriptor PluginDescriptor { get; set; }
    void Install();
    void Uninstall();
}
```

**Base Class**: `Nop.Core.Plugins.BasePlugin`

### Plugin Categories

Plugins extend base interfaces to provide specific functionality:

#### Payment Plugins
**Interface**: `Nop.Services.Payments.IPaymentMethod`

**Methods:**
- `ProcessPayment()` - Process payment
- `PostProcessPayment()` - Post-payment processing
- `GetAdditionalHandlingFee()` - Calculate fees
- `Capture()` - Capture authorized payment
- `Refund()` - Process refund
- `Void()` - Void payment

**Existing Plugins:**
- PayPalStandard, PayPalDirect
- CheckMoneyOrder, Manual, PurchaseOrder

#### Shipping Plugins
**Interface**: `Nop.Services.Shipping.IShippingRateComputationMethod`

**Methods:**
- `GetShippingOptions()` - Calculate shipping rates
- `GetFixedRate()` - Get fixed shipping rate

**Existing Plugins:**
- UPS, USPS, FedEx
- CanadaPost, AustraliaPost
- FixedOrByWeight, PickupInStore

#### Tax Plugins
**Interface**: `Nop.Services.Tax.ITaxProvider`

**Methods:**
- `GetTaxRate()` - Calculate tax rate

**Existing Plugins:**
- FixedOrByCountryStateZip

#### Widget Plugins
**Interface**: `Nop.Services.Cms.IWidgetPlugin`

**Methods:**
- `GetWidgetZones()` - Specify where widget appears
- `GetDisplayWidgetRoute()` - Return widget view

**Existing Plugins:**
- GoogleAnalytics, NivoSlider

#### Other Plugin Types
- **Discount Rules**: Custom discount conditions
- **Exchange Rate Providers**: Currency rate fetching
- **External Authentication**: OAuth/OpenID providers
- **Product Feeds**: Export to shopping engines

### Plugin Discovery

**Plugin Manager**: `Nop.Core.Plugins.PluginManager`

**Features:**
- Automatic plugin discovery on application start
- Plugin metadata from `plugin.json` or assembly attributes
- Dynamic plugin loading from `~/Plugins/` directory
- Plugin installation/uninstallation lifecycle management

### Plugin Structure

Each plugin is a separate project with:
```
Nop.Plugin.{Group}.{Name}/
├── Controllers/
├── Models/
├── Views/
├── plugin.json (metadata)
├── Description.txt
└── {PluginName}Plugin.cs (main plugin class)
```

## Infrastructure Components

### Dependency Injection (IoC)

**Container**: Autofac

**Core Classes:**
- `NopEngine` - IoC engine implementation
- `ContainerManager` - Autofac container wrapper
- `IDependencyRegistrar` - Plugin registration interface

**Registration Pattern:**
```csharp
public class DependencyRegistrar : IDependencyRegistrar
{
    public void Register(ContainerBuilder builder, ITypeFinder typeFinder)
    {
        // Register services
        builder.RegisterType<ProductService>().As<IProductService>().InstancePerLifetimeScope();
        builder.RegisterType<CategoryService>().As<ICategoryService>().InstancePerLifetimeScope();
        // ...
        
        // Register repositories
        builder.RegisterGeneric(typeof(EfRepository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
    }
    
    public int Order => 0;
}
```

**Scopes:**
- `InstancePerLifetimeScope` - One instance per HTTP request
- `SingleInstance` - Singleton
- `InstancePerDependency` - Transient

### Caching

**Interface**: `Nop.Core.Caching.ICacheManager`

**Implementations:**
- `MemoryCacheManager` - In-memory caching (System.Runtime.Caching)
- `PerRequestCacheManager` - Request-scoped cache
- `RedisCacheManager` - Distributed Redis cache

**Usage Pattern:**
```csharp
string key = string.Format(CACHE_KEY, id);
return _cacheManager.Get(key, () => 
{
    // Executed only on cache miss
    return _repository.GetById(id);
});
```

### Events System

**Event Publishing**: `Nop.Services.Events.IEventPublisher`

**Standard Events:**
- `EntityInserted<T>` - Entity created
- `EntityUpdated<T>` - Entity modified
- `EntityDeleted<T>` - Entity removed

**Event Consumers:**
```csharp
public class ProductEventConsumer : IConsumer<EntityInserted<Product>>
{
    public void HandleEvent(EntityInserted<Product> eventMessage)
    {
        // React to product insertion
    }
}
```

**Use Cases:**
- Cache invalidation
- Search index updates
- Business rule enforcement
- Audit logging
- Integration with external systems

### Type Finding

**Interface**: `Nop.Core.Infrastructure.ITypeFinder`

**Implementations:**
- `WebAppTypeFinder` - Scans web application assemblies
- `AppDomainTypeFinder` - Scans all AppDomain assemblies

**Use Cases:**
- Plugin discovery
- Dependency registration
- Entity mapping discovery
- Event consumer discovery

### Work Context

**Interface**: `Nop.Core.IWorkContext`

**Provides Access To:**
- `CurrentCustomer` - Currently logged-in customer
- `OriginalCustomerIfImpersonated` - Admin impersonation support
- `CurrentVendor` - Current vendor (for vendor portal)
- `WorkingLanguage` - Current language
- `WorkingCurrency` - Current currency
- `TaxDisplayType` - Price display (with/without tax)
- `IsAdmin` - Admin area flag

### Store Context

**Interface**: `Nop.Core.IStoreContext`

**Provides:**
- `CurrentStore` - Current store in multi-store scenario

### Localization

**Key Services:**
- `ILocalizationService` - Translation lookups
- `ILanguageService` - Language management
- `ILocalizedEntityService` - Entity-specific translations

**Pattern:**
```csharp
string text = _localizationService.GetResource("Catalog.Products.Name");
```

**Database Storage:**
- `LocaleStringResource` - Resource key-value pairs
- `LocalizedProperty` - Entity property translations

## Summary

The nopCommerce program structure demonstrates:

1. **Clear Layering**: Domain, Data, Services, Presentation separation
2. **SOLID Principles**: Interface-based design, dependency injection
3. **Repository Pattern**: Abstracted data access
4. **Service Layer**: Business logic encapsulation
5. **Plugin Architecture**: Extensibility without core modification
6. **Domain Events**: Loose coupling via pub-sub
7. **Comprehensive Domain Model**: Rich entities covering all e-commerce aspects
8. **MVC Pattern**: Clean separation in presentation layer
9. **Caching Strategy**: Multi-level caching for performance
10. **Type Safety**: Strongly-typed throughout

The structure supports maintainability, testability, and extensibility while handling complex e-commerce requirements.

---

**Related Documentation:**
- [Interfaces Documentation](interfaces.md)
- [Data Models Documentation](data-models.md)
- [Dependencies Documentation](../architecture/dependencies.md)
- [Architecture Patterns](../architecture/patterns.md)
