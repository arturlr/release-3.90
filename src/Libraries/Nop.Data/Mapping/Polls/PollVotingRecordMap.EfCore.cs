using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Polls;

namespace Nop.Data.Mapping.Polls
{
    public partial class PollVotingRecordMap : NopEntityTypeConfiguration<PollVotingRecord>
    {
        public override void Configure(EntityTypeBuilder<PollVotingRecord> builder)
        {
            builder.ToTable("PollVotingRecord");
            builder.HasKey(pvr => pvr.Id);
            builder.HasOne(pvr => pvr.PollAnswer).WithMany(pa => pa.PollVotingRecords).HasForeignKey(pvr => pvr.PollAnswerId).IsRequired();
            builder.HasOne(pvr => pvr.Customer).WithMany().HasForeignKey(pvr => pvr.CustomerId).IsRequired();
            base.Configure(builder);
        }
    }
}
