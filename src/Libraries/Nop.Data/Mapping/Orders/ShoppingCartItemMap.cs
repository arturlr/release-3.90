using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class ShoppingCartItemMap : NopEntityTypeConfiguration<ShoppingCartItem>
    {
        public override void Configure(EntityTypeBuilder<ShoppingCartItem> builder)
        {
            builder.ToTable("ShoppingCartItem");
            builder.HasKey(sci => sci.Id);

            builder.Property(sci => sci.CustomerEnteredPrice).HasPrecision(18, 4);

            builder.Ignore(sci => sci.ShoppingCartType);
            builder.Ignore(sci => sci.IsFreeShipping);
            builder.Ignore(sci => sci.IsShipEnabled);
            builder.Ignore(sci => sci.AdditionalShippingCharge);
            builder.Ignore(sci => sci.IsTaxExempt);

            builder.HasOne(sci => sci.Customer)
                .WithMany(c => c.ShoppingCartItems)
                .HasForeignKey(sci => sci.CustomerId)
                .IsRequired();

            builder.HasOne(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductId)
                .IsRequired();
        }
    }
}
