# Plugin: Widgets.GoogleAnalytics

## Bounded Context
Widget — Google Analytics tracking code injection.

## Legacy Source
- `src/Plugins/Nop.Plugin.Widgets.GoogleAnalytics/`

## Key Entities
- Implements `IWidgetPlugin`

## External Dependencies
- Google Analytics (JavaScript injection)

## Migration Notes
- **Decision**: Rewrite as .NET 10 plugin
- Injects GA tracking script into page header/footer widget zone
- Supports e-commerce tracking (order placed event)

## Acceptance Criteria
- [ ] Injects Google Analytics tracking code into configured widget zone
- [ ] E-commerce transaction tracking fires on order confirmation
- [ ] Configuration allows setting tracking ID
