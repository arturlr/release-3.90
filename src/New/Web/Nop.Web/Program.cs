using Nop.Web.Framework.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Error handling
builder.Services.AddExceptionHandler<NopExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Error handling middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePagesWithReExecute("/page-not-found", "?statusCode={0}");

app.UseRouting();

app.MapControllers();
app.MapGet("/", () => "nopCommerce");

app.Run();
