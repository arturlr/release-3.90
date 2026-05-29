using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nop.Core;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// Represents base entity mapping configuration (EF Core equivalent of EntityTypeConfiguration&lt;T&gt;)
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    public abstract class NopEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : BaseEntity
    {
        /// <summary>
        /// Configure entity mapping
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public abstract void Configure(EntityTypeBuilder<TEntity> builder);
    }
}
