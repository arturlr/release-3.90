using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core.Domain.Common;

namespace Nop.Data.Mapping.Common
{
    public partial class SearchTermMap : NopEntityTypeConfiguration<SearchTerm>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<SearchTerm> builder)
        {
            builder.ToTable("SearchTerm");
            builder.HasKey(st => st.Id);
        }
    }
}
