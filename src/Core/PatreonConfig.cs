using System;

namespace PatreonService.Core
{
    public class PatreonConfig
    {
        public Uri ApiUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string S3ObjectKey { get; set; }
    }
}
