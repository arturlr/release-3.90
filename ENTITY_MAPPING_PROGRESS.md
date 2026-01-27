# Entity Mapping Migration Progress

**Status:** In Progress  
**Last Updated:** 2026-01-27

## Progress Summary

**Total:** 21 of 106 entity mappings complete (20%)

### Completed Mappings (21)

#### Catalog Domain (4)
- ✅ ProductMap.EfCore.cs
- ✅ CategoryMap.EfCore.cs
- ✅ ManufacturerMap.EfCore.cs

#### Customers Domain (1)
- ✅ CustomerMap.EfCore.cs
- ✅ CustomerRoleMap.EfCore.cs

#### Orders Domain (1)
- ✅ OrderMap.EfCore.cs

#### Common Domain (1)
- ✅ AddressMap.EfCore.cs

#### Directory Domain (3)
- ✅ CountryMap.EfCore.cs
- ✅ StateProvinceMap.EfCore.cs
- ✅ CurrencyMap.EfCore.cs

#### Localization Domain (1)
- ✅ LanguageMap.EfCore.cs

#### Stores Domain (1)
- ✅ StoreMap.EfCore.cs

#### Tax Domain (1)
- ✅ TaxCategoryMap.EfCore.cs

#### Shipping Domain (1)
- ✅ ShippingMethodMap.EfCore.cs

#### Discounts Domain (1)
- ✅ DiscountMap.EfCore.cs

#### Blogs Domain (2)
- ✅ BlogPostMap.EfCore.cs
- ✅ BlogCommentMap.EfCore.cs

#### Vendors Domain (1)
- ✅ VendorMap.EfCore.cs

#### Configuration Domain (1)
- ✅ SettingMap.EfCore.cs

#### Tasks Domain (1)
- ✅ ScheduleTaskMap.EfCore.cs

## Remaining Mappings (85)

### High Priority (Core Entities)
- [ ] ProductVariantMap
- [ ] ProductAttributeMap
- [ ] ProductCategoryMap
- [ ] ProductManufacturerMap
- [ ] OrderItemMap
- [ ] ShipmentMap
- [ ] CustomerRoleMap
- [ ] AddressAttributeMap

### Medium Priority (Supporting Entities)
- [ ] BlogPostMap
- [ ] NewsItemMap
- [ ] ForumMap
- [ ] PollMap
- [ ] MessageTemplateMap
- [ ] EmailAccountMap
- [ ] ScheduleTaskMap
- [ ] LogMap

### Lower Priority (Configuration/Settings)
- [ ] SettingMap
- [ ] LocaleStringResourceMap
- [ ] ActivityLogMap
- [ ] UrlRecordMap

## Conversion Patterns Used

All mappings follow the established patterns from `EF6_TO_EFCORE_MAPPING_GUIDE.md`:

1. **Base class:** `NopEntityTypeConfiguration<T>` with `Configure()` override
2. **Property mapping:** `this.` → `builder.`
3. **Required relationships:** `HasRequired()` → `HasOne().IsRequired()`
4. **Optional relationships:** `HasOptional()` → `HasOne().IsRequired(false)`
5. **Many-to-many:** `Map(m => m.ToTable())` → `UsingEntity(j => j.ToTable())`
6. **Cascade delete:** `WillCascadeOnDelete(false)` → `OnDelete(DeleteBehavior.Restrict)`

## Next Steps

1. Continue converting high-priority entity mappings
2. Test compilation after each batch
3. Verify database schema compatibility
4. Update NopDbContext to discover new mappings automatically

## Estimated Completion

- **Remaining:** 85 mappings
- **Rate:** ~10 mappings/hour
- **Estimated time:** 8-9 hours
