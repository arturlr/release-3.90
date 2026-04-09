# Data Migration — File System

## Bounded Context
File system migration — uploaded pictures, themes, plugin files, and downloads.

## Legacy Source
- `~/Content/Images/` — uploaded product/category/manufacturer images
- `~/Themes/` — theme files
- `~/Plugins/` — plugin assemblies and assets
- Download files stored on disk

## Key Entities
- Product images, category images, manufacturer logos
- Theme CSS/JS/images
- Plugin DLLs and static assets
- Downloadable product files

## External Dependencies
- File system or blob storage

## Migration Notes
- **Decision**: Copy and restructure
- Image paths may change — update Picture records in DB to match new paths
- Theme structure may change for ASP.NET Core (wwwroot/themes/)
- Plugin directory structure changes for .NET 10 plugin system
- Consider Azure Blob Storage or S3 as target for cloud deployment

## Acceptance Criteria
- [ ] All uploaded images accessible at new paths
- [ ] Picture records in DB updated to reflect new file paths
- [ ] Theme files copied to ASP.NET Core static file structure
- [ ] Downloadable product files accessible via new download service
