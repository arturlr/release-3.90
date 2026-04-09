# Catalog Services

## Bounded Context
Product catalog management — products, categories, manufacturers, attributes, pricing, specifications, tags, reviews, and back-in-stock subscriptions.

## Legacy Source
- `src/Libraries/Nop.Services/Catalog/` — all files
- Key interfaces: `IProductService`, `ICategoryService`, `IManufacturerService`, `IProductAttributeService`, `IProductAttributeParser`, `IProductAttributeFormatter`, `IPriceCalculationService`, `IPriceFormatter`, `IProductTagService`, `IRecentlyViewedProductsService`, `ISpecificationAttributeService`, `ICopyProductService`, `IBackInStockSubscriptionService`, `ICompareProductsService`, `ICategoryTemplateService`, `IManufacturerTemplateService`, `IProductTemplateService`
- Domain: `Nop.Core.Domain.Catalog`

## Key Entities
- Product (100+ properties, 2142 LOC service), Category, Manufacturer
- ProductAttribute, ProductAttributeMapping, ProductAttributeValue, ProductAttributeCombination
- TierPrice, ProductTag, SpecificationAttribute, SpecificationAttributeOption
- ProductReview, ProductReviewHelpfulness
- RelatedProduct, CrossSellProduct, BackInStockSubscription
- ProductPicture, ProductCategory, ProductManufacturer

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: HIGH — `ProductService` is 2142 LOC, `PriceCalculationService` has complex multi-factor pricing
- `SearchProducts` method has 20+ parameters → consider specification pattern or query object
- `AttributesXml` XML serialization → JSON
- Price calculation: base price → tier pricing → customer role pricing → attribute adjustments → discounts → currency conversion → rounding
- Product visibility rules: Published + Date range + ACL + Store mapping + Deleted flag

## Acceptance Criteria
- [ ] `IProductService.SearchProducts` returns same results as legacy for identical filter combinations
- [ ] `IPriceCalculationService.GetFinalPrice` produces identical prices given same inputs (base, tier, role, attributes, discounts)
- [ ] Product CRUD operations trigger cache invalidation and domain events
- [ ] Category hierarchy (parent-child) navigation works correctly
- [ ] Product attribute combinations with unique SKU/price/stock are supported
