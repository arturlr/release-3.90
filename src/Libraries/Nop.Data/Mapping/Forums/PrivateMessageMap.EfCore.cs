using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Forums;

namespace Nop.Data.Mapping.Forums
{
    public partial class PrivateMessageMap : NopEntityTypeConfiguration<PrivateMessage>
    {
        public override void Configure(EntityTypeBuilder<PrivateMessage> builder)
        {
            builder.ToTable("Forums_PrivateMessage");
            builder.HasKey(pm => pm.Id);
            builder.Property(pm => pm.Subject).IsRequired().HasMaxLength(450);
            builder.Property(pm => pm.Text).IsRequired();
            base.Configure(builder);
        }
    }
}
