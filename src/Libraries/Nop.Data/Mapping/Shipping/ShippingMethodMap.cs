using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public class ShippingMethodMap : NopEntityTypeConfiguration<ShippingMethod>
    {
        public override void Configure(EntityTypeBuilder<ShippingMethod> builder)
        {
            builder.ToTable("ShippingMethod");
            builder.HasKey(sm => sm.Id);
            builder.Property(sm => sm.Name).IsRequired().HasMaxLength(400);

            builder.HasMany(sm => sm.RestrictedCountries)
                .WithMany(c => c.RestrictedShippingMethods)
                .UsingEntity(j => j.ToTable("ShippingMethodRestrictions"));

            base.Configure(builder);
        }
    }
}
