using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public partial class ShipmentMap : NopEntityTypeConfiguration<Shipment>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Shipment> builder)
        {
            builder.ToTable("Shipment");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.TotalWeight).HasPrecision(18, 4);

            builder.HasOne(s => s.Order)
                .WithMany(o => o.Shipments)
                .HasForeignKey(s => s.OrderId)
                .IsRequired();        }
    }
}
