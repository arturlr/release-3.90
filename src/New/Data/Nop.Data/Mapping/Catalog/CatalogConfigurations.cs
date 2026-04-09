using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(400);
        builder.Property(p => p.MetaKeywords).HasMaxLength(400);
        builder.Property(p => p.MetaTitle).HasMaxLength(400);
        builder.Property(p => p.Sku).HasMaxLength(400);
        builder.Property(p => p.ManufacturerPartNumber).HasMaxLength(400);
        builder.Property(p => p.Gtin).HasMaxLength(400);
        builder.Property(p => p.RequiredProductIds).HasMaxLength(1000);
        builder.Property(p => p.AllowedQuantities).HasMaxLength(1000);

        builder.Property(p => p.AdditionalShippingCharge).HasPrecision(18, 4);
        builder.Property(p => p.Price).HasPrecision(18, 4);
        builder.Property(p => p.OldPrice).HasPrecision(18, 4);
        builder.Property(p => p.ProductCost).HasPrecision(18, 4);
        builder.Property(p => p.MinimumCustomerEnteredPrice).HasPrecision(18, 4);
        builder.Property(p => p.MaximumCustomerEnteredPrice).HasPrecision(18, 4);
        builder.Property(p => p.Weight).HasPrecision(18, 4);
        builder.Property(p => p.Length).HasPrecision(18, 4);
        builder.Property(p => p.Width).HasPrecision(18, 4);
        builder.Property(p => p.Height).HasPrecision(18, 4);
        builder.Property(p => p.BasepriceAmount).HasPrecision(18, 4);
        builder.Property(p => p.BasepriceBaseAmount).HasPrecision(18, 4);
        builder.Property(p => p.OverriddenGiftCardAmount).HasPrecision(18, 4);

        builder.Ignore(p => p.ProductType);
        builder.Ignore(p => p.BackorderMode);
        builder.Ignore(p => p.DownloadActivationType);
        builder.Ignore(p => p.GiftCardType);
        builder.Ignore(p => p.LowStockActivity);
        builder.Ignore(p => p.ManageInventoryMethod);
        builder.Ignore(p => p.RecurringCyclePeriod);
        builder.Ignore(p => p.RentalPricePeriod);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Category");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(400);
        builder.Property(c => c.MetaKeywords).HasMaxLength(400);
        builder.Property(c => c.MetaTitle).HasMaxLength(400);
        builder.Property(c => c.PriceRanges).HasMaxLength(400);
        builder.Property(c => c.PageSizeOptions).HasMaxLength(200);
    }
}

public class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.ToTable("Manufacturer");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(400);
        builder.Property(m => m.MetaKeywords).HasMaxLength(400);
        builder.Property(m => m.MetaTitle).HasMaxLength(400);
        builder.Property(m => m.PriceRanges).HasMaxLength(400);
        builder.Property(m => m.PageSizeOptions).HasMaxLength(200);
    }
}

public class CategoryTemplateConfiguration : IEntityTypeConfiguration<CategoryTemplate>
{
    public void Configure(EntityTypeBuilder<CategoryTemplate> builder)
    {
        builder.ToTable("CategoryTemplate");
        builder.HasKey(ct => ct.Id);
        builder.Property(ct => ct.Name).IsRequired().HasMaxLength(400);
        builder.Property(ct => ct.ViewPath).IsRequired().HasMaxLength(400);
    }
}

public class ManufacturerTemplateConfiguration : IEntityTypeConfiguration<ManufacturerTemplate>
{
    public void Configure(EntityTypeBuilder<ManufacturerTemplate> builder)
    {
        builder.ToTable("ManufacturerTemplate");
        builder.HasKey(mt => mt.Id);
        builder.Property(mt => mt.Name).IsRequired().HasMaxLength(400);
        builder.Property(mt => mt.ViewPath).IsRequired().HasMaxLength(400);
    }
}

public class ProductTemplateConfiguration : IEntityTypeConfiguration<ProductTemplate>
{
    public void Configure(EntityTypeBuilder<ProductTemplate> builder)
    {
        builder.ToTable("ProductTemplate");
        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Name).IsRequired().HasMaxLength(400);
        builder.Property(pt => pt.ViewPath).IsRequired().HasMaxLength(400);
    }
}

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttribute");
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Name).IsRequired();
    }
}

public class ProductAttributeMappingConfiguration : IEntityTypeConfiguration<ProductAttributeMapping>
{
    public void Configure(EntityTypeBuilder<ProductAttributeMapping> builder)
    {
        builder.ToTable("Product_ProductAttribute_Mapping");
        builder.HasKey(pam => pam.Id);
        builder.Ignore(pam => pam.AttributeControlType);
    }
}

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> builder)
    {
        builder.ToTable("ProductAttributeValue");
        builder.HasKey(pav => pav.Id);
        builder.Property(pav => pav.Name).IsRequired().HasMaxLength(400);
        builder.Property(pav => pav.ColorSquaresRgb).HasMaxLength(100);
        builder.Property(pav => pav.PriceAdjustment).HasPrecision(18, 4);
        builder.Property(pav => pav.WeightAdjustment).HasPrecision(18, 4);
        builder.Property(pav => pav.Cost).HasPrecision(18, 4);
        builder.Ignore(pav => pav.AttributeValueType);
    }
}

public class ProductAttributeCombinationConfiguration : IEntityTypeConfiguration<ProductAttributeCombination>
{
    public void Configure(EntityTypeBuilder<ProductAttributeCombination> builder)
    {
        builder.ToTable("ProductAttributeCombination");
        builder.HasKey(pac => pac.Id);
    }
}

