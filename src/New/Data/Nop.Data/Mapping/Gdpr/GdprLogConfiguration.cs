using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Gdpr;

namespace Nop.Data.Mapping.Gdpr;

public class GdprLogConfiguration : IEntityTypeConfiguration<GdprLog>
{
    public void Configure(EntityTypeBuilder<GdprLog> builder)
    {
        builder.ToTable("GdprLog");
        builder.HasKey(gl => gl.Id);
        builder.Ignore(gl => gl.RequestType);
    }
}
