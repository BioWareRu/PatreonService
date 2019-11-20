using System;
using BioEngine.Core.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PatreonService.Core;

namespace PatreonService
{
    public class Startup : BioEngineApiStartup
    {
        public Startup(IConfiguration configuration, IHostEnvironment environment) : base(configuration,
            environment)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
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
