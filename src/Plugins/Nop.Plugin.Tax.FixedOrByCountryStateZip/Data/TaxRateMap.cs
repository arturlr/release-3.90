using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Data.Mapping;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    /// <summary>
    /// Maps <see cref="TaxRate"/> onto the <c>TaxRate</c> table.
    /// </summary>
    /// <remarks>
    /// Task 15.1 — the mechanical EF6 → EF Core edit task 3.2 defined for all of
    /// <c>Nop.Data</c>'s maps, applied here as task 11.2 applied it to
    /// <c>GoogleProductRecordMap</c>:
    /// <list type="bullet">
    /// <item><c>public TaxRateMap()</c> →
    /// <c>public override void Configure(EntityTypeBuilder&lt;TaxRate&gt; builder)</c> — EF Core
    /// contributes configuration through <c>IEntityTypeConfiguration&lt;T&gt;.Configure</c>.</item>
    /// <item><c>this.</c> → <c>builder.</c></item>
    /// <item><c>base.Configure(builder)</c> at the end, so
    /// <c>NopEntityTypeConfiguration&lt;T&gt;.PostInitialize</c> still runs.</item>
    /// </list>
    /// The table name, key and the <c>Percentage</c> precision are 3.90's.
    /// <c>ToTable("TaxRate")</c> is load-bearing beyond the model:
    /// <c>CountryStateZipObjectContext.Uninstall</c> resolves the name back through
    /// <c>IDbContext.GetTableName&lt;TaxRate&gt;()</c> and drops it, so an EF Core
    /// naming-convention default here would drop the wrong table. <c>HasPrecision(18, 4)</c> is
    /// EF Core's own API and is used verbatim across <c>Nop.Data</c>'s money-column maps
    /// (e.g. <c>OrderItemMap</c>, <c>ProductMap</c>).
    /// </remarks>
    public partial class TaxRateMap : NopEntityTypeConfiguration<TaxRate>
    {
        public override void Configure(EntityTypeBuilder<TaxRate> builder)
        {
            builder.ToTable("TaxRate");
            builder.HasKey(tr => tr.Id);
            builder.Property(tr => tr.Percentage).HasPrecision(18, 4);

            base.Configure(builder);
        }
    }
}
