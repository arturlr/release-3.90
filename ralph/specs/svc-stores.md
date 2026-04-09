# Store Services

## Bounded Context
Multi-store management — store CRUD and store mapping for entity visibility across stores.

## Legacy Source
- `src/Libraries/Nop.Services/Stores/` — all files
- Key interfaces: `IStoreService`, `IStoreMappingService`
- Domain: `Nop.Core.Domain.Stores`

## Key Entities
- Store (Name, Url, SslEnabled, Hosts, CompanyInfo)
- StoreMapping (EntityId, EntityName, StoreId)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Multi-store: single installation serves multiple storefronts via domain/host matching
- Store mapping: entities with `IStoreMappingSupported` can be limited to specific stores
- `IStoreContext.CurrentStore` resolved from request host header
- Store-specific settings via `ISettingService` with StoreId

## Acceptance Criteria
- [ ] Store CRUD with domain/host-based resolution
- [ ] `StoreMappingService.Authorize` correctly filters entities by current store
- [ ] `IStoreContext.CurrentStore` resolves from request host header
