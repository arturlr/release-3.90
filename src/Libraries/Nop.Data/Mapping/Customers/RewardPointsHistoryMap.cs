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

            builder.Property(rph => rph.UsedAmount).HasPrecision(18, 4);

            builder.HasOne(rph => rph.Customer)
                .WithMany()
                .HasForeignKey(rph => rph.CustomerId);

            //EF6: HasOptional(...).WithOptionalDependent(...) - a one-to-one where the
            //dependent end (RewardPointsHistory) carried an EF-generated FK. There is no
            //UsedWithOrderId CLR property on RewardPointsHistory, so EF Core needs the
            //principal/dependent ends stated explicitly and the FK declared as a shadow
            //property. "UsedWithOrder_Id" keeps the legacy 3.90 column name.
            builder.HasOne(rph => rph.UsedWithOrder)
                .WithOne(o => o.RedeemedRewardPointsEntry)
                .HasForeignKey<RewardPointsHistory>("UsedWithOrder_Id")
                .OnDelete(DeleteBehavior.Restrict);

            base.Configure(builder);
        }
    }
}