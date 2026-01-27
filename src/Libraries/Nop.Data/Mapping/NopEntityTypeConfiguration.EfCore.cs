using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// Base class for EF Core entity type configurations
    /// </summary>
    public abstract class NopEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : class
    {
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            // Derived classes will override this
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// </summary>
        protected virtual void PostInitialize()
        {
        }
    }
}
