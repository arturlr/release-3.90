using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping;

public class DeliveryDateConfiguration : IEntityTypeConfiguration<DeliveryDate>
{
    public void Configure(EntityTypeBuilder<DeliveryDate> builder)
    {
        builder.ToTable("DeliveryDate");
        builder.HasKey(dd => dd.Id);
        builder.Property(dd => dd.Name).IsRequired().HasMaxLength(400);
    }
}

public class ProductAvailabilityRangeConfiguration : IEntityTypeConfiguration<ProductAvailabilityRange>
{
    public void Configure(EntityTypeBuilder<ProductAvailabilityRange> builder)
    {
        builder.ToTable("ProductAvailabilityRange");
        builder.HasKey(par => par.Id);
        builder.Property(par => par.Name).IsRequired().HasMaxLength(400);
    }
}

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipment");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.TotalWeight).HasPrecision(18, 4);
    }
}

public class ShipmentItemConfiguration : IEntityTypeConfiguration<ShipmentItem>
{
    public void Configure(EntityTypeBuilder<ShipmentItem> builder)
    {
        builder.ToTable("ShipmentItem");
        builder.HasKey(si => si.Id);
    }
}

public class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethod");
        builder.HasKey(sm => sm.Id);
        builder.Property(sm => sm.Name).IsRequired().HasMaxLength(400);
    }
}

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouse");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(400);
    }
}
