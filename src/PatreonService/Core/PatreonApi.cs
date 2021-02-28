using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

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

            throw new Exception($"Error accessing patreon: {response.StatusCode.ToString()}");
        }

        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_tokenProvider.GetAccessToken()}");
            return httpClient;
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

            var response = JsonSerializer.Deserialize<PatreonResponse<PatreonGoal>>(json);
            return response.Included.Where(i => i.Type == "goal").Select(i => i.Attributes).ToList();
        }
    }

    public class PatreonResponse<T>
    {
        [JsonPropertyName("included")] public List<PatreonObject<T>> Included { get; set; }
    }

    public class PatreonObject<T>
    {
        [JsonPropertyName("type")] public string Type { get; set; }
        [JsonPropertyName("attributes")] public T Attributes { get; set; }
    }

    public class PatreonGoal
    {
        [JsonPropertyName("description")] public string Description { get; set; }

        [JsonPropertyName("current_amount")]
        public int CurrentAmount => (int) Math.Ceiling(AmountCents * ((double) CompletedPercentage / 100) / 100);

        [JsonPropertyName("amount")] public int Amount => AmountCents / 100;

        [JsonPropertyName("amount_cents")] public int AmountCents { get; set; }

        [JsonPropertyName("completed_percentage")]
        public int CompletedPercentage { get; set; }

        [JsonPropertyName("reached_at")] public DateTime? ReachedAt { get; set; }

        [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }
    }
}