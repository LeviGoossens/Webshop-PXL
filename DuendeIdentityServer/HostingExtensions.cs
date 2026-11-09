using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Webshop.Application.Services;
using Webshop.Application.Services.Contracts;
using Webshop.Domain.Entities;
using Webshop.Infrastructure.Data;

namespace DuendeIdentityServer
{
    internal static class HostingExtensions
    {
        public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
        {
            // uncomment if you want to add a UI
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("WebsiteDB")));

            builder.Services.AddIdentity<AppUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();


            builder.Services.AddIdentityServer(options =>
                {
                    // https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/api_scopes#authorization-based-on-scopes
                    options.EmitStaticAudienceClaim = true;
                    options.IssuerUri = builder.Configuration.GetValue<string>("Auth:Duende:Authority");
                })
                .AddInMemoryIdentityResources(Config.IdentityResources)
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.GetClients(builder.Configuration))
                .AddAspNetIdentity<AppUser>();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                // Cookies are shared across ports on localhost; keep the server's
                // session separate from the webshop's Identity cookie.
                options.Cookie.Name = ".Webshop.Duende.Identity";

                // Browsers reject SameSite=None cookies without Secure. Local
                // HTTP development needs Lax so the authorize callback sees login.
                options.Cookie.SameSite = builder.Environment.IsDevelopment()
                    ? SameSiteMode.Lax
                    : SameSiteMode.None;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
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

            builder.Services.AddScoped<IIdentityService, IdentityService>();

            return builder.Build();
        }

        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            app.UseSerilogRequestLogging();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseStaticFiles();
            app.UseRouting();
            app.UseForwardedHeaders();
            app.UseIdentityServer();
            app.UseAuthorization();
            app.MapDefaultControllerRoute();

            return app;
        }
    }
}
