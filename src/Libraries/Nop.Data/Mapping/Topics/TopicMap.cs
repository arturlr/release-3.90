using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Topics;

namespace Nop.Data.Mapping.Topics
{
    public class TopicMap : NopEntityTypeConfiguration<Topic>
    {
        public override void Configure(EntityTypeBuilder<Topic> builder)
        {
            builder.ToTable("Topic");
            builder.HasKey(t => t.Id);

            base.Configure(builder);
        }
    }
}
