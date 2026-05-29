using Autofac;
using Microsoft.Extensions.Configuration;
using Nop.Core.Configuration;

namespace Nop.Core.Infrastructure.DependencyManagement
{
    /// <summary>
    /// Dependency registrar interface. Implementations register services
    /// in the Autofac container during application startup.
    /// </summary>
    public interface IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="builder">Autofac container builder</param>
        /// <param name="typeFinder">Type finder</param>
        /// <param name="config">Nop configuration</param>
        void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config);

        /// <summary>
        /// Gets order of this dependency registrar implementation
        /// </summary>
        int Order { get; }
    }
}
