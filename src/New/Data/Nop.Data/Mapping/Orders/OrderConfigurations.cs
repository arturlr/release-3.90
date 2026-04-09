using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
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
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItem");
        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.UnitPriceInclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.UnitPriceExclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.PriceInclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.PriceExclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.DiscountAmountInclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.DiscountAmountExclTax).HasPrecision(18, 4);
        builder.Property(oi => oi.OriginalProductCost).HasPrecision(18, 4);
        builder.Property(oi => oi.ItemWeight).HasPrecision(18, 4);
    }
}

public class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.ToTable("OrderNote");
        builder.HasKey(on => on.Id);
        builder.Property(on => on.Note).IsRequired();
    }
}

public class CheckoutAttributeConfiguration : IEntityTypeConfiguration<CheckoutAttribute>
{
    public void Configure(EntityTypeBuilder<CheckoutAttribute> builder)
    {
        builder.ToTable("CheckoutAttribute");
        builder.HasKey(ca => ca.Id);
        builder.Ignore(ca => ca.AttributeControlType);
    }
}

public class CheckoutAttributeValueConfiguration : IEntityTypeConfiguration<CheckoutAttributeValue>
{
    public void Configure(EntityTypeBuilder<CheckoutAttributeValue> builder)
    {
        builder.ToTable("CheckoutAttributeValue");
        builder.HasKey(cav => cav.Id);
        builder.Property(cav => cav.Name).IsRequired().HasMaxLength(400);
        builder.Property(cav => cav.ColorSquaresRgb).HasMaxLength(100);
        builder.Property(cav => cav.PriceAdjustment).HasPrecision(18, 4);
        builder.Property(cav => cav.WeightAdjustment).HasPrecision(18, 4);
    }
}

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("GiftCard");
        builder.HasKey(gc => gc.Id);
        builder.Property(gc => gc.Amount).HasPrecision(18, 4);
        builder.Ignore(gc => gc.GiftCardType);
    }
}

public class GiftCardUsageHistoryConfiguration : IEntityTypeConfiguration<GiftCardUsageHistory>
{
    public void Configure(EntityTypeBuilder<GiftCardUsageHistory> builder)
    {
        builder.ToTable("GiftCardUsageHistory");
        builder.HasKey(gcuh => gcuh.Id);
        builder.Property(gcuh => gcuh.UsedValue).HasPrecision(18, 4);
    }
}

public class RecurringPaymentConfiguration : IEntityTypeConfiguration<RecurringPayment>
{
    public void Configure(EntityTypeBuilder<RecurringPayment> builder)
    {
        builder.ToTable("RecurringPayment");
        builder.HasKey(rp => rp.Id);
    }
}

public class RecurringPaymentHistoryConfiguration : IEntityTypeConfiguration<RecurringPaymentHistory>
{
    public void Configure(EntityTypeBuilder<RecurringPaymentHistory> builder)
    {
        builder.ToTable("RecurringPaymentHistory");
        builder.HasKey(rph => rph.Id);
    }
}

public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequest");
        builder.HasKey(rr => rr.Id);
        builder.Property(rr => rr.ReasonForReturn).IsRequired();
        builder.Property(rr => rr.RequestedAction).IsRequired();
        builder.Ignore(rr => rr.ReturnRequestStatus);
    }
}

public class ReturnRequestActionConfiguration : IEntityTypeConfiguration<ReturnRequestAction>
{
    public void Configure(EntityTypeBuilder<ReturnRequestAction> builder)
    {
        builder.ToTable("ReturnRequestAction");
        builder.HasKey(rra => rra.Id);
        builder.Property(rra => rra.Name).IsRequired().HasMaxLength(400);
    }
}

public class ReturnRequestReasonConfiguration : IEntityTypeConfiguration<ReturnRequestReason>
{
    public void Configure(EntityTypeBuilder<ReturnRequestReason> builder)
    {
        builder.ToTable("ReturnRequestReason");
        builder.HasKey(rrr => rrr.Id);
        builder.Property(rrr => rrr.Name).IsRequired().HasMaxLength(400);
    }
}

public class ShoppingCartItemConfiguration : IEntityTypeConfiguration<ShoppingCartItem>
{
    public void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
    {
        builder.ToTable("ShoppingCartItem");
        builder.HasKey(sci => sci.Id);
        builder.Property(sci => sci.CustomerEnteredPrice).HasPrecision(18, 4);
        builder.Ignore(sci => sci.ShoppingCartType);
    }
}
