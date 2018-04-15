using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PatreonService.Core
{
    [UsedImplicitly]
    public class PatreonTokenRefreshService : IHostedService
    {
        private readonly PatreonOauthTokenProvider _tokenProvider;
        private readonly ILogger<PatreonTokenRefreshService> _logger;
        private Task _task;

        public PatreonTokenRefreshService(PatreonOauthTokenProvider tokenProvider,
            ILogger<PatreonTokenRefreshService> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _tokenProvider.LoadOAuthData();
            _task = Task.Run(async () =>
            {
                _logger.LogInformation("Start token refresher");
                while (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Request token refresh");
                    await _tokenProvider.RefreshAccessToken();
                    _logger.LogInformation("Token refreshed. Sleep.");
                    await Task.Delay(TimeSpan.FromDays(10), cancellationToken);
                }

                _logger.LogInformation("Stop token refresher");
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _task;
        }
    }
}