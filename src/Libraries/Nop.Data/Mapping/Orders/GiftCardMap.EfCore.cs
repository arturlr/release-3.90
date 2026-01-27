using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class GiftCardMap : NopEntityTypeConfiguration<GiftCard>
    {
        public override void Configure(EntityTypeBuilder<GiftCard> builder)
        {
            builder.ToTable("GiftCard");
            builder.HasKey(gc => gc.Id);
            builder.Property(gc => gc.Amount).HasPrecision(18, 4);
            builder.Property(gc => gc.RecipientName).HasMaxLength(100);
            builder.Property(gc => gc.RecipientEmail).HasMaxLength(100);
            builder.Property(gc => gc.SenderName).HasMaxLength(100);
            builder.Property(gc => gc.SenderEmail).HasMaxLength(100);
            base.Configure(builder);
        }
    }
}
