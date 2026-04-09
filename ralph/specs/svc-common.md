# Common Services

## Bounded Context
Common/shared services — address management, generic attributes, full-text search, maintenance, PDF generation, and miscellaneous helpers.

## Legacy Source
- `src/Libraries/Nop.Services/Common/` — all files
- Key interfaces: `IAddressService`, `IAddressAttributeService`, `IAddressAttributeFormatter`, `IAddressAttributeParser`, `IGenericAttributeService`, `IFulltextService`, `IMaintenanceService`, `IPdfService`, `ISearchTermService`, `IMiscPlugin`
- Domain: `Nop.Core.Domain.Common`

## Key Entities
- Address, GenericAttribute, SearchTerm, AddressAttribute, AddressAttributeValue

## External Dependencies
- PDF generation library (iTextSharp or similar)

## Migration Notes
- **Decision**: Rewrite
- `GenericAttribute` is the extension property system — key/value pairs attached to any entity
- Address validation: required fields configurable
- PDF generation for invoices/packing slips
- Full-text search: SQL Server full-text index integration
- Maintenance: database backup, delete guests, delete exported files

## Acceptance Criteria
- [ ] Address CRUD with configurable required field validation
- [ ] `GenericAttribute` stores and retrieves key/value pairs for any entity type
- [ ] PDF invoice generation produces correct order details
- [ ] Search term tracking records and reports popular search terms
