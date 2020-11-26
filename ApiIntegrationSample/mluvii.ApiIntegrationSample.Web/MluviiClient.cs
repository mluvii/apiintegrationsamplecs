using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using mluvii.ApiIntegrationSample.Web.Serialization;
using mluvii.ApiModels.Sessions;
using mluvii.ApiModels.Webhooks;
using Newtonsoft.Json;

namespace mluvii.ApiIntegrationSample.Web
{
    public class MluviiClient
    {
        private class TokenHolder
        {
            private string token;
            private DateTime? tokenNextRefresh;
            private readonly Func<Task<string>> obtainToken;

            public TokenHolder(Func<Task<string>> obtainToken)
            {
                this.obtainToken = obtainToken;
            }

            public async Task<string> GetToken()
            {
                if (token != null && tokenNextRefresh.HasValue && tokenNextRefresh.Value > DateTime.Now)
                {
                    return token;
                }

                token = await obtainToken();
                tokenNextRefresh = DateTime.Now.AddMinutes(60);

                return token;
            }
        }

        private readonly IOptions<ServiceOptions> serviceOptions;
        private readonly HttpClient authHttpClient;
        private readonly HttpClient apiHttpClient;
        private readonly TokenHolder tokenHolder;

        public MluviiClient(IOptions<ServiceOptions> serviceOptions)
        {
            this.serviceOptions = serviceOptions;

            authHttpClient = new HttpClient
            {
                BaseAddress = new Uri($"https://{serviceOptions.Value.MluviiDomain}")
            };

            apiHttpClient = new HttpClient
            {
                BaseAddress = new Uri($"https://{serviceOptions.Value.MluviiDomain}"),
                DefaultRequestHeaders = { }
            };

            tokenHolder = new TokenHolder(async () =>
            {
                var post = new Dictionary<string, string>
                {
                    {"response_type", "token"},
                    {"grant_type", "client_credentials"},
                    {"client_id", serviceOptions.Value.ClientId},
                    {"client_secret", serviceOptions.Value.ClientSecret},
                };

                using (var formContent = new FormUrlEncodedContent(post))
                {
                    var resp = await authHttpClient.PostAsync("/login/connect/token", formContent);
                    resp.EnsureSuccessStatusCode();

                    var reply = JsonConvert.DeserializeAnonymousType(await resp.Content.ReadAsStringAsync(), new { access_token = string.Empty });
                    return reply.access_token;
                }
            });
        }

        public async Task SubscribeToEvents()
        {
            apiHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokenHolder.GetToken());

            var externalUri = new Uri(serviceOptions.Value.ExternalUrl);

            var webhookUriBuilder = new UriBuilder
            {
                Scheme = externalUri.Scheme,
                Host = externalUri.Host,
                Port = externalUri.Port,
                UserName = WebhookMiddleware.User,
                Password = WebhookMiddleware.Password,
                Path = "/mluviiwebhook"
            };

            var model = new WebhookAddEditModel
            {
                EventTypes = new[]
                {
                    WebhookEventType.SessionStarted,
                    WebhookEventType.SessionForwarded,
                    WebhookEventType.SessionEnded
                },
                CallbackUrl = webhookUriBuilder.Uri.ToString()
            };

            var response = await apiHttpClient.PostAsJsonAsync("/api/v1/Webhooks", model);
            if (response.StatusCode == HttpStatusCode.Conflict &&
                int.TryParse(await response.Content.ReadAsStringAsync(), out var existingWebhookId))
            {
                response = await apiHttpClient.PutAsJsonAsync($"/api/v1/Webhooks/{existingWebhookId}", model);
            }

            response.EnsureSuccessStatusCode();
        }

        public async Task<ICollection<SessionModel>> ListClosedSessions()
        {
            apiHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await tokenHolder.GetToken());

            var query = new Dictionary<string, string>()
            {
                {"Status", nameof(SessionStatus.CLOSED)},
                {"Created.Min", DateTime.Today.ToString("yyyy-MM-dd")},
                {"limit", "1000"}
            };

            var response = await apiHttpClient.GetStringAsync(QueryHelpers.AddQueryString("/api/v1/Sessions", query));

            return JsonConvert.DeserializeObject<SessionModel[]>(response, MluviiApiJsonSerializerSettings.Instance);
        }
    }
}
