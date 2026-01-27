using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Affiliates;

namespace Nop.Data.Mapping.Affiliates
{
    public partial class AffiliateMap : NopEntityTypeConfiguration<Affiliate>
    {
        public override void Configure(EntityTypeBuilder<Affiliate> builder)
        {
            builder.ToTable("Affiliate");
            builder.HasKey(a => a.Id);
            builder.HasOne(a => a.Address).WithMany().HasForeignKey(a => a.AddressId).IsRequired();
            base.Configure(builder);
        }
    }
}
