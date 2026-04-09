# Installation Services

## Bounded Context
First-run installation — database creation, seed data, default settings, and sample data.

## Legacy Source
- `src/Libraries/Nop.Services/Installation/` — CodeFirstInstallationService.cs (12269 LOC)
- `src/Presentation/Nop.Web/Infrastructure/Installation/` — `IInstallationLocalizationService`, `InstallationLocalizationService`, `InstallationLanguage` (multi-language install wizard localization, separate from main localization system)
- Key interfaces: `IInstallationService`, `IInstallationLocalizationService`

## Key Entities
- No domain entities — creates initial data for all entity types

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: HIGH — 12269 LOC single file with all seed data
- Creates: default store, languages, currencies, countries, states, email accounts, message templates, customer roles, admin user, settings, schedule tasks, permissions, topics, etc.
- Consider splitting into multiple seed data classes by domain area
- Sample data (products, categories) should be optional

## Acceptance Criteria
- [ ] Fresh installation creates database schema and seeds all required default data
- [ ] Default admin user created with configurable credentials
- [ ] All default settings, permissions, message templates, and schedule tasks are seeded
- [ ] Sample product data is optional and can be skipped
