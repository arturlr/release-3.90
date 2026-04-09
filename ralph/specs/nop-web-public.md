# Nop.Web — Public Storefront

## Bounded Context
Customer-facing web application — 27 public controllers, view models, Razor views, themes, and static assets.

## Legacy Source
- `src/Presentation/Nop.Web/Controllers/` — 27 controllers (excluding BasePublicController)
- `src/Presentation/Nop.Web/Views/` — 23 view folders
- `src/Presentation/Nop.Web/Models/` — view models
- `src/Presentation/Nop.Web/Factories/` — 21 model factory interfaces + implementations (Address, Blog, Catalog, Checkout, Common, Country, Customer, ExternalAuthentication, Forum, Newsletter, News, Order, Poll, PrivateMessages, Product [1561 LOC], Profile, ReturnRequest, ShoppingCart, Topic, Vendor, Widget)
- `src/Presentation/Nop.Web/Themes/` — theme files
- `src/Presentation/Nop.Web/Infrastructure/` — routing, DI registration, `ModelCacheEventConsumer` (1335 LOC — cache invalidation event handlers for all public view model caches)
- `src/Presentation/Nop.Web/Extensions/` — `MappingExtensions` (61 LOC, entity-to-model mapping), `HtmlExtensions` (254 LOC, Razor HTML helpers), `AttributeParserHelper` (96 LOC, form-to-attribute parsing)
- `src/Presentation/Nop.Web/Validators/` — 20 FluentValidation validator classes for public view models
- `src/Presentation/Nop.Web/Infrastructure/Installation/` — `IInstallationLocalizationService`, `InstallationLocalizationService`, `InstallationLanguage` (multi-language install wizard)

## Key Entities
Controllers: BackInStockSubscription, BackwardCompatibility1X, BackwardCompatibility2X, Blog, Boards, Catalog, Checkout, Common, Country, Customer, Download, ExternalAuthentication, Home, Install, KeepAlive, News, Newsletter, Order, Poll, PrivateMessages, Product, Profile, ReturnRequest, ShoppingCart, Topic, Vendor, Widget

View folders: BackInStockSubscription, Blog, Boards, Catalog, Checkout, Common, Customer, ExternalAuthentication, Home, Install, News, Newsletter, Order, Poll, PrivateMessages, Product, Profile, ReturnRequest, Shared, ShoppingCart, Topic, Vendor, Widget

## External Dependencies
- jQuery, jQuery UI, jQuery Validate
- Theme CSS/JS assets

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: HIGH — ShoppingCartController 1849 LOC, CheckoutController 1788 LOC, CustomerController 1507 LOC
- Model factories separate view model construction from controllers — keep this pattern
- BackwardCompatibility controllers can be dropped (legacy URL redirects)
- Install controller → separate first-run setup flow
- KeepAlive → health check endpoint
- Themes → ASP.NET Core view location expanders + static file serving
- Razor views → Razor Pages or MVC views with Tag Helpers

## Acceptance Criteria
- [ ] All 27 public controllers (minus backward compat) have equivalent endpoints
- [ ] Product catalog browsing (list, detail, search, filter) works correctly
- [ ] Shopping cart and checkout flow completes successfully
- [ ] Customer registration, login, account management, and order history work
- [ ] Blog, news, forums, polls, and topics render correctly
