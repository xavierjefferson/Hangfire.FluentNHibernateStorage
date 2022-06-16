using Newtonsoft.Json;

namespace Hangfire.FluentNHibernateStorage.WebApplication
{
    public class ExtLogItem
    {
        [JsonProperty("id")] public string id { get; set; }

        [JsonProperty("timestamp")] public string timestamp { get; set; }

        [JsonProperty("level")] public string level { get; set; }

        [JsonProperty("message")] public string message { get; set; }

        [JsonProperty("exception")] public string exception { get; set; }

        [JsonProperty("properties")] public string properties { get; set; }
    }
}