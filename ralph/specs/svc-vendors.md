# Vendor Services

## Bounded Context
Multi-vendor marketplace — vendor management, vendor notes, and vendor-specific product/order filtering.

## Legacy Source
- `src/Libraries/Nop.Services/Vendors/` — all files
- Key interfaces: `IVendorService`
- Domain: `Nop.Core.Domain.Vendors`

## Key Entities
- Vendor (Name, Email, Description, PictureId, Active, Deleted, SEO)
- VendorNote

## External Dependencies
- None beyond Nop.Core and Nop.Data

## Migration Notes
- **Decision**: Rewrite
- Vendors can manage their own products via limited admin access
- Order items tracked per vendor for commission
- Vendor notifications for their orders
- `IWorkContext.CurrentVendor` provides vendor context

## Acceptance Criteria
- [ ] Vendor CRUD with SEO slug, picture, and active/deleted filtering
- [ ] Vendor notes CRUD with timestamps
- [ ] Products and order items filterable by vendor ID
