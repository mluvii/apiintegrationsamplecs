using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace mluvii.ApiIntegrationSample.Web
{
    public class WebhookMiddleware
    {
        public const string User = "csharp";
        public const string Password = "isawesome";

        private readonly WebhookEventProcessor webhookEventProcessor;

        public WebhookMiddleware(RequestDelegate next, WebhookEventProcessor webhookEventProcessor)
        {
            this.webhookEventProcessor = webhookEventProcessor;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!Authenticate(context))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            if (!context.Request.Method.Equals("post", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (var mstr = new MemoryStream())
            {
                await context.Request.Body.CopyToAsync(mstr);
                var text = Encoding.UTF8.GetString(mstr.ToArray());
                // work is delegated to background processor so the middleware does not block
                webhookEventProcessor.ParseAndProcessEvent(text);
            }
        }

        private bool Authenticate(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"];
            if (authHeader.FirstOrDefault() == null)
            {
                return false;
            }

            var authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
            if (authHeaderVal?.Scheme.ToLower() != "basic" || string.IsNullOrEmpty(authHeaderVal.Parameter))
            {
                return false;
            }

            try
            {
                var credentials = Encoding.ASCII.GetString(Convert.FromBase64String(authHeaderVal.Parameter));
                var separator = credentials.IndexOf(':');
                var name = credentials.Substring(0, separator);
                var password = credentials.Substring(separator + 1);

                return name == User && password == Password;
            }
            catch
            {
                return false;
            }
        }
    }
}
