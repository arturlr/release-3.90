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
                .HasForeignKey(o => o.CustomerId);
            
            //code below is commented because it causes some issues on big databases - http://www.nopcommerce.com/boards/t/11126/bug-version-20-command-confirm-takes-several-minutes-using-big-databases.aspx
            //this.HasRequired(o => o.BillingAddress).WithOptional().Map(x => x.MapKey("BillingAddressId")).WillCascadeOnDelete(false);
            //this.HasOptional(o => o.ShippingAddress).WithOptionalDependent().Map(x => x.MapKey("ShippingAddressId")).WillCascadeOnDelete(false);
            builder.HasOne(o => o.BillingAddress)
                .WithMany()
                .HasForeignKey(o => o.BillingAddressId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(o => o.ShippingAddress)
                .WithMany()
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(o => o.PickupAddress)
                .WithMany()
                .HasForeignKey(o => o.PickupAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}