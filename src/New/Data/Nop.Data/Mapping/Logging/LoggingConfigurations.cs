using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Logging;

namespace Nop.Data.Mapping.Logging;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLog");
        builder.HasKey(al => al.Id);
        builder.Property(al => al.Comment).IsRequired();
        builder.Property(al => al.IpAddress).HasMaxLength(200);
    }
}

public class ActivityLogTypeConfiguration : IEntityTypeConfiguration<ActivityLogType>
{
    public void Configure(EntityTypeBuilder<ActivityLogType> builder)
    {
        builder.ToTable("ActivityLogType");
        builder.HasKey(alt => alt.Id);
        builder.Property(alt => alt.SystemKeyword).IsRequired().HasMaxLength(100);
        builder.Property(alt => alt.Name).IsRequired().HasMaxLength(200);
    }
}

public class LogConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> builder)
    {
        builder.ToTable("Log");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ShortMessage).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(200);
        builder.Ignore(l => l.LogLevel);
    }
}
