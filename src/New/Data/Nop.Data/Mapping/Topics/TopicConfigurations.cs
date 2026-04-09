using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Topics;

namespace Nop.Data.Mapping.Topics;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topic");
        builder.HasKey(t => t.Id);
    }
}

public class TopicTemplateConfiguration : IEntityTypeConfiguration<TopicTemplate>
{
    public void Configure(EntityTypeBuilder<TopicTemplate> builder)
    {
        builder.ToTable("TopicTemplate");
        builder.HasKey(tt => tt.Id);
        builder.Property(tt => tt.Name).IsRequired().HasMaxLength(400);
        builder.Property(tt => tt.ViewPath).IsRequired().HasMaxLength(400);
    }
}
