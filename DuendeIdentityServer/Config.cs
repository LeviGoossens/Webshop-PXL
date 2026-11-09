using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using System.Security.Claims;

namespace DuendeIdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                // define a new resource for roles so duende knows it can share role claims
                new IdentityResource("roles", "User Roles", new[] { "role" })
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[]
                { };

        public static IEnumerable<Client> GetClients(ConfigurationManager configuration) {
            var duendeSection = configuration.GetSection("Auth:Duende");
            string websiteHost = configuration.GetValue<string>("WebsiteHost")!;
            return new Client[]
                { 
                    new Client
                    {
                        ClientId = duendeSection.GetValue<string>("ClientId")!,
                        ClientSecrets = { new Secret(duendeSection.GetValue<string>("ClientSecret")!.Sha256()) },
                        AllowedGrantTypes = GrantTypes.Code,
                    
                        // require Proof Key for Code Exchange (PKCE) for maximum security standard
                        RequirePkce = true,
                        // allow the application to request refresh tokens to keep users logged in
                        AllowOfflineAccess = true,
                        
                        RedirectUris = { $"{websiteHost}/signin-oidc" },
                        PostLogoutRedirectUris = { $"{websiteHost}/signout-callback-oidc" },

                        AllowedScopes = { "openid", "profile", "email", "roles" },
                        AlwaysIncludeUserClaimsInIdToken = true
                    }
                };
        }
    }
}