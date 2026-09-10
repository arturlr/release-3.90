using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Nop.Data.Mapping
{
    /// <summary>
    /// Base class for every fluent entity mapping in Nop.Data.
    /// </summary>
    /// <remarks>
    /// EF6 -> EF Core port (task 3.2).
    ///
    /// In EF6 this derived from <c>System.Data.Entity.ModelConfiguration.EntityTypeConfiguration&lt;T&gt;</c>
    /// and every derived <c>*Map</c> class performed its configuration in its CONSTRUCTOR body by
    /// calling inherited members through <c>this.</c> (<c>this.ToTable(...)</c>, <c>this.HasKey(...)</c>, ...).
    ///
    /// EF Core has no equivalent base class. Configuration is contributed through
    /// <see cref="IEntityTypeConfiguration{TEntity}"/>, whose single member is
    /// <c>Configure(EntityTypeBuilder&lt;TEntity&gt; builder)</c>. There is therefore no way to keep the
    /// constructor-based shape: the builder does not exist until EF Core calls <c>Configure</c>.
    ///
    /// The shape chosen is a *virtual* <see cref="Configure"/> that derived maps override, with the
    /// <c>PostInitialize</c> extension point preserved and re-homed from "end of constructor" to
    /// "end of Configure". Each derived map is a mechanical edit:
    ///   * <c>public XyzMap()</c>            -> <c>public override void Configure(EntityTypeBuilder&lt;Xyz&gt; builder)</c>
    ///   * <c>this.</c>                      -> <c>builder.</c>
    ///   * <c>base.Configure(builder);</c> is called at the end of each override so PostInitialize still runs.
    ///
    /// Registration moves from EF6's reflection loop + <c>modelBuilder.Configurations.Add(...)</c> to
    /// <c>ModelBuilder.ApplyConfigurationsFromAssembly</c> in <see cref="NopObjectContext.OnModelCreating"/>.
    /// </remarks>
    public abstract class NopEntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : class
    {
        /// <summary>
        /// Applies the configuration for the entity type. Derived maps override this and MUST call
        /// <c>base.Configure(builder)</c> so <see cref="PostInitialize"/> still runs.
        /// </summary>
        /// <param name="builder">Entity type builder</param>
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code to configuration
        /// </summary>
        protected virtual void PostInitialize()
        {
            
        }
    }
}
