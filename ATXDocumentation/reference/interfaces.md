# nopCommerce Interfaces Reference

## Table of Contents
- [Overview](#overview)
- [Core Infrastructure Interfaces](#core-infrastructure-interfaces)
- [Data Access Interfaces](#data-access-interfaces)
- [Service Layer Interfaces](#service-layer-interfaces)
- [Plugin Interfaces](#plugin-interfaces)
- [Domain Marker Interfaces](#domain-marker-interfaces)
- [Web Framework Interfaces](#web-framework-interfaces)

## Overview

nopCommerce uses interface-based design extensively to support dependency injection, testability, and extensibility. This document catalogs all major public interfaces in the system organized by functional area.

## Core Infrastructure Interfaces

### IEngine
**Namespace**: `Nop.Core.Infrastructure`  
**Purpose**: Central service locator and IoC container access point

```csharp
public interface IEngine
{
    ContainerManager ContainerManager { get; }
    void Initialize(NopConfig config);
    T Resolve<T>() where T : class;
    object Resolve(Type type);
    T[] ResolveAll<T>();
}
```

**Usage**: Accessed via `EngineContext.Current`

### ITypeFinder
**Namespace**: `Nop.Core.Infrastructure`  
**Purpose**: Discover types in loaded assemblies

```csharp
public interface ITypeFinder
{
    IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses = true);
    IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses = true);
    IList<Assembly> GetAssemblies();
}
```

**Implementations**:
- `WebAppTypeFinder` - Web application assembly scanning
- `AppDomainTypeFinder` - Full AppDomain scanning

### IStartupTask
**Namespace**: `Nop.Core.Infrastructure`  
**Purpose**: Tasks executed during application startup

```csharp
public interface IStartupTask 
{
    void Execute();
    int Order { get; }
}
```

**Use Cases**:
- Plugin initialization
- Cache warming
- Database migrations
- Configuration validation

### IWorkContext
**Namespace**: `Nop.Core`  
**Purpose**: Access current request context (customer, language, currency)

```csharp
public interface IWorkContext
{
    Customer CurrentCustomer { get; set; }
    Customer OriginalCustomerIfImpersonated { get; }
    Vendor CurrentVendor { get; }
    Language WorkingLanguage { get; set; }
    Currency WorkingCurrency { get; set; }
    TaxDisplayType TaxDisplayType { get; set; }
    bool IsAdmin { get; set; }
}
```

**Critical Interface**: Used throughout the application to access current context

### IStoreContext
**Namespace**: `Nop.Core`  
**Purpose**: Access current store in multi-store scenario

```csharp
public interface IStoreContext
{
    Store CurrentStore { get; }
}
```

### IWebHelper
**Namespace**: `Nop.Core`  
**Purpose**: Web-related helper functions

```csharp
public interface IWebHelper
{
    string GetUrlReferrer();
    string GetCurrentIpAddress();
    string GetThisPageUrl(bool includeQueryString);
    bool IsCurrentConnectionSecured();
    string ServerVariables(string name);
    bool IsStaticResource(HttpRequest request);
    string ModifyQueryString(string url, string queryStringModification, string anchor);
    string RemoveQueryString(string url, string queryString);
    T QueryString<T>(string name);
    void RestartAppDomain(bool makeRedirect = false, string redirectUrl = "");
    bool IsRequestBeingRedirected { get; }
    bool IsPostBeingDone { get; set; }
}
```

## Data Access Interfaces

### IRepository<T>
**Namespace**: `Nop.Core.Data`  
**Purpose**: Generic repository pattern for data access

```csharp
public interface IRepository<T> where T : BaseEntity
{
    // Query
    T GetById(object id);
    IQueryable<T> Table { get; }
    IQueryable<T> TableNoTracking { get; }
    
    // Write
    void Insert(T entity);
    void Insert(IEnumerable<T> entities);
    void Update(T entity);
    void Update(IEnumerable<T> entities);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);
}
```

**Key Features**:
- Generic constraint to `BaseEntity`
- Lazy loading via `Table` property
- No-tracking queries for performance
- Bulk operations support

### IDbContext
**Namespace**: `Nop.Data`  
**Purpose**: Database context abstraction

```csharp
public interface IDbContext
{
    IDbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity;
    int SaveChanges();
    IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) 
        where TEntity : BaseEntity, new();
    IEnumerable<TEntity> SqlQuery<TEntity>(string sql, params object[] parameters);
    int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters);
    void Detach(object entity);
}
```

### IDataProvider
**Namespace**: `Nop.Core.Data`  
**Purpose**: Database provider abstraction (SQL Server, SQL CE)

```csharp
public interface IDataProvider
{
    void InitDatabase();
    void SetDatabaseInitializer();
    bool StoredProceduredSupported { get; }
    bool BackupSupported { get; }
    int SupportedLengthOfBinaryHash();
}
```

**Implementations**:
- `SqlServerDataProvider`
- `SqlCeDataProvider`

## Service Layer Interfaces

### Catalog Services

#### IProductService
**Namespace**: `Nop.Services.Catalog`  
**Purpose**: Product management operations

**Key Methods** (565 total lines):
```csharp
public interface IProductService
{
    // CRUD
    Product GetProductById(int productId);
    IList<Product> GetProductsByIds(int[] productIds);
    void InsertProduct(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
    
    // Search and filtering
    IPagedList<Product> SearchProducts(
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        IList<int> categoryIds = null,
        IList<int> manufacturerIds = null,
        int storeId = 0,
        int vendorId = 0,
        int warehouseId = 0,
        ProductType? productType = null,
        bool visibleIndividuallyOnly = false,
        bool markedAsNewOnly = false,
        bool? featuredProducts = null,
        decimal? priceMin = null,
        decimal? priceMax = null,
        int productTagId = 0,
        string keywords = null,
        bool searchDescriptions = false,
        bool searchSku = true,
        bool searchProductTags = false,
        int languageId = 0,
        IList<int> filteredSpecs = null,
        ProductSortingEnum orderBy = ProductSortingEnum.Position,
        bool showHidden = false,
        bool? overridePublished = null);
    
    // Business operations
    IList<Product> GetAllProductsDisplayedOnHomePage();
    IList<Product> GetProductsByProductAtributeId(int productAttributeId);
    IList<Product> GetLowStockProducts();
    IList<Product> GetAssociatedProducts(int parentGroupedProductId);
    Product GetProductBySku(string sku);
    void UpdateProductReviewTotals(Product product);
    void UpdateHasDiscountsApplied(Product product);
    
    // Related products
    IList<RelatedProduct> GetRelatedProductsByProductId(int productId);
    void InsertRelatedProduct(RelatedProduct relatedProduct);
    void DeleteRelatedProduct(RelatedProduct relatedProduct);
    
    // Cross-sell products
    IList<CrossSellProduct> GetCrossSellProductsByProductIds(int[] productIds);
    void InsertCrossSellProduct(CrossSellProduct crossSellProduct);
    void DeleteCrossSellProduct(CrossSellProduct crossSellProduct);
    
    // Product reviews
    IPagedList<ProductReview> GetAllProductReviews(int customerId, bool? approved, ...);
    ProductReview GetProductReviewById(int productReviewId);
    void DeleteProductReview(ProductReview productReview);
    
    // Tier prices
    IList<TierPrice> GetTierPricesByProduct(int productId, int storeId = 0);
    void InsertTierPrice(TierPrice tierPrice);
    void UpdateTierPrice(TierPrice tierPrice);
    void DeleteTierPrice(TierPrice tierPrice);
    
    // Product attributes
    IList<ProductAttributeMapping> GetProductAttributeMappingsByProductId(int productId);
    ProductAttributeMapping GetProductAttributeMappingById(int productAttributeMappingId);
    void InsertProductAttributeMapping(ProductAttributeMapping productAttributeMapping);
    void UpdateProductAttributeMapping(ProductAttributeMapping productAttributeMapping);
    void DeleteProductAttributeMapping(ProductAttributeMapping productAttributeMapping);
    
    // Product attribute values
    IList<ProductAttributeValue> GetProductAttributeValues(int productAttributeMappingId);
    ProductAttributeValue GetProductAttributeValueById(int productAttributeValueId);
    void InsertProductAttributeValue(ProductAttributeValue productAttributeValue);
    void UpdateProductAttributeValue(ProductAttributeValue productAttributeValue);
    void DeleteProductAttributeValue(ProductAttributeValue productAttributeValue);
    
    // Product attribute combinations
    IList<ProductAttributeCombination> GetAllProductAttributeCombinations(int productId);
    ProductAttributeCombination GetProductAttributeCombinationById(int productAttributeCombinationId);
    void InsertProductAttributeCombination(ProductAttributeCombination combination);
    void UpdateProductAttributeCombination(ProductAttributeCombination combination);
    void DeleteProductAttributeCombination(ProductAttributeCombination combination);
    
    // Manufacturer mappings
    IList<ProductManufacturer> GetProductManufacturersByProductId(int productId);
    ProductManufacturer GetProductManufacturerById(int productManufacturerId);
    void InsertProductManufacturer(ProductManufacturer productManufacturer);
    void UpdateProductManufacturer(ProductManufacturer productManufacturer);
    void DeleteProductManufacturer(ProductManufacturer productManufacturer);
    
    // Category mappings
    IList<ProductCategory> GetProductCategoriesByProductId(int productId);
    ProductCategory GetProductCategoryById(int productCategoryId);
    void InsertProductCategory(ProductCategory productCategory);
    void UpdateProductCategory(ProductCategory productCategory);
    void DeleteProductCategory(ProductCategory productCategory);
    
    // Pictures
    IList<ProductPicture> GetProductPicturesByProductId(int productId);
    ProductPicture GetProductPictureById(int productPictureId);
    void InsertProductPicture(ProductPicture productPicture);
    void UpdateProductPicture(ProductPicture productPicture);
    void DeleteProductPicture(ProductPicture productPicture);
    
    // Product tags
    void DeleteProductTag(ProductTag productTag);
    IPagedList<ProductTag> GetAllProductTags(int pageIndex = 0, int pageSize = int.MaxValue);
    ProductTag GetProductTagById(int productTagId);
    ProductTag GetProductTagByName(string name);
    void InsertProductTag(ProductTag productTag);
    void UpdateProductTag(ProductTag productTag);
    int GetProductCount(int productTagId, int storeId = 0);
    
    // Inventory
    int GetTotalStockQuantity(Product product);
    IList<ProductWarehouseInventory> GetAllProductWarehouseInventoryRecords(int productId);
}
```

#### ICategoryService
**Namespace**: `Nop.Services.Catalog`  

```csharp
public interface ICategoryService
{
    void DeleteCategory(Category category);
    IList<Category> GetAllCategories(string categoryName = "", int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    IList<Category> GetAllCategoriesDisplayedOnHomePage();
    Category GetCategoryById(int categoryId);
    void InsertCategory(Category category);
    void UpdateCategory(Category category);
    void DeleteProductCategory(ProductCategory productCategory);
    IPagedList<ProductCategory> GetProductCategoriesByProductId(int productId, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    IPagedList<ProductCategory> GetProductCategoriesByCategoryId(int categoryId, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    ProductCategory GetProductCategoryById(int productCategoryId);
    void InsertProductCategory(ProductCategory productCategory);
    void UpdateProductCategory(ProductCategory productCategory);
}
```

#### IManufacturerService
**Namespace**: `Nop.Services.Catalog`  

```csharp
public interface IManufacturerService
{
    void DeleteManufacturer(Manufacturer manufacturer);
    IPagedList<Manufacturer> GetAllManufacturers(string manufacturerName = "", int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    Manufacturer GetManufacturerById(int manufacturerId);
    void InsertManufacturer(Manufacturer manufacturer);
    void UpdateManufacturer(Manufacturer manufacturer);
    IList<ProductManufacturer> GetProductManufacturersByManufacturerId(int manufacturerId, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    ProductManufacturer GetProductManufacturerById(int productManufacturerId);
    void InsertProductManufacturer(ProductManufacturer productManufacturer);
    void UpdateProductManufacturer(ProductManufacturer productManufacturer);
    void DeleteProductManufacturer(ProductManufacturer productManufacturer);
}
```

#### IPriceCalculationService
**Namespace**: `Nop.Services.Catalog`  
**Purpose**: Calculate product prices with discounts, tier prices, attributes

```csharp
public interface IPriceCalculationService
{
    decimal GetFinalPrice(Product product, Customer customer, decimal additionalCharge = 0, bool includeDiscounts = true, int quantity = 1);
    decimal GetUnitPrice(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true);
    decimal GetSubTotal(ShoppingCartItem shoppingCartItem, bool includeDiscounts = true, out decimal discountAmount, out Discount appliedDiscount);
}
```

### Customer Services

#### ICustomerService
**Namespace**: `Nop.Services.Customers`  

```csharp
public interface ICustomerService
{
    IPagedList<Customer> GetAllCustomers(DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int affiliateId = 0, int vendorId = 0, int[] customerRoleIds = null, string email = null, string username = null, string firstName = null, string lastName = null, int dayOfBirth = 0, int monthOfBirth = 0, string company = null, string phone = null, string zipPostalCode = null, bool loadOnlyWithShoppingCart = false, ShoppingCartType? sct = null, int pageIndex = 0, int pageSize = int.MaxValue);
    IPagedList<Customer> GetOnlineCustomers(DateTime lastActivityFromUtc, int[] customerRoleIds, int pageIndex = 0, int pageSize = int.MaxValue);
    void DeleteCustomer(Customer customer);
    Customer GetCustomerById(int customerId);
    IList<Customer> GetCustomersByIds(int[] customerIds);
    Customer GetCustomerByGuid(Guid customerGuid);
    Customer GetCustomerByEmail(string email);
    Customer GetCustomerBySystemName(string systemName);
    Customer GetCustomerByUsername(string username);
    Customer InsertGuestCustomer();
    void InsertCustomer(Customer customer);
    void UpdateCustomer(Customer customer);
    void ResetCheckoutData(Customer customer, int storeId, bool clearCouponCodes = false, bool clearCheckoutAttributes = false, bool clearRewardPoints = true, bool clearShippingMethod = true, bool clearPaymentMethod = true);
    void DeleteGuestCustomers(DateTime? createdFromUtc, DateTime? createdToUtc, bool onlyWithoutShoppingCart);
    
    // Customer roles
    void DeleteCustomerRole(CustomerRole customerRole);
    CustomerRole GetCustomerRoleById(int customerRoleId);
    CustomerRole GetCustomerRoleBySystemName(string systemName);
    IList<CustomerRole> GetAllCustomerRoles(bool showHidden = false);
    void InsertCustomerRole(CustomerRole customerRole);
    void UpdateCustomerRole(CustomerRole customerRole);
}
```

#### ICustomerRegistrationService
**Namespace**: `Nop.Services.Customers`  

```csharp
public interface ICustomerRegistrationService
{
    CustomerLoginResults ValidateCustomer(string usernameOrEmail, string password);
    CustomerRegistrationResult RegisterCustomer(CustomerRegistrationRequest request);
    ChangePasswordResult ChangePassword(ChangePasswordRequest request);
    void SetEmail(Customer customer, string newEmail);
    void SetUsername(Customer customer, string newUsername);
    PasswordChangeResult ValidateCustomer(Customer customer);
}
```

### Order Services

#### IOrderService
**Namespace**: `Nop.Services.Orders`  

```csharp
public interface IOrderService
{
    IPagedList<Order> SearchOrders(int storeId = 0, int vendorId = 0, int customerId = 0, int productId = 0, int affiliateId = 0, int warehouseId = 0, int billingCountryId = 0, string paymentMethodSystemName = null, DateTime? createdFromUtc = null, DateTime? createdToUtc = null, int[] osIds = null, int[] psIds = null, int[] ssIds = null, string billingEmail = null, string orderNotes = null, string orderGuid = null, int pageIndex = 0, int pageSize = int.MaxValue);
    Order GetOrderById(int orderId);
    Order GetOrderByGuid(Guid orderGuid);
    void DeleteOrder(Order order);
    void InsertOrder(Order order);
    void UpdateOrder(Order order);
    
    IList<Order> GetOrdersByCustomerId(int customerId);
    Order GetOrderByCustomOrderNumber(string customOrderNumber);
    
    // Order notes
    OrderNote GetOrderNoteById(int orderNoteId);
    void DeleteOrderNote(OrderNote orderNote);
    void InsertOrderNote(OrderNote orderNote);
    
    // Recurring payments
    RecurringPayment GetRecurringPaymentById(int recurringPaymentId);
    void InsertRecurringPayment(RecurringPayment recurringPayment);
    void UpdateRecurringPayment(RecurringPayment recurringPayment);
    void DeleteRecurringPayment(RecurringPayment recurringPayment);
    IPagedList<RecurringPayment> SearchRecurringPayments(int storeId = 0, int customerId = 0, int initialOrderId = 0, int? initialOrderStatus = null, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
}
```

#### IOrderProcessingService
**Namespace**: `Nop.Services.Orders`  
**Purpose**: Order workflow and business logic

```csharp
public interface IOrderProcessingService
{
    PlaceOrderResult PlaceOrder(ProcessPaymentRequest processPaymentRequest);
    bool CanCancelOrder(Order order);
    void CancelOrder(Order order, bool notifyCustomer);
    bool CanMarkOrderAsPaid(Order order);
    void MarkOrderAsPaid(Order order);
    bool CanRefund(Order order);
    void Refund(Order order);
    bool CanRefundOffline(Order order);
    void RefundOffline(Order order);
    bool CanPartiallyRefund(Order order, decimal amountToRefund);
    void PartiallyRefund(Order order, decimal amountToRefund);
    bool CanPartiallyRefundOffline(Order order, decimal amountToRefund);
    void PartiallyRefundOffline(Order order, decimal amountToRefund);
    bool CanVoid(Order order);
    void Void(Order order);
    bool CanVoidOffline(Order order);
    void VoidOffline(Order order);
    bool CanCapture(Order order);
    void Capture(Order order);
    bool CanMarkOrderAsAuthorized(Order order);
    void MarkAsAuthorized(Order order);
    bool CanCompleteOrder(Order order);
    void CompleteOrder(Order order);
    void DeleteOrder(Order order);
    IList<string> GetOrderPlaceErrors(ProcessPaymentRequest processPaymentRequest);
}
```

#### IShoppingCartService
**Namespace**: `Nop.Services.Orders`  

```csharp
public interface IShoppingCartService
{
    void DeleteShoppingCartItem(ShoppingCartItem shoppingCartItem, bool resetCheckoutData = true, bool ensureOnlyActiveCheckoutAttributes = false);
    IList<ShoppingCartItem> GetShoppingCart(Customer customer, ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart, int storeId = 0, int? productId = null);
    IList<string> AddToCart(Customer customer, Product product, ShoppingCartType shoppingCartType, int storeId, string attributesXml = null, decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null, DateTime? rentalEndDate = null, int quantity = 1, bool addRequiredProducts = true);
    IList<string> UpdateShoppingCartItem(Customer customer, int shoppingCartItemId, string attributesXml, decimal customerEnteredPrice, DateTime? rentalStartDate = null, DateTime? rentalEndDate = null, int quantity = 1, bool resetCheckoutData = true);
    void MigrateShoppingCart(Customer fromCustomer, Customer toCustomer, bool includeCouponCodes);
    ShoppingCartItem FindShoppingCartItemInTheCart(IList<ShoppingCartItem> shoppingCart, ShoppingCartType shoppingCartType, Product product, string attributesXml = "", decimal customerEnteredPrice = 0, DateTime? rentalStartDate = null, DateTime? rentalEndDate = null);
}
```

### Payment Services

#### IPaymentService
**Namespace**: `Nop.Services.Payments`  

```csharp
public interface IPaymentService
{
    ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest);
    void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest);
    bool SupportCapture(string paymentMethodSystemName);
    CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest);
    bool SupportPartiallyRefund(string paymentMethodSystemName);
    bool SupportRefund(string paymentMethodSystemName);
    RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest);
    bool SupportVoid(string paymentMethodSystemName);
    VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest);
    RecurringPaymentType GetRecurringPaymentType(string paymentMethodSystemName);
    ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest);
    CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest);
    bool CanRePostProcessPayment(Order order);
    IList<IPaymentMethod> LoadActivePaymentMethods(Customer customer = null, int storeId = 0, int filterByCountryId = 0);
    IPaymentMethod LoadPaymentMethodBySystemName(string systemName);
    IList<IPaymentMethod> LoadAllPaymentMethods(int storeId = 0);
    decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart, string paymentMethodSystemName);
    bool HidePaymentMethod(IList<string> filterByAllowedCustomerRoles, IList<string> paymentMethodFilteredCustomerRoles);
}
```

#### IPaymentMethod
**Namespace**: `Nop.Services.Payments`  
**Purpose**: Implemented by payment plugins

```csharp
public interface IPaymentMethod : IPlugin
{
    ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest);
    void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest);
    bool HidePaymentMethod(IList<ShoppingCartItem> cart);
    decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart);
    CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest);
    RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest);
    VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest);
    ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest);
    CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest);
    bool CanRePostProcessPayment(Order order);
    void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);
    void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);
    Type GetControllerType();
    bool SupportCapture { get; }
    bool SupportPartiallyRefund { get; }
    bool SupportRefund { get; }
    bool SupportVoid { get; }
    RecurringPaymentType RecurringPaymentType { get; }
    PaymentMethodType PaymentMethodType { get; }
    bool SkipPaymentInfo { get; }
    string PaymentMethodDescription { get; }
}
```

### Shipping Services

#### IShippingService
**Namespace**: `Nop.Services.Shipping`  

```csharp
public interface IShippingService
{
    GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest, bool loadAll = false);
    IShippingRateComputationMethod LoadShippingRateComputationMethodBySystemName(string systemName);
    IList<IShippingRateComputationMethod> LoadActiveShippingRateComputationMethods(Customer customer = null, int storeId = 0);
    IList<IShippingRateComputationMethod> LoadAllShippingRateComputationMethods(int storeId = 0);
    IList<ShippingMethod> GetAllShippingMethods(int? filterByCountryId = null);
    ShippingMethod GetShippingMethodById(int shippingMethodId);
    void UpdateShippingMethod(ShippingMethod shippingMethod);
    void InsertShippingMethod(ShippingMethod shippingMethod);
    void DeleteShippingMethod(ShippingMethod shippingMethod);
}
```

#### IShippingRateComputationMethod
**Namespace**: `Nop.Services.Shipping`  
**Purpose**: Implemented by shipping rate plugins

```csharp
public interface IShippingRateComputationMethod : IPlugin
{
    GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest);
    decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest);
    void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);
    ShippingRateComputationMethodType ShippingRateComputationMethodType { get; }
    IShipmentTracker ShipmentTracker { get; }
}
```

### Security Services

#### IPermissionService
**Namespace**: `Nop.Services.Security`  

```csharp
public interface IPermissionService
{
    void DeletePermissionRecord(PermissionRecord permission);
    PermissionRecord GetPermissionRecordById(int permissionId);
    PermissionRecord GetPermissionRecordBySystemName(string systemName);
    IList<PermissionRecord> GetAllPermissionRecords();
    void InsertPermissionRecord(PermissionRecord permission);
    void UpdatePermissionRecord(PermissionRecord permission);
    void InstallPermissions(IPermissionProvider permissionProvider);
    void UninstallPermissions(IPermissionProvider permissionProvider);
    bool Authorize(PermissionRecord permission);
    bool Authorize(PermissionRecord permission, Customer customer);
    bool Authorize(string permissionRecordSystemName);
    bool Authorize(string permissionRecordSystemName, Customer customer);
}
```

#### IAclService
**Namespace**: `Nop.Services.Security`  
**Purpose**: Access control list management

```csharp
public interface IAclService
{
    void DeleteAclRecord(AclRecord aclRecord);
    AclRecord GetAclRecordById(int aclRecordId);
    IList<AclRecord> GetAclRecords<T>(T entity) where T : BaseEntity, IAclSupported;
    void InsertAclRecord(AclRecord aclRecord);
    void InsertAclRecord<T>(T entity, int customerRoleId) where T : BaseEntity, IAclSupported;
    void UpdateAclRecord(AclRecord aclRecord);
    bool Authorize<T>(T entity) where T : BaseEntity, IAclSupported;
    bool Authorize<T>(T entity, Customer customer) where T : BaseEntity, IAclSupported;
}
```

#### IEncryptionService
**Namespace**: `Nop.Services.Security`  

```csharp
public interface IEncryptionService
{
    string CreateSaltKey(int size);
    string CreatePasswordHash(string password, string saltkey, string passwordFormat = "SHA1");
    string EncryptText(string plainText, string encryptionPrivateKey = "");
    string DecryptText(string cipherText, string encryptionPrivateKey = "");
}
```

### Localization Services

#### ILocalizationService
**Namespace**: `Nop.Services.Localization`  

```csharp
public interface ILocalizationService
{
    void DeleteLocaleStringResource(LocaleStringResource localeStringResource);
    LocaleStringResource GetLocaleStringResourceById(int localeStringResourceId);
    LocaleStringResource GetLocaleStringResourceByName(string resourceName);
    LocaleStringResource GetLocaleStringResourceByName(string resourceName, int languageId, bool logIfNotFound = true);
    IList<LocaleStringResource> GetAllResources(int languageId);
    void InsertLocaleStringResource(LocaleStringResource localeStringResource);
    void UpdateLocaleStringResource(LocaleStringResource localeStringResource);
    Dictionary<string, KeyValuePair<int,string>> GetAllResourceValues(int languageId);
    string GetResource(string resourceKey);
    string GetResource(string resourceKey, int languageId, bool logIfNotFound = true, string defaultValue = "", bool returnEmptyIfNotFound = false);
    string ExportResourcesToXml(Language language);
    void ImportResourcesFromXml(Language language, string xml, bool updateExistingResources = true);
}
```

### Message Services

#### IWorkflowMessageService
**Namespace**: `Nop.Services.Messages`  
**Purpose**: Send transactional emails

```csharp
public interface IWorkflowMessageService
{
    int SendCustomerRegisteredNotificationMessage(Customer customer, int languageId);
    int SendCustomerWelcomeMessage(Customer customer, int languageId);
    int SendCustomerEmailValidationMessage(Customer customer, int languageId);
    int SendCustomerPasswordRecoveryMessage(Customer customer, int languageId);
    int SendOrderPlacedCustomerNotification(Order order, int languageId);
    int SendOrderPlacedStoreOwnerNotification(Order order, int languageId);
    int SendOrderPlacedVendorNotification(Order order, Vendor vendor, int languageId);
    int SendOrderPaidCustomerNotification(Order order, int languageId);
    int SendOrderPaidStoreOwnerNotification(Order order, int languageId);
    int SendOrderPaidVendorNotification(Order order, Vendor vendor, int languageId);
    int SendShipmentSentCustomerNotification(Shipment shipment, int languageId);
    int SendShipmentDeliveredCustomerNotification(Shipment shipment, int languageId);
    int SendOrderCompletedCustomerNotification(Order order, int languageId);
    int SendOrderCancelledCustomerNotification(Order order, int languageId);
    int SendOrderRefundedCustomerNotification(Order order, decimal refundedAmount, int languageId);
    int SendNewCustomerNoteAddedCustomerNotification(OrderNote orderNote, int languageId);
    int SendRecurringPaymentCancelledCustomerNotification(RecurringPayment recurringPayment, int languageId);
    int SendNewsletterSubscriptionActivationMessage(NewsLetterSubscription subscription, int languageId);
    int SendNewsletterSubscriptionDeactivationMessage(NewsLetterSubscription subscription, int languageId);
    int SendNewVatSubmittedStoreOwnerNotification(Customer customer, string vatName, string vatAddress, int languageId);
    int SendBlogCommentNotificationMessage(BlogComment blogComment, int languageId);
    int SendNewsCommentNotificationMessage(NewsComment newsComment, int languageId);
    int SendBackInStockNotification(BackInStockSubscription subscription, int languageId);
    int SendProductReviewNotificationMessage(ProductReview productReview, int languageId);
    int SendQuantityBelowStoreOwnerNotification(Product product, int languageId);
    int SendNewReturnRequestStoreOwnerNotification(ReturnRequest returnRequest, OrderItem orderItem, int languageId);
    int SendNewForumTopicMessage(Customer customer, ForumTopic forumTopic, Forum forum, int languageId);
    int SendNewForumPostMessage(Customer customer, ForumPost forumPost, ForumTopic forumTopic, Forum forum, int friendlyForumTopicPageIndex, int languageId);
    int SendPrivateMessageNotification(PrivateMessage privateMessage, int languageId);
    int SendNewVendorAccountApplyStoreOwnerNotification(Vendor vendor, int languageId);
    int SendVendorInformationChangeNotification(Vendor vendor, int languageId);
    int SendContactUsMessage(Customer customer, string senderEmail, string senderName, string subject, string body, int languageId);
    int SendContactVendorMessage(Customer customer, Vendor vendor, string senderEmail, string senderName, string subject, string body, int languageId);
}
```

## Plugin Interfaces

### IPlugin
**Namespace**: `Nop.Core.Plugins`  
**Purpose**: Base interface for all plugins

```csharp
public interface IPlugin
{
    PluginDescriptor PluginDescriptor { get; set; }
    void Install();
    void Uninstall();
}
```

### Specialized Plugin Interfaces

#### IWidgetPlugin
**Namespace**: `Nop.Services.Cms`  

```csharp
public interface IWidgetPlugin : IPlugin
{
    IList<string> GetWidgetZones();
    void GetDisplayWidgetRoute(string widgetZone, out string actionName, out string controllerName, out RouteValueDictionary routeValues);
}
```

#### IExternalAuthenticationMethod
**Namespace**: `Nop.Services.Authentication.External`  

```csharp
public interface IExternalAuthenticationMethod : IPlugin
{
    void GetPublicInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);
}
```

#### ITaxProvider
**Namespace**: `Nop.Services.Tax`  

```csharp
public interface ITaxProvider : IPlugin
{
    CalculateTaxResult GetTaxRate(CalculateTaxRequest calculateTaxRequest);
}
```

#### IExchangeRateProvider
**Namespace**: `Nop.Services.Directory`  

```csharp
public interface IExchangeRateProvider : IPlugin
{
    IList<Core.Domain.Directory.ExchangeRate> GetCurrencyLiveRates(string exchangeRateCurrencyCode);
}
```

## Domain Marker Interfaces

### ILocalizedEntity
**Namespace**: `Nop.Core.Domain.Localization`  
**Purpose**: Marks entities that support localization

```csharp
public interface ILocalizedEntity
{
}
```

**Usage**: Property translations stored in `LocalizedProperty` table

### ISlugSupported
**Namespace**: `Nop.Core.Domain.Seo`  
**Purpose**: Marks entities that support SEO-friendly URLs

```csharp
public interface ISlugSupported
{
}
```

**Usage**: URL slugs stored in `UrlRecord` table

### IAclSupported
**Namespace**: `Nop.Core.Domain.Security`  
**Purpose**: Marks entities that support access control

```csharp
public interface IAclSupported
{
    bool SubjectToAcl { get; set; }
}
```

**Usage**: ACL records stored in `AclRecord` table

### IStoreMappingSupported
**Namespace**: `Nop.Core.Domain.Stores`  
**Purpose**: Marks entities available in specific stores

```csharp
public interface IStoreMappingSupported
{
    bool LimitedToStores { get; set; }
}
```

**Usage**: Store mappings in `StoreMapping` table

## Web Framework Interfaces

### ICacheManager
**Namespace**: `Nop.Core.Caching`  

```csharp
public interface ICacheManager
{
    T Get<T>(string key);
    T Get<T>(string key, Func<T> acquire);
    T Get<T>(string key, int cacheTime, Func<T> acquire);
    void Set(string key, object data, int cacheTime);
    bool IsSet(string key);
    void Remove(string key);
    void RemoveByPattern(string pattern);
    void Clear();
}
```

**Implementations**:
- `MemoryCacheManager` - In-memory cache
- `RedisCacheManager` - Distributed Redis cache
- `PerRequestCacheManager` - Request-scoped cache

### IEventPublisher
**Namespace**: `Nop.Services.Events`  

```csharp
public interface IEventPublisher
{
    void Publish<T>(T eventMessage);
}
```

### IConsumer<T>
**Namespace**: `Nop.Services.Events`  
**Purpose**: Handle domain events

```csharp
public interface IConsumer<T>
{
    void HandleEvent(T eventMessage);
}
```

**Common Event Types**:
- `EntityInserted<T>` - Entity created
- `EntityUpdated<T>` - Entity modified
- `EntityDeleted<T>` - Entity removed

## Summary

The nopCommerce interface design demonstrates:

1. **Interface Segregation**: Focused, single-purpose interfaces
2. **Dependency Injection**: All major components have interfaces
3. **Plugin Extensibility**: Well-defined plugin contracts
4. **Marker Interfaces**: Cross-cutting concerns (localization, SEO, ACL)
5. **Service Layer Abstraction**: Business logic behind interfaces
6. **Repository Pattern**: Generic data access interface
7. **Event-Driven**: Publisher-consumer pattern for domain events
8. **Testability**: All dependencies can be mocked
9. **Framework Independence**: Core domain isolated from infrastructure
10. **Open/Closed Principle**: Extensible via plugins without modification

This comprehensive interface structure enables the flexible, maintainable, and testable architecture of nopCommerce.

---

**Related Documentation:**
- [Program Structure](program-structure.md)
- [Data Models](data-models.md)
- [Architecture Patterns](../architecture/patterns.md)
