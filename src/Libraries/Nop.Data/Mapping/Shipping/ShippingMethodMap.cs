using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public class ShippingMethodMap : NopEntityTypeConfiguration<ShippingMethod>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ShippingMethod> builder)
        {
            builder.ToTable("ShippingMethod");
            builder.HasKey(sm => sm.Id);
            builder.Property(sm => sm.Name).IsRequired().HasMaxLength(400);

            builder.HasMany(sm => sm.RestrictedCountries)
                .WithMany(c => c.RestrictedShippingMethods)
                .UsingEntity<Dictionary<string, object>>(
                    "ShippingMethodRestrictions",
                    right => right.HasOne<Country>().WithMany().HasForeignKey("Country_Id"),
                    left => left.HasOne<ShippingMethod>().WithMany().HasForeignKey("ShippingMethod_Id"),
                    j => j.ToTable("ShippingMethodRestrictions"));
        }
    }
}
