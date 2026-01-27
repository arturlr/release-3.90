using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.News;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Polls;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using Nop.Web.Framework.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Use Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register services
    containerBuilder.RegisterType<WebWorkContext>().As<IWorkContext>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ProductService>().As<IProductService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CategoryService>().As<ICategoryService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ManufacturerService>().As<IManufacturerService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CustomerService>().As<ICustomerService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<AuthenticationService>().As<IAuthenticationService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ShoppingCartService>().As<IShoppingCartService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<AddressService>().As<IAddressService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<NewsService>().As<INewsService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<PollService>().As<IPollService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<NewsLetterSubscriptionService>().As<INewsLetterSubscriptionService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<StoreService>().As<IStoreService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<LanguageService>().As<ILanguageService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CurrencyService>().As<ICurrencyService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<CountryService>().As<ICountryService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<ShippingService>().As<IShippingService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<PaymentService>().As<IPaymentService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<TaxService>().As<ITaxService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<DiscountService>().As<IDiscountService>().InstancePerLifetimeScope();
    containerBuilder.RegisterType<VendorService>().As<IVendorService>().InstancePerLifetimeScope();
    
    // Register repository
    containerBuilder.RegisterGeneric(typeof(EfCoreRepository<>)).As(typeof(IRepository<>)).InstancePerLifetimeScope();
});

// Add services
builder.Services.AddNopServices();

// Add DbContext
builder.Services.AddDbContext<NopDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        "Server=(localdb)\\mssqllocaldb;Database=nopCommerce;Trusted_Connection=True;MultipleActiveResultSets=true");
});

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseNopCommerce();

app.Run();
