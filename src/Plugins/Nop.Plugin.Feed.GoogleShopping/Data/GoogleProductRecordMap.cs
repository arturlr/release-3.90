using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    /// <summary>
    /// Maps <see cref="GoogleProductRecord"/> onto the <c>GoogleProduct</c> table.
    /// </summary>
    /// <remarks>
    /// Task 11.2 — the mechanical EF6 → EF Core edit task 3.2 defined for all 105 of
    /// <c>Nop.Data</c>'s maps, applied to the first PLUGIN map:
    /// <list type="bullet">
    /// <item><c>public GoogleProductRecordMap()</c> →
    /// <c>public override void Configure(EntityTypeBuilder&lt;GoogleProductRecord&gt; builder)</c>
    /// — EF Core contributes configuration through
    /// <c>IEntityTypeConfiguration&lt;T&gt;.Configure</c>, and the builder does not exist until
    /// EF Core calls it, so the constructor-based shape cannot be kept.</item>
    /// <item><c>this.</c> → <c>builder.</c></item>
    /// <item><c>base.Configure(builder)</c> at the end, so
    /// <c>NopEntityTypeConfiguration&lt;T&gt;.PostInitialize</c> still runs — the extension point
    /// a store's own partial class may have used.</item>
    /// </list>
    /// The table name and key are 3.90's. <c>ToTable("GoogleProduct")</c> is load-bearing beyond
    /// the model: <c>GoogleProductObjectContext.Uninstall</c> resolves the name back through
    /// <c>IDbContext.GetTableName&lt;GoogleProductRecord&gt;()</c> and drops it, so an EF Core
    /// naming-convention default here would drop the wrong table (or none).
    /// </remarks>
    public partial class GoogleProductRecordMap : NopEntityTypeConfiguration<GoogleProductRecord>
    {
        public override void Configure(EntityTypeBuilder<GoogleProductRecord> builder)
        {
            builder.ToTable("GoogleProduct");
            builder.HasKey(x => x.Id);

            base.Configure(builder);
        }
    }
}
