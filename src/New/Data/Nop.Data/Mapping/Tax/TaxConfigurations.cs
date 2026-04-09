using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Tax;

namespace Nop.Data.Mapping.Tax;

public class TaxCategoryConfiguration : IEntityTypeConfiguration<TaxCategory>
{
    public void Configure(EntityTypeBuilder<TaxCategory> builder)
    {
        builder.ToTable("TaxCategory");
        builder.HasKey(tc => tc.Id);
        builder.Property(tc => tc.Name).IsRequired().HasMaxLength(400);
    }
}
