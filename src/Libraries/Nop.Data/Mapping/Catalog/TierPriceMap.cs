using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class TierPriceMap : NopEntityTypeConfiguration<TierPrice>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<TierPrice> builder)
        {
            builder.ToTable("TierPrice");
            builder.HasKey(tp => tp.Id);
            builder.Property(tp => tp.Price).HasPrecision(18, 4);

            builder.HasOne(tp => tp.Product)
                .WithMany(p => p.TierPrices)
                .HasForeignKey(tp => tp.ProductId)
                .IsRequired();

            builder.HasOne(tp => tp.CustomerRole)
                .WithMany()
                .HasForeignKey(tp => tp.CustomerRoleId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
