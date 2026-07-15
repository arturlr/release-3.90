using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    public partial class ForumPostVoteMap : NopEntityTypeConfiguration<ForumPostVote>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ForumPostVote> builder)
        {
            builder.ToTable("Forums_PostVote");
            builder.HasKey(fpv => fpv.Id);

            builder.HasOne(fpv => fpv.ForumPost)
                .WithMany()
                .HasForeignKey(fpv => fpv.ForumPostId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);        }
    }
}
