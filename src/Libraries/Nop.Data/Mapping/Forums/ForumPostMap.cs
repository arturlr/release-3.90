using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    public partial class ForumPostMap : NopEntityTypeConfiguration<ForumPost>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ForumPost> builder)
        {
            builder.ToTable("Forums_Post");
            builder.HasKey(fp => fp.Id);
            builder.Property(fp => fp.Text).IsRequired();
            builder.Property(fp => fp.IPAddress).HasMaxLength(100);

            builder.HasOne(fp => fp.ForumTopic)
                .WithMany()
                .HasForeignKey(fp => fp.TopicId)
                .IsRequired();

            builder.HasOne(fp => fp.Customer)
                .WithMany()
                .HasForeignKey(fp => fp.CustomerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);        }
    }
}
