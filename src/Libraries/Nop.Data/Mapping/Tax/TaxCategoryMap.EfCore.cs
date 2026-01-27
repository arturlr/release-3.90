using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Tax;

namespace Nop.Data.Mapping.Tax
{
    public partial class TaxCategoryMap : NopEntityTypeConfiguration<TaxCategory>
    {
        public override void Configure(EntityTypeBuilder<TaxCategory> builder)
        {
            builder.ToTable("TaxCategory");
            builder.HasKey(tc => tc.Id);
            
            builder.Property(tc => tc.Name).IsRequired().HasMaxLength(400);

            base.Configure(builder);
        }
    }
}
