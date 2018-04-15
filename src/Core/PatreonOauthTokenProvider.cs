using System;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PatreonService.Core
{
    [UsedImplicitly]
    public class PatreonOauthTokenProvider
    {
        public PatreonOauthTokenProvider(IOptions<PatreonConfig> config, S3Provider s3Provider,
            ILogger<PatreonOauthTokenProvider> logger)
        {
            _s3Provider = s3Provider;
            _logger = logger;
            _config = config.Value;
        }

        private PatreonTokenData _tokenData;
        private readonly S3Provider _s3Provider;
        private readonly ILogger<PatreonOauthTokenProvider> _logger;
        private readonly PatreonConfig _config;

        public string GetAccessToken()
        {
            return _tokenData.AccessToken;
        }

        public async Task<bool> LoadOAuthData()
        {
            _tokenData = await GetTokenDataFromS3();
            return true;
        }

        private async Task<PatreonTokenData> GetTokenDataFromS3()
        {
            return await _s3Provider.DownloadJson<PatreonTokenData>(_config.S3BucketName, _config.S3ObjectKey);
        }

        private async Task<bool> SetTokenDataToS3(PatreonTokenData data)
        {
            return await _s3Provider.UploadJson(data, _config.S3BucketName, _config.S3ObjectKey);
        }

        public async Task<bool> RefreshAccessToken()
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
                await SetTokenDataToS3(_tokenData);
                return true;
            }

            _logger.LogError(
                $"Can't referesh patreon token. Status code: {response.StatusCode}. Response: {await response.Content.ReadAsStringAsync()}");
            throw new Exception("Patreon refresh token error");
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