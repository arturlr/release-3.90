using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class GiftCardUsageHistoryMap : NopEntityTypeConfiguration<GiftCardUsageHistory>
    {
        public override void Configure(EntityTypeBuilder<GiftCardUsageHistory> builder)
        {
            builder.ToTable("GiftCardUsageHistory");
            builder.HasKey(gcuh => gcuh.Id);
            builder.Property(gcuh => gcuh.UsedValue).HasPrecision(18, 4);
            builder.HasOne(gcuh => gcuh.GiftCard).WithMany(gc => gc.GiftCardUsageHistory).HasForeignKey(gcuh => gcuh.GiftCardId).IsRequired();
            builder.HasOne(gcuh => gcuh.UsedWithOrder).WithMany().HasForeignKey(gcuh => gcuh.UsedWithOrderId).IsRequired();
            base.Configure(builder);
        }
    }
}
