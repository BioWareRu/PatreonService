using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PatreonService.Core
{
    public class PatreonApi
    {
        private readonly ILogger<PatreonApi> _logger;
        private readonly S3Provider _s3Provider;
        private readonly Uri _apiUrl;
        private HttpClient _httpClient;
        private readonly PatreonConfig _config;
        private string _accessToken;
        private PatreonTokenData _tokenData;

        public PatreonApi(IOptions<PatreonConfig> config, ILogger<PatreonApi> logger, S3Provider s3Provider)
        {
            _logger = logger;
            _s3Provider = s3Provider;
            _apiUrl = config.Value.ApiUrl;
            _config = config.Value;
        }

        private async Task<string> GetReponseJsonAsync(string path)
        {
            var url = _apiUrl + path;
            var response = await (await GetHttpClient()).GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await RefreshAccessToken();
                return await GetReponseJsonAsync(path);
            }
            throw new Exception($"Error accessing patreon: {response.StatusCode}");
        }

        private async Task<string> GetAccessToken(bool forceRefreshToken = false)
        {
            if (forceRefreshToken)
            {
                await RefreshAccessToken();
            }
            return (await GetTokenData()).AccessToken;
        }


        private async Task<PatreonTokenData> GetTokenData()
        {
            return _tokenData ?? (_tokenData = await GetTokenDataFromS3());
        }

        private async Task<string> GetRefreshToken()
        {
            return (await GetTokenData()).RefreshToken;
        }

        private async Task<PatreonTokenData> GetTokenDataFromS3()
        {
            return await _s3Provider.DownloadJson<PatreonTokenData>(_config.S3BucketName, _config.S3ObjectKey);
        }

        private async Task<bool> SetTokenDataToS3(PatreonTokenData data)
        {
            return await _s3Provider.UploadJson(data, _config.S3BucketName, _config.S3ObjectKey);
        }

        private async Task<bool> RefreshAccessToken()
        {
            var url = _apiUrl + "/token?grant_type=refresh_token"
                      + $"&refresh_token={await GetRefreshToken()}"
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

        private async Task<HttpClient> GetHttpClient()
        {
            return _httpClient ?? (_httpClient = await CreateHttpClient());
        }

        private async Task<HttpClient> CreateHttpClient(bool forceRefreshToken = false)
        {
            if (string.IsNullOrEmpty(_accessToken) || forceRefreshToken)
            {
                _accessToken = await GetAccessToken(forceRefreshToken);
            }
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
            return httpClient;
        }


        private static List<T> GetIncluded<T>(string json, string type)
        {
            var jToken = JToken.Parse(json);
            var included = jToken["included"].AsJEnumerable();

            return (from value in included
                where value["type"].ToString() == type
                select value["attributes"].ToObject<T>()
                into obj
                where obj != null
                select obj).ToList();
        }

        public async Task<PatreonGoal> GetCurrentGoalAsync()
        {
            var goals = await GetGoalsAsync();
            var currentGoal = goals.Where(x => x.CompletedPercentage < 100)
                .OrderByDescending(x => x.CompletedPercentage).First();
            return currentGoal;
        }

        public async Task<List<PatreonGoal>> GetGoalsAsync()
        {
            var json = await GetReponseJsonAsync("/api/current_user/campaigns?include-goals");

            return GetIncluded<PatreonGoal>(json, "goal");
        }
    }

    public class PatreonGoal
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("amount")]
        public int Amount => (int) Math.Ceiling(AmountCents * ((double) CompletedPercentage / 100) / 100);

        [JsonProperty("amount_cents")]
        public int AmountCents { get; set; }

        [JsonProperty("completed_percentage")]
        public int CompletedPercentage { get; set; }

        [JsonProperty("reached_at")]
        public DateTime? ReachedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    internal class PatreonTokenData
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}