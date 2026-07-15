using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    public partial class ForumTopicMap : NopEntityTypeConfiguration<ForumTopic>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<ForumTopic> builder)
        {
            builder.ToTable("Forums_Topic");
            builder.HasKey(ft => ft.Id);
            builder.Property(ft => ft.Subject).IsRequired().HasMaxLength(450);
            builder.Ignore(ft => ft.ForumTopicType);

            builder.HasOne(ft => ft.Forum)
                .WithMany()
                .HasForeignKey(ft => ft.ForumId)
                .IsRequired();

            builder.HasOne(ft => ft.Customer)
                .WithMany()
                .HasForeignKey(ft => ft.CustomerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);        }
    }
}
