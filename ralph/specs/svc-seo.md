# SEO Services

## Bounded Context
Search engine optimization — URL slug management and SEO-friendly URL generation.

## Legacy Source
- `src/Libraries/Nop.Services/Seo/` — all files
- Key interfaces: `IUrlRecordService`, `ISitemapGenerator`
- Domain: `Nop.Core.Domain.Seo`

## Key Entities
- UrlRecord (EntityId, EntityName, Slug, IsActive, LanguageId)

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- URL slugs stored in `UrlRecord` table; one active slug per entity per language
- Slug generation: transliterate, lowercase, replace spaces with hyphens, remove special chars
- Entities implementing `ISlugSupported` get SEO URLs
- Slug uniqueness enforced (append `-2`, `-3` etc. for duplicates)

## Acceptance Criteria
- [ ] URL slug generation produces clean, unique, SEO-friendly slugs
- [ ] One active slug per entity per language; old slugs kept for redirects
- [ ] Slug lookup resolves entity type and ID from URL path
- [ ] Sitemap generation produces valid XML sitemap with all public entity URLs
