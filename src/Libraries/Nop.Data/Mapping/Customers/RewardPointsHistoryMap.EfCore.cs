using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Customers;

namespace Nop.Data.Mapping.Customers
{
    public partial class RewardPointsHistoryMap : NopEntityTypeConfiguration<RewardPointsHistory>
    {
        public override void Configure(EntityTypeBuilder<RewardPointsHistory> builder)
        {
            builder.ToTable("RewardPointsHistory");
            builder.HasKey(rph => rph.Id);
            builder.HasOne(rph => rph.Customer).WithMany().HasForeignKey(rph => rph.CustomerId).IsRequired();
            base.Configure(builder);
        }
    }
}
