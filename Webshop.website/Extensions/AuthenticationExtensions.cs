using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace Webshop.website.Extensions;

public static class AuthenticationExtensions
{
    public static void AddProjectAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var googleAuth = configuration.GetSection("Auth:Google");
        var duendeAuth = configuration.GetSection("Auth:Duende");

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            .AddCookie("Cookies")
            .AddGoogle("google", options =>
            {
                options.ClientId = googleAuth.GetValue<string>("ClientId")!;
                options.ClientSecret = googleAuth.GetValue<string>("ClientSecret")!;
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.SignInScheme = "Identity.Application";
                options.Authority = duendeAuth.GetValue<string>("Authority")!;

                options.ClientId = duendeAuth.GetValue<string>("ClientId")!;
                options.ClientSecret = duendeAuth.GetValue<string>("ClientSecret")!;
                options.ResponseType = "code";
                options.SaveTokens = false;

                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("roles");

                options.GetClaimsFromUserInfoEndpoint = true;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.ClaimActions.MapJsonKey("role", "role", "role");

                // Discovery, keys, tokens and user info use the internal Authority.
                // Only browser redirects need the publicly reachable Duende URL.
                var publicAuthority = duendeAuth.GetValue<string>("PublicAuthority")?.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(publicAuthority))
                {
                    options.Events.OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.IssuerAddress = $"{publicAuthority}/connect/authorize";
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                    {
                        context.ProtocolMessage.IssuerAddress = $"{publicAuthority}/connect/endsession";
                        return Task.CompletedTask;
                    };
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "email",
                    RoleClaimType = "role"
                };
            });
    }
}
