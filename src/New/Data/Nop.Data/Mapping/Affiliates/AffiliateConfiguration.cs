using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Affiliates;

namespace Nop.Data.Mapping.Affiliates;

public class AffiliateConfiguration : IEntityTypeConfiguration<Affiliate>
{
    public void Configure(EntityTypeBuilder<Affiliate> builder)
    {
        builder.ToTable("Affiliate");
        builder.HasKey(a => a.Id);
    }
}
