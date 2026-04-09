using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Polls;

namespace Nop.Data.Mapping.Polls;

public class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> builder)
    {
        builder.ToTable("Poll");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired();
    }
}

public class PollAnswerConfiguration : IEntityTypeConfiguration<PollAnswer>
{
    public void Configure(EntityTypeBuilder<PollAnswer> builder)
    {
        builder.ToTable("PollAnswer");
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Name).IsRequired();
    }
}

public class PollVotingRecordConfiguration : IEntityTypeConfiguration<PollVotingRecord>
{
    public void Configure(EntityTypeBuilder<PollVotingRecord> builder)
    {
        builder.ToTable("PollVotingRecord");
        builder.HasKey(pvr => pvr.Id);
    }
}
