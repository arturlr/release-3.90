using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public class ProductAvailabilityRangeMap : NopEntityTypeConfiguration<ProductAvailabilityRange>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ProductAvailabilityRange> builder)
        {
            builder.ToTable("ProductAvailabilityRange");
            builder.HasKey(range => range.Id);
            builder.Property(range => range.Name).IsRequired().HasMaxLength(400);        }
    }
}
