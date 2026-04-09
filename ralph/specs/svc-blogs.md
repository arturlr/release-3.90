# Blog Services

## Bounded Context
Blog content management — blog posts, comments, and blog-related queries.

## Legacy Source
- `src/Libraries/Nop.Services/Blogs/` — all files
- Key interfaces: `IBlogService`
- Domain: `Nop.Core.Domain.Blogs`

## Key Entities
- BlogPost, BlogComment

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Simple CRUD with language filtering, date range publishing, store mapping
- Comments linked to customers; admin approval optional
- Tags stored as comma-separated string on BlogPost

## Acceptance Criteria
- [ ] Blog post CRUD with language, date range, and store mapping filtering
- [ ] Blog comments linked to customers with approval workflow
- [ ] Blog tag parsing and tag-based post retrieval works correctly
