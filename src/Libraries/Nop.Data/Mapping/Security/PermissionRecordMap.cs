using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Security;

namespace Nop.Data.Mapping.Security
{
    public partial class PermissionRecordMap : NopEntityTypeConfiguration<PermissionRecord>
    {
        public override void Configure(EntityTypeBuilder<PermissionRecord> builder)
        {
            builder.ToTable("PermissionRecord");
            builder.HasKey(pr => pr.Id);
            builder.Property(pr => pr.Name).IsRequired();
            builder.Property(pr => pr.SystemName).IsRequired().HasMaxLength(255);
            builder.Property(pr => pr.Category).IsRequired().HasMaxLength(255);

            //Runtime deferral 4.12, closed by task 7.7 - see the long note in
            //Mapping/Catalog/ProductMap.cs. 3.90's EF6 column names are
            //(PermissionRecord_Id, CustomerRole_Id).
            builder.HasMany(pr => pr.CustomerRoles)
                .WithMany(cr => cr.PermissionRecords)
                .UsingEntity(j =>
                {
                    j.ToTable("PermissionRecord_Role_Mapping");
                    j.Property<int>("PermissionRecordsId").HasColumnName("PermissionRecord_Id");
                    j.Property<int>("CustomerRolesId").HasColumnName("CustomerRole_Id");
                });

            base.Configure(builder);
        }
    }
}