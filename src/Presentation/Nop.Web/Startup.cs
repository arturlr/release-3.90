/* Added by CTA: Please add the correponding references..If certs are not provided for deployment communication will be on http, please remove the https section of the kestrel config in appsettings.json and also remove middleware component app.UseHttpsRedirection(); */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nop.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            ConfigurationManager.Configuration = configuration;
            Nop.Core.CommonHelper.SetContentRootPath(env.ContentRootPath);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllersWithViews();

            services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
            {
                options.ViewLocationExpanders.Add(new Nop.Web.Framework.Themes.ThemeableViewLocationExpander());
            });

            // Initialize nopCommerce engine (mapper config only, DI done in ConfigureContainer)
            var engine = new Nop.Core.Infrastructure.NopEngine();
            Nop.Core.Infrastructure.Singleton<Nop.Core.Infrastructure.IEngine>.Instance = engine;
            engine.RegisterMapperConfiguration(new Nop.Core.Configuration.NopConfig());
        }

        // Called by Autofac after ConfigureServices
        public void ConfigureContainer(Autofac.ContainerBuilder builder)
        {
            var engine = (Nop.Core.Infrastructure.NopEngine)Nop.Core.Infrastructure.EngineContext.Current;
            var config = new Nop.Core.Configuration.NopConfig();
            engine.RegisterDependencies(config, builder);

            // Callback to set the container manager once built
            builder.RegisterBuildCallback(container =>
            {
                engine.SetContainerManager(container);
                // Run startup tasks
                var typeFinder = container.Resolve<Nop.Core.Infrastructure.ITypeFinder>();
                var startUpTaskTypes = typeFinder.FindClassesOfType<Nop.Core.Infrastructure.IStartupTask>();
                foreach (var startUpTaskType in startUpTaskTypes)
                {
                    var task = (Nop.Core.Infrastructure.IStartupTask)System.Activator.CreateInstance(startUpTaskType);
                    task.Execute();
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            // Serve generated thumbnails from content root
            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    System.IO.Path.Combine(env.ContentRootPath, "content")),
                RequestPath = "/content"
            });
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                // Register nopCommerce custom routes
                var routePublisher = Nop.Core.Infrastructure.EngineContext.Current.Resolve<Nop.Web.Framework.Mvc.Routes.IRoutePublisher>();
                routePublisher.RegisterRoutes(endpoints);

                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }

    public class ConfigurationManager
    {
        public static IConfiguration Configuration { get; set; }
    }
}