using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PatreonService.Core
{
    public class PatreonTokenRefreshService : IHostedService
    {
        private readonly PatreonOauthTokenProvider _tokenProvider;
        private readonly ILogger<PatreonTokenRefreshService> _logger;
        private Timer? _timer;

        public PatreonTokenRefreshService(PatreonOauthTokenProvider tokenProvider,
            ILogger<PatreonTokenRefreshService> logger)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_timer != null)
            {
                await _timer.DisposeAsync();
            }

            var dataLoaded = await _tokenProvider.LoadOAuthDataAsync();
            if (!dataLoaded)
            {
                throw new Exception("Can't load oauth data. Stop app");
            }

            _logger.LogInformation("Start token refresher");
            // ReSharper disable once VSTHRD101
            _timer = new Timer(async _ => { await RefreshTokenAsync(); }, null, TimeSpan.Zero,
                TimeSpan.FromDays(10));
        }

        private async Task RefreshTokenAsync()
        {
            _logger.LogInformation("Request token refresh");
            await _tokenProvider.RefreshAccessTokenAsync();
            _logger.LogInformation("Token refreshed. Sleep");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop token refresher");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
