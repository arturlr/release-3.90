using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class RelatedProductMap : NopEntityTypeConfiguration<RelatedProduct>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<RelatedProduct> builder)
        {
            builder.ToTable("RelatedProduct");
            builder.HasKey(c => c.Id);
        }
    }
}
