using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductMap : NopEntityTypeConfiguration<Product>
    {
        public override void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Product");
            builder.HasKey(p => p.Id);
            
            builder.Property(p => p.Name).IsRequired().HasMaxLength(400);
            builder.Property(p => p.MetaKeywords).HasMaxLength(400);
            builder.Property(p => p.MetaTitle).HasMaxLength(400);
            builder.Property(p => p.Sku).HasMaxLength(400);
            builder.Property(p => p.ManufacturerPartNumber).HasMaxLength(400);
            builder.Property(p => p.Gtin).HasMaxLength(400);
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
            builder.Property(p => p.RequiredProductIds).HasMaxLength(1000);
            builder.Property(p => p.AllowedQuantities).HasMaxLength(1000);
            builder.Property(p => p.BasepriceAmount).HasPrecision(18, 4);
            builder.Property(p => p.BasepriceBaseAmount).HasPrecision(18, 4);

            builder.Ignore(p => p.ProductType);
            builder.Ignore(p => p.BackorderMode);
            builder.Ignore(p => p.DownloadActivationType);
            builder.Ignore(p => p.GiftCardType);
            builder.Ignore(p => p.LowStockActivity);
            builder.Ignore(p => p.ManageInventoryMethod);
            builder.Ignore(p => p.RecurringCyclePeriod);
            builder.Ignore(p => p.RentalPricePeriod);

            builder.HasMany(p => p.ProductTags)
                .WithMany(pt => pt.Products)
                .UsingEntity(j => j.ToTable("Product_ProductTag_Mapping"));

            base.Configure(builder);
        }
    }
}
