using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Mapping.Vendors;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendor");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Name).IsRequired().HasMaxLength(400);
        builder.Property(v => v.Email).HasMaxLength(400);
        builder.Property(v => v.MetaKeywords).HasMaxLength(400);
        builder.Property(v => v.MetaTitle).HasMaxLength(400);
        builder.Property(v => v.PageSizeOptions).HasMaxLength(200);
    }
}

public class VendorNoteConfiguration : IEntityTypeConfiguration<VendorNote>
{
    public void Configure(EntityTypeBuilder<VendorNote> builder)
    {
        builder.ToTable("VendorNote");
        builder.HasKey(vn => vn.Id);
        builder.Property(vn => vn.Note).IsRequired();
    }
}
