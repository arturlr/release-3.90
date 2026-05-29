using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class ReturnRequestMap : NopEntityTypeConfiguration<ReturnRequest>
    {
        public override void Configure(EntityTypeBuilder<ReturnRequest> builder)
        {
            builder.ToTable("ReturnRequest");
            builder.HasKey(rr => rr.Id);
            builder.Property(rr => rr.ReasonForReturn).IsRequired();
            builder.Property(rr => rr.RequestedAction).IsRequired();

            builder.Ignore(rr => rr.ReturnRequestStatus);

            builder.HasOne(rr => rr.Customer)
                .WithMany(c => c.ReturnRequests)
                .HasForeignKey(rr => rr.CustomerId)
                .IsRequired();
        }
    }
}
