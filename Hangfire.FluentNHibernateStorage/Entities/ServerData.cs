using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class ServerData
    {
        public int WorkerCount { get; set; }
        public string[] Queues { get; set; }
        public DateTime? StartedAt { get; set; }
    }
}