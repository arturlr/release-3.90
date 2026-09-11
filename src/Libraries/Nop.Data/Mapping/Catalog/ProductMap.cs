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

            //Runtime deferral 4.12, closed by task 7.7. EF6's
            //HasMany(x).WithMany(y).Map(m => m.ToTable("T")) named the join FK columns
            //<EntityName>_<KeyName>; EF Core's convention names them <NavigationName>Id, so this
            //table came out as (ProductsId, ProductTagsId) instead of 3.90's
            //(Product_Id, ProductTag_Id). The deferral rated that "not required for the compile
            //gate" - correct - but it is required to INSTALL: task 7.7 measured a fresh install
            //failing with "Setup failed: Invalid column name 'ProductTag_Id'. Invalid column name
            //'Product_Id'.", because App_Data/Install/SqlServer.StoredProcedures.sql joins these
            //columns by their 3.90 names in ProductLoadAllPaged and ProductTagCountLoadAll.
            //Only the COLUMN name is pinned; the shadow property keeps its EF Core name, so no
            //query or navigation code changes.
            builder.HasMany(p => p.ProductTags)
                .WithMany(pt => pt.Products)
                .UsingEntity(j =>
                {
                    j.ToTable("Product_ProductTag_Mapping");
                    j.Property<int>("ProductsId").HasColumnName("Product_Id");
                    j.Property<int>("ProductTagsId").HasColumnName("ProductTag_Id");
                });

            base.Configure(builder);
        }
    }
}