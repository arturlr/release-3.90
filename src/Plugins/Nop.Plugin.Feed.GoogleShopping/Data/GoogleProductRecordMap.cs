using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    public partial class GoogleProductRecordMap : NopEntityTypeConfiguration<GoogleProductRecord>
    {
        public override void Configure(EntityTypeBuilder<GoogleProductRecord> builder)
        {
            builder.ToTable("GoogleProduct");
            builder.HasKey(x => x.Id);
        }
    }
}
