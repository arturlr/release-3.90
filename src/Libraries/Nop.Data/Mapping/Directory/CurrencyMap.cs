using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;

namespace Nop.Data.Mapping.Directory
{
    public partial class CurrencyMap : NopEntityTypeConfiguration<Currency>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Currency> builder)
        {
            builder.ToTable("Currency");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
            builder.Property(c => c.CurrencyCode).IsRequired().HasMaxLength(5);
            builder.Property(c => c.DisplayLocale).HasMaxLength(50);
            builder.Property(c => c.CustomFormatting).HasMaxLength(50);
            builder.Property(c => c.Rate).HasPrecision(18, 4);

            builder.Ignore(c => c.RoundingType);
        }
    }
}
