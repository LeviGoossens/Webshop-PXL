using DotNetEnv.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Webshop.Application.Repositories;
using Webshop.Application.Services;
using Webshop.Application.Services.Contracts;
using Webshop.Domain.Entities;
using Webshop.Infrastructure.Data;
using Webshop.Infrastructure.Repositories;
using Webshop.website;
using Webshop.website.Extensions;

var builder = WebApplication.CreateBuilder(args);
// Load environment variables from the .env file
builder.Configuration.AddDotNetEnv("../.env.local");

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("WebsiteDB"));
});

// Add Identity setup for Users and Roles
builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddServerSideBlazor(); // Enable Blazor Server services 


// Register application services and repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

builder.Services.AddScoped<IAppUserRepository, AppUserRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IIdentityService, IdentityService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IPageService, PageService>();

// Session configuration
builder.Services.AddDistributedMemoryCache(); // Vereist voor session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Winkelmandje blijft 30 min bewaard
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor(); // Voor toegang tot HttpContext in services
builder.Services.AddProjectAuthentication(builder.Configuration);
// Named HttpClient for Stock Web API
builder.Services.AddHttpClient("StockApi", client =>
{
    // In Docker: http://webshop.webapi:8080, locally: http://localhost:8078
    var stockApiUrl = builder.Configuration["StockApiHost"]!;
    client.BaseAddress = new Uri(stockApiUrl);
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["WebApi:Apikey"]);
});

if (!string.IsNullOrWhiteSpace(builder.Configuration.GetValue<string>("ProxyIP")))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto;

        options.KnownProxies.Add(
            System.Net.IPAddress.Parse(builder.Configuration.GetValue<string>("ProxyIP")!));
    });
}

var app = builder.Build();

// Set the default culture to ensure consistent currency and date formatting
var defaultCulture = new CultureInfo("nl-BE"); //for Euro formatting
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);

// ==========================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); // always apply migrations during startup (could cause problems)
    // more info here: https://codebuckets.com/2020/08/14/applying-entity-framework-migrations-to-a-docker-container/
    
    await DbWebsiteInitializer.InitAsync(scope.ServiceProvider, app.Configuration);
    await DbWebsiteInitializer.SeedAsync(scope.ServiceProvider);
}
// ==========================================================

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseBlazorFrameworkFiles(); // Serve WebAssembly files from Webshop.Client
app.UseStaticFiles();

app.UseRouting();
app.UseSession(); // MOET V��R UseAuthorization staan
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub(); // Map Blazor Server Hub (Open the connection for Blazor Server communication)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Shop}/{action=Index}/{id?}");

app.Run();
