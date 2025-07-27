using eBriefingWebApp.Interfaces;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace eBriefingWebApp.Models
{
    public class GetGraphUserService : IGetGraphUserService
    {
        private readonly GraphApiConfig _config;

        public GetGraphUserService()
        {
            var tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
            var clientId = ConfigurationManager.AppSettings["ida:ClientId"];
            var clientSecret = ConfigurationManager.AppSettings["ida:ClientSecret"];
            _config = new GraphApiConfig { TenantId = tenantId, ClientId = clientId, ClientSecret = clientSecret };
        }

        public async Task<UserAccount> GetAccountInfo(string userId)
        {
            string accessToken = await GetAccessToken(_config.TenantId, _config.ClientId, _config.ClientSecret);
            UserAccount user = await GetUsernameFromGraphApi(accessToken, userId);
            return user;
        }

        private async Task<string> GetAccessToken(string tenantId, string clientId, string clientSecret)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            string[] scopes = new string[] { "https://graph.microsoft.com/.default" };

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        private async Task<UserAccount> GetUsernameFromGraphApi(string accessToken, string userId)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //HttpResponseMessage response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/users/{userId}");
            HttpResponseMessage response = await httpClient.GetAsync($"https://graph.microsoft.com/beta/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                UserAccount user = JsonConvert.DeserializeObject<UserAccount>(jsonResponse);
                return user;
            }

            return null;
        }
    }
}