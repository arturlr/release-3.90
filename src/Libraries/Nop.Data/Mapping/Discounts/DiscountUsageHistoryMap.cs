using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Discounts;

namespace Nop.Data.Mapping.Discounts
{
    public partial class DiscountUsageHistoryMap : NopEntityTypeConfiguration<DiscountUsageHistory>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<DiscountUsageHistory> builder)
        {
            builder.ToTable("DiscountUsageHistory");
            builder.HasKey(duh => duh.Id);

            builder.HasOne(duh => duh.Discount)
                .WithMany()
                .HasForeignKey(duh => duh.DiscountId)
                .IsRequired();

            builder.HasOne(duh => duh.Order)
                .WithMany(o => o.DiscountUsageHistory)
                .HasForeignKey(duh => duh.OrderId)
                .IsRequired();        }
    }
}
