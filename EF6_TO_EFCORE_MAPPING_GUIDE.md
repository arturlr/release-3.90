# Entity Framework 6 to EF Core Mapping Conversion Guide

## Overview
This guide documents the conversion patterns for migrating 106 entity mappings from EF6 to EF Core.

## Base Class Change

### EF6:
```csharp
public class CustomerMap : NopEntityTypeConfiguration<Customer>
{
    public CustomerMap()
    {
        // Configuration here
    }
}
```

### EF Core:
```csharp
public class CustomerMap : NopEntityTypeConfiguration<Customer>
{
    public override void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Configuration here
        base.Configure(builder);
    }
}
```

## Common Conversion Patterns

### 1. Table and Key Configuration
**EF6:**
```csharp
this.ToTable("Customer");
this.HasKey(c => c.Id);
```

**EF Core:**
```csharp
builder.ToTable("Customer");
builder.HasKey(c => c.Id);
```

### 2. Property Configuration
**EF6:**
```csharp
this.Property(u => u.Username).HasMaxLength(1000);
this.Property(u => u.Email).IsRequired();
```

**EF Core:**
```csharp
builder.Property(u => u.Username).HasMaxLength(1000);
builder.Property(u => u.Email).IsRequired();
```

### 3. Many-to-Many Relationships
**EF6:**
```csharp
this.HasMany(c => c.CustomerRoles)
    .WithMany()
    .Map(m => m.ToTable("Customer_CustomerRole_Mapping"));
```

**EF Core:**
```csharp
builder.HasMany(c => c.CustomerRoles)
    .WithMany()
    .UsingEntity(j => j.ToTable("Customer_CustomerRole_Mapping"));
```

### 4. One-to-Many Relationships
**EF6:**
```csharp
this.HasRequired(o => o.Customer)
    .WithMany()
    .HasForeignKey(o => o.CustomerId);
```

**EF Core:**
```csharp
builder.HasOne(o => o.Customer)
    .WithMany()
    .HasForeignKey(o => o.CustomerId)
    .IsRequired();
```

### 5. Optional Relationships
**EF6:**
```csharp
this.HasOptional(c => c.BillingAddress);
```

**EF Core:**
```csharp
builder.HasOne(c => c.BillingAddress)
    .WithMany()
    .HasForeignKey("BillingAddress_Id")
    .IsRequired(false);
```

### 6. One-to-One Relationships
**EF6:**
```csharp
this.HasRequired(x => x.Address)
    .WithRequiredPrincipal();
```

**EF Core:**
```csharp
builder.HasOne(x => x.Address)
    .WithOne()
    .HasForeignKey<DependentEntity>("AddressId")
    .IsRequired();
```

### 7. Cascade Delete
**EF6:**
```csharp
this.HasRequired(x => x.Parent)
    .WithMany()
    .WillCascadeOnDelete(false);
```

**EF Core:**
```csharp
builder.HasOne(x => x.Parent)
    .WithMany()
    .OnDelete(DeleteBehavior.Restrict);
```

### 8. Indexes
**EF6:**
```csharp
this.Property(x => x.Email).HasColumnAnnotation(
    IndexAnnotation.AnnotationName,
    new IndexAnnotation(new IndexAttribute()));
```

**EF Core:**
```csharp
builder.HasIndex(x => x.Email);
```

### 9. Composite Keys
**EF6:**
```csharp
this.HasKey(x => new { x.ProductId, x.AttributeId });
```

**EF Core:**
```csharp
builder.HasKey(x => new { x.ProductId, x.AttributeId });
```

### 10. Ignore Properties
**EF6:**
```csharp
this.Ignore(x => x.CalculatedProperty);
```

**EF Core:**
```csharp
builder.Ignore(x => x.CalculatedProperty);
```

## Migration Checklist

For each of the 106 mapping files:

- [ ] Change base class method from constructor to `Configure(EntityTypeBuilder<T> builder)`
- [ ] Replace `this.` with `builder.`
- [ ] Convert `HasRequired` → `HasOne(...).IsRequired()`
- [ ] Convert `HasOptional` → `HasOne(...).IsRequired(false)`
- [ ] Convert `WithMany()` → stays the same
- [ ] Convert `WithRequired()` → `WithOne().IsRequired()`
- [ ] Convert `Map(m => m.ToTable(...))` → `UsingEntity(j => j.ToTable(...))`
- [ ] Convert `WillCascadeOnDelete` → `OnDelete(DeleteBehavior...)`
- [ ] Add `base.Configure(builder);` at end of method
- [ ] Test against existing database schema

