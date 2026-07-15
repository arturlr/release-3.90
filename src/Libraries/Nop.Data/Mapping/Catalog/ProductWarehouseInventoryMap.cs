using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductWarehouseInventoryMap : NopEntityTypeConfiguration<ProductWarehouseInventory>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ProductWarehouseInventory> builder)
        {
            builder.ToTable("ProductWarehouseInventory");
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Product)
                .WithMany(p => p.ProductWarehouseInventory)
                .HasForeignKey(x => x.ProductId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Warehouse)
                .WithMany()
                .HasForeignKey(x => x.WarehouseId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
