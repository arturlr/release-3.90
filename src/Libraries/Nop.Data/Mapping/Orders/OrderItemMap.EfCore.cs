using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class OrderItemMap : NopEntityTypeConfiguration<OrderItem>
    {
        public override void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItem");
            builder.HasKey(orderItem => orderItem.Id);

            builder.Property(orderItem => orderItem.UnitPriceInclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.UnitPriceExclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.PriceInclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.PriceExclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.DiscountAmountInclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.DiscountAmountExclTax).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.OriginalProductCost).HasPrecision(18, 4);
            builder.Property(orderItem => orderItem.ItemWeight).HasPrecision(18, 4);

            builder.HasOne(orderItem => orderItem.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(orderItem => orderItem.OrderId)
                .IsRequired();

            builder.HasOne(orderItem => orderItem.Product)
                .WithMany()
                .HasForeignKey(orderItem => orderItem.ProductId)
                .IsRequired();

            base.Configure(builder);
        }
    }
}