## Mapping Files by Domain (106 total)

### Catalog (28 files)
- BackInStockSubscriptionMap
- CategoryMap
- CategoryTemplateMap
- CrossSellProductMap
- ManufacturerMap
- ManufacturerTemplateMap
- PredefinedProductAttributeValueMap
- ProductAttributeCombinationMap
- ProductAttributeMap
- ProductAttributeMappingMap
- ProductAttributeValueMap
- ProductCategoryMap
- ProductManufacturerMap
- ProductMap
- ProductPictureMap
- ProductReviewHelpfulnessMap
- ProductReviewMap
- ProductSpecificationAttributeMap
- ProductTagMap
- ProductTemplateMap
- ProductWarehouseInventoryMap
- RelatedProductMap
- SpecificationAttributeMap
- SpecificationAttributeOptionFilterMap
- SpecificationAttributeOptionMap
- StockQuantityHistoryMap
- TierPriceMap

### Customers (9 files)
- CustomerAttributeMap
- CustomerAttributeValueMap
- CustomerMap
- CustomerPasswordMap
- CustomerRoleMap
- ExternalAuthenticationRecordMap
- RewardPointsHistoryMap

### Orders (15 files)
- CheckoutAttributeMap
- CheckoutAttributeValueMap
- GiftCardMap
- GiftCardUsageHistoryMap
- OrderItemMap
- OrderMap
- OrderNoteMap
- RecurringPaymentHistoryMap
- RecurringPaymentMap
- ReturnRequestActionMap
- ReturnRequestMap
- ReturnRequestReasonMap
- ShoppingCartItemMap

### Common (7 files)
- AddressAttributeMap
- AddressAttributeValueMap
- AddressMap
- GenericAttributeMap
- SearchTermMap

### Directory (7 files)
- CountryMap
- CurrencyMap
- MeasureDimensionMap
- MeasureWeightMap
- StateProvinceMap

### Discounts (5 files)
- DiscountMap
- DiscountRequirementMap
- DiscountUsageHistoryMap

### Shipping (8 files)
- DeliveryDateMap
- ProductAvailabilityRangeMap
- ShipmentItemMap
- ShipmentMap
- ShippingMethodMap
- WarehouseMap

### Messages (7 files)
- CampaignMap
- EmailAccountMap
- MessageTemplateMap
- NewsLetterSubscriptionMap
- QueuedEmailMap

### Other domains (20 files)
- Affiliates, Blogs, Configuration, Forums, Localization, Logging, Media, News, Polls, Security, Seo, Stores, Tasks, Tax, Topics, Vendors

## Automated Conversion Script

A PowerShell/Bash script could automate 80% of the conversion:

```bash
# Find and replace patterns
sed -i 's/this\./builder./g' *.cs
sed -i 's/public \(.*\)Map()/public override void Configure(EntityTypeBuilder<\1> builder)/g' *.cs
# ... more patterns
```

## Testing Strategy

1. **Schema Comparison**: Use EF Core migrations to generate schema and compare with existing
2. **Unit Tests**: Test each mapping can be applied without errors
3. **Integration Tests**: Query entities and verify relationships load correctly
4. **Database Validation**: Run against copy of production database

## Estimated Effort

- **Per mapping file**: 5-15 minutes
- **Total time**: 10-20 hours for all 106 files
- **Testing**: 5-10 hours
- **Total**: 15-30 hours

## Status

- [x] Base class created (`NopEntityTypeConfiguration.EfCore.cs`)
- [x] Sample mapping created (`CustomerMap.EfCore.cs`)
- [x] NopDbContext configured to auto-discover mappings
- [ ] Remaining 105 mapping files to convert
- [ ] Schema validation against existing database
- [ ] Integration testing

## Next Steps

1. Convert high-priority entities first (Customer, Order, Product)
2. Batch convert similar entities (all Catalog, all Orders, etc.)
3. Test each batch against database
4. Document any schema differences found
5. Create migration scripts if schema changes needed
