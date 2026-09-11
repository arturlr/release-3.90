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

            //Runtime deferral 4.12, closed by task 7.7 - see the long note in
            //Mapping/Catalog/ProductMap.cs. 3.90's EF6 column names are
            //(ShippingMethod_Id, Country_Id).
            builder.HasMany(sm => sm.RestrictedCountries)
                .WithMany(c => c.RestrictedShippingMethods)
                .UsingEntity(j =>
                {
                    j.ToTable("ShippingMethodRestrictions");
                    j.Property<int>("RestrictedShippingMethodsId").HasColumnName("ShippingMethod_Id");
                    j.Property<int>("RestrictedCountriesId").HasColumnName("Country_Id");
                });

            base.Configure(builder);
        }
    }
}
