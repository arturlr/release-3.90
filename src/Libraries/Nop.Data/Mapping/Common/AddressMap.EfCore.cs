using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Common;

namespace Nop.Data.Mapping.Common
{
    public partial class AddressMap : NopEntityTypeConfiguration<Address>
    {
        public override void Configure(EntityTypeBuilder<Address> builder)
        {
            builder.ToTable("Address");
            builder.HasKey(a => a.Id);

            builder.HasOne(a => a.Country)
                .WithMany()
                .HasForeignKey(a => a.CountryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(a => a.StateProvince)
                .WithMany()
                .HasForeignKey(a => a.StateProvinceId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            base.Configure(builder);
        }
    }
}
