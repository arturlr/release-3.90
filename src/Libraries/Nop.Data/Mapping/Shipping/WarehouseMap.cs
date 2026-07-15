using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public class WarehouseMap : NopEntityTypeConfiguration<Warehouse>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouse");
            builder.HasKey(wh => wh.Id);
            builder.Property(wh => wh.Name).IsRequired().HasMaxLength(400);
        }
    }
}
