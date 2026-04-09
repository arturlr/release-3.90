# nopCommerce System Architecture Overview

## Table of Contents
- [Executive Summary](#executive-summary)
- [Architectural Style](#architectural-style)
- [System Context](#system-context)
- [Layered Architecture](#layered-architecture)
- [Component Interaction](#component-interaction)
- [Data Flow](#data-flow)
- [Deployment Architecture](#deployment-architecture)

## Executive Summary

nopCommerce is a comprehensive open-source e-commerce platform built on **ASP.NET MVC 5** and **.NET Framework 4.5.1**. The system employs a **layered architecture** with clear separation of concerns, implementing enterprise patterns including **Repository**, **Service Layer**, **Dependency Injection**, and **Plugin Architecture**.

**Key Architectural Characteristics**:
- **Monolithic ASP.NET MVC application** with modular plugin system
- **Layered architecture**: Domain → Data → Services → Presentation
- **Entity Framework 6** for ORM and data access
- **Autofac** for dependency injection and IoC
- **Multi-store capable** supporting multiple storefronts
- **Multi-tenant ready** with store-specific data isolation
- **Horizontally scalable** with Redis distributed caching
- **Extensible** through plugin architecture without core modification

## Architectural Style

### Primary Pattern: Layered Architecture

nopCommerce follows a **strict layered architecture** with unidirectional dependencies:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  ASP.NET MVC 5 │ Controllers │ Views │ Web Framework       │
│  (Nop.Web, Nop.Admin, Nop.Web.Framework)                   │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on
┌────────────────────────┴────────────────────────────────────┐
│                     SERVICE LAYER                            │
│  Business Logic │ Workflows │ Domain Services                │
│  (Nop.Services)                                             │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on
┌────────────────────────┴────────────────────────────────────┐
│                     DATA ACCESS LAYER                        │
│  Entity Framework │ Repositories │ Mappings                  │
│  (Nop.Data)                                                 │
└────────────────────────┬────────────────────────────────────┘
                         │ depends on
┌────────────────────────┴────────────────────────────────────┐
│                     DOMAIN LAYER                             │
│  Entities │ Interfaces │ Infrastructure │ Plugins            │
│  (Nop.Core)                                                 │
└─────────────────────────────────────────────────────────────┘
```

### Secondary Patterns

**Plugin Architecture**: Modular extensions for payments, shipping, widgets, authentication
**Event-Driven**: Domain events for loose coupling (EntityInserted, EntityUpdated, EntityDeleted)
**Repository Pattern**: Data access abstraction via `IRepository<T>`
**Service Layer**: Business logic encapsulation
**Dependency Injection**: Autofac container for all dependencies

## System Context

### External Systems and Integrations

```
┌──────────────┐         ┌─────────────────────────────┐
│   Customer   │────────→│                             │
│   Browser    │         │      nopCommerce            │
└──────────────┘         │    Web Application          │
                         │                             │
┌──────────────┐         │  ASP.NET MVC 5 + Plugins   │
│    Admin     │────────→│                             │
│   Browser    │         └──────────┬──────────────────┘
└──────────────┘                    │
                                    ├─→ SQL Server Database
                                    ├─→ Redis Cache (optional)
                                    ├─→ Payment Gateways (PayPal, etc.)
                                    ├─→ Shipping APIs (UPS, FedEx, USPS)
                                    ├─→ SMTP Email Server
                                    ├─→ OAuth Providers (Facebook, Google)
                                    └─→ File System (uploads, themes)
```

### User Types

1. **Anonymous Visitors**: Browse catalog, view products
2. **Registered Customers**: Place orders, manage account, reviews
3. **Administrators**: Full system management
4. **Vendors**: Manage own products (multi-vendor mode)

## Layered Architecture

### Layer 1: Domain Layer (Nop.Core)

**Purpose**: Foundation layer containing domain model and core abstractions

**Responsibilities**:
- Define domain entities (Product, Customer, Order, etc.)
- Core interfaces (`IRepository`, `IEngine`, `IWorkContext`)
- Infrastructure components (DI, caching, events, plugins)
- Common utilities and helpers
- Configuration models

**Key Components**:
- **Domain Entities** (100+ classes): Rich business objects
- **Plugin System**: `IPlugin` interface and discovery
- **Caching Abstractions**: `ICacheManager` interface
- **Event System**: `IEventPublisher`, `IConsumer<T>`
- **IoC Engine**: `IEngine`, `EngineContext`
- **Context Abstractions**: `IWorkContext`, `IStoreContext`

**No Dependencies**: Only external packages (Autofac, AutoMapper, Redis, ASP.NET MVC)

### Layer 2: Data Access Layer (Nop.Data)

**Purpose**: Encapsulate all database access using Entity Framework

**Responsibilities**:
- Implement repository pattern
- Entity Framework DbContext configuration
- Fluent API entity mappings
- Database initialization and migrations
- SQL query execution

**Key Components**:
- **NopObjectContext**: Entity Framework `DbContext`
- **EfRepository<T>**: Generic repository implementation
- **Entity Mappings**: Fluent API configurations for all entities
- **Data Providers**: SQL Server and SQL Server Compact support

**Dependencies**: Nop.Core + Entity Framework 6.1.3

**Pattern Implementation**:
```csharp
// Repository provides abstraction over EF
IRepository<Product> productRepository;
var product = productRepository.GetById(id);
var products = productRepository.Table
    .Where(p => p.Published)
    .ToList();
```

### Layer 3: Service Layer (Nop.Services)

**Purpose**: Business logic and workflow orchestration

**Responsibilities**:
- Implement business rules and validations
- Coordinate workflows across multiple entities
- Integrate with external services
- Handle domain events
- Caching strategy implementation
- Transaction management

**Key Service Categories**:

**Catalog Services**:
- `IProductService` - Product management, search, filtering
- `ICategoryService` - Category hierarchy
- `IManufacturerService` - Brand management
- `IPriceCalculationService` - Complex pricing logic
- `IInventoryService` - Stock management

**Customer Services**:
- `ICustomerService` - Customer CRUD
- `ICustomerRegistrationService` - Registration, authentication
- `IPermissionService` - Authorization

**Order Services**:
- `IOrderService` - Order management
- `IOrderProcessingService` - Order workflow (place, cancel, refund, void)
- `IShoppingCartService` - Cart operations
- `IOrderTotalCalculationService` - Totals calculation

**Payment/Shipping**:
- `IPaymentService` - Payment coordination
- `IShippingService` - Shipping rate calculation

**Content Services**:
- Blogs, News, Forums, Polls, Topics

**Cross-Cutting Services**:
- `ILocalizationService` - Multi-language support
- `IWorkflowMessageService` - Transactional emails
- `ILogger` - Logging
- `ISettingService` - Configuration

**Dependencies**: Nop.Core + Nop.Data

**Service Pattern**:
```csharp
public class ProductService : IProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly ICacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    
    public virtual void InsertProduct(Product product)
    {
        _productRepository.Insert(product);
        _cacheManager.RemoveByPattern(CACHE_PATTERN);
        _eventPublisher.EntityInserted(product);
    }
}
```

### Layer 4: Presentation Layer (Nop.Web, Nop.Web.Framework, Nop.Admin)

**Purpose**: User interface and HTTP request handling

**Components**:

**Nop.Web.Framework** (Shared Infrastructure):
- Base controllers (`BasePublicController`)
- Custom MVC filters and attributes
- HTML helpers and UI components
- Routing infrastructure
- Theme engine
- Security filters
- Localization helpers

**Nop.Web** (Public Storefront):
- Customer-facing controllers (Catalog, Product, Customer, Checkout, Order)
- Razor views and layouts
- View models
- JavaScript and CSS assets
- Themes (default, custom)

**Nop.Admin** (Administration):
- Admin controllers
- Admin views
- Management interfaces
- Configuration pages
- Reporting dashboards

**Dependencies**: All lower layers (Core, Data, Services, Web.Framework)

**Controller Pattern**:
```csharp
public class ProductController : BasePublicController
{
    private readonly IProductService _productService;
    private readonly IWorkContext _workContext;
    
    public ActionResult ProductDetails(int productId)
    {
        var product = _productService.GetProductById(productId);
        var model = PrepareProductDetailsModel(product);
        return View(model);
    }
}
```

### Cross-Layer: Plugin System

**Purpose**: Extensibility without core modification

**Plugin Types**:
- **Payment Plugins**: Payment gateway integrations
- **Shipping Plugins**: Carrier rate calculation
- **Tax Plugins**: Tax calculation providers
- **Widget Plugins**: UI component injection
- **Discount Rules**: Complex discount conditions
- **External Auth**: OAuth/OpenID providers
- **Exchange Rate**: Currency rate providers

**Plugin Architecture**:
```
Plugin
  → Implements: IPlugin (Nop.Core)
  → May implement: IPaymentMethod, IShippingRateComputationMethod, etc.
  → Uses: Nop.Services (business logic)
  → Contains: Controllers, Views, Models (if UI components)
```

**Discovery**: Automatic scanning of `~/Plugins/` directory on startup

## Component Interaction

### Request Flow: Customer Places Order

```
1. Browser → CheckoutController.Confirm (HTTP POST)
   └→ Controller validates request

2. CheckoutController → IOrderProcessingService.PlaceOrder()
   └→ Service Layer orchestrates workflow

3. IOrderProcessingService:
   ├→ IShoppingCartService.GetShoppingCart() - Get cart items
   ├→ IOrderTotalCalculationService.GetShoppingCartTotal() - Calculate total
   ├→ IPaymentService.ProcessPayment() - Process payment
   │  └→ Plugin (e.g., PayPalPlugin).ProcessPayment()
   │     └→ External PayPal API call
   ├→ IOrderService.InsertOrder() - Create order
   │  └→ IRepository<Order>.Insert() - Persist to database
   │     └→ DbContext.SaveChanges() - Entity Framework
   ├→ IShoppingCartService.DeleteShoppingCartItem() - Clear cart
   ├→ IWorkflowMessageService.SendOrderPlacedCustomerNotification()
   │  └→ IQueuedEmailService.InsertQueuedEmail() - Queue email
   └→ IEventPublisher.EntityInserted(order) - Publish event
      └→ Event consumers react (logging, search index update, etc.)

4. CheckoutController ← Returns success/failure result
   └→ Redirects to Order Confirmation page

5. Background Task (ScheduleTask):
   └→ IQueuedEmailService.SendQueuedEmails() - Process email queue
      └→ SMTP Server - Send emails
```

### Dependency Injection Flow

```
Application Start:
1. NopEngine.Initialize()
   └→ Scans assemblies for IDependencyRegistrar implementations
      └→ Each registrar registers its dependencies
         └→ ContainerManager (Autofac) builds dependency graph

Per Request:
1. MVC Framework creates controller
   └→ Autofac resolves controller type
      └→ Resolves constructor dependencies
         └→ IProductService
            └→ IRepository<Product>
               └→ IDbContext
         └→ IWorkContext
         └→ ICacheManager
      └→ Creates controller instance with resolved dependencies

2. Controller action executes
   └→ Uses injected services

3. Request ends
   └→ Autofac disposes InstancePerLifetimeScope objects
```

### Caching Strategy

**Multi-Level Caching**:

1. **Per-Request Cache** (`PerRequestCacheManager`):
   - Scoped to single HTTP request
   - Prevents duplicate database calls in one request
   
2. **Application Memory Cache** (`MemoryCacheManager`):
   - In-process cache using `System.Runtime.Caching`
   - Fast but not shared across web servers
   
3. **Distributed Redis Cache** (`RedisCacheManager`):
   - Shared across multiple web servers
   - Horizontal scalability
   - Persistent cache (survives restarts)

**Cache Usage Pattern**:
```csharp
public Product GetProductById(int id)
{
    string key = $"product-{id}";
    return _cacheManager.Get(key, () => 
    {
        // Only executes on cache miss
        return _repository.GetById(id);
    });
}
```

### Event-Driven Architecture

**Domain Events**:
- `EntityInserted<T>` - Entity created
- `EntityUpdated<T>` - Entity modified
- `EntityDeleted<T>` - Entity removed

**Event Flow**:
```
Service calls IRepository.Insert(entity)
   └→ Repository saves to database
      └→ IEventPublisher.EntityInserted(entity)
         └→ All IConsumer<EntityInserted<T>> implementations invoked
            ├→ CacheEventConsumer - Invalidate cache
            ├→ SearchIndexEventConsumer - Update search index
            └→ Custom plugin consumers
```

**Benefits**:
- Loose coupling between components
- Extensibility via plugin event consumers
- Separation of concerns

## Data Flow

### Read Operation Flow

```
User Request
   ↓
Controller Action
   ↓
Service Layer (IProductService.GetProductById)
   ↓
Check Cache (ICacheManager)
   ├─ Hit → Return cached object
   └─ Miss ↓
Data Layer (IRepository<Product>.GetById)
   ↓
Entity Framework (DbContext.Products.Find)
   ↓
SQL Server Database
   ↓
Entity returned
   ↓
Cache updated
   ↓
Response to user
```

### Write Operation Flow

```
User Request (POST)
   ↓
Controller Action (with validation)
   ↓
Service Layer (IProductService.UpdateProduct)
   ├→ Business rule validation
   ├→ Data Layer (IRepository<Product>.Update)
   │    ↓
   │  Entity Framework (DbContext.SaveChanges)
   │    ↓
   │  SQL Server Database (UPDATE statement)
   ├→ Cache invalidation (_cacheManager.RemoveByPattern)
   └→ Event publishing (_eventPublisher.EntityUpdated)
        ↓
      Event Consumers notified
   ↓
Response to user
```

## Deployment Architecture

### Single Server Deployment

```
┌─────────────────────────────────────────┐
│         Windows Server                   │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  IIS / IIS Express                 │ │
│  │  ┌──────────────────────────────┐  │ │
│  │  │  nopCommerce Web App         │  │ │
│  │  │  (ASP.NET MVC)               │  │ │
│  │  └──────────────────────────────┘  │ │
│  └────────────────────────────────────┘ │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  SQL Server / SQL Server Express   │ │
│  └────────────────────────────────────┘ │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │  File System                       │ │
│  │  - Uploaded files                  │ │
│  │  - Themes                          │ │
│  │  - Plugins                         │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

### Scaled Deployment (Multiple Web Servers)

```
                    ┌──────────────┐
                    │ Load Balancer │
                    └───────┬────────┘
            ┌──────────────┼──────────────┐
            ↓              ↓              ↓
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │ Web      │    │ Web      │    │ Web      │
    │ Server 1 │    │ Server 2 │    │ Server N │
    │ (IIS +   │    │ (IIS +   │    │ (IIS +   │
    │ nopCom)  │    │ nopCom)  │    │ nopCom)  │
    └────┬─────┘    └────┬─────┘    └────┬─────┘
         │               │               │
         └───────────────┼───────────────┘
                         ↓
              ┌─────────────────────┐
              │ Redis Cache Cluster │
              │ (Distributed Cache) │
              └─────────────────────┘
                         ↓
              ┌─────────────────────┐
              │   SQL Server        │
              │   (Primary DB)      │
              └─────────────────────┘
                         ↓
              ┌─────────────────────┐
              │ Shared File Storage │
              │ (NAS / Azure Files) │
              └─────────────────────┘
```

**Horizontal Scaling Requirements**:
- Redis for distributed caching (session state, cache)
- Shared file storage for uploads and plugins
- Database connection pooling
- Load balancer with sticky sessions (if not using Redis session state)

### Cloud Deployment (Azure Example)

```
Azure App Service (Web Apps)
   ├→ nopCommerce Web Application
   └→ Automatic scaling

Azure SQL Database
   └→ Managed SQL Server

Azure Redis Cache
   └→ Distributed caching

Azure Blob Storage
   └→ File uploads, themes

Azure CDN
   └→ Static assets (CSS, JS, images)

SendGrid / Azure Communication Services
   └→ Email delivery
```

## Summary

### Architectural Strengths

1. **Clear Separation of Concerns**: Each layer has distinct responsibility
2. **Loose Coupling**: Interface-based design with DI
3. **Extensibility**: Plugin architecture allows customization
4. **Scalability**: Supports horizontal scaling with distributed cache
5. **Maintainability**: Well-organized code structure
6. **Testability**: Dependencies can be mocked
7. **Multi-Store Support**: Single installation, multiple storefronts
8. **Multi-Language**: Comprehensive localization
9. **Event-Driven**: Loose coupling via domain events
10. **Rich Domain Model**: Comprehensive e-commerce entities

### Architectural Considerations

1. **Monolithic Architecture**: All components in single application (vs. microservices)
2. **Framework Version**: .NET Framework 4.5.1 is outdated
3. **Tight ORM Coupling**: Entity Framework deeply integrated
4. **Session State**: Requires sticky sessions or Redis for multi-server
5. **Plugin Deployment**: Requires application restart for plugin changes
6. **File Storage**: Local file system limits cloud scaling (without shared storage)

---

**Related Documentation:**
- [Components Documentation](components.md)
- [Patterns Documentation](patterns.md)
- [Dependencies Documentation](dependencies.md)
- [Technical Debt Report](../technical-debt-report.md)
