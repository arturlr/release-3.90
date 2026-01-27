using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Topics;

namespace Nop.Data.Mapping.Topics
{
    public partial class TopicTemplateMap : NopEntityTypeConfiguration<TopicTemplate>
    {
        public override void Configure(EntityTypeBuilder<TopicTemplate> builder)
        {
            builder.ToTable("TopicTemplate");
            builder.HasKey(tt => tt.Id);
            builder.Property(tt => tt.Name).IsRequired().HasMaxLength(400);
            builder.Property(tt => tt.ViewPath).IsRequired().HasMaxLength(400);
            base.Configure(builder);
        }
    }
}
