using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Messages;

namespace Nop.Data.Mapping.Messages
{
    public partial class MessageTemplateMap : NopEntityTypeConfiguration<MessageTemplate>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<MessageTemplate> builder)
        {
            builder.ToTable("MessageTemplate");
            builder.HasKey(mt => mt.Id);

            builder.Property(mt => mt.Name).IsRequired().HasMaxLength(200);
            builder.Property(mt => mt.BccEmailAddresses).HasMaxLength(200);
            builder.Property(mt => mt.Subject).HasMaxLength(1000);

            builder.Ignore(mt => mt.DelayPeriod);        }
    }
}
