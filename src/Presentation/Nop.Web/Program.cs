using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Tasks;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routes;
using Nop.Web.Framework.Themes;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Use Autofac as service provider factory
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

// Add services
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession();
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies");

// Add DbContext
var dataSettings = new DataSettingsManager().LoadSettings();
if (dataSettings != null && dataSettings.IsValid())
{
    builder.Services.AddDbContext<NopObjectContext>(options =>
        options.UseSqlServer(dataSettings.DataConnectionString));
}

// Bind NopConfig
var nopConfig = new NopConfig();
builder.Configuration.GetSection("Nop").Bind(nopConfig);
builder.Services.AddSingleton(nopConfig);

// Register Autofac modules
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Initialize engine and register dependencies
    var engine = new NopEngine();
    engine.Initialize(nopConfig);
    // Register all IDependencyRegistrar implementations
});

var app = builder.Build();

// Configure middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Use the generic path route middleware for SEO-friendly URLs
app.UseMiddleware<Nop.Web.Framework.Seo.GenericPathRouteMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Start scheduled tasks if database is installed
if (DataSettingsHelper.DatabaseIsInstalled())
{
    TaskManager.Instance.Initialize();
    TaskManager.Instance.Start();
}

app.Run();
