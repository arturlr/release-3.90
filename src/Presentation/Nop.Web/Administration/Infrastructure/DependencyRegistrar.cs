using Autofac;
using Autofac.Core;
using Nop.Admin.Controllers;
using Nop.Core.Caching;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;

namespace Nop.Admin.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            // Controllers with cache manager - in ASP.NET Core, controllers are resolved by DI automatically
            // Keep named parameter registrations for controllers that need static cache
            builder.RegisterType<HomeController>()
                .WithParameter(ResolvedParameter.ForNamed<ICacheManager>("nop_cache_static"));
        }

        public int Order => 2;
    }
}
