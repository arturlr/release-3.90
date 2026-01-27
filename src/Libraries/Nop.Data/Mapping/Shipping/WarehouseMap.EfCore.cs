using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public partial class WarehouseMap : NopEntityTypeConfiguration<Warehouse>
    {
        public override void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouse");
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Name).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
