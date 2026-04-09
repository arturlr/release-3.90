# Configuration Services

## Bounded Context
Settings management — store-specific and global configuration via key/value settings.

## Legacy Source
- `src/Libraries/Nop.Services/Configuration/` — all files
- Key interfaces: `ISettingService`
- Domain: `Nop.Core.Domain.Configuration`

## Key Entities
- Setting (Name, Value, StoreId)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Settings stored as key/value pairs with optional store scope (StoreId = 0 means global)
- `ISettings` marker interface on settings classes (e.g., `CatalogSettings`, `OrderSettings`)
- `LoadSetting<T>` deserializes settings class from individual Setting records
- `SaveSetting` serializes settings class properties to individual Setting records
- Settings cached aggressively

## Acceptance Criteria
- [ ] `LoadSetting<T>` correctly deserializes a settings class from individual key/value records
- [ ] `SaveSetting` correctly serializes settings class properties to individual records
- [ ] Store-specific settings override global settings when StoreId matches
