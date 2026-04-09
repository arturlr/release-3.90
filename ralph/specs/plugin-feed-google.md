# Plugin: Feed.GoogleShopping

## Bounded Context
Product feed — generates Google Shopping product feed XML.

## Legacy Source
- `src/Plugins/Nop.Plugin.Feed.GoogleShopping/`

## Key Entities
- Google Shopping feed XML format
- Product-to-Google-category mapping

## External Dependencies
- Google Shopping feed specification

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin

## Acceptance Criteria
- [ ] Generates valid Google Shopping XML feed with all required fields
- [ ] Product-to-Google-category mapping configurable via admin
- [ ] Feed URL accessible for Google Merchant Center
