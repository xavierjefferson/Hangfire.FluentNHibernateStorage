using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    public class _Server
    {
        public virtual string Id { get; set; }
        public virtual string Data { get; set; } = string.Empty;
        public virtual DateTime? LastHeartbeat { get; set; }
    }
}