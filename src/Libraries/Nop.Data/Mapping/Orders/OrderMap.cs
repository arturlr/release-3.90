using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class OrderMap : NopEntityTypeConfiguration<Order>
    {
        public override void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Order");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.CurrencyRate).HasPrecision(18, 8);
            builder.Property(o => o.OrderSubtotalInclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderSubtotalExclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderSubTotalDiscountInclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderSubTotalDiscountExclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderShippingInclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderShippingExclTax).HasPrecision(18, 4);
            builder.Property(o => o.PaymentMethodAdditionalFeeInclTax).HasPrecision(18, 4);
            builder.Property(o => o.PaymentMethodAdditionalFeeExclTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderTax).HasPrecision(18, 4);
            builder.Property(o => o.OrderDiscount).HasPrecision(18, 4);
            builder.Property(o => o.OrderTotal).HasPrecision(18, 4);
            builder.Property(o => o.RefundedAmount).HasPrecision(18, 4);
            builder.Property(o => o.CustomOrderNumber).IsRequired();

            builder.Ignore(o => o.OrderStatus);
            builder.Ignore(o => o.PaymentStatus);
            builder.Ignore(o => o.ShippingStatus);
            builder.Ignore(o => o.CustomerTaxDisplayType);
            builder.Ignore(o => o.TaxRatesDictionary);

            builder.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .IsRequired();

            builder.HasOne(o => o.BillingAddress)
                .WithMany()
                .HasForeignKey(o => o.BillingAddressId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.PickupAddress)
                .WithMany()
                .HasForeignKey(o => o.PickupAddressId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
