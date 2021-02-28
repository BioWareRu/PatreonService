using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sitko.Core.Storage.S3;

namespace PatreonService.Core
{
    public class PatreonOauthTokenProvider
    {
        public PatreonOauthTokenProvider(IOptions<PatreonConfig> config, S3ClientProvider<S3Config> s3ClientProvider,
            S3Config s3Config,
            ILogger<PatreonOauthTokenProvider> logger)
        {
            _s3Client = s3ClientProvider.S3Client;
            _s3Config = s3Config;
            _logger = logger;
            _config = config.Value;
        }

        private PatreonTokenData? _tokenData;
        private readonly AmazonS3Client _s3Client;
        private readonly S3Config _s3Config;
        private readonly ILogger<PatreonOauthTokenProvider> _logger;
        private readonly PatreonConfig _config;

        public string GetAccessToken()
        {
            if (_tokenData is null)
            {
                throw new Exception("Empty token data");
            }

            return _tokenData.AccessToken;
        }

        public async Task<bool> LoadOAuthDataAsync()
        {
            try
            {
                var tokenData = await GetTokenDataFromS3Async();

                _tokenData = tokenData ?? throw new Exception("Empty token data");
                return true;
            }
            catch (AmazonS3Exception exception)
            {
                _logger.LogError(exception, "Error getting token data: {ErrorText}", exception.ToString());
                return false;
            }
        }

        private Task<PatreonTokenData?> GetTokenDataFromS3Async()
        {
            return _s3Client.DownloadJsonAsync<PatreonTokenData>(_config.S3ObjectKey, _s3Config.Bucket);
        }

        private Task SetTokenDataToS3Async(PatreonTokenData data)
        {
            return _s3Client.UploadJsonAsync(data, _config.S3ObjectKey, _s3Config.Bucket);
        }

        public async Task RefreshAccessTokenAsync()
        {
            var url = _config.ApiUrl + "/token?grant_type=refresh_token"
                                     + $"&refresh_token={_tokenData.RefreshToken}"
                                     + $"&client_id={_config.ClientId}"
                                     + $"&client_secret={_config.ClientSecret}";
            var httpClient = new HttpClient();
            var response = await httpClient.PostAsync(url, null);
            if (response.IsSuccessStatusCode)
            {
                _tokenData =
                    JsonSerializer.Deserialize<PatreonTokenData>(
                        await response.Content.ReadAsStringAsync());
                await SetTokenDataToS3Async(_tokenData);
            }
            else
            {
                _logger.LogError(
                    "Can't referesh patreon token. Status code: {StatusCode}. Response: {Response}",
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                throw new Exception("Patreon refresh token error");
            }
        }
    }

    internal class PatreonTokenData
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")] public string Scope { get; set; }

        [JsonPropertyName("token_type")] public string TokenType { get; set; }
    }
}