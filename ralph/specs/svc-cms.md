# CMS / Widget Services

## Bounded Context
Content management system — widget plugin coordination and widget zone rendering.

## Legacy Source
- `src/Libraries/Nop.Services/Cms/` — all files
- Key interfaces: `IWidgetService`, `IWidgetPlugin`
- Domain: `Nop.Core.Domain.Cms`

## Key Entities
- Widget zones (string identifiers: "header", "footer", "home_page_top", etc.)
- `IWidgetPlugin` implementations

## External Dependencies
- Widget plugins (GoogleAnalytics, NivoSlider, etc.)

## Migration Notes
- **Decision**: Rewrite
- `IWidgetService` loads active widget plugins and maps them to zones
- Widget zones are string identifiers embedded in views
- Each `IWidgetPlugin` declares which zones it occupies
- In .NET 10: widget rendering via View Components instead of child actions

## Acceptance Criteria
- [ ] `IWidgetService` loads active widget plugins filtered by store
- [ ] Widget zone rendering invokes correct plugin for each zone
- [ ] Multiple widgets can occupy the same zone with display ordering
