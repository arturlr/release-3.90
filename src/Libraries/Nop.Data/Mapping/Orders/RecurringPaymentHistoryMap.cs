using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class RecurringPaymentHistoryMap : NopEntityTypeConfiguration<RecurringPaymentHistory>
    {
        public override void Configure(EntityTypeBuilder<RecurringPaymentHistory> builder)
        {
            builder.ToTable("RecurringPaymentHistory");
            builder.HasKey(rph => rph.Id);

            builder.HasOne(rph => rph.RecurringPayment)
                .WithMany(rp => rp.RecurringPaymentHistory)
                .HasForeignKey(rph => rph.RecurringPaymentId)
                .IsRequired();
        }
    }
}
