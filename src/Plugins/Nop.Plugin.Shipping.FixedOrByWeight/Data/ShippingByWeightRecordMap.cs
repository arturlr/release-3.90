using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Shipping.FixedOrByWeight.Domain;

namespace Nop.Plugin.Shipping.FixedOrByWeight.Data
{
    public partial class ShippingByWeightRecordMap : NopEntityTypeConfiguration<ShippingByWeightRecord>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ShippingByWeightRecord> builder)
        {
            builder.ToTable("ShippingByWeight");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Zip).HasMaxLength(400);
        }
    }
}
