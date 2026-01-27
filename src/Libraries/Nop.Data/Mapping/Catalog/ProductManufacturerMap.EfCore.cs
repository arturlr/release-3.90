using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductManufacturerMap : NopEntityTypeConfiguration<ProductManufacturer>
    {
        public override void Configure(EntityTypeBuilder<ProductManufacturer> builder)
        {
            builder.ToTable("Product_Manufacturer_Mapping");
            builder.HasKey(pm => pm.Id);
            builder.HasOne(pm => pm.Product).WithMany(p => p.ProductManufacturers).HasForeignKey(pm => pm.ProductId).IsRequired();
            builder.HasOne(pm => pm.Manufacturer).WithMany().HasForeignKey(pm => pm.ManufacturerId).IsRequired();
            base.Configure(builder);
        }
    }
}
