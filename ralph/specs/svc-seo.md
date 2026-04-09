# SEO Services

## Bounded Context
Search engine optimization — URL slug management and SEO-friendly URL generation.

## Legacy Source
- `src/Libraries/Nop.Services/Seo/` — all files
- Key interfaces: `IUrlRecordService`, `ISitemapGenerator`
- `SeoExtensions` (1386 LOC) — static extension methods for slug generation: `GetSeName()` for Product, Category, Manufacturer, ProductTag, BlogPost, NewsItem, Topic, Forum, ForumGroup, Vendor; character transliteration table (`_seoCharacterTable`); `GetSeName(string name, bool convertNonWesternChars, bool allowUnicodeCharsInUrls)` core slug generator; `ValidateSeName()` uniqueness enforcement
- Domain: `Nop.Core.Domain.Seo`

## Key Entities
- UrlRecord (EntityId, EntityName, Slug, IsActive, LanguageId)
- SeoExtensions character transliteration table (Western + non-Western character mappings)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- URL slugs stored in `UrlRecord` table; one active slug per entity per language
- Slug generation: transliterate, lowercase, replace spaces with hyphens, remove special chars
- Entities implementing `ISlugSupported` get SEO URLs
- Slug uniqueness enforced (append `-2`, `-3` etc. for duplicates)
- `SeoExtensions` (1386 LOC) is a static class with entity-specific `GetSeName()` extension methods — convert to injectable service or keep as extensions with `IUrlRecordService` dependency
- Character transliteration table (`_seoCharacterTable`) uses lazy initialization with lock — thread-safe but consider `Lazy<T>` or `FrozenDictionary` in .NET 10

## Acceptance Criteria
- [ ] URL slug generation produces clean, unique, SEO-friendly slugs
- [ ] One active slug per entity per language; old slugs kept for redirects
- [ ] Slug lookup resolves entity type and ID from URL path
- [ ] Sitemap generation produces valid XML sitemap with all public entity URLs
