using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using mluvii.PublicApi.Api;
using mluvii.PublicApi.Client;
using mluvii.PublicApi.Model;
using Microsoft.Extensions.Options;

namespace mluvii.ApiIntegrationSample.Web
{
    public class MluviiClient
    {
        private static readonly HttpClient authHttpClient = new HttpClient();

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

        private readonly TokenHolder tokenHolder;
        private readonly IOptions<ServiceOptions> serviceOptions;

        public MluviiClient(IOptions<ServiceOptions> serviceOptions)
        {
            this.serviceOptions = serviceOptions;

            tokenHolder = new TokenHolder(async () =>
            {
                var post = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("authKey", serviceOptions.Value.AuthKey)
                };

                using (var formContent = new FormUrlEncodedContent(post))
                {
                    var resp = await authHttpClient.PostAsync(serviceOptions.Value.AuthUrl, formContent);
                    resp.EnsureSuccessStatusCode();

                    return await resp.Content.ReadAsStringAsync();
                }
            });
        }

        private async Task<WebhooksApi> GetApi()
        {
            var token = await tokenHolder.GetToken();

            var configuration = new Configuration
            {
                BasePath = "todo"
            };

            configuration.ApiKey.Add("Authorization", token);
            configuration.ApiKeyPrefix.Add("Authorization", "Bearer");

            return new WebhooksApi(configuration);
        }

        public async Task SubscribeToEvents()
        {
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

            var eventTypes = new List<PublicApiWebhookModelsWebhookAddEditModel.EventTypesEnum>
            {
                PublicApiWebhookModelsWebhookAddEditModel.EventTypesEnum.SessionStarted,
                PublicApiWebhookModelsWebhookAddEditModel.EventTypesEnum.SessionForwarded,
                PublicApiWebhookModelsWebhookAddEditModel.EventTypesEnum.SessionEnded
            };

            var api = await GetApi();

            try
            {
                await api.ApiV1WebhooksPostAsync(new PublicApiWebhookModelsWebhookAddEditModel
                {
                    CallbackUrl = webhookUriBuilder.Uri.ToString(),
                    EventTypes = eventTypes
                });
            }
            catch (ApiException apiException)
            {
                var x = apiException.ErrorCode;
                // TODO ...
            }
        }
    }
}
