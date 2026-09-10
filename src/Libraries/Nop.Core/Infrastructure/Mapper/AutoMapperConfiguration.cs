using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

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
        /// Initialize mapper
        /// </summary>
        /// <param name="configurationActions">Configuration actions</param>
        public static void Init(List<Action<IMapperConfigurationExpression>> configurationActions)
        {
            if (configurationActions == null)
                throw new ArgumentNullException("configurationActions");

            //Task 2.4: AutoMapper 5.x accepted MapperConfiguration(Action<IMapperConfigurationExpression>);
            //from AutoMapper 12 onwards an ILoggerFactory is also required. Nop.Core has no
            //logging configuration of its own at this point, so logging is disabled here;
            //the host can supply a real factory when DI is wired up (tasks 6.4 / 7.2).
            _mapperConfiguration = new MapperConfiguration(cfg =>
            {
                foreach (var ca in configurationActions)
                    ca(cfg);
            }, NullLoggerFactory.Instance);

            _mapper = _mapperConfiguration.CreateMapper();
        }

        /// <summary>
        /// Mapper
        /// </summary>
        public static IMapper Mapper
        {
            get
            {
                return _mapper;
            }
        }
        /// <summary>
        /// Mapper configuration
        /// </summary>
        public static MapperConfiguration MapperConfiguration
        {
            get
            {
                return _mapperConfiguration;
            }
        }
    }
}