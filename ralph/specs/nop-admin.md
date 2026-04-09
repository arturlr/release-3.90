# Nop.Admin — Administration Area

## Bounded Context
Administration web application — 54 admin controllers, admin views, management interfaces, configuration pages, and reporting.

## Legacy Source
- `src/Presentation/Nop.Web/Administration/Controllers/` — 54 controllers (excluding BaseAdminController)
- `src/Presentation/Nop.Web/Administration/Views/` — 51 view folders
- `src/Presentation/Nop.Web/Administration/Models/` — 156 admin view models
- `src/Presentation/Nop.Web/Administration/Infrastructure/` — DI registration, AutoMapper config, `ModelCacheEventConsumer` (144 LOC — cache invalidation for admin view models)
- `src/Presentation/Nop.Web/Administration/Extensions/` — `MappingExtensions` (1195 LOC, entity↔model mapping for all admin domains), `AttributeParserHelper` (96 LOC, form-to-attribute parsing)
- `src/Presentation/Nop.Web/Administration/Infrastructure/Mapper/AdminMapperConfiguration.cs` (1031 LOC — AutoMapper profiles for all admin entity↔model mappings)
- `src/Presentation/Nop.Web/Administration/Validators/` — 57 FluentValidation validator classes for admin view models

## Key Entities
Controllers: ActivityLog, AddressAttribute, Affiliate, Blog, Campaign, Category, CheckoutAttribute, Common, Country, Currency, Customer, CustomerAttribute, CustomerRole, Discount, Download, EmailAccount, ExternalAuthentication, Forum, GiftCard, Home, Jbimages, Language, Log, Manufacturer, Measure, MessageTemplate, News, NewsLetterSubscription, OnlineCustomer, Order, Payment, Picture, Plugin, Poll, Preferences, ProductAttribute, Product, ProductReview, QueuedEmail, RecurringPayment, ReturnRequest, RoxyFileman, ScheduleTask, Security, Setting, Shipping, ShoppingCart, SpecificationAttribute, Store, Tax, Template, Topic, Vendor, Widget

View folders: ActivityLog, AddressAttribute, Affiliate, Blog, Campaign, Category, CheckoutAttribute, Common, Country, Currency, Customer, CustomerAttribute, CustomerRole, Discount, EmailAccount, ExternalAuthentication, Forum, GiftCard, Home, Jbimages, Language, Log, Manufacturer, Measure, MessageTemplate, News, NewsLetterSubscription, OnlineCustomer, Order, Payment, Plugin, Poll, Product, ProductAttribute, ProductReview, QueuedEmail, RecurringPayment, ReturnRequest, ScheduleTask, Security, Setting, Shared, Shipping, ShoppingCart, SpecificationAttribute, Store, Tax, Template, Topic, Vendor, Widget

## External Dependencies
- Kendo UI (admin grids, editors)
- jQuery, jQuery UI
- TinyMCE or similar rich text editor

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: CRITICAL — ProductController 4857 LOC, OrderController 4379 LOC, CustomerController 2396 LOC, SettingController 2339 LOC
- Admin uses Kendo UI grids extensively → evaluate Telerik UI for ASP.NET Core or alternative
- Jbimages/RoxyFileman → modern file manager
- Settings controller manages all store settings across multiple tabs
- Plugin controller handles plugin installation/uninstallation/configuration
- Consider admin as separate ASP.NET Core Area or separate project
- `MappingExtensions` (1195 LOC) and `AdminMapperConfiguration` (1031 LOC) define all entity↔model mappings — replace AutoMapper with Mapster or manual mapping; these two files define the complete admin mapping surface
- 57 FluentValidation validators need migration to FluentValidation 11.x (API changes: `RuleFor` syntax compatible, but `WithMessage` lambda overloads changed)

## Acceptance Criteria
- [ ] All 54 admin controllers have equivalent endpoints with proper authorization
- [ ] Product management (CRUD, attributes, pictures, categories, inventory) works completely
- [ ] Order management (list, detail, status changes, refunds, shipments) works completely
- [ ] Customer management (list, detail, roles, addresses, activity) works completely
- [ ] Settings management saves and loads all configuration sections correctly
- [ ] Plugin management (list, install, uninstall, configure) works correctly
