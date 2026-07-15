using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Messages;

namespace Nop.Data.Mapping.Messages
{
    public partial class QueuedEmailMap : NopEntityTypeConfiguration<QueuedEmail>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<QueuedEmail> builder)
        {
            builder.ToTable("QueuedEmail");
            builder.HasKey(qe => qe.Id);

            builder.Property(qe => qe.From).IsRequired().HasMaxLength(500);
            builder.Property(qe => qe.FromName).HasMaxLength(500);
            builder.Property(qe => qe.To).IsRequired().HasMaxLength(500);
            builder.Property(qe => qe.ToName).HasMaxLength(500);
            builder.Property(qe => qe.ReplyTo).HasMaxLength(500);
            builder.Property(qe => qe.ReplyToName).HasMaxLength(500);
            builder.Property(qe => qe.CC).HasMaxLength(500);
            builder.Property(qe => qe.Bcc).HasMaxLength(500);
            builder.Property(qe => qe.Subject).HasMaxLength(1000);

            builder.Ignore(qe => qe.Priority);

            builder.HasOne(qe => qe.EmailAccount)
                .WithMany()
                .HasForeignKey(qe => qe.EmailAccountId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);        }
    }
}
