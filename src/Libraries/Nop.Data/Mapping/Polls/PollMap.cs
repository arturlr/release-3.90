using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Polls;

namespace Nop.Data.Mapping.Polls
{
    public partial class PollMap : NopEntityTypeConfiguration<Poll>
    {
        public override void Configure(EntityTypeBuilder<Poll> builder)
        {
            builder.ToTable("Poll");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired();

            builder.HasOne(p => p.Language)
                .WithMany()
                .HasForeignKey(p => p.LanguageId)
                .IsRequired();
        }
    }
}
