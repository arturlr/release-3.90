# Localization

## Bounded Context
Localization and multi-language — language management, string resources, entity property translations, and localization helpers.

## Legacy Source
- `src/Libraries/Nop.Services/Localization/` — all files
- Key interfaces: `ILocalizationService`, `ILanguageService`, `ILocalizedEntityService`
- `src/Presentation/Nop.Web.Framework/Localization/` — localization helpers, localized routes
- Domain: `Nop.Core.Domain.Localization`

## Key Entities
- Language (Name, LanguageCulture, UniqueSeoCode, Rtl, Published)
- LocaleStringResource (LanguageId, ResourceName, ResourceValue)
- LocalizedProperty (EntityId, LocaleKeyGroup, LocaleKey, LocaleValue, LanguageId)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- String resources: key/value pairs per language stored in DB, cached aggressively
- Entity localization: any property of `ILocalizedEntity` can have per-language translations via `LocalizedProperty`
- Resource import/export via XML
- Localized routes: `/en/product/widget` vs `/de/produkt/widget`
- Fallback: if translation missing, use default language
- In .NET 10: integrate with `IStringLocalizer` from Microsoft.Extensions.Localization

## Acceptance Criteria
- [ ] String resource lookup returns correct translation for current language with fallback to default
- [ ] Entity property localization stores and retrieves per-language translations
- [ ] Resource import/export via XML works correctly
- [ ] Localized URL routing resolves language from URL prefix
