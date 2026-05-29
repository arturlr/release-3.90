using System.Collections.Generic;
using AutoMapper;
using Nop.Admin.Infrastructure.Mapper;
using Nop.Core.Infrastructure.Mapper;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Admin.Infrastructure
{
    [TestFixture]
    public class AutoMapperConfigurationTest
    {
        [Test]
        public void Configuration_is_valid()
        {
            var profiles = new List<Profile>();
            profiles.Add(new AdminProfile());
            AutoMapperConfiguration.Init(profiles);
            AutoMapperConfiguration.MapperConfiguration.AssertConfigurationIsValid();
        }
    }
}
