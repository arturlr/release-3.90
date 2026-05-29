using AutoMapper;

namespace Nop.Core.Infrastructure.Mapper
{
    /// <summary>
    /// Mapper configuration registrar interface.
    /// Implementations provide AutoMapper Profile instances for mapping configuration.
    /// </summary>
    public interface IMapperConfiguration
    {
        /// <summary>
        /// Get the AutoMapper profile for this configuration
        /// </summary>
        /// <returns>AutoMapper Profile instance</returns>
        Profile GetProfile();

        /// <summary>
        /// Order of this mapper implementation
        /// </summary>
        int Order { get; }
    }
}
