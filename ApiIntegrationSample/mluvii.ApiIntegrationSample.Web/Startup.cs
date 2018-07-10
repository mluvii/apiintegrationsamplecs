using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace mluvii.ApiIntegrationSample.Web
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

            builder.AddEnvironmentVariables();
            configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMemoryCache();

            services.Configure<ServiceOptions>(configuration.GetSection("Service"));

            services.AddSingleton(sp => new WebhookEventProcessor());

            services.AddSingleton(sp => new MluviiClient(sp.GetService<IOptions<ServiceOptions>>()));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.Map("/mluviiwebhook", map =>
            {
                map.UseMiddleware<WebhookMiddleware>();
            });

            Task.Run(async () =>
            {
                await app.ApplicationServices.GetService<MluviiClient>().SubscribeToEvents();
            });
        }
    }
}
