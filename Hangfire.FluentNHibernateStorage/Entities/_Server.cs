using System;

namespace Hangfire.FluentNHibernateStorage.Entities
{
    internal class _Server
    {
        public virtual string Id { get; set; }
        public virtual string Data { get; set; }
        public virtual DateTime LastHeartbeat { get; set; }
    }
}