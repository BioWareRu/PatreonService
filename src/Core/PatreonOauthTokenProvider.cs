using System;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.S3;
using BioEngine.Core.Storage.S3;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PatreonService.Core
{
    [UsedImplicitly]
    public class PatreonOauthTokenProvider
    {
        public PatreonOauthTokenProvider(IOptions<PatreonConfig> config, S3Client s3Client,
            ILogger<PatreonOauthTokenProvider> logger)
        {
            _s3Client = s3Client;
            _logger = logger;
            _config = config.Value;
        }

        private PatreonTokenData _tokenData;
        private readonly S3Client _s3Client;
        private readonly ILogger<PatreonOauthTokenProvider> _logger;
        private readonly PatreonConfig _config;

        public string GetAccessToken()
        {
            return _tokenData.AccessToken;
        }

        public async Task<bool> LoadOAuthDataAsync()
        {
            try
            {
                _tokenData = await GetTokenDataFromS3Async();
                return true;
            }
            catch (AmazonS3Exception exception)
            {
                _logger.LogError(exception, exception.ToString());
                return false;
            }
        }

        private Task<PatreonTokenData> GetTokenDataFromS3Async()
        {
            return _s3Client.DownloadJsonAsync<PatreonTokenData>(_config.S3ObjectKey);
        }

        private Task SetTokenDataToS3Async(PatreonTokenData data)
        {
            return _s3Client.UploadJsonAsync(data, _config.S3ObjectKey);
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
                    JsonConvert.DeserializeObject<PatreonTokenData>(
                        await response.Content.ReadAsStringAsync());
                await SetTokenDataToS3Async(_tokenData);
            }
            else
            {
                _logger.LogError(
                    $"Can't referesh patreon token. Status code: {response.StatusCode.ToString()}. Response: {await response.Content.ReadAsStringAsync()}");
                throw new Exception("Patreon refresh token error");
            }
        }
    }

    internal class PatreonTokenData
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }

        [JsonProperty("refresh_token")] public string RefreshToken { get; set; }

        [JsonProperty("expires_in")] public int ExpiresIn { get; set; }

        [JsonProperty("scope")] public string Scope { get; set; }

        [JsonProperty("token_type")] public string TokenType { get; set; }
    }
}
