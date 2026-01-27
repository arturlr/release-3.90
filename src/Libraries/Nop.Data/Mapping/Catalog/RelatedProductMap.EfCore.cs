using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class RelatedProductMap : NopEntityTypeConfiguration<RelatedProduct>
    {
        public override void Configure(EntityTypeBuilder<RelatedProduct> builder)
        {
            builder.ToTable("RelatedProduct");
            builder.HasKey(rp => rp.Id);
            base.Configure(builder);
        }
    }
}
