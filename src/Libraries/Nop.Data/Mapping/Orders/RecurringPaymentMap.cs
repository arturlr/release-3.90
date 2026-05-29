using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class RecurringPaymentMap : NopEntityTypeConfiguration<RecurringPayment>
    {
        public override void Configure(EntityTypeBuilder<RecurringPayment> builder)
        {
            builder.ToTable("RecurringPayment");
            builder.HasKey(rp => rp.Id);

            builder.Ignore(rp => rp.NextPaymentDate);
            builder.Ignore(rp => rp.CyclesRemaining);
            builder.Ignore(rp => rp.CyclePeriod);

            builder.HasOne(rp => rp.InitialOrder)
                .WithMany()
                .HasForeignKey(o => o.InitialOrderId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
