using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Shipping.FixedOrByWeight.Domain;

namespace Nop.Plugin.Shipping.FixedOrByWeight.Data
{
    /// <summary>
    /// Maps <see cref="ShippingByWeightRecord"/> onto the <c>ShippingByWeight</c> table.
    /// </summary>
    /// <remarks>
    /// Task 14.4 — the mechanical EF6 → EF Core edit task 3.2 defined for all maps, applied here
    /// exactly as task 11.2 applied it to <c>GoogleProductRecordMap</c>:
    /// <list type="bullet">
    /// <item><c>public ShippingByWeightRecordMap()</c> →
    /// <c>public override void Configure(EntityTypeBuilder&lt;ShippingByWeightRecord&gt; builder)</c>.</item>
    /// <item><c>this.</c> → <c>builder.</c> — including the <c>Zip</c> length facet
    /// (<c>HasMaxLength(400)</c>), which is preserved so the generated column is
    /// <c>nvarchar(400)</c> as in 3.90.</item>
    /// <item><c>base.Configure(builder)</c> at the end so
    /// <c>NopEntityTypeConfiguration&lt;T&gt;.PostInitialize</c> still runs.</item>
    /// </list>
    /// The table name and key are 3.90's. <c>ToTable("ShippingByWeight")</c> is load-bearing beyond
    /// the model: <c>ShippingByWeightObjectContext.Uninstall</c> resolves the name back through
    /// <c>IDbContext.GetTableName&lt;ShippingByWeightRecord&gt;()</c> and drops it, so an EF Core
    /// naming-convention default here would drop the wrong table (or none).
    /// </remarks>
    public partial class ShippingByWeightRecordMap : NopEntityTypeConfiguration<ShippingByWeightRecord>
    {
        public override void Configure(EntityTypeBuilder<ShippingByWeightRecord> builder)
        {
            builder.ToTable("ShippingByWeight");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Zip).HasMaxLength(400);

            base.Configure(builder);
        }
    }
}
