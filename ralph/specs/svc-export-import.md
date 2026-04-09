# Export/Import Services

## Bounded Context
Data export and import — product/category/manufacturer/order export to Excel/XML and import from Excel/XML.

## Legacy Source
- `src/Libraries/Nop.Services/ExportImport/` — ExportManager.cs (1530 LOC), ImportManager.cs (1498 LOC)
- Key interfaces: `IExportManager`, `IImportManager`

## Key Entities
- No domain entities — operates on existing catalog/order entities
- Excel/XML file formats

## External Dependencies
- EPPlus or similar Excel library (OfficeOpenXml)

## Migration Notes
- **Decision**: Rewrite
- **Complexity**: MEDIUM-HIGH — ExportManager 1530 LOC, ImportManager 1498 LOC
- Export: products, categories, manufacturers, orders to Excel
- Import: products, categories, manufacturers from Excel with update-or-insert logic
- Product import handles pictures, categories, manufacturers, attributes
- Consider using `ClosedXML` or `EPPlus` 7.x for .NET 10

## Acceptance Criteria
- [ ] Product export to Excel includes all key fields and produces valid .xlsx files
- [ ] Product import from Excel creates new products or updates existing (by SKU match)
- [ ] Category and manufacturer export/import works correctly
- [ ] Import handles picture URLs and downloads images
