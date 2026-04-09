# Nop.Core Domain Layer

## Bounded Context
Foundation domain model — all entities, enums, value objects, and marker interfaces that every other layer depends on.

## Legacy Source
- `src/Libraries/Nop.Core/Domain/` — 26 subdirectories, 207 entity files
- `src/Libraries/Nop.Core/BaseEntity.cs`
- Namespaces: `Nop.Core.Domain.*` (Affiliates, Blogs, Catalog, Cms, Common, Configuration, Customers, Directory, Discounts, Forums, Localization, Logging, Media, Messages, News, Orders, Payments, Polls, Security, Seo, Shipping, Stores, Tasks, Tax, Topics, Vendors)

## Key Entities
- `BaseEntity` (abstract base with int Id)
- Product, Category, Manufacturer, ProductAttribute, ProductAttributeMapping, ProductAttributeValue, ProductAttributeCombination, TierPrice, ProductTag, SpecificationAttribute
- Customer, CustomerRole, CustomerPassword, CustomerAttribute, ExternalAuthenticationRecord, RewardPointsHistory
- Order, OrderItem, OrderNote, ShoppingCartItem, CheckoutAttribute, GiftCard, ReturnRequest, RecurringPayment
- Address, GenericAttribute, Country, StateProvince, Currency, MeasureWeight, MeasureDimension
- Discount, DiscountRequirement, DiscountUsageHistory
- BlogPost, BlogComment, NewsItem, NewsComment, ForumGroup, Forum, ForumTopic, ForumPost, PrivateMessage, Poll, PollAnswer, Topic
- Shipment, ShipmentItem, ShippingMethod, Warehouse, DeliveryDate
- Picture, Download, EmailAccount, MessageTemplate, QueuedEmail, NewsLetterSubscription, Campaign
- Language, LocaleStringResource, LocalizedProperty, UrlRecord
- PermissionRecord, AclRecord, Store, StoreMapping, ScheduleTask, Setting, Log, ActivityLog, ActivityLogType
- TaxCategory, Vendor, VendorNote, Affiliate
- Marker interfaces: `ILocalizedEntity`, `ISlugSupported`, `IAclSupported`, `IStoreMappingSupported`

## External Dependencies
- None (domain layer has zero project dependencies)

## Migration Notes
- **Decision**: Rewrite to .NET 10 / C# 13
- All entities become records or classes with init-only properties where appropriate
- Replace `partial class` pattern with clean sealed classes
- Add nullable reference type annotations throughout
- Replace XML-serialized attribute storage (`AttributesXml`) with JSON
- Enums stay as enums; consider using `SmartEnum` for complex ones
- `BaseEntity` → keep int PK, add `IEntity` interface for generic constraints
- Marker interfaces remain — they drive cross-cutting query filters

## Acceptance Criteria
- [ ] All 207 entity types compile under .NET 10 with nullable reference types enabled
- [ ] Marker interfaces (`ILocalizedEntity`, `ISlugSupported`, `IAclSupported`, `IStoreMappingSupported`) are defined and applied to the same entities as legacy
- [ ] `BaseEntity` provides `Id` property with proper equality semantics
- [ ] All enums from legacy `Domain` namespace are present with identical values
- [ ] No dependency on any other project — domain layer is leaf
