using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductWarehouseInventoryMap : NopEntityTypeConfiguration<ProductWarehouseInventory>
    {
        public override void Configure(EntityTypeBuilder<ProductWarehouseInventory> builder)
        {
            builder.ToTable("ProductWarehouseInventory");
            builder.HasKey(pwi => pwi.Id);
            builder.HasOne(pwi => pwi.Product).WithMany().HasForeignKey(pwi => pwi.ProductId).IsRequired();
            builder.HasOne(pwi => pwi.Warehouse).WithMany().HasForeignKey(pwi => pwi.WarehouseId).IsRequired();
            base.Configure(builder);
        }
    }
}
