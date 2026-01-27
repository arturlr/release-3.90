using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Messages;

namespace Nop.Data.Mapping.Messages
{
    public partial class CampaignMap : NopEntityTypeConfiguration<Campaign>
    {
        public override void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.ToTable("Campaign");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired();
            builder.Property(c => c.Subject).IsRequired();
            builder.Property(c => c.Body).IsRequired();
            base.Configure(builder);
        }
    }
}
