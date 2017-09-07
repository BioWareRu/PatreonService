using System;
using Amazon;
using Amazon.S3;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PatreonService.Core;

namespace PatreonService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }


        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(Configuration["S3_REGION"])
            };
            if (Configuration.GetValue<bool>("S3_USE_MINIO"))
            {
                config.ForcePathStyle = true;
                config.ServiceURL = Configuration.GetValue<string>("S3_SERVICE_URL");
            }
            var amazonS3Client = new AmazonS3Client(Configuration.GetValue<string>("S3_ACCESS_KEY"),
                Configuration.GetValue<string>("S3_SECRET_KEY"), config);
            services.AddSingleton(amazonS3Client);
            services.AddSingleton<S3Provider>();
            services.AddDistributedMemoryCache();
            services.Configure<PatreonConfig>(o =>
            {
                o.ClientId = Configuration["PATREON_API_CLIENT_ID"];
                o.ClientSecret = Configuration["PATREON_API_CLIENT_SECRET"];
                o.ApiUrl = new Uri(Configuration["PATREON_API_URL"]);
                o.S3BucketName = Configuration["PATREON_S3_BUCKET_NAME"];
                o.S3ObjectKey = Configuration["PATREON_S3_OBJECT_KEY"];
            });
            services.AddSingleton<PatreonApi>();

            services.AddMvc();
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseMvc();
        }
    }
}