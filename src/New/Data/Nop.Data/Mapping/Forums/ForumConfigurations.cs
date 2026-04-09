using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums;

public class ForumGroupConfiguration : IEntityTypeConfiguration<ForumGroup>
{
    public void Configure(EntityTypeBuilder<ForumGroup> builder)
    {
        builder.ToTable("Forums_Group");
        builder.HasKey(fg => fg.Id);
        builder.Property(fg => fg.Name).IsRequired().HasMaxLength(200);
    }
}

public class ForumConfiguration : IEntityTypeConfiguration<Forum>
{
    public void Configure(EntityTypeBuilder<Forum> builder)
    {
        builder.ToTable("Forums_Forum");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(200);
    }
}

public class ForumTopicConfiguration : IEntityTypeConfiguration<ForumTopic>
{
    public void Configure(EntityTypeBuilder<ForumTopic> builder)
    {
        builder.ToTable("Forums_Topic");
        builder.HasKey(ft => ft.Id);
        builder.Property(ft => ft.Subject).IsRequired().HasMaxLength(450);
        builder.Ignore(ft => ft.ForumTopicType);
        builder.Ignore(ft => ft.NumReplies);
    }
}

public class ForumPostConfiguration : IEntityTypeConfiguration<ForumPost>
{
    public void Configure(EntityTypeBuilder<ForumPost> builder)
    {
        builder.ToTable("Forums_Post");
        builder.HasKey(fp => fp.Id);
        builder.Property(fp => fp.Text).IsRequired();
        builder.Property(fp => fp.IPAddress).HasMaxLength(100);
    }
}

public class ForumPostVoteConfiguration : IEntityTypeConfiguration<ForumPostVote>
{
    public void Configure(EntityTypeBuilder<ForumPostVote> builder)
    {
        builder.ToTable("Forums_PostVote");
        builder.HasKey(fpv => fpv.Id);
    }
}

public class ForumSubscriptionConfiguration : IEntityTypeConfiguration<ForumSubscription>
{
    public void Configure(EntityTypeBuilder<ForumSubscription> builder)
    {
        builder.ToTable("Forums_Subscription");
        builder.HasKey(fs => fs.Id);
    }
}

public class PrivateMessageConfiguration : IEntityTypeConfiguration<PrivateMessage>
{
    public void Configure(EntityTypeBuilder<PrivateMessage> builder)
    {
        builder.ToTable("Forums_PrivateMessage");
        builder.HasKey(pm => pm.Id);
        builder.Property(pm => pm.Subject).IsRequired().HasMaxLength(450);
        builder.Property(pm => pm.Text).IsRequired();
    }
}
