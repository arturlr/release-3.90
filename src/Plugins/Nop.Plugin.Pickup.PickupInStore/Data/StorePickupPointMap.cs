using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Pickup.PickupInStore.Domain;

namespace Nop.Plugin.Pickup.PickupInStore.Data
{
    /// <summary>
    /// Maps <see cref="StorePickupPoint"/> onto the <c>StorePickupPoint</c> table.
    /// </summary>
    /// <remarks>
    /// Task 13.1 — the mechanical EF6 → EF Core edit task 3.2 defined for all of <c>Nop.Data</c>'s
    /// maps, applied to this plugin map (worked example: <c>GoogleProductRecordMap</c>, 11.2):
    /// <list type="bullet">
    /// <item><c>public StorePickupPointMap()</c> →
    /// <c>public override void Configure(EntityTypeBuilder&lt;StorePickupPoint&gt; builder)</c>
    /// — EF Core contributes configuration through <c>IEntityTypeConfiguration&lt;T&gt;.Configure</c>,
    /// and the builder does not exist until EF Core calls it, so the constructor shape cannot be
    /// kept.</item>
    /// <item><c>this.</c> → <c>builder.</c></item>
    /// <item><c>Property(...).HasPrecision(18, 4)</c> is unchanged in spelling: EF Core's
    /// <c>PropertyBuilder</c> also exposes <c>HasPrecision(int precision, int scale)</c> (since EF
    /// Core 6), and it configures the same <c>decimal(18,4)</c> column facet EF6 did.</item>
    /// <item><c>base.Configure(builder)</c> at the end, so
    /// <c>NopEntityTypeConfiguration&lt;T&gt;.PostInitialize</c> still runs.</item>
    /// </list>
    /// <c>ToTable("StorePickupPoint")</c> is 3.90's and is load-bearing beyond the model:
    /// <c>StorePickupPointObjectContext.Uninstall</c> resolves the name back through
    /// <c>IDbContext.GetTableName&lt;StorePickupPoint&gt;()</c> and drops it, so an EF Core
    /// naming-convention default here would drop the wrong table (or none).
    /// </remarks>
    public partial class StorePickupPointMap : NopEntityTypeConfiguration<StorePickupPoint>
    {
        public override void Configure(EntityTypeBuilder<StorePickupPoint> builder)
        {
            builder.ToTable("StorePickupPoint");
            builder.HasKey(point => point.Id);
            builder.Property(point => point.PickupFee).HasPrecision(18, 4);

            base.Configure(builder);
        }
    }
}
