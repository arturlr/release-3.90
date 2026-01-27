using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Logging;

namespace Nop.Data.Mapping.Logging
{
    public partial class LogMap : NopEntityTypeConfiguration<Log>
    {
        public override void Configure(EntityTypeBuilder<Log> builder)
        {
            builder.ToTable("Log");
            builder.HasKey(l => l.Id);
            
            builder.Property(l => l.ShortMessage).IsRequired();
            builder.Property(l => l.IpAddress).HasMaxLength(200);

            base.Configure(builder);
        }
    }
}
