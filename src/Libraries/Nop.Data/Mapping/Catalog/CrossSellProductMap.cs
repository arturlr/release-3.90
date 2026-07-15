using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Catalog;

namespace Nop.Data.Mapping.Catalog
{
    public partial class CrossSellProductMap : NopEntityTypeConfiguration<CrossSellProduct>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<CrossSellProduct> builder)
        {
            builder.ToTable("CrossSellProduct");
            builder.HasKey(c => c.Id);
        }
    }
}
