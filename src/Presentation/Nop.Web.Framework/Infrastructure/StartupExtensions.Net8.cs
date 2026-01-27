using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Web.Framework.Infrastructure
{
    /// <summary>
    /// Startup configuration extensions
    /// </summary>
    public static class StartupExtensions
    {
        /// <summary>
        /// Add nopCommerce services
        /// </summary>
        public static IServiceCollection AddNopServices(this IServiceCollection services)
        {
            // Add MVC
            services.AddControllersWithViews();

            // Add session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add HTTP context accessor
            services.AddHttpContextAccessor();

            return services;
        }

        /// <summary>
        /// Configure Autofac container
        /// </summary>
        public static void ConfigureContainer(ContainerBuilder builder)
        {
            // Register dependencies here
            // This will be called from Program.cs
        }

        /// <summary>
        /// Configure application
        /// </summary>
        public static void UseNopCommerce(this WebApplication app)
        {
            // Use static files
            app.UseStaticFiles();

            // Use routing
            app.UseRouting();

            // Use session
            app.UseSession();

            // Use authentication
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
