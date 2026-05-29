using System;
using System.Collections.Generic;
using AutoMapper;

namespace Nop.Core.Infrastructure.Mapper
{
    /// <summary>
    /// AutoMapper configuration
    /// </summary>
    public static class AutoMapperConfiguration
    {
        private static MapperConfiguration _mapperConfiguration;
        private static IMapper _mapper;

        /// <summary>
        /// Initialize mapper with Profile instances (AutoMapper 16+ pattern)
        /// </summary>
        /// <param name="profiles">List of AutoMapper profiles</param>
        public static void Init(List<Profile> profiles)
        {
            if (profiles == null)
                throw new ArgumentNullException(nameof(profiles));

            _mapperConfiguration = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles)
                    cfg.AddProfile(profile);
            }, loggerFactory: null);

            _mapper = _mapperConfiguration.CreateMapper();
        }

        /// <summary>
        /// Mapper
        /// </summary>
        public static IMapper Mapper
        {
            get { return _mapper; }
        }

        /// <summary>
        /// Mapper configuration
        /// </summary>
        public static MapperConfiguration MapperConfiguration
        {
            get { return _mapperConfiguration; }
        }
    }
}
