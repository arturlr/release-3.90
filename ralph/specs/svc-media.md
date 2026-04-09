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
- `Microsoft.WindowsAzure.Storage` → `Azure.Storage.Blobs` for Azure Blob Storage (`AzurePictureService`, 167 LOC)

## Migration Notes
- **Decision**: Rewrite
- Pictures can be stored in DB (PictureBinary), file system, or Azure Blob Storage — keep all three options
- `AzurePictureService` extends `PictureService` for Azure Blob container storage (configured via `NopConfig.AzureBlobStorageConnectionString`)
- Image resizing/thumbnailing needed for product images
- SEO-friendly image filenames
- Download management for digital products (activation, expiration, max downloads)

## Acceptance Criteria
- [ ] Picture upload, retrieval, and deletion work for both DB and file system storage
- [ ] Image thumbnails generated at requested dimensions
- [ ] Download files served with correct content type and access control (activation, expiration)
- [ ] Azure Blob Storage provider stores and retrieves pictures from configured blob container
