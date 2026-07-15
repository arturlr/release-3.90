using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class GiftCardMap : NopEntityTypeConfiguration<GiftCard>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<GiftCard> builder)
        {
            builder.ToTable("GiftCard");
            builder.HasKey(gc => gc.Id);

            builder.Property(gc => gc.Amount).HasPrecision(18, 4);

            builder.Ignore(gc => gc.GiftCardType);

            builder.HasOne(gc => gc.PurchasedWithOrderItem)
                .WithMany(orderItem => orderItem.AssociatedGiftCards)
                .HasForeignKey(gc => gc.PurchasedWithOrderItemId)
                .IsRequired(false);        }
    }
}
