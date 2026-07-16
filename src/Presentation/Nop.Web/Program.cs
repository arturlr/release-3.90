using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Services.Tasks;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routes;
using Nop.Web.Framework.Themes;

var builder = WebApplication.CreateBuilder(args);

// Set the application base directory for path mapping (used by CommonHelper.MapPath)
CommonHelper.ApplicationBaseDirectory = builder.Environment.ContentRootPath;

// Set web root to current directory (nopCommerce serves static files from Content/, Scripts/, Themes/)
builder.Environment.WebRootPath = builder.Environment.ContentRootPath;

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Use Autofac as service provider factory
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add services
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddSession();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

// Don't require authentication globally - nopCommerce handles auth via its own filters
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // No global auth requirement
});

// Bind NopConfig
var nopConfig = new NopConfig();
builder.Configuration.GetSection("Nop").Bind(nopConfig);
builder.Services.AddSingleton(nopConfig);

// Register Autofac modules - this is where all nopCommerce DI registrations happen
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register the engine and config
    var typeFinder = new WebAppTypeFinder();
    containerBuilder.RegisterInstance(nopConfig).As<NopConfig>().SingleInstance();
    containerBuilder.RegisterInstance(typeFinder).As<ITypeFinder>().SingleInstance();

    // Find and execute all IDependencyRegistrar implementations
    var drTypes = typeFinder.FindClassesOfType<IDependencyRegistrar>();
    var drInstances = new List<IDependencyRegistrar>();
    foreach (var drType in drTypes)
    {
        try
        {
            drInstances.Add((IDependencyRegistrar)Activator.CreateInstance(drType));
        }
        catch { }
    }
    drInstances = drInstances.OrderBy(t => t.Order).ToList();
    foreach (var dependencyRegistrar in drInstances)
    {
        dependencyRegistrar.Register(containerBuilder, typeFinder, nopConfig);
    }

    // Register the engine itself
    var engine = new NopEngine();
    containerBuilder.RegisterInstance(engine).As<IEngine>().SingleInstance();
});

var app = builder.Build();

// Initialize the EngineContext with the service provider so EngineContext.Current works
var engine = app.Services.GetService<IEngine>() as NopEngine;
if (engine != null)
{
    // Set up the ContainerManager so EngineContext.Current.Resolve<T>() works
    var lifetimeScope = app.Services.GetAutofacRoot();
    var containerManager = new ContainerManager(lifetimeScope);
    engine.SetContainerManager(containerManager);
    Singleton<IEngine>.Instance = engine;
}

// Configure middleware pipeline - show detailed errors
app.UseDeveloperExceptionPage();

app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        builder.Environment.ContentRootPath),
    ServeUnknownFileTypes = true
});

// Use the generic path route middleware for SEO-friendly URLs (BEFORE routing)
app.UseMiddleware<Nop.Web.Framework.Seo.GenericPathRouteMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Named routes for SEO-friendly URL generation (used by Url.RouteUrl in views)
app.MapControllerRoute("Product", "{SeName}", new { controller = "Product", action = "ProductDetails" });
app.MapControllerRoute("Category", "{SeName}", new { controller = "Catalog", action = "Category" });
app.MapControllerRoute("Manufacturer", "{SeName}", new { controller = "Catalog", action = "Manufacturer" });
app.MapControllerRoute("Vendor", "{SeName}", new { controller = "Catalog", action = "Vendor" });
app.MapControllerRoute("NewsItem", "{SeName}", new { controller = "News", action = "NewsItem" });
app.MapControllerRoute("BlogPost", "{SeName}", new { controller = "Blog", action = "BlogPost" });
app.MapControllerRoute("Topic", "{SeName}", new { controller = "Topic", action = "TopicDetails" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Start scheduled tasks if database is installed
if (DataSettingsHelper.DatabaseIsInstalled())
{
    try
    {
        TaskManager.Instance.Initialize();
        TaskManager.Instance.Start();
    }
    catch (Exception ex)
    {
        // Log but don't crash on task initialization failure
        Console.WriteLine($"Warning: Task manager initialization failed: {ex.Message}");
    }
}

app.Run();
