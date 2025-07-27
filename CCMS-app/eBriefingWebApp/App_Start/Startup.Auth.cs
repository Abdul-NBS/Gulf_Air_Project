using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace eBriefingWebApp
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string aadInstance = EnsureTrailingSlash(ConfigurationManager.AppSettings["ida:AADInstance"]);
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];
        private static string authority = aadInstance + tenantId + "/v2.0";

        //    public void ConfigureAuth(IAppBuilder app)
        //    {
        //        // Enforce TLS 1.2
        //        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //        ServicePointManager.Expect100Continue = true;
        //        ServicePointManager.CheckCertificateRevocationList = true;
        //        ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

        //        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        //        app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

        //        app.UseCookieAuthentication(new CookieAuthenticationOptions
        //        {
        //            AuthenticationType = "Cookies",
        //            CookieManager = new Microsoft.Owin.Host.SystemWeb.SystemWebChunkingCookieManager()
        //        });

        //        app.UseOpenIdConnectAuthentication(
        //new OpenIdConnectAuthenticationOptions
        //{
        //    ClientId = clientId,
        //    Authority = authority,
        //    PostLogoutRedirectUri = postLogoutRedirectUri,

        //    Notifications = new OpenIdConnectAuthenticationNotifications()
        //    {
        //        SecurityTokenValidated = async (context) =>
        //        {
        //            var identity = context.AuthenticationTicket.Identity;

        //            // Log all claims for debugging purposes
        //            var allClaims = identity.Claims.ToList();
        //            foreach (var claim in allClaims)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        //            }

        //            // Extract and add the preferred_username as Name claim
        //            string name = identity.FindFirst("preferred_username")?.Value;
        //            if (name != null)
        //            {
        //                identity.AddClaim(new Claim(ClaimTypes.Name, name));
        //                System.Diagnostics.Debug.WriteLine($"Added Name Claim: {name}");
        //            }

        //            // Search the list for claims containing "role:" and add them
        //            foreach (var claim in allClaims)
        //            {
        //                if (claim.Type == "roles" && ((claim.Value.StartsWith("CSM.")) || (claim.Value.StartsWith("Transport."))))
        //                {
        //                    identity.AddClaim(new Claim(ClaimTypes.Role, claim.Value));
        //                    System.Diagnostics.Debug.WriteLine($"Added Role Claim: {claim.Value}");
        //                }
        //            }

        //            await Task.FromResult(0);
        //        }
        //    }
        //});



        //    }

        public void ConfigureAuth(IAppBuilder app)
        {
            // This setup is common for both real auth and the bypass.
            // It ensures the application knows how to handle the auth cookie.
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // --- Conditional Authentication Logic ---
            // Read the bypass setting from Web.config.
            bool.TryParse(ConfigurationManager.AppSettings["Authentication:Bypass"], out bool bypassAuth);

            // If the bypass is enabled, use our fake authentication.
            if (bypassAuth)
            {
                // This is a special message that will show up in your Output window
                // in Visual Studio to remind you that the bypass is active.
                System.Diagnostics.Debug.WriteLine("**************************************************");
                System.Diagnostics.Debug.WriteLine("WARNING: Authentication is BYPASSED for local dev.");
                System.Diagnostics.Debug.WriteLine("**************************************************");

                // Use a simple inline middleware to create and assign a fake user.
                app.Use((context, next) =>
                {
                    var claims = new List<Claim>
            {
                // Mimic the claims your real login provides.
                new Claim("preferred_username", "dev@example.com"),
                new Claim(ClaimTypes.Name, "Dev Local User"),
                new Claim(ClaimTypes.NameIdentifier, "local-dev-user-01"),
                
                // Add any roles you need to test authorization
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "User")
            };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);
                    var principal = new ClaimsPrincipal(identity);

                    // Set the user on the OWIN context. This is the key step.
                    context.Authentication.User = principal;

                    // For broader compatibility in .NET Framework (e.g., older MVC/Web API code)
                    System.Threading.Thread.CurrentPrincipal = principal;
                    if (HttpContext.Current != null)
                    {
                        HttpContext.Current.User = principal;
                    }

                    // Call the next middleware in the OWIN pipeline.
                    return next.Invoke();
                });
            }
            else
            {
                // --- PRODUCTION LOGIC ---
                // If bypass is false, this is your original, unchanged production code.
                app.UseOpenIdConnectAuthentication(
                    new OpenIdConnectAuthenticationOptions
                    {
                        ClientId = clientId,
                        Authority = authority,
                        PostLogoutRedirectUri = postLogoutRedirectUri,

                        Notifications = new OpenIdConnectAuthenticationNotifications()
                        {
                            SecurityTokenValidated = (context) =>
                            {
                                string name = context.AuthenticationTicket.Identity.FindFirst("preferred_username").Value;
                                context.AuthenticationTicket.Identity.AddClaim(new Claim(ClaimTypes.Name, name, string.Empty));
                                return System.Threading.Tasks.Task.FromResult(0);
                            }
                        }
                    });
            }
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}