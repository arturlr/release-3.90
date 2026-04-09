using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Common;

namespace Nop.Data.Mapping.Common;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Address");
        builder.HasKey(a => a.Id);
    }
}

public class AddressAttributeConfiguration : IEntityTypeConfiguration<AddressAttribute>
{
    public void Configure(EntityTypeBuilder<AddressAttribute> builder)
    {
        builder.ToTable("AddressAttribute");
        builder.HasKey(aa => aa.Id);
        builder.Property(aa => aa.Name).IsRequired().HasMaxLength(400);
        builder.Ignore(aa => aa.AttributeControlType);
    }
}

public class AddressAttributeValueConfiguration : IEntityTypeConfiguration<AddressAttributeValue>
{
    public void Configure(EntityTypeBuilder<AddressAttributeValue> builder)
    {
        builder.ToTable("AddressAttributeValue");
        builder.HasKey(aav => aav.Id);
        builder.Property(aav => aav.Name).IsRequired().HasMaxLength(400);
    }
}

public class GenericAttributeConfiguration : IEntityTypeConfiguration<GenericAttribute>
{
    public void Configure(EntityTypeBuilder<GenericAttribute> builder)
    {
        builder.ToTable("GenericAttribute");
        builder.HasKey(ga => ga.Id);
        builder.Property(ga => ga.KeyGroup).IsRequired().HasMaxLength(400);
        builder.Property(ga => ga.Key).IsRequired().HasMaxLength(400);
        builder.Property(ga => ga.Value).IsRequired();
    }
}

public class SearchTermConfiguration : IEntityTypeConfiguration<SearchTerm>
{
    public void Configure(EntityTypeBuilder<SearchTerm> builder)
    {
        builder.ToTable("SearchTerm");
        builder.HasKey(st => st.Id);
    }
}
