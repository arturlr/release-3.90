# Media Services

## Bounded Context
Media management — picture storage/retrieval, download management, and image processing.

## Legacy Source
- `src/Libraries/Nop.Services/Media/` — all files
- Key interfaces: `IPictureService`, `IDownloadService`
- Domain: `Nop.Core.Domain.Media`

## Key Entities
- Picture (binary storage, MIME type, SEO filename, alt/title attributes)
- Download (binary or URL-based, content type, filename, extension)

## External Dependencies
- File system for picture storage (alternative to DB blob)

## Migration Notes
- **Decision**: Rewrite
- Pictures can be stored in DB (PictureBinary) or file system — keep both options
- Image resizing/thumbnailing needed for product images
- SEO-friendly image filenames
- Download management for digital products (activation, expiration, max downloads)
- Consider Azure Blob Storage as additional storage option

## Acceptance Criteria
- [ ] Picture upload, retrieval, and deletion work for both DB and file system storage
- [ ] Image thumbnails generated at requested dimensions
- [ ] Download files served with correct content type and access control (activation, expiration)
