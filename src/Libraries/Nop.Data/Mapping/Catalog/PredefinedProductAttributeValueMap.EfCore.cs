using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class PredefinedProductAttributeValueMap : NopEntityTypeConfiguration<PredefinedProductAttributeValue>
    {
        public override void Configure(EntityTypeBuilder<PredefinedProductAttributeValue> builder)
        {
            builder.ToTable("PredefinedProductAttributeValue");
            builder.HasKey(ppav => ppav.Id);
            builder.Property(ppav => ppav.Name).IsRequired().HasMaxLength(400);
            builder.Property(ppav => ppav.PriceAdjustment).HasPrecision(18, 4);
            builder.Property(ppav => ppav.WeightAdjustment).HasPrecision(18, 4);
            builder.Property(ppav => ppav.Cost).HasPrecision(18, 4);
            base.Configure(builder);
        }
    }
}
