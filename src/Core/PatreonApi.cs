using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PatreonService.Core
{
    public class PatreonApi
    {
        private readonly PatreonOauthTokenProvider _tokenProvider;
        private readonly Uri _apiUrl;

        public PatreonApi(IOptions<PatreonConfig> config, PatreonOauthTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
            _apiUrl = config.Value.ApiUrl;
        }

        private async Task<string> GetReponseJsonAsync(string path)
        {
            var url = _apiUrl + path;
            var response = await GetHttpClient().GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw new Exception($"Error accessing patreon: {response.StatusCode}");
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokenProvider.GetAccessToken()}");
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

        private async Task<List<PatreonGoal>> GetGoalsAsync()
        {
            var json = await GetReponseJsonAsync("/api/current_user/campaigns?include-goals");

            return GetIncluded<PatreonGoal>(json, "goal");
        }
    }

    public class PatreonGoal
    {
        [JsonProperty("description")] public string Description { get; set; }

        [JsonProperty("current_amount")]
        public int CurrentAmount => (int) Math.Ceiling(AmountCents * ((double) CompletedPercentage / 100) / 100);

        [JsonProperty("amount")] public int Amount => AmountCents / 100;

        [JsonProperty("amount_cents")] public int AmountCents { get; set; }

        [JsonProperty("completed_percentage")] public int CompletedPercentage { get; set; }

        [JsonProperty("reached_at")] public DateTime? ReachedAt { get; set; }

        [JsonProperty("created_at")] public DateTime? CreatedAt { get; set; }
    }
}