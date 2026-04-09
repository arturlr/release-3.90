using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Directory;

namespace Nop.Data.Mapping.Directory;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Country");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.TwoLetterIsoCode).HasMaxLength(2);
        builder.Property(c => c.ThreeLetterIsoCode).HasMaxLength(3);
    }
}

public class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currency");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(50);
        builder.Property(c => c.CurrencyCode).IsRequired().HasMaxLength(5);
        builder.Property(c => c.Rate).HasPrecision(18, 8);
        builder.Property(c => c.DisplayLocale).HasMaxLength(50);
        builder.Property(c => c.CustomFormatting).HasMaxLength(50);
        builder.Ignore(c => c.RoundingType);
    }
}

public class StateProvinceConfiguration : IEntityTypeConfiguration<StateProvince>
{
    public void Configure(EntityTypeBuilder<StateProvince> builder)
    {
        builder.ToTable("StateProvince");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Name).IsRequired().HasMaxLength(100);
        builder.Property(sp => sp.Abbreviation).HasMaxLength(100);
    }
}

public class MeasureDimensionConfiguration : IEntityTypeConfiguration<MeasureDimension>
{
    public void Configure(EntityTypeBuilder<MeasureDimension> builder)
    {
        builder.ToTable("MeasureDimension");
        builder.HasKey(md => md.Id);
        builder.Property(md => md.Name).IsRequired().HasMaxLength(100);
        builder.Property(md => md.SystemKeyword).IsRequired().HasMaxLength(100);
        builder.Property(md => md.Ratio).HasPrecision(18, 8);
    }
}

public class MeasureWeightConfiguration : IEntityTypeConfiguration<MeasureWeight>
{
    public void Configure(EntityTypeBuilder<MeasureWeight> builder)
    {
        builder.ToTable("MeasureWeight");
        builder.HasKey(mw => mw.Id);
        builder.Property(mw => mw.Name).IsRequired().HasMaxLength(100);
        builder.Property(mw => mw.SystemKeyword).IsRequired().HasMaxLength(100);
        builder.Property(mw => mw.Ratio).HasPrecision(18, 8);
    }
}
