using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Serilog.Events;

namespace Hangfire.FluentNHibernate.SampleStuff
{
    public class LogItem
    {
        public LogItem()
        {
        }

        public LogItem(LogEvent logEvent)
        {
            var properties = new Dictionary<string, string>();
            if (logEvent.Properties != null)
                foreach (var m in logEvent.Properties.Keys)
                    properties[m] = logEvent.Properties[m].ToString();
            Id = Guid.NewGuid().ToString();
            Timestamp = logEvent.Timestamp.ToString("yyyy.MM.dd HH:mm:ss.fff");
            dt = logEvent.Timestamp.UtcDateTime;
            Level = logEvent.Level.ToString();
            Message = logEvent.RenderMessage();
            Exception = logEvent.Exception?.ToString() ?? "-";
            Properties = JsonConvert.SerializeObject(properties);
        }

        [JsonProperty("dt")] public virtual DateTime dt { get; set; }
        [JsonProperty("id")] public virtual string Id { get; set; }

        [JsonProperty("timestamp")] public virtual string Timestamp { get; set; }

        [JsonProperty("level")] public virtual string Level { get; set; }

        [JsonProperty("exception")] public virtual string Exception { get; set; }

        [JsonProperty("message")] public virtual string Message { get; set; }

        [JsonProperty("properties")] public virtual string Properties { get; set; }
    }
}