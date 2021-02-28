using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatreonService.Core;
using Sitko.Core.App.Web;

namespace PatreonService
{
    public class Startup : BaseStartup<PatreonApplication>
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }

        protected override void ConfigureAppServices(IServiceCollection services)
        {
            base.ConfigureAppServices(services);
            services.Configure<PatreonConfig>(o =>
            {
                o.ClientId = Configuration["PATREON_API_CLIENT_ID"];
                o.ClientSecret = Configuration["PATREON_API_CLIENT_SECRET"];
                o.ApiUrl = new Uri(Configuration["PATREON_API_URL"]);
                o.S3ObjectKey = Configuration["PATREON_S3_OBJECT_KEY"];
            });

            services.AddSingleton<PatreonOauthTokenProvider>();
            services.AddSingleton<PatreonApi>();
            services.AddSingleton<IHostedService, PatreonTokenRefreshService>();
        }
    }
}