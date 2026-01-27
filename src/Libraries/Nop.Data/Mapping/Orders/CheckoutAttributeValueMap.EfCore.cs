using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class CheckoutAttributeValueMap : NopEntityTypeConfiguration<CheckoutAttributeValue>
    {
        public override void Configure(EntityTypeBuilder<CheckoutAttributeValue> builder)
        {
            builder.ToTable("CheckoutAttributeValue");
            builder.HasKey(cav => cav.Id);
            builder.Property(cav => cav.Name).IsRequired().HasMaxLength(400);
            builder.Property(cav => cav.PriceAdjustment).HasPrecision(18, 4);
            builder.Property(cav => cav.WeightAdjustment).HasPrecision(18, 4);
            base.Configure(builder);
        }
    }
}
