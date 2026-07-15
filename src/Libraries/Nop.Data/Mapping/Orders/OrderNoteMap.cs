using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class OrderNoteMap : NopEntityTypeConfiguration<OrderNote>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<OrderNote> builder)
        {
            builder.ToTable("OrderNote");
            builder.HasKey(on => on.Id);
            builder.Property(on => on.Note).IsRequired();

            builder.HasOne(on => on.Order)
                .WithMany(o => o.OrderNotes)
                .HasForeignKey(on => on.OrderId)
                .IsRequired();        }
    }
}
