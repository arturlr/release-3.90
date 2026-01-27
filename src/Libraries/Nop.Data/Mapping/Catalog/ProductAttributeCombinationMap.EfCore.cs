using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class ProductAttributeCombinationMap : NopEntityTypeConfiguration<ProductAttributeCombination>
    {
        public override void Configure(EntityTypeBuilder<ProductAttributeCombination> builder)
        {
            builder.ToTable("ProductAttributeCombination");
            builder.HasKey(pac => pac.Id);
            builder.Property(pac => pac.Sku).HasMaxLength(400);
            builder.Property(pac => pac.OverriddenPrice).HasPrecision(18, 4);
            base.Configure(builder);
        }
    }
}
