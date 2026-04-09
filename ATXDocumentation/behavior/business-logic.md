# nopCommerce Business Logic Documentation

## Overview

This document captures the core business logic embedded within the nopCommerce e-commerce platform. The business rules span multiple domains including catalog management, customer management, order processing, pricing, discounts, inventory, and payment processing.

## Table of Contents
- [Catalog Business Logic](#catalog-business-logic)
- [Customer Management Logic](#customer-management-logic)
- [Order Processing Logic](#order-processing-logic)
- [Pricing and Discount Logic](#pricing-and-discount-logic)
- [Inventory Management Logic](#inventory-management-logic)
- [Payment Processing Logic](#payment-processing-logic)
- [Shipping Logic](#shipping-logic)
- [Tax Calculation Logic](#tax-calculation-logic)
- [Security and Access Control Logic](#security-and-access-control-logic)

---

## Catalog Business Logic

### Product Visibility Rules

**Business Rule**: Products are visible to customers based on multiple conditions
- **Published Status**: `Product.Published` must be `true`
- **Date Range**: Current date must be within `AvailableStartDateTimeUtc` and `AvailableEndDateTimeUtc` (if set)
- **ACL**: Customer's role must be in product's allowed roles (if `SubjectToAcl` is true)
- **Store Mapping**: Product must be available in current store (if `LimitedToStores` is true)
- **Deleted Flag**: `Product.Deleted` must be `false`

**Location**: `IProductService.SearchProducts()` method applies these filters

### Product Inventory Rules

**Business Rule**: Product availability depends on inventory tracking settings

**For Simple Products**:
- If `ManageInventoryMethod == ManageInventoryMethod.DontManageStock`: Always available
- If `ManageInventoryMethod == ManageInventoryMethod.ManageStock`:
  - Check `StockQuantity` against minimum stock level
  - If `AllowBackInStockSubscriptions`: Allow customers to subscribe when out of stock
  - If `DisplayStockAvailability`: Show stock quantity to customers
  - Honor `MinStockQuantity` and `NotifyAdminForQuantityBelow` thresholds

**For Grouped Products**:
- Availability determined by associated products
- Each associated product follows its own inventory rules

**Location**: 
- `Nop.Services.Catalog.ProductService`
- `Nop.Services.Catalog.IProductAttributeParser`

### Category Assignment Logic

**Business Rule**: Products can belong to multiple categories with ordering
- Products linked to categories via `ProductCategory` mapping
- Each mapping has `DisplayOrder` property for sorting within category
- Each mapping has `IsFeaturedProduct` flag for promotion
- Deleting a category does NOT delete products (only mapping)
- Category visibility follows same ACL and store mapping rules as products

**Location**: `Nop.Services.Catalog.CategoryService`

### Product Attribute Combinations

**Business Rule**: Product variants are managed through attribute combinations
- Each combination can have unique SKU, price adjustment, stock quantity
- `AttributesXml` stores selected attribute values in XML format
- Combinations can be marked as "not available" (`NotAvailableAttributeCombinationIfNotAvailable`)
- Price adjustments are additive to base product price
- Stock is tracked per combination when enabled

**Location**: `Nop.Services.Catalog.ProductAttributeParser`

---

## Customer Management Logic

### Customer Registration Rules

**Business Rule**: Customer registration validation requirements
- **Username uniqueness**: Must be unique across all customers (if username enabled)
- **Email uniqueness**: Must be unique across all customers
- **Password requirements**: Configurable via `CustomerSettings`
  - Minimum length
  - Require uppercase/lowercase
  - Require digits
  - Require special characters
- **Default customer roles**: New customers assigned to "Registered" role
- **Guest to registered conversion**: Guest accounts converted on registration

**Location**: `Nop.Services.Customers.CustomerRegistrationService.RegisterCustomer()`

### Customer Authentication Logic

**Business Rule**: Login validation process
1. Lookup customer by email or username
2. Verify password hash using stored salt and hash format (SHA1/SHA256/MD5)
3. Check `Customer.Deleted` flag
4. Check `Customer.Active` flag
5. Check `Customer.CannotLoginUntilDateUtc` (for temporary lockouts)
6. Apply customer roles and permissions
7. Create authentication cookie via `IAuthenticationService`

**Location**: `Nop.Services.Customers.CustomerRegistrationService.ValidateCustomer()`

### Guest Customer Logic

**Business Rule**: Anonymous browsing support
- Each anonymous user gets a `Customer` record with `IsGuest = true`
- Guest customers stored in database for cart persistence
- Guest GUID stored in cookie for identification
- Guest data migrated to registered account on registration
- Periodic cleanup job removes old guest records without shopping carts

**Location**: 
- `Nop.Services.Customers.CustomerService.InsertGuestCustomer()`
- `Nop.Services.Customers.CustomerService.DeleteGuestCustomers()`

### Customer Role Permissions

**Business Rule**: Role-based access control
- Customers can have multiple roles simultaneously
- Permissions are assigned to roles, not individual customers
- Special system roles:
  - `Administrators`: Full system access
  - `Registered`: Standard customer access
  - `Guests`: Limited anonymous access
  - `Vendors`: Multi-vendor marketplace access
- Permission check: User has permission if ANY of their roles has that permission

**Location**: `Nop.Services.Security.PermissionService.Authorize()`

---

## Order Processing Logic

### Order Placement Workflow

**Business Rule**: Complete order placement sequence

**Phase 1: Validation**
1. Validate customer (exists, active, not deleted)
2. Validate shopping cart (has items, items available, stock sufficient)
3. Validate billing/shipping addresses
4. Validate selected payment method (active, allowed for customer)
5. Validate selected shipping method (available for address, active)
6. Validate checkout attributes (if required)
7. Validate coupon codes (valid, not expired, minimum order met)
8. Calculate order totals and verify payment amount matches

**Phase 2: Payment Processing**
1. Call payment gateway via `IPaymentMethod.ProcessPayment()`
2. Handle payment result:
   - **Authorized**: Payment approved, capture later
   - **Paid**: Payment completed immediately
   - **Pending**: Awaiting external confirmation
   - **Failed**: Order cancelled, inventory restored

**Phase 3: Order Creation**
1. Generate order GUID and order number
2. Create `Order` entity with totals, addresses, customer info
3. Create `OrderItem` entities for each cart item with snapshot pricing
4. Apply discounts and create `DiscountUsageHistory` records
5. Create gift card records if applicable
6. Reduce product inventory (`StockQuantity -= quantity`)
7. Clear shopping cart items
8. Save order to database

**Phase 4: Post-Processing**
1. Send order placed notification emails (customer, admin, vendor)
2. Execute post-payment processing (redirect to payment gateway if needed)
3. Update affiliate commission records
4. Create recurring payment schedule (if applicable)
5. Publish `OrderPlacedEvent` for custom handling

**Location**: `Nop.Services.Orders.OrderProcessingService.PlaceOrder()`

### Order State Transitions

**Business Rule**: Valid order status transitions

```
Pending → Processing → Complete
         → Cancelled

Authorized → Paid → Complete
          → Cancelled
```

**Status Meanings**:
- **Pending**: Order created, payment not yet processed
- **Processing**: Payment successful, order being fulfilled
- **Complete**: Order fulfilled and delivered
- **Cancelled**: Order cancelled (before shipping)

**Payment Status Transitions**:
```
Pending → Authorized → Paid
        → Partially Refunded
        → Refunded
        → Voided
```

**Location**: `Nop.Services.Orders.OrderProcessingService` (various methods)

### Order Cancellation Rules

**Business Rule**: Orders can be cancelled under specific conditions
- Order status must be `Pending` or `Processing`
- Payment status must NOT be `Refunded` or `Voided`
- If order is not shipped, inventory is restored
- If payment was captured, refund must be processed separately
- Cancellation notifications sent to customer and admin
- Gift cards deactivated
- Recurring payments cancelled

**Location**: `Nop.Services.Orders.OrderProcessingService.CancelOrder()`

### Order Item Price Snapshot

**Business Rule**: Order items store pricing at time of purchase
- `OrderItem.UnitPriceInclTax` and `UnitPriceExclTax` frozen at order time
- `OrderItem.PriceInclTax` and `PriceExclTax` are totals (unit price × quantity)
- `OrderItem.DiscountAmountInclTax` and `DiscountAmountExclTax` recorded
- Product price changes after order do NOT affect order totals
- Enables accurate historical reporting and refund calculations

**Location**: `Nop.Services.Orders.OrderProcessingService.PlaceOrder()`

---

## Pricing and Discount Logic

### Price Calculation Algorithm

**Business Rule**: Final product price calculation sequence

1. **Start with base price**: `Product.Price` or `Product.OldPrice`
2. **Apply tier pricing**: If customer quantity meets tier threshold, use tier price
3. **Apply customer role pricing**: Special pricing for specific customer roles
4. **Apply product attributes**: Add/subtract attribute price adjustments
5. **Apply discounts**: Calculate and apply eligible discounts
6. **Apply currency conversion**: Convert to working currency
7. **Round according to settings**: Apply rounding rules

**Tier Pricing Logic**:
- Tier prices defined per quantity threshold
- Multiple tiers can exist: quantity 1-9 → $10, quantity 10-49 → $8, quantity 50+ → $7
- Customer must purchase minimum quantity to qualify
- Tier can be limited to specific customer role
- Tier can be limited to specific store

**Location**: `Nop.Services.Catalog.PriceCalculationService.GetFinalPrice()`

### Discount Application Rules

**Business Rule**: Discounts applied based on type and conditions

**Discount Types**:
1. **Assigned to Products**: Apply to specific products
2. **Assigned to Categories**: Apply to all products in category
3. **Assigned to Manufacturers**: Apply to all products by manufacturer
4. **Assigned to Shipping**: Reduce shipping cost
5. **Assigned to Order Total**: Reduce entire order
6. **Assigned to Order Subtotal**: Reduce product subtotal only

**Discount Requirements**:
- Must be active (`Discount.IsActive = true`)
- Current date within `StartDateUtc` and `EndDateUtc`
- Quantity limits not exceeded (`LimitationTimes`)
- Meets discount requirements (customer role, has product, etc.)
- Coupon code valid (if required)

**Discount Limitations**:
- `LimitationTimes`: Maximum uses across all customers
- `PerCustomerLimitation`: Maximum uses per customer
- `RequiresCouponCode`: Must enter specific code
- Discount requirements can be extended via plugins

**Location**: 
- `Nop.Services.Discounts.DiscountService`
- `Nop.Services.Orders.OrderTotalCalculationService`

### Coupon Code Logic

**Business Rule**: Coupon code validation and application
- Customer enters coupon code at checkout
- System validates code against active discounts
- Case-insensitive matching
- Code must be associated with active discount
- All discount requirements must be met
- Customer can apply multiple valid coupon codes
- Invalid codes rejected with error message
- Applied codes stored in `Customer.DiscountCouponCode` generic attribute

**Location**: `Nop.Services.Discounts.DiscountService.ValidateDiscount()`

### Gift Card Logic

**Business Rule**: Gift card purchase and redemption
- Gift cards are special product types (`IsGiftCard = true`)
- Customer specifies recipient email and message
- Gift card created upon order completion
- Gift card has unique code and initial amount
- Can be applied to future orders as payment method
- Partial redemption allowed (remaining balance tracked)
- Gift card amount deducted from order total
- Expiration date can be set
- Deactivated if order refunded/cancelled

**Location**: 
- `Nop.Services.Orders.GiftCardService`
- `Nop.Services.Orders.OrderProcessingService`

---

## Inventory Management Logic

### Stock Reduction Rules

**Business Rule**: When to reduce inventory

**Trigger Points**:
- **On Order Placement**: Default behavior, reduce immediately when order placed
- **On Shipment**: Reduce when order shipped (alternative setting)
- **Never**: Manual inventory management

**Reduction Amount**:
- Simple Product: Reduce by order quantity
- Product with Attributes: Reduce from specific attribute combination stock
- Grouped Product: Reduce from associated product inventory

**Stock Reservation**:
- Reserved but not yet reduced for authorized/pending payments
- Ensures inventory not oversold while payment processing
- Released if payment fails or order cancelled

**Location**: 
- `Nop.Services.Orders.OrderProcessingService.PlaceOrder()`
- `Nop.Services.Shipping.ShipmentService.Ship()`

### Low Stock Notifications

**Business Rule**: Alert administrators of low inventory
- Each product has `NotifyAdminForQuantityBelow` threshold
- When stock falls below threshold during order placement
- System sends email notification to administrators
- Notification includes product name, SKU, current quantity
- One notification per threshold crossing (not on every order)

**Location**: 
- `Nop.Services.Orders.OrderProcessingService` (internal notification trigger)
- `Nop.Services.Messages.WorkflowMessageService.SendQuantityBelowStoreOwnerNotification()`

### Back In Stock Subscriptions

**Business Rule**: Customer notification when product available
- Customers can subscribe to out-of-stock products
- When `StockQuantity` increases above `MinStockQuantity`
- System sends email to all subscribers
- Each subscriber notified once per subscription
- Subscription automatically deleted after notification
- Customer can unsubscribe manually

**Location**: 
- `Nop.Services.Catalog.BackInStockSubscriptionService`
- `Nop.Services.Messages.WorkflowMessageService.SendBackInStockNotification()`

### Warehouse Support

**Business Rule**: Multi-warehouse inventory tracking
- Products can be stocked in multiple warehouses
- Each warehouse has independent stock quantity
- Order fulfillment can specify warehouse
- Stock reduced from specified warehouse
- "Use multiple warehouses" setting enables feature
- Warehouse priority determines fulfillment order

**Location**: `Nop.Services.Catalog.ProductService.GetTotalStockQuantity()`

---

## Payment Processing Logic

### Payment Method Selection

**Business Rule**: Available payment methods filtered by conditions
- Payment method must be active
- Must be available in customer's country (if country restriction set)
- Must be available in current store (if store limitation set)
- Must support customer's shopping cart items
- Customer's role must be allowed (if role restriction set)
- Additional fee calculated per payment method
- Methods sorted by display order

**Location**: `Nop.Services.Payments.PaymentService.LoadActivePaymentMethods()`

### Payment Transaction Types

**Business Rule**: Different payment processing flows

**Authorize Only**:
1. Verify funds available
2. Hold amount on customer's card
3. Order status: `Authorized`
4. Admin manually captures payment later
5. Authorization can expire (typically 7-30 days)
6. Can void authorization if order cancelled

**Authorize and Capture**:
1. Verify and immediately charge customer
2. Order status: `Paid`
3. Funds transferred to merchant account
4. Can refund if needed later

**Payment Flow Determination**:
- Configured per payment method
- Depends on `PaymentMethodType` enum
- Admin can override for manual payment methods

**Location**: `Nop.Services.Payments.PaymentService.ProcessPayment()`

### Refund Processing Rules

**Business Rule**: Order refund validation and execution

**Full Refund**:
- Order payment status must be `Paid`
- Payment method must support refunds
- Refund entire order amount
- Sets payment status to `Refunded`
- Optionally restore inventory
- Send refund notification email

**Partial Refund**:
- Can refund less than order total
- Multiple partial refunds allowed
- Payment status becomes `PartiallyRefunded`
- Sum of refunds cannot exceed order total
- Track refund history per order

**Offline Refund**:
- Marks order as refunded without processing through payment gateway
- Used for manual refunds (check, cash, etc.)
- Same status transitions as online refund

**Location**: 
- `Nop.Services.Orders.OrderProcessingService.Refund()`
- `Nop.Services.Orders.OrderProcessingService.PartiallyRefund()`
- `Nop.Services.Orders.OrderProcessingService.RefundOffline()`

### Recurring Payment Logic

**Business Rule**: Subscription-based product purchases
- Products can be marked as recurring (`IsRecurring = true`)
- Define cycle: period (days/weeks/months) and length (number of payments)
- Initial order creates `RecurringPayment` schedule
- Subsequent payments processed automatically on cycle date
- Each cycle creates new order
- Customer notified of each payment
- Can be cancelled by customer or admin
- Payment failures increment failure count
- Auto-cancel after configured number of failures

**Location**: 
- `Nop.Services.Orders.OrderProcessingService.ProcessNextRecurringPayment()`
- `Nop.Services.Tasks.RecurringPaymentsTask` (scheduled task)

---

## Shipping Logic

### Shipping Method Selection

**Business Rule**: Available shipping options based on multiple factors

**Filtering Criteria**:
1. Shipping address country must be supported by method
2. Method must be active
3. Weight/dimensions must be within method limits (if specified)
4. Order total must meet method's minimum (if specified)
5. Store must be supported (if store limitation set)
6. Restricted countries excluded
7. Customer role must be allowed (if restriction set)

**Rate Calculation**:
- Shipping rate computed by `IShippingRateComputationMethod`
- Can be fixed rate, weight-based, or real-time carrier API
- Multiple shipping plugins can be active simultaneously
- Customer chooses from available options at checkout

**Location**: 
- `Nop.Services.Shipping.ShippingService.GetShippingOptions()`
- Shipping plugins implement `IShippingRateComputationMethod`

### Shipping Cost Calculation

**Business Rule**: Total shipping charge determination

**Factors in Calculation**:
1. **Base Rate**: From selected shipping method
2. **Weight Charges**: Additional fees for heavy orders
3. **Handling Fees**: Flat fee per order or per item
4. **Free Shipping**: If order qualifies (by amount or product)
5. **Shipping Discounts**: Applied after base calculation
6. **Multiple Shipments**: Costs can vary per shipment

**Free Shipping Rules**:
- Order total exceeds configured amount
- Product marked as "Free Shipping"
- Discount provides free shipping
- Customer role receives free shipping

**Location**: `Nop.Services.Orders.OrderTotalCalculationService.GetShoppingCartShippingTotal()`

### Shipment Creation Logic

**Business Rule**: Splitting orders into shipments
- Orders can be shipped partially (multiple shipments)
- Each shipment tracks subset of order items
- Each item specifies quantity being shipped
- Tracking number assigned per shipment
- Shipped date recorded
- Delivery date recorded when arrived
- Customer notified for each shipment
- Order complete when all items shipped

**Location**: `Nop.Services.Shipping.ShipmentService`

### Address Validation Rules

**Business Rule**: Shipping address requirements
- Required fields: First name, last name, address1, city, state/province, zip code, country
- Address2 optional for apartment numbers
- Phone number required/optional based on settings
- Email required for shipment notifications
- Addresses reusable (saved to customer account)
- Address can be validated against external services (plugins)
- Billing and shipping addresses can differ

**Location**: Enforced in `Nop.Core.Domain.Common.Address` entity and controllers

---

## Tax Calculation Logic

### Tax Rate Determination

**Business Rule**: Tax calculated based on multiple factors

**Tax Calculation Basis**:
- **Shipping Address**: Tax based on where product shipped (most common)
- **Billing Address**: Tax based on billing address
- **Default Address**: Use store's default address

**Tax Classification**:
- Products assigned to tax categories
- Tax rates defined per:
  - Tax category
  - Country
  - State/Province
  - Zip code
- Multiple tax rates can apply (state + local)

**Tax Exemptions**:
- Customer marked as tax exempt
- Customer role has tax exemption
- Product tax category set to "No Tax"
- Shipping can be taxed separately

**Location**: 
- `Nop.Services.Tax.TaxService.GetProductPrice()`
- Tax plugins implement `ITaxProvider`

### Tax Display Rules

**Business Rule**: Showing prices with or without tax

**Display Options** (per customer):
- `Including Tax`: Show $120.00 (includes $20 tax)
- `Excluding Tax`: Show $100.00 + $20.00 tax

**Settings Controlled By**:
- Store-level default setting
- Customer's tax display type (can override)
- Based on customer location
- Affects product pages, cart, and order totals

**Both Prices Stored**:
- Database stores both inclusive and exclusive amounts
- Calculations maintain both values throughout
- Reports can show either perspective

**Location**: `Nop.Services.Tax.TaxService` and `IWorkContext.TaxDisplayType`

---

## Security and Access Control Logic

### Permission-Based Authorization

**Business Rule**: Feature access controlled by permissions

**Permission Model**:
- Permissions defined as records (e.g., "ManageProducts", "ManageOrders")
- Permissions assigned to customer roles
- Customer inherits permissions from all their roles
- OR logic: Customer has permission if ANY role grants it
- Standard permissions installed during setup
- Custom permissions can be added by plugins

**Permission Check Flow**:
1. Retrieve current customer
2. Get all customer's roles
3. Check if any role has requested permission
4. Grant or deny access
5. Unauthorized requests redirect to access denied page

**Location**: `Nop.Services.Security.PermissionService.Authorize()`

### Access Control Lists (ACL)

**Business Rule**: Entity-level access restrictions

**ACL Support**:
- Entities implement `IAclSupported` interface
- Each entity can restrict visibility to specific customer roles
- If `SubjectToAcl = true`, check ACL records
- If customer's role not in allowed list, entity hidden
- Applies to categories, products, manufacturers, topics
- Used for VIP products, member-only content, etc.

**ACL Evaluation**:
```
if (entity.SubjectToAcl)
    customerRoles ∩ entityAllowedRoles must be non-empty
else
    allow access
```

**Location**: `Nop.Services.Security.AclService.Authorize()`

### Store Mapping Logic

**Business Rule**: Multi-store content isolation

**Store Mapping Support**:
- Entities implement `IStoreMappingSupported`
- Each entity can be limited to specific stores
- If `LimitedToStores = true`, check store mappings
- Entity only visible in mapped stores
- Shared entities available in all stores
- Applies to products, categories, discounts, etc.

**Use Case**: 
- Separate product catalogs per store
- Store-specific promotions
- Regional content variations

**Location**: `Nop.Services.Stores.StoreMappingService.Authorize()`

### Password Security Rules

**Business Rule**: Password hashing and validation

**Hash Algorithm Support**:
- **SHA1**: Default, backward compatible
- **SHA256**: More secure option
- **MD5**: Legacy support only

**Hashing Process**:
1. Generate random salt (5-64 characters)
2. Combine password + salt
3. Apply hash function
4. Store hash and salt separately
5. Store hash format identifier

**Password Validation**:
1. Retrieve customer's salt and hash format
2. Hash provided password with same salt and format
3. Compare hashes (constant-time comparison)
4. Grant or deny authentication

**Location**: `Nop.Services.Security.EncryptionService`

### GDPR and Customer Data

**Business Rule**: Customer data privacy controls

**Data Rights**:
- **Right to Access**: Customer can export all their data
- **Right to Deletion**: Customer can request account deletion
- **Consent Tracking**: Log customer consent for data processing
- **Data Minimization**: Only collect necessary data
- **Retention Policies**: Delete old guest accounts

**Anonymization**:
- Customer personal data removed
- Order history preserved (anonymized)
- Email changed to deleted+GUID@store.com format
- Addresses removed
- Reviews anonymized

**Location**: `Nop.Services.Gdpr.GdprService`

---

## Cross-Cutting Business Rules

### Multi-Store Support

**Business Rule**: Single installation serves multiple stores
- Each store has unique domain/URL
- Shared database and codebase
- Store-specific:
  - Products and categories
  - Discounts and promotions
  - Content and topics
  - Settings and configuration
- Shared:
  - Customers and orders (visible across stores)
  - Product catalog (with store mapping)

**Location**: `IStoreContext.CurrentStore` and `IStoreMappingService`

### Multi-Vendor Support

**Business Rule**: Marketplace with multiple sellers
- Vendors can manage their own products
- Vendors have limited admin access
- Order items tracked per vendor
- Commission tracking for platform
- Vendor notifications for their orders
- Vendor-specific reports
- Products can be assigned to vendor

**Location**: `IWorkContext.CurrentVendor` and vendor filtering throughout services

### Localization and Multi-Language

**Business Rule**: Content in multiple languages
- Language resources stored per language
- Entity properties can be localized
- Current language from customer preference or browser
- Fallback to default language if translation missing
- Admin can manage translations
- Plugins can add their own resources

**Location**: `Nop.Services.Localization.LocalizationService`

### Multi-Currency Support

**Business Rule**: Prices in different currencies
- Products priced in primary currency
- Real-time or manual exchange rates
- Prices converted to customer's selected currency
- Currency formatting per culture
- Exchange rates updated via scheduled task
- Payments processed in store's primary currency (conversion at payment gateway)

**Location**: `Nop.Services.Directory.CurrencyService` and `IWorkContext.WorkingCurrency`

---

## Summary

The nopCommerce business logic demonstrates:

1. **Rich E-commerce Domain**: Comprehensive rules covering all aspects of online retail
2. **Configurability**: Most rules can be adjusted via settings without code changes
3. **Extensibility**: Plugin architecture allows custom business rules
4. **Multi-Tenancy**: Store mapping and ACL enable sophisticated multi-store scenarios
5. **Security**: Strong authentication, authorization, and data protection
6. **Flexibility**: Support for various business models (B2C, B2B, marketplace)
7. **Compliance**: GDPR, PCI, and tax regulations considered
8. **Performance**: Caching strategies for expensive business logic operations

**Related Documentation**:
- [Workflows](workflows.md) - Process flows and sequences
- [Decision Logic](decision-logic.md) - Decision trees and conditional logic
- [Error Handling](error-handling.md) - Exception patterns and validation

---

**Document Version**: 1.0  
**Analysis Method**: Static code analysis of service layer implementations
