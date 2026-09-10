using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Mapping.Vendors
{
    public partial class VendorNoteMap : NopEntityTypeConfiguration<VendorNote>
    {
        public override void Configure(EntityTypeBuilder<VendorNote> builder)
        {
            builder.ToTable("VendorNote");
            builder.HasKey(vn => vn.Id);
            builder.Property(vn => vn.Note).IsRequired();

            builder.HasOne(vn => vn.Vendor)
                .WithMany(v => v.VendorNotes)
                .HasForeignKey(vn => vn.VendorId);

            base.Configure(builder);
        }
    }
}