public class PredefinedProductAttributeValueConfiguration : IEntityTypeConfiguration<PredefinedProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<PredefinedProductAttributeValue> builder)
    {
        builder.ToTable("PredefinedProductAttributeValue");
        builder.HasKey(ppav => ppav.Id);
        builder.Property(ppav => ppav.Name).IsRequired().HasMaxLength(400);
        builder.Property(ppav => ppav.PriceAdjustment).HasPrecision(18, 4);
        builder.Property(ppav => ppav.WeightAdjustment).HasPrecision(18, 4);
        builder.Property(ppav => ppav.Cost).HasPrecision(18, 4);
    }
}

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("Product_Category_Mapping");
        builder.HasKey(pc => pc.Id);
    }
}

public class ProductManufacturerConfiguration : IEntityTypeConfiguration<ProductManufacturer>
{
    public void Configure(EntityTypeBuilder<ProductManufacturer> builder)
    {
        builder.ToTable("Product_Manufacturer_Mapping");
        builder.HasKey(pm => pm.Id);
    }
}

public class ProductPictureConfiguration : IEntityTypeConfiguration<ProductPicture>
{
    public void Configure(EntityTypeBuilder<ProductPicture> builder)
    {
        builder.ToTable("Product_Picture_Mapping");
        builder.HasKey(pp => pp.Id);
    }
}

public class ProductReviewConfiguration : IEntityTypeConfiguration<ProductReview>
{
    public void Configure(EntityTypeBuilder<ProductReview> builder)
    {
        builder.ToTable("ProductReview");
        builder.HasKey(pr => pr.Id);
    }
}

public class ProductReviewHelpfulnessConfiguration : IEntityTypeConfiguration<ProductReviewHelpfulness>
{
    public void Configure(EntityTypeBuilder<ProductReviewHelpfulness> builder)
    {
        builder.ToTable("ProductReviewHelpfulness");
        builder.HasKey(prh => prh.Id);
    }
}

public class ProductSpecificationAttributeConfiguration : IEntityTypeConfiguration<ProductSpecificationAttribute>
{
    public void Configure(EntityTypeBuilder<ProductSpecificationAttribute> builder)
    {
        builder.ToTable("Product_SpecificationAttribute_Mapping");
        builder.HasKey(psa => psa.Id);
        builder.Ignore(psa => psa.AttributeType);
    }
}

public class ProductTagConfiguration : IEntityTypeConfiguration<ProductTag>
{
    public void Configure(EntityTypeBuilder<ProductTag> builder)
    {
        builder.ToTable("ProductTag");
        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Name).IsRequired().HasMaxLength(400);
    }
}

public class SpecificationAttributeConfiguration : IEntityTypeConfiguration<SpecificationAttribute>
{
    public void Configure(EntityTypeBuilder<SpecificationAttribute> builder)
    {
        builder.ToTable("SpecificationAttribute");
        builder.HasKey(sa => sa.Id);
        builder.Property(sa => sa.Name).IsRequired();
    }
}

public class SpecificationAttributeOptionConfiguration : IEntityTypeConfiguration<SpecificationAttributeOption>
{
    public void Configure(EntityTypeBuilder<SpecificationAttributeOption> builder)
    {
        builder.ToTable("SpecificationAttributeOption");
        builder.HasKey(sao => sao.Id);
        builder.Property(sao => sao.Name).IsRequired();
    }
}

public class TierPriceConfiguration : IEntityTypeConfiguration<TierPrice>
{
    public void Configure(EntityTypeBuilder<TierPrice> builder)
    {
        builder.ToTable("TierPrice");
        builder.HasKey(tp => tp.Id);
        builder.Property(tp => tp.Price).HasPrecision(18, 4);
    }
}

public class BackInStockSubscriptionConfiguration : IEntityTypeConfiguration<BackInStockSubscription>
{
    public void Configure(EntityTypeBuilder<BackInStockSubscription> builder)
    {
        builder.ToTable("BackInStockSubscription");
        builder.HasKey(b => b.Id);
    }
}

public class CrossSellProductConfiguration : IEntityTypeConfiguration<CrossSellProduct>
{
    public void Configure(EntityTypeBuilder<CrossSellProduct> builder)
    {
        builder.ToTable("CrossSellProduct");
        builder.HasKey(c => c.Id);
    }
}

public class RelatedProductConfiguration : IEntityTypeConfiguration<RelatedProduct>
{
    public void Configure(EntityTypeBuilder<RelatedProduct> builder)
    {
        builder.ToTable("RelatedProduct");
        builder.HasKey(rp => rp.Id);
    }
}

public class ProductWarehouseInventoryConfiguration : IEntityTypeConfiguration<ProductWarehouseInventory>
{
    public void Configure(EntityTypeBuilder<ProductWarehouseInventory> builder)
    {
        builder.ToTable("ProductWarehouseInventory");
        builder.HasKey(pwi => pwi.Id);
    }
}

public class StockQuantityHistoryConfiguration : IEntityTypeConfiguration<StockQuantityHistory>
{
    public void Configure(EntityTypeBuilder<StockQuantityHistory> builder)
    {
        builder.ToTable("StockQuantityHistory");
        builder.HasKey(sqh => sqh.Id);
    }
}

public class ProductProductTagMappingConfiguration : IEntityTypeConfiguration<ProductProductTagMapping>
{
    public void Configure(EntityTypeBuilder<ProductProductTagMapping> builder)
    {
        builder.ToTable("Product_ProductTag_Mapping");
        builder.HasKey(m => m.Id);
    }
}
