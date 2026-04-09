# News Services

## Bounded Context
News content management — news items and comments.

## Legacy Source
- `src/Libraries/Nop.Services/News/` — all files
- Key interfaces: `INewsService`
- Domain: `Nop.Core.Domain.News`

## Key Entities
- NewsItem, NewsComment

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Simple CRUD with language filtering, date range publishing, store mapping
- Comments linked to customers

## Acceptance Criteria
- [ ] News item CRUD with language, date range, and store mapping filtering
- [ ] News comments linked to customers with store context
- [ ] Published/unpublished filtering works correctly
