using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public class DeliveryDateMap : NopEntityTypeConfiguration<DeliveryDate>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DeliveryDate> builder)
        {
            builder.ToTable("DeliveryDate");
            builder.HasKey(dd => dd.Id);
            builder.Property(dd => dd.Name).IsRequired().HasMaxLength(400);        }
    }
}
